using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
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
        private MarbleScheduler _scheduler;

        [SetUp]
        public void SetupScheduler()
        {
            _scheduler = new MarbleScheduler();
        }

        [TearDown]
        public void FlushScheduler()
        {
            if (_scheduler != null)
            {
                _scheduler.Flush();
            }
        }
        
        [Test]
        public void Should_create_a_cold_observable()
        {
            var expected = new [] {"A", "B"};
            var source = _scheduler.CreateColdObservable<string>("--a---b--|", new {a = "A", b = "B"});
            var i = 0;
            source.Subscribe(x => Check.That(x).IsEqualTo(expected[i++]));
            _scheduler.Start();
            Check.That(i).IsEqualTo(2);
        }

        [Test]
        public void Should_create_a_hot_observable()
        {
            var expected = new[] { "A", "B" };
            var source = _scheduler.CreateHotObservable<string>("--a---b--|", new { a = "A", b = "B" });
            var i = 0;
            source.Subscribe(x => Check.That(x).IsEqualTo(expected[i++]));
            _scheduler.Start();
            Check.That(i).IsEqualTo(2);
        }

        [Test]
        public void Should_parse_a_simple_time_marble_string_to_a_number()
        {
            var time = _scheduler.CreateTime("-----|");
            Check.That(time).IsEqualTo(TimeSpan.FromTicks(50));
        }

        [Test]
        public void Should_throw_if_not_given_good_marble_input()
        {
            Check.ThatCode(() => _scheduler.CreateTime("-a-b-c-#")).ThrowsAny();
        }

        [Test]
        public void Should_expect_empty_observable()
        {
            var scheduler = new MarbleScheduler();
            scheduler.ExpectObservable(Observable.Empty<string>()).ToBe("|", new {});
            _scheduler.Flush();
        }

        [Test]
        public void Should_expect_never_observable()
        {
            _scheduler.ExpectObservable(Observable.Never<string>()).ToBe("-", new { });
            _scheduler.ExpectObservable(Observable.Never<string>()).ToBe("---", new { });
            _scheduler.Flush();
        }

        [Test]
        public void Should_expect_one_value_observable()
        {
            _scheduler.ExpectObservable(Observable.Return("hello")).ToBe("(h|)", new { h = "hello" });
            _scheduler.Flush();
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
        public void Should_demo_with_a_simple_operator()
        {
            var sourceEvents = _scheduler.CreateColdObservable("a-b-c-|");
            var upperEvents = sourceEvents.Select(s => s.ToUpper());
            _scheduler.ExpectObservable(upperEvents).ToBe("A-B-C-|");
            _scheduler.Flush();
        }

        [Test]
        public void Should_use_unsubscription_diagram()
        {
            var source = _scheduler.CreateHotObservable("---^-a-b-|");
            var unsubscribe =                              "---!";
            var expected =                                 "--a";
            _scheduler.ExpectObservable(source, unsubscribe).ToBe(expected);
        }

        [Test]
        public void Should_expect_subscription_on_a_cold_observable()
        {
            var source = _scheduler.CreateColdObservable("---a---b-|");
            var subscription = source.Subscribe();
            _scheduler.ScheduleAbsolute(subscription, 90, (scheduler, sub) =>
            {
                sub.Dispose();
                return Disposable.Empty;
            });

            var subs =                                   "^--------!";
            _scheduler.ExpectSubscription(source.Subscriptions).ToBe(subs);
        }

        [Test]
        public void Should_support_testing_metastreams()
        {
            var x = _scheduler.CreateColdObservable("-a-b|");
            var y = _scheduler.CreateColdObservable("-c-d|");
            var myObservable = _scheduler.CreateHotObservable<IObservable<string>>("---x---y----|", new {x = x, y = y});
            var expected = "---x---y----|";
            var expectedx = _scheduler.CreateColdObservable("-a-b|");
            var expectedy = _scheduler.CreateColdObservable("-c-d|");
            _scheduler.ExpectObservable(myObservable).ToBe(expected, new { x = expectedx, y = expectedy});
        }

        [Test]
        public void Should_demo_metastreams_with_windows()
        {
            var input   =                                 "a---b---c---d-|";
            var myObservable = _scheduler.CreateColdObservable(input);

            var result = myObservable.Window(2, 1);
            
            var aWindow = _scheduler.CreateColdObservable("a---(b|)");
            var bWindow = _scheduler.CreateColdObservable("----b---(c|)");
            var cWindow = _scheduler.CreateColdObservable(    "----c---(d|)");
            var dWindow = _scheduler.CreateColdObservable(        "----d-|");
            var eWindow = _scheduler.CreateColdObservable(            "--|");

            var expected = "(ab)c---d---e-|";
            _scheduler.ExpectObservable(result).ToBe(expected, new { a = aWindow, b = bWindow, c = cWindow, d = dWindow, e = eWindow });
        }


    }
}
