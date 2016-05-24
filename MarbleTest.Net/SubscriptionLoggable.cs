using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace MarbleTest.Net
{
    public class SubscriptionLoggable
    {
        public IList<SubscriptionLog> Subscriptions;
        public IScheduler Scheduler;

        public int LogSubscribedFrame() {
            this.Subscriptions.Add(new SubscriptionLog(0));
            return this.Subscriptions.Count - 1;
        }

        public void LogUnsubscribedFrame(int index) {
            var subscriptionLogs = this.Subscriptions;
            var oldSubscriptionLog = subscriptionLogs[index];
            subscriptionLogs[index] = new SubscriptionLog(
                oldSubscriptionLog.SubscribedFrame,
                0
            );
        }
    }
}
