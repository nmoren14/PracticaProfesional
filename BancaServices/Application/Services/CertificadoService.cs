using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data.SqlClient;
using System.Globalization;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using BancaServices.Models.DTO;
using BancaServices.Models;
using System.Text;
using BancaServices.Domain.Interfaces;
using BancaServices.Application.Services.SerfiUtils;

namespace BancaServices.Application.Services
{
    public class CertificadoService : ICertificadoService
    {
        private readonly IProductosService productoService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly BancaServicesLogsEntities Context; // Inject the DbContext

        public CertificadoService(IConfiguration configuration, BancaServicesLogsEntities dbContext, IHttpClientFactory httpClientFactory, IProductosService productoService, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment)
        {
            this.productoService = productoService;
            _httpContextAccessor = httpContextAccessor;
            _webHostEnvironment = webHostEnvironment;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            Context = dbContext; // Assign the injected DbContext
        }

        public async Task<bool> EstaAlDia(string tipoId, string idCliente)
        {
            var listaCMS = await productoService.ConsultaCMS(tipoId, idCliente, false);
            decimal saldoVencido = decimal.Zero;
            if (listaCMS != null && listaCMS.Count > 0)
            {
                var numProducto = "";
                JObject detalleCMS = new JObject();
                JObject detallePrestamo = new JObject();
                foreach (JObject cms in listaCMS)
                {
                    numProducto = cms.GetValue("numProducto").Value<string>();
                    detalleCMS = await productoService.DetalleCMS(tipoId, idCliente, numProducto);
                    if (detalleCMS != null)
                    {
                        saldoVencido += detalleCMS.GetValue("pagominimoVencido").Value<decimal>();
                    }
                }
            }
            ConsultaRecaudoFacturaSR.ConsultasRecaudoFacturacionClient recaudoFacturacion = new ConsultaRecaudoFacturaSR.ConsultasRecaudoFacturacionClient();
            var tipoIdEibs = tipoId.Equals("1") ? "CC" : "NIT";
            var reqPrestamos = await recaudoFacturacion.ListarPrestamosAsync(tipoIdEibs, idCliente);
            if (reqPrestamos != null)
            {
                var lPrestamos = reqPrestamos.@return;

                if (lPrestamos.estado.Equals("true") && lPrestamos.cuentas != null)
                {

                    foreach (ConsultaRecaudoFacturaSR.cuentasPorCobrarWSBean cuenta in lPrestamos.cuentas)
                    {

                        saldoVencido += cuenta.cuenta.valorCuotasVencidas;

                    }
                }
            }
            return saldoVencido == 0;
        }

        public async Task<byte[]> CertificadoAlDia(string tipoId, string idCliente, string nombre, bool cifrado)
        {
            byte[] pdfFile = new byte[0];
            bool estaAlDia = await EstaAlDia(tipoId, idCliente);
            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document();

                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                writer.PageEvent = new PDFFooter();
                document.Open();


                //TITUTLO
                var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                var FontBoldTable = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
                string titulo1 = @"A QUIEN INTERESE";
                var ptitle = new Paragraph(titulo1, boldFont);
                ptitle.Alignment = Element.ALIGN_CENTER;
                ptitle.SpacingBefore = 70;
                document.Add(ptitle);

                //LOGO SUPER
                string imageURLSuper = Path.Combine(_webHostEnvironment.WebRootPath, "Assets/Images/vigilado_super.jpg");
                Image logosuper = Image.GetInstance(imageURLSuper);
                logosuper.Alignment = Image.TEXTWRAP | Element.ALIGN_LEFT;
                logosuper.Rotation = (float)Math.PI / 2;
                logosuper.RotationDegrees = 90f;
                logosuper.ScalePercent(50, 30);
                logosuper.SetAbsolutePosition(10, 200);
                document.Add(logosuper);

                //CUERPO
                var courier = FontFactory.GetFont(FontFactory.HELVETICA, 11);
                string tipoIdTexto = TipoIdentificacion(tipoId);

                string tcuerpo = "Banco Serfinanza S.A se permite informar que el cliente ";

                Chunk beginning = new Chunk(tcuerpo, courier);
                Phrase p4 = new Phrase(beginning);

                string nombr = nombre;

                Chunk Cnombre = new Chunk(nombr, boldFont);
                Phrase pnombre = new Phrase(Cnombre);

                string tidentificacion = string.Format(", identificado(a) con {0}", tipoIdTexto);

                Chunk Cidentificacion = new Chunk(tidentificacion, courier);
                Phrase pidentificacion = new Phrase(Cidentificacion);

                string tcuerpo5 = string.Format(" N° {0}", idCliente);

                Chunk Cp5 = new Chunk(tcuerpo5, boldFont);
                Phrase p5 = new Phrase(Cp5);

                string tcuerpo6 = ", se encuentra AL DIA por todo concepto con sus obligaciones:";

                Chunk Cp6 = new Chunk(tcuerpo6, courier);
                Phrase p6 = new Phrase(Cp6);


                Paragraph p = new Paragraph();
                p.Alignment = Element.ALIGN_JUSTIFIED;
                p.SpacingBefore = 70;
                p.Add(p4);
                p.Add(pnombre);
                p.Add(pidentificacion);
                p.Add(p5);
                p.Add(p6);
                document.Add(p);

                //TABLA2: datos de la obligacion 
                var tabla2 = new PdfPTable(4);
                Phrase pObligacion = new Phrase("Obligación", FontBoldTable);
                Paragraph prOblg = new Paragraph(pObligacion);
                prOblg.Alignment = Element.ALIGN_CENTER;

                Phrase pSaldo = new Phrase("Referencia", FontBoldTable);
                Paragraph prSdo = new Paragraph(pSaldo);
                prSdo.Alignment = Element.ALIGN_CENTER;

                Phrase pInterPag = new Phrase("Fecha Emisión", FontBoldTable);
                Paragraph prIPag = new Paragraph(pInterPag);
                prIPag.Alignment = Element.ALIGN_CENTER;

                Phrase pOtrosPag = new Phrase("Estado", FontBoldTable);
                Paragraph prOPag = new Paragraph(pOtrosPag);
                prOPag.Alignment = Element.ALIGN_CENTER;

                PdfPCell phc = new PdfPCell();
                phc.AddElement(prOblg);
                phc.BackgroundColor = BaseColor.LightGray;
                phc.HorizontalAlignment = Element.ALIGN_CENTER;
                phc.VerticalAlignment = Element.ALIGN_MIDDLE;
                tabla2.AddCell(phc);


                PdfPCell phcSaldo = new PdfPCell();
                phcSaldo.AddElement(prSdo);
                phcSaldo.BackgroundColor = BaseColor.LightGray;
                phcSaldo.HorizontalAlignment = Element.ALIGN_CENTER;
                tabla2.AddCell(phcSaldo);

                PdfPCell phcInter = new PdfPCell();
                phcInter.AddElement(prIPag);
                phcInter.BackgroundColor = BaseColor.LightGray;
                phcInter.HorizontalAlignment = Element.ALIGN_CENTER;
                tabla2.AddCell(phcInter);

                PdfPCell phcOtrosPag = new PdfPCell();
                phcOtrosPag.AddElement(prOPag);
                phcOtrosPag.BackgroundColor = BaseColor.LightGray;
                phcOtrosPag.HorizontalAlignment = Element.ALIGN_CENTER;
                tabla2.AddCell(phcOtrosPag);

                var listaCms = await productoService.ConsultaCMS(tipoId, idCliente, false);
                if (listaCms != null && listaCms.Count > 0)
                {
                    foreach (JObject cms in listaCms)
                    {
                        var numProducto = cms.GetValue("numProducto").Value<string>();
                        var nombremProducto = cms.GetValue("nomProducto").Value<string>();
                        var fechaEmision = cms.GetValue("fechaEmision").Value<string>();
                        var estado = cms.GetValue("estado").Value<string>();
                        PdfPCell cellObli = new PdfPCell(new Phrase(numProducto, courier));
                        cellObli.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellObli);

                        PdfPCell cellNombre = new PdfPCell(new Phrase(nombremProducto, courier));
                        cellNombre.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellNombre);

                        PdfPCell cellFecha = new PdfPCell(new Phrase(fechaEmision, courier));
                        cellFecha.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellFecha);

