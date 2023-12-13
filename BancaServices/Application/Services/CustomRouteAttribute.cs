using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;


namespace BancaServices.Application.Services
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class CustomRouteAttribute : RouteAttribute, IControllerModelConvention
    {
        public CustomRouteAttribute(string template) : base(template)
        {
        }

        public void Apply(ControllerModel controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            var prefix = configuration["appsettings:app_context_path"];
            //var prefix = "api/v1"; // Tu prefijo personalizado
            foreach (var selector in controller.Selectors)
            {
                selector.AttributeRouteModel.Template = $"{prefix}/{selector.AttributeRouteModel.Template}";
            }
        }
    }
}
