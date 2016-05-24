using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;

namespace MarbleTest.Net
{
    public class MarbleScheduler : TestScheduler
    {
        private readonly int _frameTimeFactor;
        private readonly  IList<FlushableTest> flushTests = new List<FlushableTest>();  

        public MarbleScheduler(int frameTimeFactor = 10)
        {
            _frameTimeFactor = frameTimeFactor;
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

        public ICheck ExpectObservable<T>(IObservable<T> observable, string unsubscriptionMarbles = null)
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

            flushTests.Add(flushTest);

            return new Check(flushTest, _frameTimeFactor);
        }

        class Check : ICheck
        {
            private readonly FlushableTest _flushTest;
            private readonly int _frameTimeFactor;

            public Check(FlushableTest flushTest, int frameTimeFactor)
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

        class FlushableTest {
            public bool Ready { get; set; }
            public IList<Recorded<Notification<object>>> Actual { get; set; }
            public IList<Recorded<Notification<object>>> Expected { get; set; }

            public void Run()
            {
                if (Actual.Count != Expected.Count)
                {
                    throw new Exception(Expected.Count + " event(s) expected, only " + Actual.Count + " observed");
                }
                for (int i = 0; i < Actual.Count; i++)
                {
                    if (Actual[i] != Expected[i])
                    {
                        throw new Exception("Expected event was " + Expected[i] + ", instead received " + Actual[i]);
                    }
                }
            }
        }

        public void Flush()
        {
            base.Start();
            var readyFlushTests = flushTests.Where(test => test.Ready);
            readyFlushTests.ToList().ForEach(test => test.Run());
        }
    }
}
