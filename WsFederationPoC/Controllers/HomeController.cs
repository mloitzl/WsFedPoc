using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;
using ClaimTypes = System.IdentityModel.Claims.ClaimTypes;

namespace WsFederationPoC.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            ViewBag.ClaimsIdentity = Thread.CurrentPrincipal.Identity;
            var claimsIdentity = Thread.CurrentPrincipal.Identity as ClaimsIdentity;
            ViewBag.DisplayName = claimsIdentity.Claims.First(c => c.Type == ClaimTypes.GivenName).Value;
            return View();
        }

        public ActionResult LogOff()
        {
            if (User.Identity.IsAuthenticated)
            {
                var owinContext = Request.GetOwinContext();
                var authProperties = new AuthenticationProperties
                {
                    RedirectUri = new Uri(HttpContext.Request.Url,
                        new UrlHelper(ControllerContext.RequestContext).Action("PostLogOff")).AbsoluteUri
                };
                owinContext.Authentication.SignOut(authProperties);
                return View();
            }
            throw new InvalidOperationException("User is not authenticated");
        }

        [AllowAnonymous]
        public ActionResult PostLogOff()
        {
            return View();
        }
    }
}