using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;
using Xunit;
using FluentAssertions;

namespace MarbleTest.Net.Test
{
    public class ParserTest
    {
        [Fact]
        public void Should_parse_a_marble_string_into_a_series_of_notifications_and_types()
        {
            var result = Parser.ParseMarbles<string>("-------a---b---|", new { a = "A", b = "B" });

            result.Should().ContainInOrder(new object[]
            {
                new Recorded<Notification<string>>(70, Notification.CreateOnNext("A")),
                new Recorded<Notification<string>>(110, Notification.CreateOnNext("B")),
                new Recorded<Notification<string>>(150, Notification.CreateOnCompleted<string>())
            });
        }

        [Fact]
        public void Should_parse_a_marble_string_allowing_spaces_too()
        {
            var result = Parser.ParseMarbles<string>("--a--b--|   ", new { a = "A", b = "B" });

            result.Should().ContainInOrder(new object[]
            {
                new Recorded<Notification<string>>(20, Notification.CreateOnNext("A")),
                new Recorded<Notification<string>>(50, Notification.CreateOnNext("B")),
                new Recorded<Notification<string>>(80, Notification.CreateOnCompleted<string>())
            });
        }

        [Fact]
        public void Should_parse_a_marble_string_with_a_subscription_point()
        {
            var result = Parser.ParseMarbles<string>("---^---a---b---|", new { a = "A", b = "B" });

            result.Should().ContainInOrder(new object[]
            {
                new Recorded<Notification<string>>(40, Notification.CreateOnNext("A")),
                new Recorded<Notification<string>>(80, Notification.CreateOnNext("B")),
                new Recorded<Notification<string>>(120, Notification.CreateOnCompleted<string>())
            });
        }

        [Fact]
        public void Should_parse_a_marble_string_with_an_error()
        {
            var errorValue = new Exception("omg error!");
            var result = Parser.ParseMarbles<string>("-------a---b---#", new { a = "A", b = "B" }, errorValue);

            result.Should().ContainInOrder(new object[]
            {
                new Recorded<Notification<string>>(70, Notification.CreateOnNext("A")),
                new Recorded<Notification<string>>(110, Notification.CreateOnNext("B")),
                new Recorded<Notification<string>>(150, Notification.CreateOnError<string>(errorValue))
            });
        }

        [Fact]
        public void Should_default_in_the_letter_for_the_value_if_no_value_hash_was_passed()
        {
            var result = Parser.ParseMarbles<string>("--a--b--|");

            result.Should().ContainInOrder(new object[]
            {
                new Recorded<Notification<string>>(20, Notification.CreateOnNext("a")),
                new Recorded<Notification<string>>(50, Notification.CreateOnNext("b")),
                new Recorded<Notification<string>>(80, Notification.CreateOnCompleted<string>())
            });
        }

        [Fact]
        public void Should_handle_grouped_values()
        {
            var result = Parser.ParseMarbles<string>("---(abc)---");

            result.Should().ContainInOrder(new object[]
            {
                new Recorded<Notification<string>>(30, Notification.CreateOnNext("a")),
                new Recorded<Notification<string>>(30, Notification.CreateOnNext("b")),
                new Recorded<Notification<string>>(30, Notification.CreateOnNext("c"))
            });
        }

        [Fact]
        public void Should_handle_grouped_values_at_zero_time()
        {
            var result = Parser.ParseMarbles<string>("(abc)---");

            result.Should().ContainInOrder(new object[]
            {
                new Recorded<Notification<string>>(0, Notification.CreateOnNext("a")),
                new Recorded<Notification<string>>(0, Notification.CreateOnNext("b")),
                new Recorded<Notification<string>>(0, Notification.CreateOnNext("c"))
            });
        }

        [Fact]
        public void Should_handle_value_after_grouped_values()
        {
            var result = Parser.ParseMarbles<string>("---(abc)d--");

            result.Should().ContainInOrder(new object[]
            {
                new Recorded<Notification<string>>(30, Notification.CreateOnNext("a")),
                new Recorded<Notification<string>>(30, Notification.CreateOnNext("b")),
                new Recorded<Notification<string>>(30, Notification.CreateOnNext("c")),
                new Recorded<Notification<string>>(80, Notification.CreateOnNext("d"))
            });
        }

        [Fact]
        public void Should_parse_a_subscription_marble_string_into_a_subscriptionLog()
        {
            var result = Parser.ParseMarblesAsSubscriptions("---^---!-");

            result.Subscribe.Should().Be(30);
            result.Unsubscribe.Should().Be(70);
        }

        [Fact]
        public void Should_parse_a_subscription_marble_without_an_unsubscription()
        {
            var result = Parser.ParseMarblesAsSubscriptions("---^---");

            result.Subscribe.Should().Be(30);
            result.Unsubscribe.Should().Be(int.MaxValue);
        }

        [Fact]
        public void Should_parse_a_subscription_marble_with_a_synchronous_unsubscription()
        {
            var result = Parser.ParseMarblesAsSubscriptions("---(^!)---");

            result.Subscribe.Should().Be(30);
            result.Unsubscribe.Should().Be(30);
        }
    }
}
