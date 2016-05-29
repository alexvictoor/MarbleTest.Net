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


        /*
         * const values = { a: 1, b: 2 };
        const myObservable = cold('---a---b--|', values);
        const subs =              '^---------!';
        expectObservable(myObservable).toBe('---a---b--|', values);
        expectSubscriptions(myObservable.subscriptions).toBe(subs);
         */

        [Test]
        public void Should_test_everything_and_be_awesome()
        {
            var values = new {a = 1, b = 2};
            var source = _scheduler.CreateColdObservable<int>("---a---b--|", values);
            var subscription = source.Subscribe();
            _scheduler.ScheduleAbsolute(subscription, 90, (scheduler, sub) =>
            {
                sub.Dispose();
                return Disposable.Empty;
            });
            _scheduler.ExpectObservable(source).ToBe("---a---b--|", values);
            var subs =                                        "^--------!";
            _scheduler.ExpectSubscription(source.Subscriptions).ToBe(subs);
        }

        [Test]
        public void Should_()
        {
            var source = _scheduler.CreateColdObservable("---a---b-|");
            var subs = "^--------!";
            var disposable = source.Subscribe(n => Console.WriteLine("coucou " + n), () => Console.WriteLine("finito"));
            _scheduler.ScheduleAbsolute((object)null, 90, (scheduler, st) =>
            {
                disposable.Dispose();
                return Disposable.Empty;
            });
            _scheduler.Flush();
            source.Subscriptions.ToList().ForEach(s => Console.WriteLine("sub " + s));
            Console.Out.WriteLine("end");
        }

        /*
         * it('should support testing metastreams', () => {
        const x = cold('-a-b|');
        const y = cold('-c-d|');
        const myObservable = hot('---x---y----|', { x: x, y: y });
        const expected =         '---x---y----|';
        const expectedx = cold('-a-b|');
        const expectedy = cold('-c-d|');
        expectObservable(myObservable).toBe(expected, { x: expectedx, y: expectedy });
      });
         * 
         */

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

            IObservable<string> ss = Observable.Return("");
            var bb = "";
            Console.Out.WriteLine("toto" + (ss is IObservable<object>));
            Console.Out.WriteLine("toto" + (bb is IObservable<object>));

        }


    }
}
