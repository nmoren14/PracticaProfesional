using Newtonsoft.Json.Linq;
using NLog;
using System.Data.Entity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Dynamic;
using System.Text;
using BancaServices.Domain.Interfaces;
using BancaServices.Application.Services;

using BancaServices.Application.Services.SerfiUtils;
using BancaServices.Domain.Models;

namespace BancaServices.Application.Controllers
{
    //[Route("dev/ResumenProducto")]
    [CustomRoute("")]
    [ApiController]
    public class ProductoController : ControllerBase
    {
        private readonly IProductosService productosService;
        private readonly IASNetServices asNetServices;
        private readonly ICROService croService;
        private readonly NLog.ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IConsultasCms _consultasCms;
        private readonly IMemoryCache cache;
        private readonly BancaServicesLogsEntities Context;
        private readonly ICacheProvider _cacheProvider;

        public ProductoController(
            IProductosService productoService,
            BancaServicesLogsEntities dbContext,
            IASNetServices asNetServices,
            ICROService croService,
            IConsultasCms consultasCms,
            ICacheProvider cacheProvider
            )
        {
            productosService = productoService;
            this.asNetServices = asNetServices;
            this.croService = croService;
            cache = new MemoryCache(new MemoryCacheOptions());
            _consultasCms = consultasCms;
            Context = dbContext;
            _cacheProvider = cacheProvider;
        }

