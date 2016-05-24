using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;
using NFluent;
using NUnit.Framework;

namespace MarbleTest.Net.Test
{
    public class ParserTest
    {


        [Test]
        public void Should_parse_a_marble_string_into_a_series_of_notifications_and_types()
        {
            var result = Parser.ParseMarbles<string>("-------a---b---|", new { a = "A", b = "B" });
            
            Check.That(result).ContainsExactly(new object[]
            {
                new Recorded<Notification<string>>(70, Notification.CreateOnNext("A")),
                new Recorded<Notification<string>>(110, Notification.CreateOnNext("B")),
                new Recorded<Notification<string>>(150, Notification.CreateOnCompleted<string>())
            });
        }

        [Test]
        public void Should_parse_a_marble_string_allowing_spaces_too()
        {
            var result = Parser.ParseMarbles<string>("--a--b--|   ", new { a = "A", b = "B" });

            Check.That(result).ContainsExactly(new object[]
            {
                new Recorded<Notification<string>>(20, Notification.CreateOnNext("A")),
                new Recorded<Notification<string>>(50, Notification.CreateOnNext("B")),
                new Recorded<Notification<string>>(80, Notification.CreateOnCompleted<string>())
            });
        }

        [Test]
        public void Should_parse_a_marble_string_with_a_subscription_point()
        {
            var result = Parser.ParseMarbles<string>("---^---a---b---|", new { a = "A", b = "B" });

            Check.That(result).ContainsExactly(new object[]
            {
                new Recorded<Notification<string>>(40, Notification.CreateOnNext("A")),
                new Recorded<Notification<string>>(80, Notification.CreateOnNext("B")),
                new Recorded<Notification<string>>(120, Notification.CreateOnCompleted<string>())
            });
        }

        [Test]
        public void Should_parse_a_marble_string_with_an_error()
        {
            var errorValue = new Exception("omg error!");
            var result = Parser.ParseMarbles<string>("-------a---b---#", new { a = "A", b = "B" }, errorValue);

            Check.That(result).ContainsExactly(new object[]
            {
                new Recorded<Notification<string>>(70, Notification.CreateOnNext("A")),
                new Recorded<Notification<string>>(110, Notification.CreateOnNext("B")),
                new Recorded<Notification<string>>(150, Notification.CreateOnError<string>(errorValue))
            });
        }

        [Test]
        public void Should_default_in_the_letter_for_the_value_if_no_value_hash_was_passed()
        {
            var result = Parser.ParseMarbles<string>("--a--b--|");

            Check.That(result).ContainsExactly(new object[]
            {
                new Recorded<Notification<string>>(20, Notification.CreateOnNext("a")),
                new Recorded<Notification<string>>(50, Notification.CreateOnNext("b")),
                new Recorded<Notification<string>>(80, Notification.CreateOnCompleted<string>())
            });
        }


        [Test]
        public void Should_handle_grouped_values()
        {
            var result = Parser.ParseMarbles<string>("---(abc)---");

            Check.That(result).ContainsExactly(new object[]
            {
                new Recorded<Notification<string>>(30, Notification.CreateOnNext("a")),
                new Recorded<Notification<string>>(30, Notification.CreateOnNext("b")),
                new Recorded<Notification<string>>(30, Notification.CreateOnNext("c"))
            });
        }

        // should parse a subscription marble string into a subscriptionLog
        [Test]
        public void Should_parse_a_subscription_marble_string_into_a_subscriptionLog()
        {
            var result = Parser.ParseMarblesAsSubscriptions("---^---!-");

            Check.That(result.Subscribe).IsEqualTo(30);
            Check.That(result.Unsubscribe).IsEqualTo(70);
        }

        [Test]
        public void Should_parse_a_subscription_marble_without_an_unsubscription()
        {
            var result = Parser.ParseMarblesAsSubscriptions("---^---");

            Check.That(result.Subscribe).IsEqualTo(30);
            Check.That(result.Unsubscribe).IsEqualTo(int.MaxValue);
        }

        [Test]
        public void Should_parse_a_subscription_marble_with_a_synchronous_unsubscription()
        {
            var result = Parser.ParseMarblesAsSubscriptions("---(^!)---");

            Check.That(result.Subscribe).IsEqualTo(30);
            Check.That(result.Unsubscribe).IsEqualTo(30);
        }


        public object GetProperty(object o, string propName)
        {
            Type t = o.GetType();
            PropertyInfo p = t.GetProperty(propName);
            object v = p.GetValue(o);
            return v;
        }
    }
}
