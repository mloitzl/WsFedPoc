using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using WsFederationPoC.Models;

namespace WsFederationPoC.Api
{
    [RoutePrefix("api/demo")]
    public class DemoController : ApiController
    {
        [HttpGet]
        [Route("all")]
        public async Task<IHttpActionResult> AllItems()
        {
            return Ok(await Task.FromResult(DataContext.GetItems()));
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IHttpActionResult> Item(int id)
        {
            return Ok(await Task.FromResult(DataContext.GetItems().Where(i => i.Id == id)));
        }
    }
}

