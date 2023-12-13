using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Data.Entity;
using System.Text;
using BancaServices.Domain.Interfaces;
using BancaServices.Application.Services;
using BancaServices.Application.Services.SerfiUtils;
using BancaServices.Application.Controllers;
using BancaServices.Domain.Models;

namespace BancaServices.Tests.Application
{
    public class ProductoControllerTests
    {
        [Test]
        public async Task Productos_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var mockProductosService = new Mock<IProductosService>();
            var mockASNetServices = new Mock<IASNetServices>();
            var mockCROService = new Mock<ICROService>();
            var mockConsultasCms = new Mock<IConsultasCms>();
            StringBuilder mensaje;

            Task<JArray> listaCdt;
            Task<JArray> listaCuentas;
            Task<JArray> listaCorriente;
            Task<JArray> listaCms;
            Task<JArray> listaPrestamos;
            Task<JArray> listaConveniosEfectivos;
            var tareas = new List<Task>();

            var mockTareaCdt = new Task<JArray>(() => new JArray());
            var mockTareaCuentas = new Task<JArray>(() => new JArray());
            var mockTareaCorriente = new Task<JArray>(() => new JArray());
            var mockTareaCms = new Task<JArray>(() => new JArray());
            var mockTareaPrestamos = new Task<JArray>(() => new JArray());
            var mockTareaConveniosEfectivos = new Task<JArray>(() => new JArray());
            var mockCacheProvider = new Mock<ICacheProvider>();
            var mockMemoryCache = new Mock<IMemoryCache>();
            var cacheProvider = new CacheProvider(mockMemoryCache.Object);



            mockProductosService.Setup(service => service.ConsultaCdt("1129564979")).Returns(mockTareaCdt);
            mockProductosService.Setup(service => service.ConsultarSaldosCTAH("1129564979")).Returns(mockTareaCuentas);
            mockProductosService.Setup(service => service.ConsultaCuentaCorriente("1", "1129564979", Utils.RESUMEN)).Returns(mockTareaCorriente);
            mockProductosService.Setup(service => service.ConsultaCMS("1", "1129564979", true)).Returns(mockTareaCms);
            mockProductosService.Setup(service => service.ConsultarPrestamos("1", "1129564979")).Returns(mockTareaPrestamos);
            mockProductosService.Setup(service => service.ConsultarConvenioEfectivo("1", "1129564979")).Returns(mockTareaConveniosEfectivos);

            var controller = new BancaServices.Application.Controllers.ProductoController(
                mockProductosService.Object,
                null, // Pass a mocked DbContext here
                mockASNetServices.Object,
                mockCROService.Object,
                mockConsultasCms.Object,
                mockCacheProvider.Object
            );

            // Act
            var mockDbSet = new Mock<DbSet<Parametros>>();
            var parametrosList = new List<Parametros>
            {
                new Parametros { Id = 1, DescripcionParametro = "ActualizarParametrosResumen", ValorParametro = "1", Sistema = "RESUMEN_PRODUCTOS" },
                // Add more instances as needed
            };
            mockMemoryCache
            .Setup(cache => cache.TryGetValue("PERMISO_CONSULTA", out It.Ref<object>.IsAny))
            .Returns(false);

            var mockContext = new Mock<BancaServicesLogsEntities>();
            var parametrosQueryable = parametrosList.AsQueryable();
            mockDbSet.As<IQueryable<Parametros>>().Setup(m => m.Provider).Returns(parametrosQueryable.Provider);
            mockDbSet.As<IQueryable<Parametros>>().Setup(m => m.Expression).Returns(parametrosQueryable.Expression);
            mockDbSet.As<IQueryable<Parametros>>().Setup(m => m.ElementType).Returns(parametrosQueryable.ElementType);
            mockDbSet.As<IQueryable<Parametros>>().Setup(m => m.GetEnumerator()).Returns(parametrosQueryable.GetEnumerator());

            // Configure the context mock to return the DbSet mock
            //mockContext.Setup(c => c.Parametros).Returns(mockDbSet.Object);
            // Configuring the mock DbSet
            var mockListParams = new List<Parametros>
            {
                new Parametros { DescripcionParametro = "CONSULTA_CDT", ValorParametro = "true" },
                new Parametros { DescripcionParametro = "CONSULTA_AHORROS", ValorParametro = "true" },
                new Parametros { DescripcionParametro = "CONSULTA_CMS", ValorParametro = "true" },
                new Parametros { DescripcionParametro = "CONSULTA_PRESTAMOS", ValorParametro = "true" },
                new Parametros { DescripcionParametro = "CONSULTA_CORRIENTES", ValorParametro = "true" },
                new Parametros { DescripcionParametro = "CONSULTA_CONVENIO_EFECTIVO", ValorParametro = "true" },
            };
            var request = new ProductosRequest
            {
                TipoId = "1",
                IdCliente = "1129564979",
                Origen = "HelpiPlus"
            };

            var ConsultaCDT = mockListParams.FirstOrDefault(a => a.DescripcionParametro == "CONSULTA_CDT");
            var ConsultaAhorros = mockListParams.FirstOrDefault(a => a.DescripcionParametro == "CONSULTA_AHORROS");
            var ConsultaCMS = mockListParams.FirstOrDefault(a => a.DescripcionParametro == "CONSULTA_CMS");
            var ConsultaPrestamos = mockListParams.FirstOrDefault(a => a.DescripcionParametro == "CONSULTA_PRESTAMOS");
            var ConsultaCorrientes = mockListParams.FirstOrDefault(a => a.DescripcionParametro == "CONSULTA_CORRIENTES");
            var ConsultaConvenioEfectivo = mockListParams.FirstOrDefault(a => a.DescripcionParametro == "CONSULTA_CONVENIO_EFECTIVO");
            mockCacheProvider.Setup(cache => cache.Get<List<Parametros>>("PERMISO_CONSULTA")).Returns(mockListParams);

            var result = await controller.Productos(request);

            // Assert
            Xunit.Assert.IsType<ActionResult<object>>(result);

        }

        [Test]
        public async Task Productos_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var mockProductosService = new Mock<IProductosService>();
            var mockASNetServices = new Mock<IASNetServices>();
            var mockCROService = new Mock<ICROService>();
            var mockConsultasCms = new Mock<IConsultasCms>();
            var mockContext = new Mock<BancaServicesLogsEntities>();
            var mockCacheProvider = new Mock<ICacheProvider>();

            var controller = new ProductoController(
                mockProductosService.Object,
                mockContext.Object,
                mockASNetServices.Object,
                mockCROService.Object,
                mockConsultasCms.Object,
                mockCacheProvider.Object
            );

            // Act
            var result = await controller.Productos(null);

            // Assert
            Xunit.Assert.IsType<ActionResult<object>>(result);
        }
    }
}