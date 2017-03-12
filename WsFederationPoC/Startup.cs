using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.WsFederation;
using Owin;

[assembly: OwinStartup(typeof(WsFederationPoC.Startup))]

namespace WsFederationPoC
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }

        private void ConfigureAuth(IAppBuilder app)
        {
            app.UseCookieAuthentication(
            new CookieAuthenticationOptions
            {
                AuthenticationType = CookieAuthenticationDefaults.AuthenticationType
            });
            app.UseWsFederationAuthentication(
            new WsFederationAuthenticationOptions
            {
                MetadataAddress = "https://sts.swd.corp/federationmetadata/2007-06/federationmetadata.xml",
                Wtrealm = "urn:wsfederationpoc",
                //Wreply = "https://localhost:44311/"
            });

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
        }
    }
}
