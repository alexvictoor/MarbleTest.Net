using NFluent;
using System;
using System.Linq;
using System.Reactive;
using Xunit;

namespace MarbleTest.Net.Test
{
    public class ReflectionHelperTest
    {
        [Fact]
        public void Should_retrieve_property_value()
        {
            // given
            var foo = new {First = 1, Second = 2};

            // when
            var result = ReflectionHelper.GetProperty<int>(foo, "First");

            // then
            Check.That(result).IsEqualTo(1);
        }

        [Fact]
        public void Should_detect_testable_observable_of_objects()
        {
            // given
            var cold = new MarbleScheduler().CreateColdObservable<object>("--a--", new { a = this });

            // when
            var result = ReflectionHelper.IsTestableObservable(cold);

            // then
            Check.That(result).IsTrue();
        }

        [Fact]
        public void Should_detect_testable_observable_of_strings()
        {
            // given
            var cold = new MarbleScheduler().CreateColdObservable("--a--");

            // when
            var result = ReflectionHelper.IsTestableObservable(cold);

            // then
            Check.That(result).IsTrue();
        }

        [Fact]
        public void Should_detect_object_not_being_observable()
        {
            // given
            var dummy = new object();

            // when
            var result = ReflectionHelper.IsTestableObservable(dummy);

            // then
            Check.That(result).IsFalse();
        }

        [Fact]
        public void Should_retrieve_notifications_from_string_testable_observable()
        {
            // given
            object cold = new MarbleScheduler().CreateColdObservable("--a--");

            // when
            var notifications = ReflectionHelper.RetrieveNotificationsFromTestableObservable(cold);

            // then
            Check.That(notifications.Count).IsEqualTo(1);
            Check.That(notifications[0].Time).IsEqualTo(20);
            Check.That(notifications[0].Value.Value).IsEqualTo("a");
        }

        [Fact]
        public void Should_retrieve_completed_notification_from_testable_observable()
        {
            // given
            object cold = new MarbleScheduler().CreateColdObservable("---|");

            // when
            var notifications = ReflectionHelper.RetrieveNotificationsFromTestableObservable(cold);

            // then
            Check.That(notifications.Count).IsEqualTo(1);
            Check.That(notifications[0].Time).IsEqualTo(30);
            Check.That(notifications[0].Value.Kind).IsEqualTo(NotificationKind.OnCompleted);
        }

        [Fact]
        public void Should_retrieve_error_notification_from_testable_observable()
        {
            // given
            object cold = new MarbleScheduler().CreateColdObservable<object>("---#", null, new Exception("omg"));

            // when
            var notifications = ReflectionHelper.RetrieveNotificationsFromTestableObservable(cold);

            // then
            Check.That(notifications).HasSize(1);
            Check.That(notifications[0].Time).IsEqualTo(30);
            Check.That(notifications[0].Value.Kind).IsEqualTo(NotificationKind.OnError);
        }
    }
}
