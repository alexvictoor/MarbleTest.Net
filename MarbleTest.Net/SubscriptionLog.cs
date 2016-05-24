using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarbleTest.Net
{
    public class SubscriptionLog
    {
        public long SubscribedFrame { get; set; }
        public long UnsubscribedFrame { get; set; }

        public SubscriptionLog(long subscribedFrame)
        {
            SubscribedFrame = subscribedFrame;
            UnsubscribedFrame = long.MaxValue;
        }

        public SubscriptionLog(long subscribedFrame, long unsubscribedFrame)
        {
            SubscribedFrame = subscribedFrame;
            UnsubscribedFrame = unsubscribedFrame;
        }
    }
}
