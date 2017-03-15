using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.WsFederation;
using Owin;
using WsFederationPoC;

[assembly: OwinStartup(typeof(Startup))]

namespace WsFederationPoC
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            ConfigureMvc(app);
        }

        private void ConfigureAuth(IAppBuilder app)
        {
            app.UseCookieAuthentication(
                new CookieAuthenticationOptions
                {
                    AuthenticationType = CookieAuthenticationDefaults.AuthenticationType
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
                Wtrealm = "urn:wsfederationpoc"
            });


            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
        }

        private static void ConfigureMvc(IAppBuilder app)
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }
}