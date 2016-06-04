using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;

namespace MarbleTest.Net
{
    public class MarbleScheduler : TestScheduler
    {
        private readonly int _frameTimeFactor;
        private readonly  IList<ITestOnFlush> _flushTests = new List<ITestOnFlush>();  

        public MarbleScheduler(int frameTimeFactor = 10)
        {
            _frameTimeFactor = frameTimeFactor;
        }

        public override IDisposable ScheduleAbsolute<TState>(TState state, long dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            bool clockHacked = false;
            if (dueTime <= Clock)
            {
                // Ugly Hack to workaround strange behaviour
                // and painful visibility keywords in TestScheduler class
                Clock = Clock - 1;
                clockHacked = true;
            }
                
            var result = base.ScheduleAbsolute(state, dueTime, action);
            if (clockHacked)
            {
                Clock++;
            }
            return result;
        }

        public ITestableObservable<string> CreateColdObservable(string marbles)
        {
            return CreateColdObservable<string>(marbles);
        }

        public ITestableObservable<T> CreateColdObservable<T>(string marbles,
            object values = null,
            Exception errorValue = null)
        {
            var messages = Parser.ParseMarbles<T>(marbles, values, errorValue, _frameTimeFactor);
            return CreateColdObservable(messages.ToArray());
        }

        public ITestableObservable<string> CreateHotObservable(string marbles)
        {
            return CreateHotObservable<string>(marbles);
        }

        public ITestableObservable<T> CreateHotObservable<T>(string marbles,
            object values = null,
            Exception errorValue = null)
        {
            var messages = Parser.ParseMarbles<T>(marbles, values, errorValue, _frameTimeFactor);
            return CreateHotObservable(messages.ToArray());
        }

        public TimeSpan CreateTime(string marbles)
        {
            var endIndex = marbles.IndexOf("|");
            if (endIndex == -1)
            {
                throw new Exception("Marble diagram for time should have a completion marker '|'");
            }
            
            return TimeSpan.FromTicks(endIndex * _frameTimeFactor);
        }

        public ISetupTest ExpectObservable<T>(IObservable<T> observable, string unsubscriptionMarbles = null)
        {
            var actual = new List<Recorded<Notification<object>>>();
            var flushTest = new FlushableTest() { Actual = actual, Ready = false };
            var unsubscriptionFrame = long.MaxValue;
            if (unsubscriptionMarbles != null)
            {
                unsubscriptionFrame 
                    = Parser.ParseMarblesAsSubscriptions(unsubscriptionMarbles, _frameTimeFactor).Unsubscribe;                    
            }
            IDisposable subscription = observable.Subscribe(x =>
            {
                object value = x;
                // Support Observable-of-Observables
                if (value is IObservable<object>)
                {
                    value = MaterializeInnerObservable(value as IObservable<object>, Clock);
                }
                actual.Add(new Recorded<Notification<object>>(Clock, Notification.CreateOnNext(value)));
            },
            (error) => actual.Add(new Recorded<Notification<object>>(Clock, Notification.CreateOnError<object>(error))),
            () => actual.Add(new Recorded<Notification<object>>(Clock, Notification.CreateOnCompleted<object>())));

            if (unsubscriptionFrame != long.MaxValue)
            {
                ScheduleAbsolute((object) null, unsubscriptionFrame, (scheduler, state) =>
                {
                    subscription.Dispose();
                    return Disposable.Empty;
                });
            }

            _flushTests.Add(flushTest);

            return new SetupTest(flushTest, _frameTimeFactor);
        }

        private IList<Recorded<Notification<object>>> MaterializeInnerObservable(IObservable<object> observable, long outerFrame)
        {
            var messages = new List<Recorded<Notification<object>>>();
            observable.Subscribe(
                x => messages.Add(new Recorded<Notification<object>>(Clock - outerFrame, Notification.CreateOnNext(x))),
                error => messages.Add(new Recorded<Notification<object>>(Clock - outerFrame, Notification.CreateOnError<object>(error))),
                () => messages.Add(new Recorded<Notification<object>>(Clock - outerFrame, Notification.CreateOnCompleted<object>()))
            );
            return messages;
        }

        class SetupTest : ISetupTest
        {
            private readonly FlushableTest _flushTest;
            private readonly int _frameTimeFactor;

            public SetupTest(FlushableTest flushTest, int frameTimeFactor)
            {
                _flushTest = flushTest;
                _frameTimeFactor = frameTimeFactor;
            }

