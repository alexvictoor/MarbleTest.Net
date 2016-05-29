using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;

namespace MarbleTest.Net
{
    public static class ReflectionHelper
    {

        public static T GetProperty<T>(object o, string propName)
        {
            Type t = o.GetType();
            PropertyInfo p = t.GetProperty(propName);
            object v = p.GetValue(o);
            return (T)v;
        }


        public static bool IsTestableObservable(object value)
        {
            bool result = false;
            try
            {
                GetProperty<object>(value, "Messages");
                result = true;
            }
            catch
            {

            }
            return result;
        }

        public static IList<Recorded<Notification<object>>> RetrieveNotificationsFromTestableObservable(
            object testableObservable)
        {
            var messages = GetProperty<IEnumerable>(testableObservable, "Messages");
            return CastNotificationsFromTestableObservable(messages);
        }

        // Not very proud of this piece of code... if you find a smarter way of transforming 
        // a IList<Recorded<Notification<T>>> to a IList<Recorded<Notification<object>>>
        // please share it and send a PR :)
        public static IList<Recorded<Notification<object>>> CastNotificationsFromTestableObservable(IEnumerable messages)
        {
            var result = new List<Recorded<Notification<object>>>();
            foreach (var message in messages)
            {
                var time = GetProperty<long>(message, "Time");
                var rawNotification = GetProperty<object>(message, "Value");
                var kind = GetProperty<NotificationKind>(rawNotification, "Kind");
                Notification<object> notification;
                switch (kind)
                {
                    case NotificationKind.OnNext:
                        var value = GetProperty<object>(rawNotification, "Value");
                        notification = Notification.CreateOnNext(value);
                        break;
                    case NotificationKind.OnError:
                        var exception = GetProperty<object>(rawNotification, "Exception");
                        notification = Notification.CreateOnError<object>((Exception)exception);
                        break;
                    default:
                        notification = Notification.CreateOnCompleted<object>();
                        break;
                }
                var record = new Recorded<Notification<object>>((long) time, notification);
                result.Add(record);
            }

            return result;
        }
    }
}
