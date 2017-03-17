using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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