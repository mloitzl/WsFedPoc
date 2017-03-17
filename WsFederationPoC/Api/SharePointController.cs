using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using WsFederationPoC.Models;

namespace WsFederationPoC.Api
{
    [RoutePrefix("api/sharepoint")]
    public class SharePointController : ApiController
    {
        [HttpGet]
        [Route("hostwebuser")]
        public async Task<IHttpActionResult> GetUser()
        {
            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext.Current);
            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                var user = clientContext.Web.CurrentUser;
                clientContext.Load(user, u => u.Title, u => u.UserId);
                clientContext.ExecuteQuery();
                return Ok(await Task.FromResult(user.Title));
            }
        }
    }
}

