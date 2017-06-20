using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;
using Xunit;
using FluentAssertions;

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
            result.Should().Be(1);
        }

        [Fact]
        public void Should_detect_testable_observable_of_objects()
        {
            // given
            var cold = new MarbleScheduler().CreateColdObservable<object>("--a--", new { a = this });
            // when
            var result = ReflectionHelper.IsTestableObservable(cold);
            // then
            result.Should().BeTrue();
        }

        [Fact]
        public void Should_detect_testable_observable_of_strings()
        {
            // given
            var cold = new MarbleScheduler().CreateColdObservable("--a--");
            // when
            var result = ReflectionHelper.IsTestableObservable(cold);
            // then
            result.Should().BeTrue();
        }

        [Fact]
        public void Should_detect_object_not_being_observable()
        {
            // given
            var dummy = new object();
            // when
            var result = ReflectionHelper.IsTestableObservable(dummy);
            // then
            result.Should().BeFalse();
        }

        [Fact]
        public void Should_retrieve_notifications_from_string_testable_observable()
        {
            // given
            object cold = new MarbleScheduler().CreateColdObservable("--a--");
            // when
            var notifications = ReflectionHelper.RetrieveNotificationsFromTestableObservable(cold);
            // then
            notifications.Count.Should().Be(1);
            notifications[0].Time.Should().Be(20);
            notifications[0].Value.Value.Should().Be("a");
        }

        [Fact]
        public void Should_retrieve_completed_notification_from_testable_observable()
        {
            // given
            object cold = new MarbleScheduler().CreateColdObservable("---|");
            
            // when
            var notifications = ReflectionHelper.RetrieveNotificationsFromTestableObservable(cold);

            // then
            notifications.Count.Should().Be(1);
            notifications[0].Time.Should().Be(30);
            notifications[0].Value.Kind.Should().Be(NotificationKind.OnCompleted);
        }

        [Fact]
        public void Should_retrieve_error_notification_from_testable_observable()
        {
            // given
            object cold = new MarbleScheduler().CreateColdObservable<object>("---#", null, new Exception("omg"));
            // when
            var notifications = ReflectionHelper.RetrieveNotificationsFromTestableObservable(cold);
            // then
            notifications.Count.Should().Be(1);
            notifications[0].Time.Should().Be(30);
            notifications[0].Value.Kind.Should().Be(NotificationKind.OnError);
        }
    }
}
