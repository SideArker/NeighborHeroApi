using System.Web.Http;
namespace ApiEndPoints
{
    public static class WebApiConfig
    {

        public static void Config(HttpConfiguration config)
        {

            config.MapHttpAttributeRoutes();


            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
                );
        }

    }
}
