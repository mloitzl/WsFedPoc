using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Microsoft.SharePoint.Client;
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
            var cookies = HttpContext.Current.Request.Cookies;
            var id = HttpContext.Current.User.Identity;
            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext.Current);
            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                var user = clientContext.Web.CurrentUser;
                clientContext.Load(user, u => u.Title, u => u.UserId);
                clientContext.ExecuteQuery();
                return Ok(await Task.FromResult(user.Title));
            }
        }

        [HttpGet]
        [Route("user/lists")]
        public async Task<IHttpActionResult> GetUserLists()
        {
            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext.Current);
            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                var result = LoadLists(clientContext, true);
                return Ok(await Task.FromResult(result));
            }
        }

        private static List<ListViewModel> LoadLists(ClientContext clientContext, bool hideHidden = false)
        {
            var lists = clientContext.Web.Lists;
            clientContext.Load(lists,
                ls =>
                    ls.Include(l => l.Title, l => l.RootFolder.ServerRelativeUrl, l => l.Id, l => l.Created, l => l.ItemCount,
                        l => l.EventReceivers, l => l.Hidden));
            clientContext.ExecuteQuery();
            var result = new List<ListViewModel>();
            foreach (List list in lists)
            {
                if (!(hideHidden && list.Hidden))
                {
                    result.Add(new ListViewModel
                    {
                        Id = list.Id,
                        Url = new Uri(list.RootFolder.ServerRelativeUrl, UriKind.Relative),
                        Title = list.Title,
                        Created = list.Created,
                        ItemCount = list.ItemCount,
                        EventReceiversCount = list.EventReceivers.Count
                    });
                }
            }
            return result;
        }


        [HttpGet]
        [Route("app/lists")]
        public async Task<IHttpActionResult> GetAppLists()
        {
            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext.Current);
            using (var clientContext = spContext.CreateAppOnlyClientContextForSPHost())
            {
                var result = LoadLists(clientContext);
                return Ok(await Task.FromResult(result));
            }
        }

        [HttpPost]
        [Route("user/create/listitem")]
        public async Task<IHttpActionResult> CreateListItem([FromBody] ListItemViewModel value, Guid listId)
        {
            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext.Current);
            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                var listItem = clientContext.Web.Lists.GetById(listId).AddItem(new ListItemCreationInformation());
                listItem["Title"] = value.Title;
                listItem.Update();
                clientContext.Load(listItem);
                clientContext.ExecuteQuery();

                return Ok(await Task.FromResult(value.Title));
            }
        }

        [HttpPost]
        [Route("create/list")]
        public async Task<IHttpActionResult> CreateList([FromBody] ListViewModel value)
        {
            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext.Current);
            using (var clientContext = spContext.CreateAppOnlyClientContextForSPHost())
            {
                var list = clientContext.Web.Lists.Add(new ListCreationInformation
                {
                    Title = value.Title,
                    TemplateType = 100,
                    QuickLaunchOption = QuickLaunchOptions.On
                });
                clientContext.Load(list);
                clientContext.ExecuteQuery();

                return Ok(await Task.FromResult(list.Title));
            }
        }

        [HttpPost]
        [Route("delete/list")]
        public async Task<IHttpActionResult> DeleteList([FromBody] ListViewModel value)
        {
            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext.Current);
            using (var clientContext = spContext.CreateAppOnlyClientContextForSPHost())
            {
                var list = clientContext.Web.Lists.GetById(value.Id);
                list.DeleteObject();
                clientContext.ExecuteQuery();

                return Ok(await Task.FromResult($"deleted {value.Id}"));
            }
        }
        [HttpPost]
        [Route("attach/listeventreceiver")]
        public async Task<IHttpActionResult> AttachListEventReceiver([FromBody] ListViewModel value)
        {
            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext.Current);
            using (var clientContext = spContext.CreateAppOnlyClientContextForSPHost())
            {
                var remoteAppUrl = $"{HttpContext.Current.Request.Url.Scheme}://{HttpContext.Current.Request.Url.DnsSafeHost}:{HttpContext.Current.Request.Url.Port}{HttpContext.Current.Request.ApplicationPath}";
                var list = clientContext.Web.Lists.GetById(value.Id);
                list.EventReceivers.Add(new EventReceiverDefinitionCreationInformation
                {
                    EventType = EventReceiverType.ItemAdded,
                    SequenceNumber = 1000,
                    ReceiverUrl = remoteAppUrl + "Services/EventReceiver.svc",
                    ReceiverName = "WsFedPoc",
                    Synchronization = EventReceiverSynchronization.DefaultSynchronization
                });

                clientContext.Load(list);

                clientContext.ExecuteQuery();

                return Ok(await Task.FromResult($"deleted {value.Id}"));
            }
        }


    }
}