            public void ToBe(string marble, object values, Exception errorValue)
            {
                _flushTest.Ready = true;
                _flushTest.Expected = Parser.ParseMarbles<object>(marble, values, errorValue, _frameTimeFactor, true);
            }
        }

        interface ITestOnFlush
        {
            void Run();
            bool Ready { get; set; }
        }

        class FlushableTest : ITestOnFlush {
            public bool Ready { get; set; }
            public IList<Recorded<Notification<object>>> Actual { get; set; }
            public IList<Recorded<Notification<object>>> Expected { get; set; }

            private readonly Type _notificationsType = typeof(IList<Recorded<Notification<object>>>);

            public void Run()
            {
                CheckEquality(Actual, Expected);
            }

            private void CheckEquality(
                IList<Recorded<Notification<object>>> actual, 
                IList<Recorded<Notification<object>>> expected)
            {
                if (actual.Count != expected.Count)
                {
                    throw new Exception(
                        expected.Count + " event(s) expected, " 
                        + actual.Count + " observed");
                }
                for (int i = 0; i < actual.Count; i++)
                {
                    

                    if (actual[i].Time != expected[i].Time)
                    {
                        throw new Exception(
                            "Expected event \"" + expected[i].Value + "\" at " + expected[i].Time
                            + ", instead received \"" + actual[i].Value + "\" at " + actual[i].Time);
                    }

                    var actualNotification = actual[i].Value;
                    var expectedNotification = expected[i].Value;

                    if (actualNotification.Kind != expectedNotification.Kind)
                    {
                        throw new Exception(
                            "Expected event " + expectedNotification.Kind
                            + ", instead received at " + actualNotification.Kind);
                    }

                    if (actualNotification != expectedNotification)
                    {
                        if (actualNotification.Value is IEnumerable 
                            && expectedNotification.Value is IEnumerable 
                            && !(actualNotification.Value is string))
                        {
                            var actualNotifications 
                                = ReflectionHelper.CastNotificationsFromTestableObservable(
                                    actualNotification.Value as IEnumerable);

                            var expectedNotifications
                                = ReflectionHelper.CastNotificationsFromTestableObservable(
                                    expectedNotification.Value as IEnumerable);

                            CheckEquality(
                                actualNotifications, 
                                expectedNotifications);
                        }
                        else
                        {
                            throw new Exception(
                                "Expected event was " + expectedNotification 
                                + ", instead received " + actualNotification);                            
                        }
                    }
                }
            }
        }

        public void Flush()
        {
            base.Start();
            var readyFlushTests = _flushTests.Where(test => test.Ready);
            readyFlushTests.ToList().ForEach(test => test.Run());
        }

        public ISetupSubscriptionsTest ExpectSubscription(IList<Subscription> subscriptions)
        {
            var flushTest = new FlushableSubscriptionTest() { Actual = subscriptions, Ready = false };
            _flushTests.Add(flushTest);
            return new SetupSubscriptionsTest( flushTest, _frameTimeFactor);
        }

        class SetupSubscriptionsTest : ISetupSubscriptionsTest
        {
            private readonly FlushableSubscriptionTest _flushTest;
            private readonly int _frameTimeFactor;

            public SetupSubscriptionsTest(FlushableSubscriptionTest flushTest, int frameTimeFactor)
            {
                _flushTest = flushTest;
                _frameTimeFactor = frameTimeFactor;
            }

            public void ToBe(params string[] marbles)
            {
                _flushTest.Ready = true;
                _flushTest.Expected =
                    marbles.Select(m => Parser.ParseMarblesAsSubscriptions(m, _frameTimeFactor)).ToList();
            }
        }

        class FlushableSubscriptionTest : ITestOnFlush
        {
            public bool Ready { get; set; }
            public IList<Subscription> Actual { get; set; }
            public IList<Subscription> Expected { get; set; }

            public void Run()
            {
                if (Actual.Count != Expected.Count)
                {
                    throw new Exception(Expected.Count + " subscription(s) expected, only " + Actual.Count + " observed");
                }
                for (int i = 0; i < Actual.Count; i++)
                {
                    if (Actual[i] != Expected[i])
                    {
                        throw new Exception("Expected subscription was " + Expected[i] + ", instead received " + Actual[i]);
                    }
                }
            }
        }
    }
}
