using System.Linq;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Cors;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.WsFederation;
using Newtonsoft.Json.Serialization;
using Owin;
using WsFederationPoC;
using WsFederationPoC.Models;

[assembly: OwinStartup(typeof(Startup))]

namespace WsFederationPoC
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            app.MapSignalR();
            ConfigureCors(app);
            ConfigureWebApi(app);
            ConfigureMvc(app);
        }

        private void ConfigureAuth(IAppBuilder app)
        {
            app.UseCookieAuthentication(
                new CookieAuthenticationOptions
                {
                    AuthenticationType = CookieAuthenticationDefaults.AuthenticationType,
                    CookieName = "WsFederationPoC"
                });

            //app.UseWsFederationAuthentication(
            //new WsFederationAuthenticationOptions
            //{
            //    MetadataAddress = "https://sts.swd.corp/federationmetadata/2007-06/federationmetadata.xml",
            //    Wtrealm = "urn:wsfederationpoc"
            //    //Wreply = "https://localhost:44311/"
            //});

            app.UseWsFederationAuthentication(
                new WsFederationAuthenticationOptions
                {
                    MetadataAddress = "https://sts.acme.lab/federationmetadata/2007-06/federationmetadata.xml",
                    Wtrealm = "urn:wsfederationpoc",
                    Wreply = WebConfigurationManager.AppSettings.Get("RedirectUri")
                });


            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
        }

        private static void ConfigureMvc(IAppBuilder app)
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        private static void ConfigureCors(IAppBuilder app)
        {
            var corsPolicy = new CorsPolicy
            {
                AllowAnyHeader = true,
                SupportsCredentials = true,
                Methods =
                {
                    "OPTIONS",
                    "GET",
                    "POST",
                    "PUT",
                    "DELETE"
                }
            };
            EnvironmentConfig.AllowedOriginUrls.ToList().ForEach(item => corsPolicy.Origins.Add(item));

            var corsOptions = new CorsOptions
            {
                PolicyProvider = new CorsPolicyProvider
                {
                    PolicyResolver = context => Task.FromResult(corsPolicy)
                }
            };

            app.UseCors(corsOptions);
        }

        private void ConfigureWebApi(IAppBuilder app)
        {
            GlobalConfiguration.Configure(RegisterWebApiConfiguration);
        }

        private static void RegisterWebApiConfiguration(HttpConfiguration config)
        {
            // Web API configuration and services
            GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings.ContractResolver =
                new CamelCasePropertyNamesContractResolver();

            // Web API routes
            config.MapHttpAttributeRoutes();
        }
    }
}