using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.SessionState;
using WsFederationPoC.Models;

namespace WsFederationPoC
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static string WebApiUrlPrefixRelative => "~/api";

        protected void Application_BeginRequest()
        {
            if (!IsWebApiRequest())
            {
                return;
            }

            if (Request.HttpMethod != "OPTIONS")
            {
                return;
            }

            ProcessOriginHeader();
            Response.Headers.Add("Access-Control-Allow-Headers",
                Request.Headers.Get("Access-Control-Request-Headers") ?? "");
            Response.Headers.Add("Access-Control-Allow-Methods", "OPTIONS, GET, POST, PUT, DELETE");
            Response.Headers.Add("Access-Control-Allow-Credentials", "true");
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }

        protected void Application_PostAuthorizeRequest()
        {
            if (IsWebApiRequest())
            {
                HttpContext.Current.SetSessionStateBehavior(SessionStateBehavior.ReadOnly);
            }
        }

        private static bool IsWebApiRequest()
        {
            return HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath != null &&
                   HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath.StartsWith(WebApiUrlPrefixRelative);
        }

        /// <summary>
        ///     adds the Origin Header based on the white list
        /// </summary>
        private void ProcessOriginHeader()
        {
            var allowedOrigins = EnvironmentConfig.AllowedOriginUrls.ToList();

            Debug.WriteLine("Allowed Origins: \r\n");
            Trace.Write("Allowed Origins: \r\n");
            allowedOrigins?.ForEach(o =>
            {
                Debug.WriteLine("\t" + o);
                Trace.Write("\t" + o);
            });
            var originUrl = Request.Headers.Get("Origin");

            if (originUrl == null || allowedOrigins == null || !allowedOrigins.Contains(originUrl))
            {
                return;
            }

            Response.Headers.Remove("Access-Control-Allow-Origin");
            Response.Headers.Add("Access-Control-Allow-Origin", originUrl);
        }
    }
}

