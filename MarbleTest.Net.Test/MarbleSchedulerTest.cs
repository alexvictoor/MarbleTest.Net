using NFluent;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Xunit;

namespace MarbleTest.Net.Test
{
    public class MarbleSchedulerTest
    {
        [Fact]
        public void Should_create_a_cold_observable()
        {
            var scheduler = new MarbleScheduler();
            var expected = new[] { "A", "B" };
            var source = scheduler.CreateColdObservable<string>("--a---b--|", new { a = "A", b = "B" });
            var i = 0;

            source.Subscribe(x => Check.That(x).IsEqualTo(expected[i++]));
            scheduler.Start();

            Check.That(i).IsEqualTo(2);
            scheduler.Flush();
        }

        [Fact]
        public void Should_create_a_hot_observable()
        {
            var scheduler = new MarbleScheduler();
            var expected = new[] { "A", "B" };
            var source = scheduler.CreateHotObservable<string>("--a---b--|", new { a = "A", b = "B" });
            var i = 0;

            source.Subscribe(x => Check.That(x).IsEqualTo(expected[i++]));
            scheduler.Start();

            Check.That(i).IsEqualTo(2);
            scheduler.Flush();
        }

        [Fact]
        public void Should_parse_a_simple_time_marble_string_to_a_number()
        {
            var scheduler = new MarbleScheduler();
            var time = scheduler.CreateTime("-----|");
            Check.That(time).IsEqualTo(TimeSpan.FromTicks(50));
            scheduler.Flush();
        }

        [Fact]
        public void Should_throw_if_not_given_good_marble_input()
        {
            var scheduler = new MarbleScheduler();

            Action badMarbleInput = () => scheduler.CreateTime("-a-b-c-#");
            Check.ThatCode(badMarbleInput).ThrowsAny();

            scheduler.Flush();
        }

        [Fact]
        public void Should_expect_empty_observable()
        {
            var scheduler = new MarbleScheduler();

            scheduler.ExpectObservable(Observable.Empty<string>()).ToBe("|", new { });
            scheduler.Flush();
        }

        [Fact]
        public void Should_expect_never_observable()
        {
            var scheduler = new MarbleScheduler();

            scheduler.ExpectObservable(Observable.Never<string>()).ToBe("-", new { });
            scheduler.ExpectObservable(Observable.Never<string>()).ToBe("---", new { });
            scheduler.Flush();
        }

        [Fact]
        public void Should_expect_one_value_observable()
        {
            var scheduler = new MarbleScheduler();

            scheduler.ExpectObservable(Observable.Return("hello")).ToBe("(h|)", new { h = "hello" });
            scheduler.Flush();
        }

        [Fact]
        public void Should_fail_when_event_values_differ()
        {
            var scheduler = new MarbleScheduler();

            Action valueDiffer = () =>
            {
                scheduler.ExpectObservable(Observable.Return("hello")).ToBe("h", new { h = "bye" });
                scheduler.Flush();
            };

            Check.ThatCode(valueDiffer).ThrowsAny();
        }

        [Fact]
        public void Should_fail_when_event_timing_differs()
        {
            var scheduler = new MarbleScheduler();

            Action timingDiffer = () =>
            {
                scheduler.ExpectObservable(Observable.Return("hello")).ToBe("--h", new { h = "hello" });
                scheduler.Flush();
            };

            Check.ThatCode(timingDiffer).ThrowsAny();
        }

        [Fact]
        public void Should_expect_observable_on_error()
        {
            var scheduler = new MarbleScheduler();

            var source = scheduler.CreateColdObservable<Unit>("---#", null, new Exception());

            scheduler.ExpectObservable(source).ToBe("---#", null, new Exception());
            scheduler.Flush();
        }

        [Fact]
        public void Should_fail_when_observables_end_with_different_error_types()
        {
            var scheduler = new MarbleScheduler();

            Action endingWithDifferentExceptionType = () =>
            {
                var source = scheduler.CreateColdObservable<Unit>("---#", null, new ArgumentException());
                scheduler.ExpectObservable(source).ToBe("---#", null, new Exception());
                scheduler.Flush();
            };

            Check.ThatCode(endingWithDifferentExceptionType).ThrowsAny();
        }

        [Fact]
        public void Should_demo_with_a_simple_operator()
        {
            var scheduler = new MarbleScheduler();
            var sourceEvents = scheduler.CreateColdObservable("a-b-c-|");

            var upperEvents = sourceEvents.Select(s => s.ToUpper());

            scheduler.ExpectObservable(upperEvents).ToBe("A-B-C-|");
            scheduler.Flush();
        }

        [Fact]
        public void Should_use_unsubscription_diagram()
        {
            var scheduler = new MarbleScheduler();
            var source = scheduler.CreateHotObservable("---^-a-b-|");
            var unsubscribe = "---!";
            var expected = "--a";

            scheduler.ExpectObservable(source, unsubscribe).ToBe(expected);
            scheduler.Flush();
        }

        [Fact]
        public void Should_expect_subscription_on_a_cold_observable()
        {
            var scheduler = new MarbleScheduler();
            var source = scheduler.CreateColdObservable("---a---b-|");
            var subscription = source.Subscribe();

            scheduler.ScheduleAbsolute(subscription, 90, (sched, sub) =>
            {
                sub.Dispose();
                return Disposable.Empty;
            });

            var subs = "^--------!";

            scheduler.ExpectSubscription(source.Subscriptions).ToBe(subs);
            scheduler.Flush();
        }

        [Fact]
        public void Should_support_testing_metastreams()
        {
            var scheduler = new MarbleScheduler();
            var x = scheduler.CreateColdObservable("-a-b|");
            var y = scheduler.CreateColdObservable("-c-d|");

            var myObservable = scheduler.CreateHotObservable<IObservable<string>>("---x---y----|", new { x = x, y = y });
            var expected = "---x---y----|";
            var expectedx = scheduler.CreateColdObservable("-a-b|");
            var expectedy = scheduler.CreateColdObservable("-c-d|");

            scheduler.ExpectObservable(myObservable).ToBe(expected, new { x = expectedx, y = expectedy });
            scheduler.Flush();
        }

        [Fact]
        public void Should_demo_metastreams_with_windows()
        {
            var scheduler = new MarbleScheduler();
            var input = "a---b---c---d-|";
            var myObservable = scheduler.CreateColdObservable(input);

            var result = myObservable.Window(2, 1);

            var aWindow = scheduler.CreateColdObservable("a---(b|)");
            var bWindow = scheduler.CreateColdObservable("----b---(c|)");
            var cWindow = scheduler.CreateColdObservable("----c---(d|)");
            var dWindow = scheduler.CreateColdObservable("----d-|");
            var eWindow = scheduler.CreateColdObservable("--|");

            var expected = "(ab)c---d---e-|";
            scheduler.ExpectObservable(result).ToBe(expected, new { a = aWindow, b = bWindow, c = cWindow, d = dWindow, e = eWindow });
            scheduler.Flush();
        }
    }
}