        [HttpPost("Productos")]
        [Produces("application/json")]
        public async Task<ActionResult<dynamic>> Productos([FromBody] ProductosRequest data)
        {
            try
            {
                var prueba = data;
                var listParams = new List<Parametros>();

                var consultas = _cacheProvider.Get<List<Parametros>>("PERMISO_CONSULTA");
                if (consultas == null)
                {
                    listParams = Context.Parametros.AsNoTracking().Where(a => a.Sistema == "RESUMEN_PRODUCTOS").ToList();
                    cache.Set("PERMISO_CONSULTA", listParams, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpiration = DateTime.Now.AddHours(1),
                        SlidingExpiration = TimeSpan.FromSeconds(3600) // 1 hora en segundos
                    });

                }
                else
                {

                    var actualizar = await Context.Parametros.AsNoTracking().Where(a => a.Sistema == "RESUMEN_PRODUCTOS" && a.DescripcionParametro.Equals("ActualizarParametrosResumen")).FirstOrDefaultAsync();

                    if (actualizar != null && actualizar.ValorParametro.Equals("1"))
                    {

                        listParams = Context.Parametros.AsNoTracking().Where(a => a.Sistema == "RESUMEN_PRODUCTOS").ToList();
                        cache.Set("PERMISO_CONSULTA", listParams, new MemoryCacheEntryOptions
                        {
                            AbsoluteExpiration = DateTime.Now.AddHours(1),
                            SlidingExpiration = TimeSpan.Zero // Reemplaza Cache.NoSlidingExpiration
                        });

                    }
                    else
                    {
                        listParams = consultas;
                    }

                }

                var ConsultaCDT = listParams.FirstOrDefault(a => a.DescripcionParametro == "CONSULTA_CDT");
                var ConsultaAhorros = listParams.FirstOrDefault(a => a.DescripcionParametro == "CONSULTA_AHORROS");
                var ConsultaCMS = listParams.FirstOrDefault(a => a.DescripcionParametro == "CONSULTA_CMS");
                var ConsultaPrestamos = listParams.FirstOrDefault(a => a.DescripcionParametro == "CONSULTA_PRESTAMOS");
                var ConsultaCorrientes = listParams.FirstOrDefault(a => a.DescripcionParametro == "CONSULTA_CORRIENTES");
                var ConsultaConvenioEfectivo = listParams.FirstOrDefault(a => a.DescripcionParametro == "CONSULTA_CONVENIO_EFECTIVO");

                var jsonResponse = new JObject();
                string tipoId = data.TipoId;
                string idCliente = data.IdCliente;
                string origen = data.Origen;
                JObject respuesta = new JObject();
                bool hasError = false;
                StringBuilder mensaje = null;

                Task<JArray> listaCdt = null;
                Task<JArray> listaCuentas = null;
                Task<JArray> listaCorriente = null;
                Task<JArray> listaCms = null;
                Task<JArray> listaPrestamos = null;
                Task<JArray> listaConveniosEfectivos = null;
                List<Task> tareas = new List<Task>();

                //RESUMEN DE PRODUCTOS
                var origenesTodos = "PQR,BPM".Split(',');
                bool muestraTodo = origenesTodos.Contains(origen);
                //Muestra todo si el origen del request se encuentra en listado

                if (bool.Parse(ConsultaCDT.ValorParametro))
                {
                    listaCdt = productosService.ConsultaCdt(idCliente);
                    tareas.Add(listaCdt);
                }

                if (bool.Parse(ConsultaAhorros.ValorParametro))
                {
                    listaCuentas = productosService.ConsultarSaldosCTAH(idCliente);
                    tareas.Add(listaCuentas);
                }

                if (bool.Parse(ConsultaCorrientes.ValorParametro))
                {
                    listaCorriente = productosService.ConsultaCuentaCorriente(tipoId, idCliente, Utils.RESUMEN);
                    tareas.Add(listaCorriente);
                }

                if (bool.Parse(ConsultaCMS.ValorParametro))
                {
                    listaCms = productosService.ConsultaCMS(tipoId, idCliente, muestraTodo);
                    tareas.Add(listaCms);
                }

                if (bool.Parse(ConsultaPrestamos.ValorParametro))
                {
                    listaPrestamos = productosService.ConsultarPrestamos(tipoId, idCliente);
                    tareas.Add(listaPrestamos);
                }

                if (bool.Parse(ConsultaConvenioEfectivo.ValorParametro))
                {
                    listaConveniosEfectivos = productosService.ConsultarConvenioEfectivo(tipoId, idCliente);
                    tareas.Add(listaConveniosEfectivos);
                }
                await Task.WhenAll(tareas);

                var listaProductos = new JArray();
                //Se valida que no haya ocurrido un error al consultar un producto 
                if (listaCdt == null || listaCms == null || listaCuentas == null || listaPrestamos == null || listaCorriente == null || listaConveniosEfectivos == null)
                {
                    mensaje = new StringBuilder();
                    mensaje.Append(listaCdt == null || listaCdt.Result == null ? string.Format("consulta de CDT-EIBS {0}", " - ") : string.Empty);
                    mensaje.Append(listaCms == null || listaCms.Result == null ? string.Format("consulta de Tarjetas-CMS {0}", " - ") : string.Empty);
                    mensaje.Append(listaCuentas == null || listaCuentas.Result == null ? string.Format("consulta de Cuentas de Ahorros-EIBS {0}", " - ") : string.Empty);
                    mensaje.Append(listaPrestamos == null || listaPrestamos.Result == null ? string.Format("consulta de Prestamos-EIBS {0}", " - ") : string.Empty);
                    mensaje.Append(listaCorriente == null || listaCorriente.Result == null ? string.Format("consulta de Cuentas Corrientes-EIBS {0}", " - ") : string.Empty);
                    mensaje.Append(listaConveniosEfectivos == null || listaConveniosEfectivos.Result == null ? string.Format("consulta de Convenios Efectivos {0}", " - ") : string.Empty);
                    hasError = true;
                }

                try
                {

                    if (listaCuentas != null && listaCuentas.Result != null && listaCuentas.Result.Count > 0)
                    {
                        listaProductos.Merge(listaCuentas.Result);
                    }

                    if (listaCdt != null && listaCdt.Result != null && listaCdt.Result.Count > 0)
                    {
                        listaProductos.Merge(listaCdt.Result);
                    }

                    if (listaCms != null && listaCms.Result != null && listaCms.Result.Count > 0)
                    {
                        listaProductos.Merge(listaCms.Result);
                    }

                    if (listaPrestamos != null && listaPrestamos.Result != null && listaPrestamos.Result.Count > 0)
                    {
                        listaProductos.Merge(listaPrestamos.Result);
                    }

                    if (listaCorriente != null && listaCorriente.Result != null && listaCorriente.Result.Count > 0)
                    {
                        listaProductos.Merge(listaCorriente.Result);
                    }

                    if (listaConveniosEfectivos != null && listaConveniosEfectivos.Result != null && listaConveniosEfectivos.Result.Count > 0)
                    {
                        listaProductos.Merge(listaConveniosEfectivos.Result);
                    }

                    if (listaProductos.Count > 0)
                    {
                        if (!ModelState.IsValid)
                        {
                            return BadRequest(new
                            {
                                codigo = "01",
                                descripcion = "Datos de entrada no válidos",
                                detalleError = ModelState.Values
                                    .SelectMany(v => v.Errors)
                                    .Select(e => e.ErrorMessage)
                            });
                        }
                        else
                        {
                            jsonResponse["codigo"] = "00";
                            jsonResponse["descripcion"] = "Transacción exitosa";
                        }

                        jsonResponse["productos"] = listaProductos; 
                    }
                    else
                    {
                        jsonResponse["codigo"] = "04";
                        jsonResponse["descripcion"] = "No se encontraron productos asosciados al cliente";
                    }
                }
                catch (Exception ex)
                {
                    respuesta.Add("codigo", "02");
                    respuesta.Add("descripcion", "Ha ocurrido un problema consultando los productos");
                    respuesta.Add("descripcionError", ex.ToString());

                }
                dynamic response = new ExpandoObject();
                response.codigo = jsonResponse["codigo"].ToString();
                response.descripcion = jsonResponse["descripcion"].ToString();
                response.productos = new JArray();

                foreach (var producto in listaProductos)
                {
                    response.productos.Add(producto);
                }
                var jsonR = JsonConvert.SerializeObject(response, Formatting.Indented);
                return new ContentResult
                {
                    Content = jsonR,
                    ContentType = "application/json",
                    StatusCode = 200
                };

            }
            catch (Exception ex)
            {
               
                var errorResponse = new
                {
                    codigo = "99",
                    descripcion = "Error interno del servidor",
                    descripcionError = ex.Message 
                };
                return StatusCode(500, JsonConvert.SerializeObject(errorResponse, Formatting.Indented));
            }
        }
    }
}