using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;
using NFluent;
using NUnit.Framework;

namespace MarbleTest.Net.Test
{
    public class MarbleSchedulerTest
    {
        [Test]
        public void Should_create_a_cold_observable()
        {
            var expected = new [] {"A", "B"};
            var scheduler = new MarbleScheduler();
            var source = scheduler.CreateColdObservable<string>("--a---b--|", new {a = "A", b = "B"});
            var i = 0;
            source.Subscribe(x => Check.That(x).IsEqualTo(expected[i++]));
            scheduler.Start();
            Check.That(i).IsEqualTo(2);
        }

        [Test]
        public void Should_create_a_hot_observable()
        {
            var expected = new[] { "A", "B" };
            var scheduler = new MarbleScheduler();
            var source = scheduler.CreateHotObservable<string>("--a---b--|", new { a = "A", b = "B" });
            var i = 0;
            source.Subscribe(x => Check.That(x).IsEqualTo(expected[i++]));
            scheduler.Start();
            Check.That(i).IsEqualTo(2);
        }

        [Test]
        public void Should_expect_empty_observable()
        {
            var scheduler = new MarbleScheduler();
            scheduler.ExpectObservable(Observable.Empty<string>()).ToBe("|", new {});
            scheduler.Flush();
        }

        [Test]
        public void Should_expect_never_observable()
        {
            var scheduler = new MarbleScheduler();
            scheduler.ExpectObservable(Observable.Never<string>()).ToBe("-", new { });
            scheduler.ExpectObservable(Observable.Never<string>()).ToBe("---", new { });
            scheduler.Flush();
        }

        [Test]
        public void Should_expect_one_value_observable()
        {
            var scheduler = new MarbleScheduler();
            scheduler.ExpectObservable(Observable.Return("hello")).ToBe("(h|)", new { h = "hello" });
            scheduler.Flush();
        }

        [Test]
        public void Should_fail_when_event_values_differ()
        {
            var scheduler = new MarbleScheduler();
            Check.ThatCode(() =>
            {
                scheduler.ExpectObservable(Observable.Return("hello")).ToBe("h", new { h = "bye" });                
                scheduler.Flush();
            }).ThrowsAny();
        }

        [Test]
        public void Should_fail_when_event_timing_differs()
        {
            var scheduler = new MarbleScheduler();
            Check.ThatCode(() =>
            {
                scheduler.ExpectObservable(Observable.Return("hello")).ToBe("--h", new { h = "hello" });
                scheduler.Flush();
            }).ThrowsAny();
        }

        [Test]
        public void Should_use_unsubscription_diagram()
        {
            var scheduler = new MarbleScheduler();
            var source = scheduler.CreateHotObservable<string>("---^-a-b-|", new { a = "A", b = "B" });
            var unsubscribe =                                     "---!";
            var expected =                                        "--a";
            scheduler.ExpectObservable(source, unsubscribe).ToBe(expected);
        }

        [Test]
        public void Should_expect_subscription_on_a_cold_observable()
        {
            var scheduler = new MarbleScheduler();
            var source = scheduler.CreateColdObservable("---a---b-|");
            var subs =                                  "^--------!";
            
            //source.Subscriptions[0].
            //scheduler.ExpectSubscription(source, unsubscribe).ToBe(expected);
        }


    }
}
