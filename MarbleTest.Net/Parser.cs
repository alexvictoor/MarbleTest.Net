using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;

namespace MarbleTest.Net
{
    public class Parser
    {


        public static SubscriptionLog ParseMarblesAsSubscriptions(string marbles, int frameTimeFactor = 10)
        {

            var len = marbles.Length;
            var groupStart = -1;
            var subscriptionFrame = int.MaxValue;
            var unsubscriptionFrame = int.MaxValue;

            for (var i = 0; i < len; i++)
            {
                var frame = i * frameTimeFactor;
                var c = marbles[i];
                switch (c)
                {
                    case '-':
                    case ' ':
                        break;
                    case '(':
                        groupStart = frame;
                        break;
                    case ')':
                        groupStart = -1;
                        break;
                    case '^':
                        if (subscriptionFrame != int.MaxValue)
                        {
                            throw new Exception("Found a second subscription point \'^\' in a " +
                              "subscription marble diagram. There can only be one.");
                        }
                        subscriptionFrame = groupStart > -1 ? groupStart : frame;
                        break;
                    case '!':
                        if (unsubscriptionFrame != int.MaxValue)
                        {
                            throw new Exception("Found a second subscription point \'^\' in a " +
                              "subscription marble diagram. There can only be one.");
                        }
                        unsubscriptionFrame = groupStart > -1 ? groupStart : frame;
                        break;
                    default:
                        throw new Exception("There can only be \'^\' and \'!\' markers in a " +
                          "subscription marble diagram. Found instead \'' + c + '\'.");
                }
            }

            if (unsubscriptionFrame < 0)
            {
                return new SubscriptionLog(subscriptionFrame);
            }
            else
            {
                return new SubscriptionLog(subscriptionFrame, unsubscriptionFrame);
            }
        }

        public static IList<Recorded<Notification<T>>> ParseMarbles<T>(string marbles,
            object values = null,
            Exception errorValue = null,
            int frameTimeFactor = 10,
            bool materializeInnerObservables = false)
        {
            if (marbles.IndexOf('!') != -1)
            {
                throw new Exception("Conventional marble diagrams cannot have the unsubscription marker '!'");
            }

            int len = marbles.Count();
            IList<Recorded<Notification<T>>> testMessages = new List<Recorded<Notification<T>>>();
            int subIndex = marbles.IndexOf('^');
            int frameOffset = subIndex == -1 ? 0 : (subIndex * -frameTimeFactor);

            long groupStart = -1;

            for (var i = 0; i < len; i++)
            {
                long frame = i * frameTimeFactor + frameOffset;
                Notification<T> notification = null;
                var c = marbles[i];
                switch (c)
                {
                    case '-':
                    case ' ':
                        break;
                    case '(':
                        groupStart = frame;
                        break;
                    case ')':
                        groupStart = -1;
                        break;
                    case '|':
                        notification = Notification.CreateOnCompleted<T>();
                        break;
                    case '^':
                        break;
                    case '#':
                        notification = Notification.CreateOnError<T>(errorValue);
                        break;
                    default:
                        T value;
                        if (values == null)
                        {
                            value = (T)(object)c.ToString();
                        }
                        else
                        {
                            value = GetProperty<T>(values, c.ToString());
                        }
                        notification = Notification.CreateOnNext(value);
                        break;
                }

                if (notification != null)
                {
                    var messageFrame = groupStart > -1 ? groupStart : frame;
                    testMessages.Add(new Recorded<Notification<T>>(messageFrame, notification));
                }
            }
            return testMessages;
        }

        public static T GetProperty<T>(object o, string propName)
        {
            Type t = o.GetType();
            PropertyInfo p = t.GetProperty(propName);
            object v = p.GetValue(o);
            return (T)v;
        }
    }
}
