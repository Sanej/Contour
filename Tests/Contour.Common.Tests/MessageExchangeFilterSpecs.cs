﻿namespace Contour.Common.Tests
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using FluentAssertions;

    using Contour.Filters;

    using NUnit.Framework;
    using FluentAssertions.Extensions;

    /// <summary>
    /// The message exchange filter specs.
    /// </summary>
    // ReSharper disable InconsistentNaming
    public class MessageExchangeFilterSpecs
    {
        /// <summary>
        /// The boo.
        /// </summary>
        public class Boo
        {
            #region Public Properties

            /// <summary>
            /// Gets or sets the num.
            /// </summary>
            public int Num { get; set; }

            /// <summary>
            /// Gets or sets the str.
            /// </summary>
            public string Str { get; set; }

            #endregion
        }

        /// <summary>
        /// The logging filter.
        /// </summary>
        public class LoggingFilter : IMessageExchangeFilter
        {
            #region Fields

            /// <summary>
            /// The _log.
            /// </summary>
            private readonly IList<string> _log;

            /// <summary>
            /// The _name.
            /// </summary>
            private readonly string _name;

            /// <summary>
            /// The _stop.
            /// </summary>
            private readonly bool _stop;

            #endregion

            #region Constructors and Destructors

            /// <summary>
            /// Инициализирует новый экземпляр класса <see cref="LoggingFilter"/>.
            /// </summary>
            /// <param name="name">
            /// The name.
            /// </param>
            /// <param name="log">
            /// The log.
            /// </param>
            /// <param name="stop">
            /// The stop.
            /// </param>
            public LoggingFilter(string name, IList<string> log, bool stop = false)
            {
                this._name = name;
                this._log = log;
                this._stop = stop;
            }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// The process.
            /// </summary>
            /// <param name="exchange">
            /// The exchange.
            /// </param>
            /// <param name="invoker">
            /// The invoker.
            /// </param>
            /// <returns>
            /// The <see cref="Task"/>.
            /// </returns>
            public Task<MessageExchange> Process(MessageExchange exchange, MessageExchangeFilterInvoker invoker)
            {
                this._log.Add(this._name + ": before");

                if (this._stop)
                {
                    return Filter.Result(exchange);
                }

                return invoker.Continue(exchange).
                    ContinueWith(
                        t =>
                            {
                                this._log.Add(this._name + ": after");
                                return t.Result;
                            });
            }

            #endregion
        }

        /// <summary>
        /// The when_filtering_out.
        /// </summary>
        [TestFixture]
        [Category("Unit")]
        public class when_filtering_out
        {
            #region Public Methods and Operators

            /// <summary>
            /// The should_skip_remaining_filters.
            /// </summary>
            [Test]
            public void should_skip_remaining_filters()
            {
                var log = new List<string>();
                var filters = new[] { new LoggingFilter("A", log), new LoggingFilter("B", log, true), new LoggingFilter("C", log) };
                var exchange = new MessageExchange(new Message("msg".ToMessageLabel(), new object()));
                var invoker = new MessageExchangeFilterInvoker(filters);

                invoker.Process(exchange).
                    Wait(1.Seconds());

                log.Should().
                    BeEquivalentTo(new object[] { "C: before", "B: before", "C: after" });
            }

            #endregion
        }

        /// <summary>
        /// The when_invoking_filter_list.
        /// </summary>
        [TestFixture]
        [Category("Unit")]
        public class when_invoking_filter_list
        {
            #region Public Methods and Operators

            /// <summary>
            /// The should_invoke_starting_with_recent.
            /// </summary>
            [Test]
            public void should_invoke_starting_with_recent()
            {
                var log = new List<string>();
                var filters = new[] { new LoggingFilter("A", log), new LoggingFilter("B", log), new LoggingFilter("C", log) };
                var exchange = new MessageExchange(new Message("msg".ToMessageLabel(), new object()));
                var invoker = new MessageExchangeFilterInvoker(filters);

                invoker.Process(exchange).
                    Wait(1.Seconds());

                log.Should().
                    BeEquivalentTo(new object[] { "C: before", "B: before", "A: before", "A: after", "B: after", "C: after" });
            }

            #endregion
        }
    }

    // ReSharper restore InconsistentNaming
}