                        PdfPCell cellEstado = new PdfPCell(new Phrase(estado, courier));
                        cellEstado.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellEstado);
                    }
                }
                var listaPrestamos = await productoService.ConsultarPrestamos(tipoId, idCliente);
                if (listaPrestamos != null && listaPrestamos.Count > 0)
                {
                    foreach (JObject prestamo in listaPrestamos)
                    {
                        var numProducto = prestamo.GetValue("numProducto").Value<string>();
                        var nombremProducto = prestamo.GetValue("nomProducto").Value<string>();
                        var fechaEmision = prestamo.GetValue("fechaEmision").Value<string>();
                        var estado = prestamo.GetValue("estado").Value<string>();
                        PdfPCell cellObli = new PdfPCell(new Phrase(numProducto, courier));
                        cellObli.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellObli);

                        PdfPCell cellNombre = new PdfPCell(new Phrase(nombremProducto, courier));
                        cellNombre.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellNombre);

                        PdfPCell cellFecha = new PdfPCell(new Phrase(fechaEmision, courier));
                        cellFecha.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellFecha);

                        PdfPCell cellEstado = new PdfPCell(new Phrase(estado, courier));
                        cellEstado.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellEstado);
                    }
                }

                tabla2.SpacingBefore = 20f;
                tabla2.SpacingAfter = 30f;
                //fix the absolute width of the table
                tabla2.LockedWidth = true;
                //relative col widths in proportions - 1/3 and 2/3
                float[] widths2 = new float[] { 1.3f, 1.3f, 0.9f, 0.7f };
                tabla2.SetWidths(widths2);
                tabla2.WidthPercentage = 100f;
                tabla2.TotalWidth = 500f;
                tabla2.HorizontalAlignment = Element.ALIGN_CENTER;
                document.Add(tabla2);

                string serfinansa = "Banco SERFINANZA ";
                Chunk serfi = new Chunk(serfinansa, boldFont);
                Phrase pserfi = new Phrase(serfi);

                string body2 = "se reserva la posibilidad de efectuar el cobro de cualquier transacción realizada y no cobrada con anterioridad y se encuentra debidamente documentada y contabilizada (En los términos del artículo 880 de Código de Comercio). ";
                Chunk cbody2 = new Chunk(body2, courier);
                Phrase pbody2 = new Phrase(cbody2);

                Paragraph prbody2 = new Paragraph();
                prbody2.Alignment = Element.ALIGN_JUSTIFIED;
                prbody2.Add(pserfi);
                prbody2.Add(pbody2);
                document.Add(prbody2);


                string temecion = string.Format("Se expide la presente certificación a los ({0}) días del mes de ", DateTime.Now.Day);
                Chunk chemicion = new Chunk(temecion, courier);
                Phrase pdemicion = new Phrase(chemicion);

                string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month).ToUpper();
                Chunk chMonth = new Chunk(monthName, boldFont);
                Phrase phMont = new Phrase(chMonth);

                string resto = string.Format(" de {0}, a solicitud de la parte interesada.", DateTime.Now.Year);
                Chunk chResto = new Chunk(resto, courier);
                Phrase phResto = new Phrase(chResto);



                Paragraph paremi = new Paragraph();
                paremi.Alignment = Element.ALIGN_JUSTIFIED;
                paremi.SpacingBefore = 30;
                paremi.SpacingAfter = 78;
                paremi.Add(pdemicion);
                paremi.Add(phMont);
                paremi.Add(chResto);
                document.Add(paremi);

                //var courier2 = FontFactory.GetFont(FontFactory.HELVETICA, 9);


                //String footer2 = "Según el decreto 836/91, este certificado no requiere firma autografa.";
                //Chunk chfooter2 = new Chunk(footer2, courier2);
                //Phrase pfooter2 = new Phrase(chfooter2);
                //Paragraph pfoot2 = new Paragraph();
                //pfoot2.Alignment = Element.ALIGN_JUSTIFIED;
                //pfoot2.SpacingBefore = (float)78;
                //pfoot2.Add(pfooter2);
                //document.Add(pfoot2);

                var courier2 = FontFactory.GetFont(FontFactory.HELVETICA, 9);


                string footer2 = "Atentamente,";
                Chunk chfooter2 = new Chunk(footer2, courier);
                Phrase pfooter2 = new Phrase(chfooter2);
                Paragraph pfoot2 = new Paragraph();
                pfoot2.Alignment = Element.ALIGN_JUSTIFIED;
                pfoot2.SpacingBefore = 38;
                pfoot2.Add(pfooter2);
                document.Add(pfoot2);

                //FIRMA
                string imageURLFirma = Path.Combine(_webHostEnvironment.WebRootPath, "Assets/Images/Firma.jpg");
                Image firma = Image.GetInstance(imageURLFirma);
                firma.Alignment = Element.ALIGN_LEFT;
                firma.ScalePercent(60, 60);
                document.Add(firma);

                string footer3 = "Dpto. de Servicio al Cliente";
                Chunk chfooter3 = new Chunk(footer3, boldFont);
                Phrase pfooter3 = new Phrase(chfooter3);
                Paragraph pfoot3 = new Paragraph();
                pfoot3.Alignment = Element.ALIGN_LEFT;
                pfoot3.SpacingBefore = 0;
                pfoot3.Add(pfooter3);
                document.Add(pfoot3);


                document.Close();
                writer.Flush();


                if (cifrado)
                {
                    pdfFile = encryptPDF(ms.GetBuffer(), idCliente);
                }
                else
                {
                    pdfFile = ms.GetBuffer();
                }

            }

            return pdfFile;
        }

        public byte[] CertificadoRetefuente(string tipoId, string idCliente, string nombre, string ano, bool cifrado)
        {
            byte[] pdfFile = new byte[0];
            List<DTS_RETEFUENTE> proRetefuente = null;

            var parameters = new object[] { ano, idCliente };
            var query = "SELECT * FROM DTS_RETEFUENTE WHERE ano=@p0 AND nit=@p1";
            //proRetefuente = Context.Database.SqlQuery<DTS_RETEFUENTE>(query, parameters).ToList();

            if (proRetefuente != null && proRetefuente.Count > 0)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Document document = new Document();

                    PdfWriter writer = PdfWriter.GetInstance(document, ms);
                    writer.PageEvent = new PDFFooter();
                    document.Open();

                    //TITUTLO
                    var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
                    var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                    var FontBoldTable = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);

                    string titulo1 = @"CERTIFICADO DE RETENCIÓN EN LA FUENTE";
                    var ptitle = new Paragraph(titulo1, boldFont);
                    ptitle.Alignment = Element.ALIGN_CENTER;
                    ptitle.SpacingBefore = 62;
                    document.Add(ptitle);

                    string titulo2 = string.Format("AÑO GRAVABLE {0}", ano);
                    var ptitle2 = new Paragraph(titulo2, boldFont);
                    ptitle2.Alignment = Element.ALIGN_CENTER;
                    ptitle2.SpacingBefore = 0;
                    document.Add(ptitle2);

                    //LOGO SUPER
                    string imageURLSuper = Path.Combine(_webHostEnvironment.WebRootPath, "~/Assets/Images/vigilado_super.jpg");
                    Image logosuper = Image.GetInstance(imageURLSuper);
                    logosuper.Alignment = Image.TEXTWRAP | Element.ALIGN_LEFT;
                    logosuper.Rotation = (float)Math.PI / 2;
                    logosuper.RotationDegrees = 90f;
                    logosuper.ScalePercent(50, 30);
                    logosuper.SetAbsolutePosition(10, 200);
                    document.Add(logosuper);

                    //CUERPO
                    var courier = FontFactory.GetFont(FontFactory.HELVETICA, 11);
                    string serfi = "BANCO SERFINANZA S.A. ";
                    string sCon = "con ";
                    string sNit = "NIT 860.043.186 ";
                    string tcuerpo = string.Format(@"certifica que recibió durante el año gravable {0} por parte de:", ano);

                    Chunk cSerfi = new Chunk(serfi, boldFont);
                    Chunk ccon = new Chunk(sCon, courier);
                    Chunk cNit = new Chunk(sNit, boldFont);
                    Chunk beginning = new Chunk(tcuerpo, courier);
                    Phrase p1 = new Phrase(cSerfi);
                    Phrase p2 = new Phrase(ccon);
                    Phrase p3 = new Phrase(cNit);
                    Phrase p4 = new Phrase(beginning);

                    Paragraph p = new Paragraph();
                    p.Alignment = Element.ALIGN_JUSTIFIED;
                    p.SpacingBefore = 78;
                    p.Add(p1);
                    p.Add(p2);
                    p.Add(p3);
                    p.Add(p4);
                    document.Add(p);

                    //TABLA1: datos del cliente nit y nombre
                    var tabla1 = new PdfPTable(2);
                    Phrase pNombre = new Phrase("Nombre o razón social:", normalFont);
                    Phrase pNit = new Phrase("NIT:", normalFont);
                    tabla1.AddCell(pNombre);
                    tabla1.AddCell(nombre);
                    tabla1.AddCell(pNit);
                    tabla1.AddCell(idCliente);
                    tabla1.SpacingAfter = 25;
                    tabla1.SpacingBefore = 20f;
                    tabla1.SpacingAfter = 30f;
                    //fix the absolute width of the table
                    tabla1.LockedWidth = true;
                    //relative col widths in proportions - 1/3 and 2/3
                    float[] widths = new float[] { 1f, 2f };
                    tabla1.SetWidths(widths);
                    tabla1.WidthPercentage = 100f;
                    tabla1.TotalWidth = 500f;
                    tabla1.HorizontalAlignment = 0;
                    document.Add(tabla1);


                    //Texto 2
                    string tdetallado = "Lo detallado a continuación:";
                    Chunk chdetallado = new Chunk(tdetallado, courier);
                    Phrase pdetallado = new Phrase(chdetallado);
                    Paragraph pardetall = new Paragraph();
                    pardetall.Alignment = Element.ALIGN_JUSTIFIED;
                    pardetall.Add(pdetallado);
                    document.Add(pardetall);



                    //TABLA2: datos de la obligacion 
                    var tabla2 = new PdfPTable(5);
                    Phrase pObligacion = new Phrase("Obligación", FontBoldTable);
                    Phrase pSaldo = new Phrase("Saldo", FontBoldTable);
                    Phrase pInterPag = new Phrase("Intereses Pagados", FontBoldTable);
                    Phrase pOtrosPag = new Phrase("Cuota de Manejo", FontBoldTable);
                    Phrase pConcept = new Phrase("Otros Pagados", FontBoldTable);

                    PdfPCell phc = new PdfPCell();
                    phc.AddElement(pObligacion);
                    phc.BackgroundColor = BaseColor.LightGray;
                    phc.HorizontalAlignment = Element.ALIGN_CENTER;
                    tabla2.AddCell(phc);

                    PdfPCell phcSaldo = new PdfPCell();
                    phcSaldo.AddElement(pSaldo);
                    phcSaldo.BackgroundColor = BaseColor.LightGray;
                    phcSaldo.HorizontalAlignment = Element.ALIGN_CENTER;
                    tabla2.AddCell(phcSaldo);

                    PdfPCell phcInter = new PdfPCell();
                    phcInter.AddElement(pInterPag);
                    phcInter.BackgroundColor = BaseColor.LightGray;
                    phcInter.HorizontalAlignment = Element.ALIGN_CENTER;
                    tabla2.AddCell(phcInter);

                    PdfPCell phcOtrosPag = new PdfPCell();
                    phcOtrosPag.AddElement(pOtrosPag);
                    phcOtrosPag.BackgroundColor = BaseColor.LightGray;
                    phcOtrosPag.HorizontalAlignment = Element.ALIGN_CENTER;
                    tabla2.AddCell(phcOtrosPag);

                    PdfPCell phcConcept = new PdfPCell();
                    phcConcept.AddElement(pConcept);
                    phcConcept.BackgroundColor = BaseColor.LightGray;
                    phcConcept.HorizontalAlignment = Element.ALIGN_CENTER;
                    tabla2.AddCell(phcConcept);

                    foreach (DTS_RETEFUENTE pro in proRetefuente)
                    {
                        tabla2.AddCell(pro.obligacion);

                        PdfPCell cellSaldoCap = new PdfPCell(new Phrase(pro.saldo_capital != null ? pro.saldo_capital.ToString("C2", CultureInfo.GetCultureInfo(9226)) : "0"));
                        cellSaldoCap.HorizontalAlignment = Element.ALIGN_RIGHT;
                        tabla2.AddCell(cellSaldoCap);

                        double intereses_pagados = double.Parse(pro.intereses_pagados != null ? pro.intereses_pagados.ToString() : "0");
                        PdfPCell cellItersesPag = new PdfPCell(new Phrase(intereses_pagados.ToString("C2", CultureInfo.GetCultureInfo(9226))));
                        cellItersesPag.HorizontalAlignment = Element.ALIGN_RIGHT;
                        tabla2.AddCell(cellItersesPag);

                        double otros_pagados = double.Parse(pro.otros_pagados != null ? pro.otros_pagados.ToString() : "0");
                        PdfPCell cellOtrosPag = new PdfPCell(new Phrase(otros_pagados.ToString("C2", CultureInfo.GetCultureInfo(9226))));
                        cellOtrosPag.HorizontalAlignment = Element.ALIGN_RIGHT;
                        tabla2.AddCell(cellOtrosPag);

                        double concepto_otros = double.Parse(pro.concepto_otros != null ? pro.concepto_otros.ToString() : "0");
                        PdfPCell cellConceptosOtr = new PdfPCell(new Phrase(concepto_otros.ToString("C2", CultureInfo.GetCultureInfo(9226))));
                        cellConceptosOtr.HorizontalAlignment = Element.ALIGN_RIGHT;
                        tabla2.AddCell(cellConceptosOtr);
                    }

                    tabla2.SpacingBefore = 20f;
                    tabla2.SpacingAfter = 30f;
                    //fix the absolute width of the table
                    tabla2.LockedWidth = true;
                    //relative col widths in proportions - 1/3 and 2/3
                    float[] widths2 = new float[] { 1.3f, 1f, 1f, 1f, 1f };
                    tabla2.SetWidths(widths2);
                    tabla2.WidthPercentage = 100f;
                    tabla2.TotalWidth = 500f;
                    tabla2.HorizontalAlignment = 0;
                    document.Add(tabla2);

                    string temecion = string.Format("Certificado emitido en la ciudad de Barranquilla, el {0}", DateTime.Now.ToShortDateString());
                    Chunk chemicion = new Chunk(temecion, courier);
                    Phrase pdemicion = new Phrase(chemicion);
                    Paragraph paremi = new Paragraph();
                    paremi.Alignment = Element.ALIGN_JUSTIFIED;
                    paremi.Add(pdemicion);
                    document.Add(paremi);

                    var courier2 = FontFactory.GetFont(FontFactory.HELVETICA, 9);
                    string footer = string.Format(@"Nota: Este certificado no señala el tratamiento tributario que se debe dar a los diferentes rubros, por lo tanto es deber de cada persona precisar las disposiciones legales para dar el tratamiento tributario correspondiente");

                    Chunk chfooter = new Chunk(footer, courier2);
                    Phrase pfooter = new Phrase(chfooter);
                    Paragraph pfoot = new Paragraph();
                    pfoot.Alignment = Element.ALIGN_JUSTIFIED;
                    pfoot.SpacingBefore = 80f;
                    pfoot.Add(pfooter);
                    document.Add(pfoot);


                    string footer2 = "Este certificado se expide sin firma autógrafa de conformidad con lo dispuesto en el Artículo 10 del Decreto Reglamentario 836 de 1991.";
                    Chunk chfooter2 = new Chunk(footer2, courier2);
                    Phrase pfooter2 = new Phrase(chfooter2);
                    Paragraph pfoot2 = new Paragraph();
                    pfoot2.Alignment = Element.ALIGN_JUSTIFIED;
                    pfoot2.Add(pfooter2);
                    document.Add(pfoot2);

                    document.Close();
                    writer.Flush();


                    if (cifrado)
                    {
                        pdfFile = encryptPDF(ms.GetBuffer(), idCliente);
                    }
                    else
                    {
                        pdfFile = ms.GetBuffer();
                    }

                }
            }
            return pdfFile;
        }


        public byte[] CertificadoRetefuenteCDT(string tipoId, string idCliente, string nombre, string ano, bool cifrado)
        {
            byte[] pdfFile = new byte[0];
            List<DTS_RETEFUENTE_CDT> proRetefuente = null;
            decimal anio = Convert.ToDecimal(ano);
            proRetefuente = Context.DTS_RETEFUENTE_CDTs.Where(x => x.ano == anio).Where(x => x.nit == idCliente).ToList();


            if (proRetefuente != null && proRetefuente.Count > 0)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Document document = new Document();

                    PdfWriter writer = PdfWriter.GetInstance(document, ms);
                    writer.PageEvent = new PDFFooter();
                    document.Open();

                    //TITUTLO
                    var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
                    var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                    var FontBoldTable = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);

                    string titulo1 = @"CERTIFICADO DE RETENCIÓN EN LA FUENTE";
                    var ptitle = new Paragraph(titulo1, boldFont);
                    ptitle.Alignment = Element.ALIGN_CENTER;
                    ptitle.SpacingBefore = 62;
                    document.Add(ptitle);

                    string titulo2 = string.Format("AÑO GRAVABLE {0}", ano);
                    var ptitle2 = new Paragraph(titulo2, boldFont);
                    ptitle2.Alignment = Element.ALIGN_CENTER;
                    ptitle2.SpacingBefore = 0;
                    document.Add(ptitle2);

                    //LOGO SUPER
                    string imageURLSuper = Path.Combine(_webHostEnvironment.WebRootPath, "~/Assets/Images/vigilado_super.jpg");
                    Image logosuper = Image.GetInstance(imageURLSuper);
                    logosuper.Alignment = Image.TEXTWRAP | Element.ALIGN_LEFT;
                    logosuper.Rotation = (float)Math.PI / 2;
                    logosuper.RotationDegrees = 90f;
                    logosuper.ScalePercent(50, 30);
                    logosuper.SetAbsolutePosition(10, 200);
                    document.Add(logosuper);

                    //CUERPO
                    var courier = FontFactory.GetFont(FontFactory.HELVETICA, 11);
                    string serfi = "BANCO SERFINANZA S.A. ";
                    string sCon = "con ";
                    string sNit = "NIT 860.043.186 ";
                    string tcuerpo = string.Format(@"certifica que recibió durante el año gravable {0} por parte de:", ano);

                    Chunk cSerfi = new Chunk(serfi, boldFont);
                    Chunk ccon = new Chunk(sCon, courier);
                    Chunk cNit = new Chunk(sNit, boldFont);
                    Chunk beginning = new Chunk(tcuerpo, courier);
                    Phrase p1 = new Phrase(cSerfi);
                    Phrase p2 = new Phrase(ccon);
                    Phrase p3 = new Phrase(cNit);
                    Phrase p4 = new Phrase(beginning);

                    Paragraph p = new Paragraph();
                    p.Alignment = Element.ALIGN_JUSTIFIED;
                    p.SpacingBefore = 78;
                    p.Add(p1);
                    p.Add(p2);
                    p.Add(p3);
                    p.Add(p4);
                    document.Add(p);

                    //TABLA1: datos del cliente nit y nombre
                    var tabla1 = new PdfPTable(2);
                    Phrase pNombre = new Phrase("Nombre o razón social:", normalFont);
                    Phrase pNit = new Phrase("NIT:", normalFont);
                    tabla1.AddCell(pNombre);
                    tabla1.AddCell(nombre);
                    tabla1.AddCell(pNit);
                    tabla1.AddCell(idCliente);
                    tabla1.SpacingAfter = 25;
                    tabla1.SpacingBefore = 20f;
                    tabla1.SpacingAfter = 30f;
                    //fix the absolute width of the table
                    tabla1.LockedWidth = true;
                    //relative col widths in proportions - 1/3 and 2/3
                    float[] widths = new float[] { 1f, 2f };
                    tabla1.SetWidths(widths);
                    tabla1.WidthPercentage = 100f;
                    tabla1.TotalWidth = 500f;
                    tabla1.HorizontalAlignment = 0;
                    document.Add(tabla1);


                    //Texto 2
                    string tdetallado = "Lo detallado a continuación:";
                    Chunk chdetallado = new Chunk(tdetallado, courier);
                    Phrase pdetallado = new Phrase(chdetallado);
                    Paragraph pardetall = new Paragraph();
                    pardetall.Alignment = Element.ALIGN_JUSTIFIED;
                    pardetall.Add(pdetallado);
                    document.Add(pardetall);



                    //TABLA2: datos de la obligacion 
                    var tabla2 = new PdfPTable(5);
                    Paragraph pSaldo = new Paragraph("Saldo Capital", FontBoldTable);
                    pSaldo.Alignment = Element.ALIGN_CENTER;
                    Paragraph pInterCausa = new Paragraph("Intereses Causados", FontBoldTable);
                    pInterCausa.Alignment = Element.ALIGN_CENTER;
                    Paragraph pInterPag = new Paragraph("Intereses Pagados", FontBoldTable);
                    pInterPag.Alignment = Element.ALIGN_CENTER;
                    Paragraph pRetPract = new Paragraph("Retención Practicada", FontBoldTable);
                    pRetPract.Alignment = Element.ALIGN_CENTER;
                    Paragraph pIntExcent = new Paragraph("Intereses Exentos", FontBoldTable);
                    pIntExcent.Alignment = Element.ALIGN_CENTER;


                    PdfPCell phcSaldo = new PdfPCell();
                    phcSaldo.AddElement(pSaldo);
                    phcSaldo.BackgroundColor = BaseColor.LightGray;
                    phcSaldo.HorizontalAlignment = Element.ALIGN_CENTER;
                    phcSaldo.VerticalAlignment = Element.ALIGN_CENTER;
                    tabla2.AddCell(phcSaldo);

                    PdfPCell phcInter = new PdfPCell();
                    phcInter.AddElement(pInterCausa);
                    phcInter.BackgroundColor = BaseColor.LightGray;
                    phcInter.HorizontalAlignment = Element.ALIGN_CENTER;
                    phcInter.VerticalAlignment = Element.ALIGN_CENTER;
                    tabla2.AddCell(phcInter);

                    PdfPCell phcOtrosPag = new PdfPCell();
                    phcOtrosPag.AddElement(pInterPag);
                    phcOtrosPag.BackgroundColor = BaseColor.LightGray;
                    phcOtrosPag.HorizontalAlignment = Element.ALIGN_CENTER;
                    phcOtrosPag.VerticalAlignment = Element.ALIGN_CENTER;
                    tabla2.AddCell(phcOtrosPag);

                    PdfPCell phcConcept = new PdfPCell();
                    phcConcept.AddElement(pRetPract);
                    phcConcept.BackgroundColor = BaseColor.LightGray;
                    phcConcept.HorizontalAlignment = Element.ALIGN_CENTER;
                    phcConcept.VerticalAlignment = Element.ALIGN_CENTER;
                    tabla2.AddCell(phcConcept);

                    PdfPCell phc = new PdfPCell();
                    phc.AddElement(pIntExcent);
                    phc.BackgroundColor = BaseColor.LightGray;
                    phc.HorizontalAlignment = Element.ALIGN_CENTER;
                    phc.VerticalAlignment = Element.ALIGN_CENTER;
                    tabla2.AddCell(phc);

                    foreach (DTS_RETEFUENTE_CDT cdt in proRetefuente)
                    {
                        double saldo_capital = double.Parse(cdt.saldo_capital.ToString());
                        PdfPCell cellSaldoCap = new PdfPCell(new Phrase(saldo_capital.ToString("C2", CultureInfo.GetCultureInfo(9226))));
                        cellSaldoCap.HorizontalAlignment = Element.ALIGN_RIGHT;
                        tabla2.AddCell(cellSaldoCap);

                        double intereses_causado = double.Parse(cdt.intereses_causados.ToString());
                        PdfPCell cellIntCaus = new PdfPCell(new Phrase(intereses_causado.ToString("C2", CultureInfo.GetCultureInfo(9226))));
                        cellIntCaus.HorizontalAlignment = Element.ALIGN_RIGHT;
                        tabla2.AddCell(cellIntCaus);

                        double interes_pagado = double.Parse(cdt.interes_pagado.ToString());
                        PdfPCell cellItersesPag = new PdfPCell(new Phrase(interes_pagado.ToString("C2", CultureInfo.GetCultureInfo(9226))));
                        cellItersesPag.HorizontalAlignment = Element.ALIGN_RIGHT;
                        tabla2.AddCell(cellItersesPag);

                        double retefuente_cobrada = double.Parse(cdt.retefuente_cobrada.ToString());
                        PdfPCell cellRetPract = new PdfPCell(new Phrase(retefuente_cobrada.ToString("C2", CultureInfo.GetCultureInfo(9226))));
                        cellRetPract.HorizontalAlignment = Element.ALIGN_RIGHT;
                        tabla2.AddCell(cellRetPract);

                        double porcentaje_excento = double.Parse(cdt.porcentaje_excento.ToString());
                        PdfPCell cellIntExcen = new PdfPCell(new Phrase(porcentaje_excento.ToString("C2", CultureInfo.GetCultureInfo(9226))));
                        cellIntExcen.HorizontalAlignment = Element.ALIGN_RIGHT;
                        tabla2.AddCell(cellIntExcen);
                    }

                    tabla2.SpacingBefore = 20f;
                    tabla2.SpacingAfter = 30f;
                    //fix the absolute width of the table
                    tabla2.LockedWidth = true;
                    //relative col widths in proportions - 1/3 and 2/3
                    float[] widths2 = new float[] { 1.3f, 1f, 1f, 1f, 1f };
                    tabla2.SetWidths(widths2);
                    tabla2.WidthPercentage = 100f;
                    tabla2.TotalWidth = 500f;
                    tabla2.HorizontalAlignment = 0;
                    document.Add(tabla2);

                    string temecion = string.Format("Certificado emitido en la ciudad de Barranquilla, el {0}", DateTime.Now.ToShortDateString());
                    Chunk chemicion = new Chunk(temecion, courier);
                    Phrase pdemicion = new Phrase(chemicion);
                    Paragraph paremi = new Paragraph();
                    paremi.Alignment = Element.ALIGN_JUSTIFIED;
                    paremi.Add(pdemicion);
                    document.Add(paremi);

                    var courier2 = FontFactory.GetFont(FontFactory.HELVETICA, 9);
                    string footer = string.Format("Nota: Este certificado no señala el tratamiento tributario que se debe dar a los diferentes rubros, por lo tanto es deber de cada persona precisar las disposiciones legales para dar el tratamiento tributario correspondiente");

                    Chunk chfooter = new Chunk(footer, courier2);
                    Phrase pfooter = new Phrase(chfooter);
                    Paragraph pfoot = new Paragraph();
                    pfoot.Alignment = Element.ALIGN_JUSTIFIED;
                    pfoot.SpacingBefore = 180f;
                    pfoot.Add(pfooter);
                    document.Add(pfoot);


                    string footer2 = "Este certificado se expide sin firma autógrafa de conformidad con lo dispuesto en el Artículo 10 del Decreto Reglamentario 836 de 1991.";
                    Chunk chfooter2 = new Chunk(footer2, courier2);
                    Phrase pfooter2 = new Phrase(chfooter2);
                    Paragraph pfoot2 = new Paragraph();
                    pfoot2.Alignment = Element.ALIGN_JUSTIFIED;
                    pfoot2.Add(pfooter2);
                    document.Add(pfoot2);

                    document.Close();
                    writer.Flush();


                    if (cifrado)
                    {
                        pdfFile = encryptPDF(ms.GetBuffer(), idCliente);
                    }
                    else
                    {
                        pdfFile = ms.GetBuffer();
                    }

                }
            }

            return pdfFile;

        }

        public async Task<byte[]> ReferenciaBancaria(string tipoId, string idCliente, string nombre, bool cifrado)
        {
            byte[] pdfFile = new byte[0];

            using (MemoryStream ms = new MemoryStream())
            {

                var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);
                var FontBoldTable = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);


                Document document = new Document();
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                writer.PageEvent = new PDFFooter();
                document.Open();



                //Fecha 
                //String fecha = DateTime.Now.ToLongDateString();
                //String fechaUpper = DateTime.Now.ToLongDateString().ToUpper();
                //fecha = fecha.Replace(fecha[0], fechaUpper[0]);
                //Chunk chMonth = new Chunk(fecha, boldFont);
                //Phrase phMont = new Phrase(chMonth);
                //Paragraph ph = new Paragraph();
                //ph.SpacingBefore = (float)152;
                //ph.SpacingAfter = (float)32;
                //ph.Add(phMont);
                //document.Add(ph);

                //TITUTLO

                string titulo1 = @"CERTIFICADO REFERENCIA COMERCIAL";
                var ptitle = new Paragraph(titulo1, boldFont);
                ptitle.Alignment = Element.ALIGN_CENTER;
                ptitle.SpacingBefore = 100;
                ptitle.SpacingAfter = 32;
                document.Add(ptitle);

                //LOGO SUPER
                string imageURLSuper = Path.Combine(_webHostEnvironment.WebRootPath, "~/Assets/Images/vigilado_super.jpg");
                Image logosuper = Image.GetInstance(imageURLSuper);
                logosuper.Alignment = Image.TEXTWRAP | Element.ALIGN_LEFT;
                logosuper.Rotation = (float)Math.PI / 2;
                logosuper.RotationDegrees = 90f;
                logosuper.ScalePercent(50, 30);
                logosuper.SetAbsolutePosition(10, 200);
                document.Add(logosuper);

                //CUERPO
                var courier = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                string tcuerpo = string.Format("Banco Serfinanza S.A se permite informar que el cliente ");

                Chunk beginning = new Chunk(tcuerpo, courier);
                Phrase p4 = new Phrase(beginning);

                Chunk chNomb = new Chunk(string.Format("{0},", nombre.ToUpper()), boldFont);
                Phrase pNomb = new Phrase(chNomb);

                string tipoIdentificacionText = TipoIdentificacion(tipoId);
                Chunk chtcuerpor2 = new Chunk(string.Format(" Identificado (a) con {0} ", tipoIdentificacionText), courier);
                Phrase pchCuerpo = new Phrase(chtcuerpor2);

                Chunk chcedula = new Chunk(string.Format("N° {0}, ", idCliente), boldFont);
                Phrase phcedula = new Phrase(chcedula);

                string fechaAper = string.Format("a la fecha de expedición de esta certificación, tiene los siguientes productos:");


                Chunk chtcuerpor3 = new Chunk(fechaAper, courier);
                Phrase pchCuerpo3 = new Phrase(chtcuerpor3);

                Paragraph p = new Paragraph();
                p.Alignment = Element.ALIGN_JUSTIFIED;
                p.SpacingBefore = 38;
                p.Add(p4);
                p.Add(pNomb);
                p.Add(pchCuerpo);
                p.Add(phcedula);
                p.Add(pchCuerpo3);
                document.Add(p);

                //TABLA2: datos de la obligacion 
                var tabla2 = new PdfPTable(4);
                Phrase pObligacion = new Phrase("Obligación", FontBoldTable);
                Paragraph prObli = new Paragraph(pObligacion);
                prObli.Alignment = Element.ALIGN_CENTER;

                Phrase pSaldo = new Phrase("Referencia", FontBoldTable);
                Paragraph prSaldo = new Paragraph(pSaldo);
                prSaldo.Alignment = Element.ALIGN_CENTER;

                Phrase pInterPag = new Phrase("Fecha Emisión", FontBoldTable);
                Paragraph prInterPag = new Paragraph(pInterPag);
                prInterPag.Alignment = Element.ALIGN_CENTER;

                Phrase pOtrosPag = new Phrase("Estado", FontBoldTable);
                Paragraph prOtrosPag = new Paragraph(pOtrosPag);
                prOtrosPag.Alignment = Element.ALIGN_CENTER;

                PdfPCell phc = new PdfPCell();
                phc.AddElement(prObli);
                phc.BackgroundColor = BaseColor.LightGray;
                phc.HorizontalAlignment = Element.ALIGN_CENTER;
                tabla2.AddCell(phc);

                PdfPCell phcSaldo = new PdfPCell();
                phcSaldo.AddElement(prSaldo);
                phcSaldo.BackgroundColor = BaseColor.LightGray;
                phcSaldo.HorizontalAlignment = Element.ALIGN_CENTER;
                tabla2.AddCell(phcSaldo);

                PdfPCell phcInter = new PdfPCell();
                phcInter.AddElement(prInterPag);
                phcInter.BackgroundColor = BaseColor.LightGray;
                phcInter.HorizontalAlignment = Element.ALIGN_CENTER;
                tabla2.AddCell(phcInter);

                PdfPCell phcOtrosPag = new PdfPCell();
                phcOtrosPag.AddElement(prOtrosPag);
                phcOtrosPag.BackgroundColor = BaseColor.LightGray;
                phcOtrosPag.HorizontalAlignment = Element.ALIGN_CENTER | Element.ALIGN_MIDDLE;
                tabla2.AddCell(phcOtrosPag);

                var listaCms = await productoService.ConsultaCMS(tipoId, idCliente, false);
                if (listaCms != null)
                {
                    foreach (JObject cms in listaCms)
                    {
                        var numProducto = cms.GetValue("numProducto").Value<string>();
                        var nombremProducto = cms.GetValue("nomProducto").Value<string>();
                        var fechaEmision = cms.GetValue("fechaEmision").Value<string>();
                        var estado = cms.GetValue("estado").Value<string>();

                        PdfPCell cellObli = new PdfPCell(new Phrase(numProducto, courier));
                        cellObli.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellObli);

                        PdfPCell cellNombre = new PdfPCell(new Phrase(nombremProducto, courier));
                        cellNombre.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellNombre);

                        PdfPCell cellFecha = new PdfPCell(new Phrase(fechaEmision, courier));
                        cellFecha.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellFecha);

                        PdfPCell cellEstado = new PdfPCell(new Phrase(estado, courier));
                        cellEstado.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellEstado);
                    }
                }
                var listaPrestamos = await productoService.ConsultarPrestamos(tipoId, idCliente);
                if (listaPrestamos != null)
                {
                    foreach (JObject prestamo in listaPrestamos)
                    {
                        var numProducto = prestamo.GetValue("numProducto").Value<string>();
                        var nombremProducto = prestamo.GetValue("nomProducto").Value<string>();
                        var fechaEmision = prestamo.GetValue("fechaEmision").Value<string>();
                        var estado = prestamo.GetValue("estado").Value<string>();

                        PdfPCell cellObli = new PdfPCell(new Phrase(numProducto, courier));
                        cellObli.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellObli);

                        PdfPCell cellNombre = new PdfPCell(new Phrase(nombremProducto, courier));
                        cellNombre.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellNombre);

                        PdfPCell cellFecha = new PdfPCell(new Phrase(fechaEmision, courier));
                        cellFecha.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellFecha);

                        PdfPCell cellEstado = new PdfPCell(new Phrase(estado, courier));
                        cellEstado.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellEstado);
                    }
                }
                var listaAhorros = await productoService.ConsultarSaldosCTAH(idCliente);
                if (listaAhorros != null)
                {
                    foreach (JObject ahorro in listaAhorros)
                    {
                        var numProducto = ahorro.GetValue("numProducto").Value<string>();
                        var nombremProducto = ahorro.GetValue("nomProducto").Value<string>();
                        var fechaEmision = ahorro.GetValue("fechaEmision").Value<string>();
                        var estado = ahorro.GetValue("estado").Value<string>();

                        PdfPCell cellObli = new PdfPCell(new Phrase(numProducto, courier));
                        cellObli.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellObli);

                        PdfPCell cellNombre = new PdfPCell(new Phrase(nombremProducto, courier));
                        cellNombre.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellNombre);

                        PdfPCell cellFecha = new PdfPCell(new Phrase(fechaEmision, courier));
                        cellFecha.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellFecha);

                        PdfPCell cellEstado = new PdfPCell(new Phrase(estado, courier));
                        cellEstado.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellEstado);
                    }
                }
                var listaCorriente = await productoService.ConsultaCuentaCorriente(tipoId, idCliente, Utils.RESUMEN);
                if (listaCorriente != null)
                {
                    foreach (JObject corriente in listaCorriente)
                    {
                        var numProducto = corriente.GetValue("numProducto").Value<string>();
                        var nombremProducto = corriente.GetValue("nomProducto").Value<string>();
                        var fechaEmision = corriente.GetValue("fechaEmision").Value<string>();
                        var estado = corriente.GetValue("estado").Value<string>();

                        PdfPCell cellObli = new PdfPCell(new Phrase(numProducto, courier));
                        cellObli.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellObli);

                        PdfPCell cellNombre = new PdfPCell(new Phrase(nombremProducto, courier));
                        cellNombre.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellNombre);

                        PdfPCell cellFecha = new PdfPCell(new Phrase(fechaEmision, courier));
                        cellFecha.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellFecha);

                        PdfPCell cellEstado = new PdfPCell(new Phrase(estado, courier));
                        cellEstado.HorizontalAlignment = Element.ALIGN_CENTER;
                        tabla2.AddCell(cellEstado);
                    }
                }

                tabla2.SpacingBefore = 20f;
                tabla2.SpacingAfter = 15f;
                //fix the absolute width of the table
                tabla2.LockedWidth = true;
                //relative col widths in proportions - 1/3 and 2/3
                float[] widths2 = new float[] { 1f, 1.3f, 0.7f, 0.7f };
                tabla2.SetWidths(widths2);
                tabla2.WidthPercentage = 90f;
                tabla2.TotalWidth = 450f;
                tabla2.HorizontalAlignment = Element.ALIGN_CENTER;
                document.Add(tabla2);


                string temecionn = string.Format("Se expide la presente certificación a los ({0}) días del mes de ", DateTime.Now.Day);
                Chunk chemicionn = new Chunk(temecionn, courier);
                Phrase pdemicionn = new Phrase(chemicionn);

                string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month).ToUpper();
                Chunk chMonthT = new Chunk(monthName, boldFont);
                Phrase phMontT = new Phrase(chMonthT);

                string resto = string.Format(" de {0}, a solicitud de la parte interesada.", DateTime.Now.Year);
                Chunk chResto = new Chunk(resto, courier);
                Phrase phResto = new Phrase(chResto);



                Paragraph paremiT = new Paragraph();
                paremiT.Alignment = Element.ALIGN_JUSTIFIED;
                paremiT.SpacingBefore = 30;
                paremiT.SpacingAfter = 10;
                paremiT.Add(pdemicionn);
                paremiT.Add(phMontT);
                paremiT.Add(chResto);
                document.Add(paremiT);

                string temecion = string.Format("*Importante: ");
                Chunk chemicion = new Chunk(temecion, boldFont);
                Phrase pdemicion = new Phrase(chemicion);

                string tObserv = "Esta constancia solo hace referencia a los productos mencionados anteriormente.";
                Chunk chObserv = new Chunk(tObserv, courier);
                Phrase pObserv = new Phrase(chObserv);

                Paragraph paremi = new Paragraph();
                paremi.Alignment = Element.ALIGN_JUSTIFIED;
                paremi.SpacingBefore = 5;
                paremi.SpacingAfter = 10;
                paremi.Add(pdemicion);
                paremi.Add(pObserv);
                document.Add(paremi);


                //String body2 = "*Si desea verificar la veracidad de esta información, puede comunicarse a la linea de servicio al cliente de Banco Serfinanza al 3235997000 y 018000510513.";
                //Chunk cbody2 = new Chunk(body2, courier);
                //Phrase pbody2 = new Phrase(cbody2);

                //Paragraph prbody2 = new Paragraph();
                //prbody2.Alignment = Element.ALIGN_JUSTIFIED;
                //prbody2.Add(pbody2);
                //prbody2.SpacingBefore = (float)5;
                //document.Add(prbody2);



                var courier2 = FontFactory.GetFont(FontFactory.HELVETICA, 9);


                string footer2 = "Atentamente,";
                Chunk chfooter2 = new Chunk(footer2, courier);
                Phrase pfooter2 = new Phrase(chfooter2);
                Paragraph pfoot2 = new Paragraph();
                pfoot2.Alignment = Element.ALIGN_JUSTIFIED;
                pfoot2.SpacingBefore = 38;
                pfoot2.Add(pfooter2);
                document.Add(pfoot2);

                //FIRMA
                string imageURLFirma = Path.Combine(_webHostEnvironment.WebRootPath, "~/Assets/Images/Firma.jpg");
                Image firma = Image.GetInstance(imageURLFirma);
                firma.Alignment = Element.ALIGN_LEFT;
                firma.ScalePercent(60, 60);
                document.Add(firma);

                string footer3 = "Dpto. de Servicio al Cliente";
                Chunk chfooter3 = new Chunk(footer3, boldFont);
                Phrase pfooter3 = new Phrase(chfooter3);
                Paragraph pfoot3 = new Paragraph();
                pfoot3.Alignment = Element.ALIGN_LEFT;
                pfoot3.SpacingBefore = 0;
                pfoot3.Add(pfooter3);
                document.Add(pfoot3);

                //var courier2 = FontFactory.GetFont(FontFactory.HELVETICA, 9);


                //String footer2 = "Según el decreto 836/91, este certificado no requiere firma autografa.";
                //Chunk chfooter2 = new Chunk(footer2, courier2);
                //Phrase pfooter2 = new Phrase(chfooter2);
                //Paragraph pfoot2 = new Paragraph();
                //pfoot2.Alignment = Element.ALIGN_JUSTIFIED;
                //pfoot2.SpacingBefore = (float)78;
                //pfoot2.Add(pfooter2);
                //document.Add(pfoot2);


                document.Close();
                writer.Flush();

                if (cifrado)
                {
                    pdfFile = encryptPDF(ms.GetBuffer(), idCliente);
                }
                else
                {
                    pdfFile = ms.GetBuffer();
                }

            }

            return pdfFile;
        }


        private byte[] encryptPDF(byte[] pdf, string clave)
        {
            using (MemoryStream pdfStream = new MemoryStream(pdf))
            {
                using (MemoryStream encryptPdfStream = new MemoryStream())
                {
                    PdfReader reader = new PdfReader(pdfStream);
                    PdfEncryptor.Encrypt(reader, encryptPdfStream, true, clave, clave, PdfWriter.ALLOW_SCREENREADERS);
                    return encryptPdfStream.ToArray();
                }
            }

        }

        public string getSecureText(string text)
        {
            var builder = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                string chr = text[i].ToString();
                builder.Append(text.Length - i <= 4 ? chr : "*");

            }

            return builder.ToString();

        }

        public byte[] CertificadoRetencion(string tipoId, string idCliente, string nombre, string ano, bool cifrado)
        {
            bool valid = false;
            byte[] pdfFile = new byte[0];

            decimal anio = Convert.ToDecimal(ano);

            IClienteServices clienteServices = new ClienteServices(productoService, Context, _configuration);
            Models.Cliente cliente = clienteServices.ConsultaClienteByDoc(tipoId, idCliente);


            string clienteFullName = cliente != null ? cliente.PrimerNombre + (!string.IsNullOrEmpty(cliente.SegundoNombre) ? " " + cliente.SegundoNombre : "") + (!string.IsNullOrEmpty(cliente.PrimerApellido) ? " " + cliente.PrimerApellido : "") + (!string.IsNullOrEmpty(cliente.SegundoApellido) ? " " + cliente.SegundoApellido : "") : nombre;
            List<DTS_RETEFUENTE_AHO> retefuenteAho = null;
            List<DTS_RETEFUENTE_CDT> retefuenteCdt = null;
            List<DTS_RETEFUENTE> retefuente = null;

            //retefuenteAho = Context.DTS_RETEFUENTE_AHO.Where(x => x.ano == anio && x.nit == idCliente).ToList();
            //retefuenteCdt = Context.DTS_RETEFUENTE_CDT.Where(x => x.ano == anio && x.nit == idCliente).ToList();

            var parameters = new SqlParameter[] { new SqlParameter("@ano", ano), new SqlParameter("@nit", idCliente) };
            //retefuente = Context.Database.SqlQuery<DTS_RETEFUENTE>($"SELECT * FROM DTS_RETEFUENTE Where ano=@ano and nit=@nit", parameters).ToList(); // 01-08-2022 se cambia así porque la tabla no tiene llave primara y da erro duplicando registros.


            valid = retefuenteAho != null && retefuenteAho.Count > 0 || retefuenteCdt != null && retefuenteCdt.Count > 0 || retefuente != null && retefuente.Count > 0;

            if (valid)
            {

                using (MemoryStream ms = new MemoryStream())
                {
                    Document document = new Document();

                    PdfWriter writer = PdfWriter.GetInstance(document, ms);
                    writer.PageEvent = new PDFFooter();
                    document.Open();

                    //TITUTLO
                    var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                    var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 11);
                    var FontBoldTable = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);

                    string titulo1 = @"CERTIFICADO DE RETENCIÓN EN LA FUENTE";
                    var ptitle = new Paragraph(titulo1, boldFont);
                    ptitle.Alignment = Element.ALIGN_CENTER;
                    ptitle.SpacingBefore = 60;
                    document.Add(ptitle);

                    string titulo2 = string.Format("AÑO GRAVABLE {0}", ano);
                    var ptitle2 = new Paragraph(titulo2, boldFont);
                    ptitle2.Alignment = Element.ALIGN_CENTER;
                    ptitle2.SpacingBefore = 0;
                    document.Add(ptitle2);

                    //LOGO SUPER
                    string imageURLSuper = Path.Combine(_webHostEnvironment.WebRootPath, "~/Assets/Images/vigilado_super.jpg");
                    Image logosuper = Image.GetInstance(imageURLSuper);
                    logosuper.Alignment = Image.TEXTWRAP | Element.ALIGN_LEFT;
                    logosuper.Rotation = (float)Math.PI / 2;
                    logosuper.RotationDegrees = 90f;
                    logosuper.ScalePercent(50, 30);
                    logosuper.SetAbsolutePosition(10, 200);
                    document.Add(logosuper);

                    //CUERPO
                    var courier = FontFactory.GetFont(FontFactory.HELVETICA, 11);


                    Chunk cSerfi = new Chunk("BANCO SERFINANZA S.A. ", normalFont);
                    Paragraph pSerfi = new Paragraph();
                    pSerfi.Alignment = Element.ALIGN_JUSTIFIED;
                    pSerfi.SpacingBefore = 20;
                    pSerfi.Add(cSerfi);
                    document.Add(pSerfi);

                    Chunk cNit = new Chunk("NIT 860.043.186 ", normalFont);
                    Paragraph pNit = new Paragraph();
                    pNit.Alignment = Element.ALIGN_JUSTIFIED;
                    pNit.Add(cNit);
                    document.Add(pNit);

                    Chunk cAddress = new Chunk("Calle 72 # 54-35", normalFont);
                    Paragraph pAddress = new Paragraph();
                    pAddress.Alignment = Element.ALIGN_JUSTIFIED;
                    pAddress.Add(cAddress);
                    document.Add(pAddress);

                    Chunk cNameClient = new Chunk("CLIENTE: " + clienteFullName, normalFont); ;
                    Paragraph pNameClient = new Paragraph();
                    pNameClient.Alignment = Element.ALIGN_JUSTIFIED;
                    pNameClient.SpacingBefore = 20;
                    pNameClient.Add(cNameClient);
                    document.Add(pNameClient);

                    Chunk cIdClient = new Chunk("IDENTIFICACIÓN: " + idCliente, normalFont);
                    Paragraph pIdClient = new Paragraph();
                    pIdClient.Alignment = Element.ALIGN_JUSTIFIED;
                    pIdClient.Add(cIdClient);
                    pIdClient.SpacingAfter = 20;
                    document.Add(pIdClient);

                    if (retefuenteAho != null && retefuenteAho.Count > 0)
                    {
                        Chunk chTitleTable = new Chunk("CUENTAS DE AHORRO Y CORRIENTE", courier);
                        Phrase pTitleTable = new Phrase(chTitleTable);
                        Paragraph pgTitleTable = new Paragraph();
                        pgTitleTable.Alignment = Element.ALIGN_JUSTIFIED;
                        pNameClient.SpacingBefore = 50;
                        pgTitleTable.Add(pTitleTable);
                        document.Add(pgTitleTable);



                        //TABLA2: datos de la obligacion 
                        var tabla = new PdfPTable(6);

                        Paragraph paragraph1 = new Paragraph("PRODUCTO", FontBoldTable);
                        paragraph1.Alignment = Element.ALIGN_CENTER;
                        PdfPCell PdfPCell1 = new PdfPCell();
                        PdfPCell1.AddElement(paragraph1);
                        PdfPCell1.BackgroundColor = BaseColor.LightGray;
                        PdfPCell1.HorizontalAlignment = Element.ALIGN_CENTER;
                        PdfPCell1.VerticalAlignment = Element.ALIGN_CENTER;
                        tabla.AddCell(PdfPCell1);

                        Paragraph paragraph2 = new Paragraph("SALDO DIC 31/" + anio, FontBoldTable);
                        paragraph2.Alignment = Element.ALIGN_CENTER;
                        PdfPCell PdfPCell2 = new PdfPCell();
                        PdfPCell2.AddElement(paragraph2);
                        PdfPCell2.BackgroundColor = BaseColor.LightGray;
                        PdfPCell2.HorizontalAlignment = Element.ALIGN_CENTER;
                        PdfPCell2.VerticalAlignment = Element.ALIGN_CENTER;
                        tabla.AddCell(PdfPCell2);

                        Paragraph paragraph3 = new Paragraph("RENDIMIENTOS PAGADOS", FontBoldTable);
                        paragraph3.Alignment = Element.ALIGN_CENTER;
                        PdfPCell PdfPCell3 = new PdfPCell();
                        PdfPCell3.AddElement(paragraph3);
                        PdfPCell3.BackgroundColor = BaseColor.LightGray;
                        PdfPCell3.HorizontalAlignment = Element.ALIGN_CENTER;
                        PdfPCell3.VerticalAlignment = Element.ALIGN_CENTER;
                        tabla.AddCell(PdfPCell3);

                        Paragraph paragraph4 = new Paragraph("RETENCIÓN EN LA FUENTE", FontBoldTable);
                        paragraph4.Alignment = Element.ALIGN_CENTER;
                        PdfPCell PdfPCell4 = new PdfPCell();
                        PdfPCell4.AddElement(paragraph4);
                        PdfPCell4.BackgroundColor = BaseColor.LightGray;
                        PdfPCell4.HorizontalAlignment = Element.ALIGN_CENTER;
                        PdfPCell4.VerticalAlignment = Element.ALIGN_CENTER;
                        tabla.AddCell(PdfPCell4);

                        Paragraph paragraph5 = new Paragraph("G.M.F.", FontBoldTable);
                        paragraph5.Alignment = Element.ALIGN_CENTER;
                        PdfPCell PdfPCell5 = new PdfPCell();
                        PdfPCell5.AddElement(paragraph5);
                        PdfPCell5.BackgroundColor = BaseColor.LightGray;
                        PdfPCell5.HorizontalAlignment = Element.ALIGN_CENTER;
                        PdfPCell5.VerticalAlignment = Element.ALIGN_CENTER;
                        tabla.AddCell(PdfPCell5);

                        Paragraph paragraph6 = new Paragraph("BASE GMF", FontBoldTable);
                        paragraph6.Alignment = Element.ALIGN_CENTER;
                        PdfPCell PdfPCell6 = new PdfPCell();
                        PdfPCell6.AddElement(paragraph6);
                        PdfPCell6.BackgroundColor = BaseColor.LightGray;
                        PdfPCell6.HorizontalAlignment = Element.ALIGN_CENTER;
                        PdfPCell6.VerticalAlignment = Element.ALIGN_CENTER;
                        tabla.AddCell(PdfPCell6);


                        foreach (DTS_RETEFUENTE_AHO row in retefuenteAho)
                        {

                            string nomProducto = row.nomProducto != null ? row.nomProducto.ToString() : "";
                            PdfPCell pdfPCell1 = new PdfPCell(new Phrase(nomProducto));
                            pdfPCell1.HorizontalAlignment = Element.ALIGN_RIGHT;
                            tabla.AddCell(pdfPCell1);

                            string saldox = row.saldo != null ? row.saldo.ToString() : "0";
                            double saldo = double.Parse(saldox);
                            PdfPCell pdfPCell2 = new PdfPCell(new Phrase(saldo.ToString("C2", CultureInfo.GetCultureInfo(9226))));
                            pdfPCell2.HorizontalAlignment = Element.ALIGN_RIGHT;
                            tabla.AddCell(pdfPCell2);

                            string interesesx = row.intereses != null ? row.intereses.ToString() : "0";
                            double intereses = double.Parse(interesesx);
                            PdfPCell pdfPCell3 = new PdfPCell(new Phrase(intereses.ToString("C2", CultureInfo.GetCultureInfo(9226))));
                            pdfPCell3.HorizontalAlignment = Element.ALIGN_RIGHT;
                            tabla.AddCell(pdfPCell3);

                            string retencionx = row.retencion != null ? row.retencion.ToString() : "0";
                            double retencion = double.Parse(retencionx);
                            PdfPCell pdfPCell4 = new PdfPCell(new Phrase(retencion.ToString("C2", CultureInfo.GetCultureInfo(9226))));
                            pdfPCell4.HorizontalAlignment = Element.ALIGN_RIGHT;
                            tabla.AddCell(pdfPCell4);

                            string gmfx = row.gmf != null ? row.gmf.ToString() : "0";
                            double gmf = double.Parse(gmfx);
                            PdfPCell pdfPCell5 = new PdfPCell(new Phrase(gmf.ToString("C2", CultureInfo.GetCultureInfo(9226))));
                            pdfPCell5.HorizontalAlignment = Element.ALIGN_RIGHT;
                            tabla.AddCell(pdfPCell5);

                            string baseGmfx = row.baseGmf != null ? row.baseGmf.ToString() : "0";
                            double baseGmf = double.Parse(baseGmfx);
                            PdfPCell pdfPCell6 = new PdfPCell(new Phrase(baseGmf.ToString("C2", CultureInfo.GetCultureInfo(9226))));
                            pdfPCell6.HorizontalAlignment = Element.ALIGN_RIGHT;
                            tabla.AddCell(pdfPCell6);
                        }

                        tabla.SpacingBefore = 10f;
                        //fix the absolute width of the table
                        tabla.LockedWidth = true;
                        //relative col widths in proportions - 1/3 and 2/3
                        float[] widths2 = new float[] { 1.5f, 1f, 1.3f, 1.3f, 1f, 1f };
                        tabla.SetWidths(widths2);
                        tabla.WidthPercentage = 100f;
                        tabla.TotalWidth = 530f;
                        tabla.HorizontalAlignment = 0;
                        document.Add(tabla);

                    }


                    if (retefuenteCdt != null && retefuenteCdt.Count > 0)
                    {
                        Chunk chTitleTable = new Chunk("CDT", courier);
                        Phrase pTitleTable = new Phrase(chTitleTable);
                        Paragraph pgTitleTable = new Paragraph();
                        pgTitleTable.Alignment = Element.ALIGN_JUSTIFIED;
                        pgTitleTable.SpacingBefore = 30;
                        pgTitleTable.Add(pTitleTable);
                        document.Add(pgTitleTable);



                        //TABLA2: datos de la obligacion 
                        var tabla = new PdfPTable(5);



                        Paragraph paragraph2 = new Paragraph("SALDO DIC 31/" + anio, FontBoldTable);
                        paragraph2.Alignment = Element.ALIGN_CENTER;
                        PdfPCell PdfPCell2 = new PdfPCell();
                        PdfPCell2.AddElement(paragraph2);
                        PdfPCell2.BackgroundColor = BaseColor.LightGray;
                        PdfPCell2.HorizontalAlignment = Element.ALIGN_CENTER;
                        PdfPCell2.VerticalAlignment = Element.ALIGN_CENTER;
                        tabla.AddCell(PdfPCell2);

                        Paragraph paragraph3 = new Paragraph("RENDIMIENTOS PAGADOS", FontBoldTable);
                        paragraph3.Alignment = Element.ALIGN_CENTER;
                        PdfPCell PdfPCell3 = new PdfPCell();
                        PdfPCell3.AddElement(paragraph3);
                        PdfPCell3.BackgroundColor = BaseColor.LightGray;
                        PdfPCell3.HorizontalAlignment = Element.ALIGN_CENTER;
                        PdfPCell3.VerticalAlignment = Element.ALIGN_CENTER;
                        tabla.AddCell(PdfPCell3);

                        Paragraph paragraph4 = new Paragraph("RETENCIÓN EN LA FUENTE", FontBoldTable);
                        paragraph4.Alignment = Element.ALIGN_CENTER;
                        PdfPCell PdfPCell4 = new PdfPCell();
                        PdfPCell4.AddElement(paragraph4);
                        PdfPCell4.BackgroundColor = BaseColor.LightGray;
                        PdfPCell4.HorizontalAlignment = Element.ALIGN_CENTER;
                        PdfPCell4.VerticalAlignment = Element.ALIGN_CENTER;
                        tabla.AddCell(PdfPCell4);

                        Paragraph paragraph5 = new Paragraph("INGRESOS NO CONSTITUTIVOS DE RENTA", FontBoldTable);
                        paragraph5.Alignment = Element.ALIGN_CENTER;
                        PdfPCell PdfPCell5 = new PdfPCell();
                        PdfPCell5.AddElement(paragraph5);
                        PdfPCell5.BackgroundColor = BaseColor.LightGray;
                        PdfPCell5.HorizontalAlignment = Element.ALIGN_CENTER;
                        PdfPCell5.VerticalAlignment = Element.ALIGN_CENTER;
                        tabla.AddCell(PdfPCell5);

                        Paragraph paragraph6 = new Paragraph("INTERESES CAUSADOS A DIC 31/" + anio, FontBoldTable);
                        paragraph6.Alignment = Element.ALIGN_CENTER;
                        PdfPCell PdfPCell6 = new PdfPCell();
                        PdfPCell6.AddElement(paragraph6);
                        PdfPCell6.BackgroundColor = BaseColor.LightGray;
                        PdfPCell6.HorizontalAlignment = Element.ALIGN_CENTER;
                        PdfPCell6.VerticalAlignment = Element.ALIGN_CENTER;
                        tabla.AddCell(PdfPCell6);


                        foreach (DTS_RETEFUENTE_CDT row in retefuenteCdt)
                        {


                            string saldo_capital = row.saldo_capital != null ? row.saldo_capital.ToString() : "0";
                            double saldo = double.Parse(saldo_capital);
                            PdfPCell pdfPCell2 = new PdfPCell(new Phrase(saldo.ToString("C2", CultureInfo.GetCultureInfo(9226))));
                            pdfPCell2.HorizontalAlignment = Element.ALIGN_RIGHT;
                            tabla.AddCell(pdfPCell2);

                            string interes_pagado = row.interes_pagado != null ? row.interes_pagado.ToString() : "0";
                            double intereses = double.Parse(interes_pagado);
                            PdfPCell pdfPCell3 = new PdfPCell(new Phrase(intereses.ToString("C2", CultureInfo.GetCultureInfo(9226))));
                            pdfPCell3.HorizontalAlignment = Element.ALIGN_RIGHT;
                            tabla.AddCell(pdfPCell3);

                            string retefuente_cobrada = row.retefuente_cobrada != null ? row.retefuente_cobrada.ToString() : "0";
                            double retencion = double.Parse(retefuente_cobrada);
                            PdfPCell pdfPCell4 = new PdfPCell(new Phrase(retencion.ToString("C2", CultureInfo.GetCultureInfo(9226))));
                            pdfPCell4.HorizontalAlignment = Element.ALIGN_RIGHT;
                            tabla.AddCell(pdfPCell4);

                            string porcentaje_excento = row.porcentaje_excento != null ? row.porcentaje_excento.ToString() : "0";
                            double gmf = double.Parse(porcentaje_excento);
                            PdfPCell pdfPCell5 = new PdfPCell(new Phrase(gmf.ToString("C2", CultureInfo.GetCultureInfo(9226))));
                            pdfPCell5.HorizontalAlignment = Element.ALIGN_RIGHT;
                            tabla.AddCell(pdfPCell5);

                            string interesesCausados = row.intereses_causados != null ? row.intereses_causados.ToString() : "0";
                            double intereses_causados = double.Parse(interesesCausados);
                            PdfPCell pdfPCell6 = new PdfPCell(new Phrase(intereses_causados.ToString("C2", CultureInfo.GetCultureInfo(9226))));
                            pdfPCell6.HorizontalAlignment = Element.ALIGN_RIGHT;
                            tabla.AddCell(pdfPCell6);
                        }

                        tabla.SpacingBefore = 10f;
                        //fix the absolute width of the table
                        tabla.LockedWidth = true;
                        //relative col widths in proportions - 1/3 and 2/3
                        float[] widths2 = new float[] { 1f, 1f, 1f, 1f, 1f };
                        tabla.SetWidths(widths2);
                        tabla.WidthPercentage = 100f;
                        tabla.TotalWidth = 530f;
                        tabla.HorizontalAlignment = 0;
                        document.Add(tabla);

                    }

                    if (retefuente != null && retefuente.Count > 0)
                    {
                        Chunk chTitleTable = new Chunk("CRÉDITOS DE CONSUMO", courier);
                        Phrase pTitleTable = new Phrase(chTitleTable);
                        Paragraph pgTitleTable = new Paragraph();
                        pgTitleTable.Alignment = Element.ALIGN_JUSTIFIED;
                        pgTitleTable.SpacingBefore = 30;
                        pgTitleTable.Add(pTitleTable);
                        document.Add(pgTitleTable);


                        var tabla = new PdfPTable(5);

                        Paragraph paragraph1 = new Paragraph("NÚMERO", FontBoldTable);
                        paragraph1.Alignment = Element.ALIGN_CENTER;
                        PdfPCell PdfPCell1 = new PdfPCell();
                        PdfPCell1.AddElement(paragraph1);
                        PdfPCell1.BackgroundColor = BaseColor.LightGray;
                        PdfPCell1.HorizontalAlignment = Element.ALIGN_CENTER;
                        PdfPCell1.VerticalAlignment = Element.ALIGN_CENTER;
                        tabla.AddCell(PdfPCell1);

                        Paragraph paragraph2 = new Paragraph("SALDO DIC 31/" + anio, FontBoldTable);
                        paragraph2.Alignment = Element.ALIGN_CENTER;
                        PdfPCell PdfPCell2 = new PdfPCell();
                        PdfPCell2.AddElement(paragraph2);
                        PdfPCell2.BackgroundColor = BaseColor.LightGray;
                        PdfPCell2.HorizontalAlignment = Element.ALIGN_CENTER;
                        PdfPCell2.VerticalAlignment = Element.ALIGN_CENTER;
                        tabla.AddCell(PdfPCell2);

                        Paragraph paragraph3 = new Paragraph("INTERESES PAGADOS", FontBoldTable);
                        paragraph3.Alignment = Element.ALIGN_CENTER;
                        PdfPCell PdfPCell3 = new PdfPCell();
                        PdfPCell3.AddElement(paragraph3);
                        PdfPCell3.BackgroundColor = BaseColor.LightGray;
                        PdfPCell3.HorizontalAlignment = Element.ALIGN_CENTER;
                        PdfPCell3.VerticalAlignment = Element.ALIGN_CENTER;
                        tabla.AddCell(PdfPCell3);


                        Paragraph paragraph5 = new Paragraph("CUOTA DE MANEJO", FontBoldTable);
                        paragraph5.Alignment = Element.ALIGN_CENTER;
                        PdfPCell PdfPCell5 = new PdfPCell();
                        PdfPCell5.AddElement(paragraph5);
                        PdfPCell5.BackgroundColor = BaseColor.LightGray;
                        PdfPCell5.HorizontalAlignment = Element.ALIGN_CENTER;
                        PdfPCell5.VerticalAlignment = Element.ALIGN_CENTER;
                        tabla.AddCell(PdfPCell5);

                        Paragraph paragraph6 = new Paragraph("OTROS PAGADOS", FontBoldTable);
                        paragraph6.Alignment = Element.ALIGN_CENTER;
                        PdfPCell PdfPCell6 = new PdfPCell();
                        PdfPCell6.AddElement(paragraph6);
                        PdfPCell6.BackgroundColor = BaseColor.LightGray;
                        PdfPCell6.HorizontalAlignment = Element.ALIGN_CENTER;
                        PdfPCell6.VerticalAlignment = Element.ALIGN_CENTER;
                        tabla.AddCell(PdfPCell6);


                        foreach (DTS_RETEFUENTE row in retefuente)
                        {
                            string obligacion = row.obligacion != null ? getSecureText(row.obligacion) : "";

                            PdfPCell pdfPCell1 = new PdfPCell(new Phrase(obligacion));
                            pdfPCell1.HorizontalAlignment = Element.ALIGN_RIGHT;
                            tabla.AddCell(pdfPCell1);

                            string saldo_capital = row.saldo_capital != null ? row.saldo_capital.ToString() : "0";
                            double saldo = double.Parse(saldo_capital);
                            PdfPCell pdfPCell2 = new PdfPCell(new Phrase(saldo.ToString("C2", CultureInfo.GetCultureInfo(9226))));
                            pdfPCell2.HorizontalAlignment = Element.ALIGN_RIGHT;
                            tabla.AddCell(pdfPCell2);

                            string interesesPagados = row.intereses_pagados != null ? row.intereses_pagados.ToString() : "0";
                            double intereses_pagados = double.Parse(interesesPagados);
                            PdfPCell pdfPCell3 = new PdfPCell(new Phrase(intereses_pagados.ToString("C2", CultureInfo.GetCultureInfo(9226))));
                            pdfPCell3.HorizontalAlignment = Element.ALIGN_RIGHT;
                            tabla.AddCell(pdfPCell3);

                            string otros_pagados = row.otros_pagados != null ? row.otros_pagados.ToString() : "0";
                            double cuota_manejo = double.Parse(otros_pagados);
                            PdfPCell pdfPCell4 = new PdfPCell(new Phrase(cuota_manejo.ToString("C2", CultureInfo.GetCultureInfo(9226))));
                            pdfPCell4.HorizontalAlignment = Element.ALIGN_RIGHT;
                            tabla.AddCell(pdfPCell4);

                            string conceptoOtros = row.concepto_otros != null ? row.concepto_otros.ToString() : "0";
                            double concepto_otros = double.Parse(conceptoOtros);
                            PdfPCell pdfPCell5 = new PdfPCell(new Phrase(concepto_otros.ToString("C2", CultureInfo.GetCultureInfo(9226))));
                            pdfPCell5.HorizontalAlignment = Element.ALIGN_RIGHT;
                            tabla.AddCell(pdfPCell5);

                        }

                        tabla.SpacingBefore = 10f;
                        //fix the absolute width of the table
                        tabla.LockedWidth = true;
                        //relative col widths in proportions - 1/3 and 2/3
                        float[] widths2 = new float[] { 1.3f, 1f, 1f, 1f, 1f };
                        tabla.SetWidths(widths2);
                        tabla.WidthPercentage = 100f;
                        tabla.TotalWidth = 530f;
                        tabla.HorizontalAlignment = 0;
                        document.Add(tabla);

                    }

                    string temecion = string.Format("Se expide la presente certificación a los ({0}) días del mes de ", DateTime.Now.Day);
                    Chunk chemicion = new Chunk(temecion, courier);
                    Phrase pdemicion = new Phrase(chemicion);

                    string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month).ToUpper();
                    Chunk chMonth = new Chunk(monthName, boldFont);
                    Phrase phMont = new Phrase(chMonth);

                    string resto = string.Format(" de {0}, a solicitud de la parte interesada.", DateTime.Now.Year);
                    Chunk chResto = new Chunk(resto, courier);
                    Phrase phResto = new Phrase(chResto);



                    Paragraph paremi = new Paragraph();
                    paremi.Alignment = Element.ALIGN_JUSTIFIED;
                    paremi.SpacingBefore = 30;
                    paremi.SpacingAfter = 10;
                    paremi.Add(pdemicion);
                    paremi.Add(phMont);
                    paremi.Add(chResto);
                    document.Add(paremi);


                    var courier2 = FontFactory.GetFont(FontFactory.HELVETICA, 9);

                    string footer2 = "Este certificado se expide sin firma autógrafa de conformidad con lo dispuesto en el Artículo 10 del Decreto Reglamentario 836 de 1991.";
                    Chunk chfooter2 = new Chunk(footer2, courier2);
                    Phrase pfooter2 = new Phrase(chfooter2);
                    Paragraph pfoot2 = new Paragraph();
                    pfoot2.Alignment = Element.ALIGN_JUSTIFIED;
                    pfoot2.Add(pfooter2);
                    pfoot2.SpacingBefore = 20f;
                    document.Add(pfoot2);

                    document.Close();
                    writer.Flush();

                    if (cifrado)
                    {
                        pdfFile = encryptPDF(ms.GetBuffer(), idCliente);
                    }
                    else
                    {
                        pdfFile = ms.GetBuffer();
                    }

                }

            }


            return pdfFile;

        }

        public List<ReporteAnualCostoDTO> ReportesPorIdCliente(string tipoId, string idCliente)
        {
            string token = GetToken();
            int i = 1;
            var reportes = new List<ReporteAnualCostoDTO>();

            var result = "";
            FelecFacturaSR.WsdlFacturaWebPortTypeClient felectDocument = new FelecFacturaSR.WsdlFacturaWebPortTypeClient();
            using (new OperationContextScope(felectDocument.InnerChannel))
            {
                HttpRequestMessageProperty requestMessage = new HttpRequestMessageProperty();
                requestMessage.Headers["token"] = token;
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = requestMessage;
                var tipo = "RAC";
                result = felectDocument.consultarDocumentos(idCliente, DateTime.Now.AddYears(-3).ToString("dd/MM/yyyy"), DateTime.Now.ToString("dd/MM/yyyy"), "", "pdf", idCliente, "SERFINANSA", tipo);
                if (!result.StartsWith("co.experian.computec.web.exception"))
                {
                    var lines = result.Split('\n');
                    string docID = "";
                    string NumCuenta = "";
                    string FechaFactu = "";
                    string Folder = "";
                    foreach (string line in lines)
                    {
                        if (i != 1)
                        {
                            if (!string.IsNullOrEmpty(line))
                            {
                                JObject documento = new JObject();
                                string[] l = line.Split('\t');
                                docID = l[0];
                                if (tipo.Equals("CA"))
                                {
                                    NumCuenta = l[5].Trim();
                                    FechaFactu = l[2].Trim();
                                    Folder = l[8].Trim();
                                }
                                else
                                {
                                    NumCuenta = tipo.Equals("TC") || tipo.Equals("RAC") ? l[4].Trim() : l[2].Trim();
                                    FechaFactu = tipo.Equals("TC") || tipo.Equals("RAC") ? l[5] : l[6];
                                    Folder = tipo.Equals("TC") || tipo.Equals("RAC") ? l[8] : l[9];
                                }
                                var reporteAnualCosto = new ReporteAnualCostoDTO()
                                {
                                    Anio = int.Parse(FechaFactu.Substring(6, 4)),
                                    Certificado = getDocById(idCliente, FechaFactu, docID, tipo, Folder)
                                };
                                reportes.Add(reporteAnualCosto);
                            }
                        }
                        i++;
                    }
                }
            }
            return reportes;
        }

        public async Task<List<ReporteAnualCostoDTO>> GetRACByExtractos(string idCliente)
        {
            List<ReporteAnualCostoDTO> ReportesRac = new List<ReporteAnualCostoDTO>();
            JObject requestExtractos = new JObject();
            string initialDate = "{0}-01-01";
            string endDate = "{0}-01-01";
            string[] listFields = { "Nombre", "UCID", "Fecha_Factura", "URL", "IdDocumento", "CodigoProducto" };
            try
            {
                initialDate = string.Format(initialDate, DateTime.Now.AddYears(-2).Year.ToString());
                endDate = string.Format(endDate, DateTime.Now.Year.ToString());
                string url = _configuration["url_extractos_RAC"].ToString();
                requestExtractos.Add("documentId", idCliente);
                requestExtractos.Add("initialDate", initialDate);
                requestExtractos.Add("endDate", endDate);
                requestExtractos.Add("type", "20");
                requestExtractos.Add("listFields", JArray.FromObject(listFields));
                var res = await ConsumirApiRest.ConsumirApiSalidaJObject(url, requestExtractos);
                if (res["Codigo"].ToString().Equals("01"))
                {
                    List<Extracto> extracto = JsonConvert.DeserializeObject<List<Extracto>>(res["Data"].ToString());
                    foreach (var extractos in extracto)
                    {
                        foreach (var item in extractos.Detalle)
                        {
                            DateTime convertedDate = Convert.ToDateTime(item.Fecha_Factura);
                            ReporteAnualCostoDTO reporte = new ReporteAnualCostoDTO()
                            {
                                Anio = convertedDate.Year,
                                Certificado = GetPdfbase64ByUrl(item.URL)
                            };
                            ReportesRac.Add(reporte);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReportesRac = new List<ReporteAnualCostoDTO>();

            }
            return ReportesRac;
        }

        private string GetPdfbase64ByUrl(string pdfUrl)
        {
            string base64String = string.Empty;
            using (WebClient client = new WebClient())
            {
                var bytes = client.DownloadData(pdfUrl);
                base64String = Convert.ToBase64String(bytes);
            }

            return base64String;
        }
        private string getDocById(string id, string fecha, string docID, string tipo, string folder)
        {
            var token = GetToken();
            byte[] result;
            string extract64 = string.Empty;
            FelecFacturaSR.WsdlFacturaWebPortTypeClient felectDocument = new FelecFacturaSR.WsdlFacturaWebPortTypeClient();
            using (new OperationContextScope(felectDocument.InnerChannel))
            {
                HttpRequestMessageProperty requestMessage = new HttpRequestMessageProperty();
                requestMessage.Headers["token"] = token;
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = requestMessage;
                result = felectDocument.obtenerDocumentoId(id, fecha, fecha, docID, "", "pdf", id, "SERFINANSA", tipo, folder);
                extract64 = Convert.ToBase64String(result);

            }
            return extract64;
        }



        private string GetToken()
        {
            FelecSegToken.ComputecSTSDelegateClient felecToken = new FelecSegToken.ComputecSTSDelegateClient();
            var token = felecToken.obtenerToken("computec", "apache");
            return token;
        }
        private string TipoIdentificacion(string tipoId)
        {
            string tipoIdTexto = string.Empty;
            switch (tipoId)
            {
                case "1":
                    tipoIdTexto = "Cédula de Ciudadanía";
                    break;
                case "2":
                    tipoIdTexto = "Cédula de Extranjería";
                    break;
                case "3":
                    tipoIdTexto = "NIT";
                    break;
                case "4":
                    tipoIdTexto = "Tarjeta de Identidad";
                    break;
                case "5":
                    tipoIdTexto = "Pasaporte";
                    break;
                case "6":
                    tipoIdTexto = "Carnet Diplomático";
                    break;
                case "7":
                    tipoIdTexto = "Documento de Entidades internacionales";
                    break;
                case "8":
                    tipoIdTexto = "NUIP";
                    break;
                case "9":
                    tipoIdTexto = "Fideicomiso";
                    break;
            }
            return tipoIdTexto;
        }

        public async Task<byte[]> CertificadoPazYSalvo(string tipoId, string idCliente, string numObligacion)
        {
            byte[] pdf = new byte[0];
            string url = _configuration["CertificadoPazYSalvo"].ToString();
            JObject res = new JObject();
            JObject data = new JObject();
            data.Add("idCliente", idCliente);
            data.Add("tipoId", tipoId);
            data.Add("numObligacion", numObligacion);


            try
            {
                res = await ConsumirApiRest.ConsumirApiSalidaJObject(url, data);
                if (res["Codigo"].ToString().Equals("01"))
                {
                    pdf = Convert.FromBase64String(res["base64Report"].ToString());
                }

            }
            catch (Exception ex)
            {


            }

            return pdf;
        }

        public async Task<byte[]> CertificadoCDT(string tipoId, string idCliente)
        {
            byte[] pdf = new byte[0];
            string url = _configuration["CertificadoCdt"].ToString();
            JObject res = new JObject();
            JObject data = new JObject();
            data.Add("idCliente", idCliente);
            data.Add("tipoId", tipoId);

            try
            {
                res = await ConsumirApiRest.ConsumirApiSalidaJObject(url, data);
                if (res["Codigo"].ToString().Equals("01"))
                {
                    pdf = Convert.FromBase64String(res["pdfCdt"].ToString());
                }

            }
            catch (Exception ex)
            {


            }
            return pdf;
        }
    }
}