using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using ClaimTypes = System.IdentityModel.Claims.ClaimTypes;

namespace WsFederationPoC.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public ActionResult Echo()
        {
            return View();
        }
        // GET: Home
        public ActionResult Index()
        {

            //var clientContext = TokenHelper.GetS2SClientContextWithClaimsIdentity(sharepointUrl,
            //   Thread.CurrentPrincipal.Identity,
            //   TokenHelper.IdentityClaimType.SMTP, TokenHelper.ClaimProviderType.SAML, false);
            try
            {
                var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);
                using (var clientContext = spContext.CreateUserClientContextForSPHost())
                {
                    var web = clientContext.Web;
                    var user = clientContext.Web.CurrentUser;
                    clientContext.Load(web, w => w.Title);
                    clientContext.Load(user, u => u.Title, u => u.UserId);

                    clientContext.ExecuteQuery();
                    ViewBag.SharePointUser = user.Title;
                    ViewBag.SharePointUserNameId = user.UserId.NameId;
                    ViewBag.SharePointUserNameIdIssuer = user.UserId.NameIdIssuer;
                    ViewBag.SharePointWeb = web.Title;
                }
            }
            catch (Exception)
            {
                ViewBag.SharePointUser = "Error in ExecuteQuery";
                ViewBag.SharePointWeb = "Error in ExecuteQuery";
            }


            ViewBag.ClaimsIdentity = Thread.CurrentPrincipal.Identity;

            var claimsIdentity = Thread.CurrentPrincipal.Identity as ClaimsIdentity;
            ViewBag.DisplayName = claimsIdentity.Claims.First(c => c.Type == ClaimTypes.GivenName).Value;
            return View();
        }

        [AllowAnonymous]
        public ActionResult Setup()
        {
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

        public ActionResult Install()
        {
           
            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);
            using (var clientContext = spContext.CreateAppOnlyClientContextForSPHost())
            {
                var web = clientContext.Web;
                
                clientContext.Load(web, w => w.Title);
                clientContext.ExecuteQuery();
                ViewBag.SharePointWeb = web.Title;
                var remoteAppUrl = $"{Request.Url.Scheme}://{Request.Url.DnsSafeHost}:{Request.Url.Port}{Request.ApplicationPath}";
                clientContext.Site.AddJsBlock("WsFederationPocScriptLink",
                    $"{RegistrationScript}({JsonConvert.SerializeObject(new {remoteAppUrl})});", 1000);
            }

            return View("InstallResult");
        }

        private const string RegistrationScript = @"
			(function (siteInfo, jsFiles) {
                function init(){
                    console.log('iFrame loaded');
                    loadScript(siteInfo.remoteAppUrl + '/Scripts/jquery-3.1.1.min.js', function() {
    				    loadScript(siteInfo.remoteAppUrl + '/Scripts/app.js', function() {
                            siteInfo['SPHostUrl'] = _spPageContextInfo.siteAbsoluteUrl;
                            siteInfo['SPLanguage'] = _spPageContextInfo.currentUICultureName;
                            siteInfo['SPClientTag'] = _spPageContextInfo.siteClientTag;
                            siteInfo['SPProductNumber'] = 16;
                            WsFederationPoc.Page.init(siteInfo, jsFiles);
				        });				
				    });
                }

				var iFrame = document.createElement('iframe');
                iFrame.onload = function() {
                    init();
                };

                var body = document.getElementsByTagName('body')[0];
                body.appendChild(iFrame);
                
                iFrame.src = siteInfo.remoteAppUrl + 'Home/Echo/';
		
				window.WsFederationPoc = window.WsFederationPoc || {};
                window.WsFederationPoc._readyCallbackQueue = [];
			    window.WsFederationPoc.ready = function(callback) { 
                    WsFederationPoc._readyCallbackQueue.push(callback); 
                }

				function loadScript(url, init) {
					var head = document.getElementsByTagName('head')[0];
					var script = document.createElement('script');
					script.src = url;					

                    if (init) {
						var done = false;
						script.onload = script.onreadystatechange = function () {
							if (!done && (!this.readyState
										|| this.readyState == 'loaded'
										|| this.readyState == 'complete')) {
								done = true;
																			
								// Handle memory leak in IE
								script.onload = script.onreadystatechange = null;
								head.removeChild(script);
								
								init();
							}
						};

						if (typeof g_MinimalDownload != 'undefined' && g_MinimalDownload && (window.location.pathname.toLowerCase()).endsWith('/_layouts/15/start.aspx') && typeof asyncDeltaManager != 'undefined') {
							// Register script for MDS if possible
							RegisterModuleInit(url, init); //MDS registration
						}
					}
					
					head.appendChild(script);
				}
			})";
    }
}