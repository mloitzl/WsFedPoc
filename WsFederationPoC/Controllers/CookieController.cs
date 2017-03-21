using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataHandler;
using WsFederationPoC.Models;

namespace WsFederationPoC.Controllers
{
    public class CookieController : Controller
    {
        // GET: Cookie
        public ActionResult Index()
        {
            var ticket = GetAuthenticationTicketFromCookie(HttpContext);

            ViewBag.Ticket = ticket;

            return View();
        }

        // https://long2know.com/2016/05/extracting-bearer-token-from-owin-cookie/
        public static AuthenticationTicket GetAuthenticationTicketFromCookie(HttpContextBase context)
        {
            var cookie = context.Request.Cookies.Get("WsFederationPoC");
            var ticket = cookie.Value;
            // Deal with URL encoding
            ticket = ticket.Replace('-', '+').Replace('_', '/');
            var padding = 3 - ((ticket.Length + 3) % 4);
            if (padding != 0) { ticket = ticket + new string('=', padding); }
            var secureDataFormat = new TicketDataFormat(new MachineKeyProtector("cookie"));
            return secureDataFormat.Unprotect(ticket);
        }
    }
}