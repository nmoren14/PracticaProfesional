using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using BancaServices.Domain.Interfaces;
using BancaServices.Domain.Interfaces.ConsignarTransferir;
using BancaServices.Application.Services;
using BancaServices.Application.Services.ConsignarTransferir;

namespace BancaServices
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddHttpClient();
            services.AddControllers();
            services.AddMemoryCache();
            services.AddDbContext<BancaServicesLogsEntities>(options =>
            options.UseSqlServer(Configuration.GetConnectionString("BancaServicesLogsEntities")));
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "ResumenProducto",
                    Version = "v1",
                    Description = ""
                });
            });
            services.AddScoped<IActivacionServices, ActivacionServices>();
            services.AddScoped<IAgilService, AgilServices>();
            services.AddScoped<IASNetServices, ASNetServices>();
            services.AddScoped<IAvanceService>(provider =>
                    new AvanceService(
                        provider.GetRequiredService<IClienteServices>(),
                        provider.GetRequiredService<INotificacionesServices>(),
                        provider.GetRequiredService<IProductosService>(),
                        provider.GetRequiredService<IConsigarTransferir>(),
                        provider.GetRequiredService<IBloqueoServices>()
                    ));
            services.AddScoped<IBloqueoServices, BloqueoServices>();
            services.AddScoped<ICampaignServices, CampaignServices>();
            services.AddScoped<ICashServices, CashServices>();
            services.AddScoped<IClienteServices, ClienteServices>();
            services.AddScoped<IConsultasCms, ConsultaCmsServices>();
            services.AddScoped<ICROService>(provider =>
        new CROService(
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IASNetServices>(),
            provider.GetRequiredService<IAgilService>(),
            provider.GetRequiredService<IConsigarTransferir>(),
            provider.GetRequiredService<IConsultasCms>(),
            provider.GetRequiredService<IAvanceService>(),
            provider.GetRequiredService<INotificacionesServices>(),
            provider.GetRequiredService<IClienteServices>(),
            provider.GetRequiredService<IProductosService>(),
            provider.GetRequiredService<IBloqueoServices>()
        ));
            services.AddScoped<INotificacionesServices, NotificacionesServices>();
            services.AddScoped<IPagosServices, PagosServices>();
            services.AddScoped<IProductosService, ProductosService>();
            services.AddScoped<IPseServices, PseServices>();
            services.AddScoped<IRediferirServices, RediferirServices>();
            services.AddScoped<IRediferirServicesV2, RediferirServicesV2>();
            services.AddScoped<IRenovacionTCServices, RenovacionTCServices>();
            services.AddScoped<ITarjetaVirtualService, TarjetaVirtualService>();
            services.AddScoped<ITransferenciasService, TransferenciasService>();
            services.AddScoped<IConsigarTransferir, ConsignarTransferir>();
            services.AddSingleton<ICacheProvider, CacheProvider>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();            
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            string appContextPath = Configuration.GetSection("appsettings:app_context_path").Value;
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger(c =>
                {
                    c.SerializeAsV2 = true;
                });
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ResumenProducto V1");
                });
            }
            else {
                app.UseSwagger(c =>
                {
                    c.SerializeAsV2 = true;
                    c.RouteTemplate = appContextPath + "/swagger/{documentName}/ResumenProducto.json";


                });
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/" + appContextPath + "/swagger/v1/ResumenProducto.json", "ResumenProducto V1");
                    c.RoutePrefix = $"{appContextPath}";
                });
            }
            app.UseHttpsRedirection();            
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: $"{appContextPath}/[controller]/{{action?}}/{{id?}}");
                endpoints.MapControllers();
            });
        }
    }
}