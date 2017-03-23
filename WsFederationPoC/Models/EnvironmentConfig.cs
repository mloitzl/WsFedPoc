using System.Web.Configuration;

namespace WsFederationPoC.Models
{
    public class EnvironmentConfig
    {
        private static string[] _allowedOriginUrls;

        public static string[] AllowedOriginUrls
            =>
                _allowedOriginUrls ??
                (_allowedOriginUrls = WebConfigurationManager.AppSettings.Get("AllowedOriginUrls").Split(';'));
    }
}