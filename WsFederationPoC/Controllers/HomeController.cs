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
            //var sharepointUrl = new Uri(Request.QueryString["SPHostUrl"]);
            //var clientContext = TokenHelper.GetS2SClientContextWithClaimsIdentity(sharepointUrl,
            //   Thread.CurrentPrincipal,
            //   TokenHelper.IdentityClaimType.SMTP, TokenHelper.ClaimProviderType.SAML, false);

            //var web = clientContext.Web;
            //var user = clientContext.Web.CurrentUser;
            //clientContext.Load(web, w => w.Title);
            //clientContext.Load(user, u => u.Title);
            //clientContext.ExecuteQuery();

            ViewBag.ClaimsIdentity = Thread.CurrentPrincipal.Identity;
            //ViewBag.SharePointUser = user.Title;
            //ViewBag.SharePointWeb = web.Title;

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