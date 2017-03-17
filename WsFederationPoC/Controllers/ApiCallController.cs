using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WsFederationPoC.Controllers
{
    [Authorize]
    public class ApiCallController : Controller
    {
        // GET: ApiCall
        public ActionResult Index()
        {
            ViewBag.RemoteAppUrl  = $"{Request.Url.Scheme}://{Request.Url.DnsSafeHost}:{Request.Url.Port}{Request.ApplicationPath}";
            return View();
        }
    }
}