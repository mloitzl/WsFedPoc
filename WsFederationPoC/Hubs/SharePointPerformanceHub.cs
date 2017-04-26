using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace WsFederationPoC.Hubs
{
    [HubName("SharePointPerformance")]
    public class SharePointPerformanceHub: Hub
    {
        private static int _viewerCount;
        private static int _successCount = 0;
        private static int _failureCount = 0;
        private static SharePointPerformanceHub _hub;

        public static SharePointPerformanceHub Instance => _hub;

        public SharePointPerformanceHub()
        {
            _hub = this;
        }

        public void Reset()
        {
            _successCount = 0;
            _failureCount = 0;
        }

        public void ViewerCountChanged(int viewerCount)
        {
            Clients.All.viewerCountChanged(viewerCount);
        }

        public void SuccessCountChanged(bool success)
        {
            if(success)
                Interlocked.Increment(ref _successCount);
            else
                Interlocked.Increment(ref _failureCount);

            Clients.All.successRatio(_successCount, _failureCount);
        }

        public override Task OnConnected()
        {
            Interlocked.Increment(ref _viewerCount);
            ViewerCountChanged(_viewerCount);

            return base.OnConnected();
        }
    }
}