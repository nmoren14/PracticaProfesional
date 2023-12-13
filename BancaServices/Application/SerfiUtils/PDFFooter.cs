using iTextSharp.text;
using iTextSharp.text.pdf;

namespace BancaServices.Application.Services.SerfiUtils
{
    public class PDFFooter : PdfPageEventHelper
    {
        // write on top of document
        public override void OnOpenDocument(PdfWriter writer, Document document)
        {
            base.OnOpenDocument(writer, document);
            //LOGO
            string imageURL = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Images", "logoserfinansa.png");
            Image logo = Image.GetInstance(imageURL);
            logo.Alignment = Element.ALIGN_LEFT;
            logo.SetAbsolutePosition(0, 750);
            logo.ScalePercent(100, 100);
            document.Add(logo);
        }

        // write on start of each page
        public override void OnStartPage(PdfWriter writer, Document document)
        {
            base.OnStartPage(writer, document);
        }

        // write on end of each page
        public override void OnEndPage(PdfWriter writer, Document document)
        {
            base.OnEndPage(writer, document);

            var courier3 = FontFactory.GetFont(FontFactory.HELVETICA, 9, new BaseColor(169, 173, 173));
            string direccion = "Calle 72 # 54-35 Barranquilla. PBX: (5) 3091919.";
            Chunk chfooter3 = new Chunk(direccion, courier3);
            Phrase pfooter3 = new Phrase(chfooter3);
            Paragraph pfoot3 = new Paragraph();
            pfoot3.Alignment = Element.ALIGN_RIGHT;
            pfoot3.SpacingBefore = 80;
            pfoot3.Add(pfooter3);
            PdfPTable tabFot1 = new PdfPTable(new float[] { 1F });
            PdfPCell cell1;
            tabFot1.TotalWidth = 300F;
            cell1 = new PdfPCell(pfoot3);
            cell1.Border = Rectangle.NO_BORDER;
            cell1.HorizontalAlignment = Element.ALIGN_RIGHT;
            tabFot1.AddCell(cell1);
            tabFot1.WriteSelectedRows(0, -1, 270, document.Bottom, writer.DirectContent);


            var courier4 = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8);
            string url = "www.bancoserfinanza.com";
            Chunk chfooter4 = new Chunk(url, courier4);
            Phrase pfooter4 = new Phrase(chfooter4);
            Paragraph pfoot4 = new Paragraph();
            pfoot4.Alignment = Element.ALIGN_RIGHT;
            pfoot4.Add(pfooter4);
            pfoot4.SpacingBefore = 5;

            PdfPTable tabFot = new PdfPTable(new float[] { 1F });
            PdfPCell cell;
            tabFot.TotalWidth = 300F;
            cell = new PdfPCell(pfoot4);
            cell.Border = Rectangle.NO_BORDER;
            cell.HorizontalAlignment = Element.ALIGN_RIGHT;
            tabFot.AddCell(cell);
            tabFot.WriteSelectedRows(0, -1, 270, document.Bottom - 12, writer.DirectContent);

            string nit = "Nit 860043186-6";
            Chunk chfooter5 = new Chunk(nit, courier4);
            Phrase pfooter5 = new Phrase(chfooter5);
            Paragraph pfoot5 = new Paragraph();
            pfoot5.Alignment = Element.ALIGN_RIGHT;
            pfoot5.Add(pfooter5);
            pfoot5.SpacingBefore = 5;

            PdfPTable tabFot2 = new PdfPTable(new float[] { 1F });
            PdfPCell cell2;
            tabFot2.TotalWidth = 300F;
            cell2 = new PdfPCell(new Phrase(pfoot5));
            cell2.Border = Rectangle.NO_BORDER;
            cell2.HorizontalAlignment = Element.ALIGN_RIGHT;
            tabFot2.AddCell(cell2);
            tabFot2.WriteSelectedRows(0, -1, 270, document.Bottom - 20, writer.DirectContent);



        }

        //write on close of document
        public override void OnCloseDocument(PdfWriter writer, Document document)
        {
            base.OnCloseDocument(writer, document);
        }
    }
}