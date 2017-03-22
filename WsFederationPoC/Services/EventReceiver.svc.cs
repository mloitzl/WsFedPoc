using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;

namespace WsFederationPoC.Services
{
    public class EventReceiver : IRemoteEventService
    {
        public SPRemoteEventResult ProcessEvent(SPRemoteEventProperties properties)
        {
            return new SPRemoteEventResult
            {
                Status = SPRemoteEventServiceStatus.Continue
            };
        }

        public void ProcessOneWayEvent(SPRemoteEventProperties properties)
        {
            using (var ctx = TokenHelper.CreateRemoteEventReceiverClientContext(properties))
            {
                var list = ctx.Web.Lists.GetByTitle("EventReceiverLog");
                var item = list.AddItem(new ListItemCreationInformation());
                item["Title"] =
                    $"User {properties.ItemEventProperties.UserDisplayName} triggered an {properties.EventType} Event on List {properties.ItemEventProperties.ListTitle}";
                item.Update();
                ctx.Load(item);
                ctx.ExecuteQuery();
            }
        }
    }
}