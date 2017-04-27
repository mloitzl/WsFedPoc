using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Utilities;
using WsFederationPoC.Hubs;
using WsFederationPoC.Models;

namespace WsFederationPoC.Api
{
    [RoutePrefix("api/sharepoint")]
    public class SharePointController : ApiController
    {
        [HttpGet]
        [Route("user/hostwebuser")]
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

        private static List<ListViewModel> LoadLists(ClientContext clientContext, bool omitHidden = false)
        {
            var lists = clientContext.Web.Lists;
            clientContext.Load(lists,
                ls =>
                    ls.Include(l => l.Title, l => l.RootFolder.ServerRelativeUrl, l => l.Id, l => l.Created,
                        l => l.ItemCount,
                        l => l.EventReceivers, l => l.Hidden));
            clientContext.ExecuteQuery();
            var result = new List<ListViewModel>();
            foreach (var list in lists)
                if (!(omitHidden && list.Hidden))
                    result.Add(new ListViewModel
                    {
                        Id = list.Id,
                        Url = new Uri(list.RootFolder.ServerRelativeUrl, UriKind.Relative),
                        Title = list.Title,
                        Created = list.Created,
                        ItemCount = list.ItemCount,
                        EventReceiversCount = list.EventReceivers.Count
                    });
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
        [Route("app/create/list")]
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
        [Route("app/delete/list")]
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
        [Route("app/attach/listeventreceiver")]
        public async Task<IHttpActionResult> AttachListEventReceiver([FromBody] ListViewModel value)
        {
            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext.Current);
            using (var clientContext = spContext.CreateAppOnlyClientContextForSPHost())
            {
                var remoteAppUrl =
                    $"{HttpContext.Current.Request.Url.Scheme}://{HttpContext.Current.Request.Url.DnsSafeHost}:{HttpContext.Current.Request.Url.Port}{HttpContext.Current.Request.ApplicationPath}";
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

        [HttpPost]
        [Route("app/attach/listeventreceiver")]
        public Task<SharePointResult> CreateListItem(string title, string listName, SharePointContext context)
        {
            return Task.Factory.StartNew(() =>
            {
                using (var clientContext = context.CreateAppOnlyClientContextForSPHost())
                {
                    var list = clientContext.Web.Lists.GetByTitle(listName);
                    var item = list.AddItem(new ListItemCreationInformation());
                    item["Title"] = title;
                    item.Update();
                    try
                    {
                        clientContext.ExecuteQuery();
                    }
                    catch (Exception ex)
                    {
                        //if (ex.Message.Contains("429"))
                        SharePointPerformanceHub.Instance.SuccessCountChanged(false);

                        return new SharePointResult(false)
                        {
                            Message = ex.Message,
                            Exception = ex
                        };
                    }
                }

                SharePointPerformanceHub.Instance.SuccessCountChanged(true);
                return new SharePointResult(true);
            }, TaskCreationOptions.LongRunning);
        }

        [HttpGet]
        [ResponseType(typeof(bool))]
        [Route("app/isinrole/{role=}")]
        public async Task<IHttpActionResult> IsMember([FromUri] RoleViewModel model)
        {
            var claimsId = HttpContext.Current.User.Identity as ClaimsIdentity;
            if (claimsId == null)
            {
                return NotFound();
            }

            var roleClaimsOfCurrentUser = TokenHelper.GetRoleClaims(claimsId);

            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext.Current);
            using (var clientContext = spContext.CreateAppOnlyClientContextForSPHost())
            {
                if (!clientContext.Web.GroupExists(model.Role)) return Ok(await Task.FromResult(false));

                var group = clientContext.Web.SiteGroups.GetByName(model.Role);
                clientContext.Load(@group, g => g.Users);
                clientContext.ExecuteQuery();

                foreach (var groupUser in @group.Users)
                {
                    if (groupUser.PrincipalType != PrincipalType.SecurityGroup) continue;

                    if (
                        roleClaimsOfCurrentUser.Any(
                            rc =>
                                rc.Item2.ToLowerInvariant() ==
                                ClaimsEncoding.Parse(groupUser.LoginName).ClaimValue.ToLowerInvariant()))
                    {
                        return Ok(await Task.FromResult(true));
                    }
                }
            }

            return Ok(await Task.FromResult(false));
        }

        [HttpGet]
        [ResponseType(typeof(IEnumerable<GroupMembersViewModel>))]
        [Route("app/rolemembers/{role=}")]
        public async Task<IHttpActionResult> GetRoleMember([FromUri] RoleViewModel model)
        {
            var result = new List<GroupMembersViewModel>();
            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext.Current);
            using (var clientContext = spContext.CreateAppOnlyClientContextForSPHost())
            {
                if (!clientContext.Web.GroupExists(model.Role)) return Ok(await Task.FromResult(result));

                var group = clientContext.Web.SiteGroups.GetByName(model.Role);
                clientContext.Load(@group, g => g.Users);
                clientContext.ExecuteQuery();

                result.Add(new GroupMembersViewModel
                {
                    RoleName = model,
                    Members = @group.Users.Select(m => ClaimsEncoding.Parse(m.LoginName)).ToList()
                });
            }

            return Ok(await Task.FromResult(result));
        }


        [HttpGet]
        [Route("app/load")]
        public async Task<IHttpActionResult> CreateLoad()
        {
            int workerThreads;
            int completionPortThreads;
            ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);

            Console.WriteLine($"workerThreads : {workerThreads} - completionPortThreads {completionPortThreads}");
            SharePointPerformanceHub.Instance.Reset();

            var sharePointContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext.Current);

            var tasks = new List<Task<SharePointResult>>();

            // for (var i = 0; i < 10000; i++)
            Parallel.ForEach(Enumerable.Range(0, 100),
                j =>
                    Parallel.ForEach(Enumerable.Range(0, 100),
                        i => tasks.Add(CreateListItem($"Item {j} - {i}", "LoadTest", sharePointContext))));

            //tasks.Add(CreateListItem($"Item {i}", "LoadTest",
            //        SharePointContextProvider.Current.GetSharePointContext(HttpContext.Current)));

            return Ok(await Task.WhenAll(tasks));
        }
    }


    public class SharePointResult
    {
        private bool _success;

        public SharePointResult(bool success)
        {
            _success = success;
        }

        public string Message { get; set; }
        public Exception Exception { get; set; }
    }
}


                                                                          