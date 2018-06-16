﻿using System.Linq;
using Contour.Configuration;
using Contour.Helpers;
using Contour.Serialization;
using Contour.Testing.Transport.RabbitMq;
using Contour.Transport.RabbitMQ;

namespace Contour.Configurator.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Dynamic;
    using System.Threading;

    using FluentAssertions;
    using Contour.Operators;
    using Contour.Receiving;
    using Contour.Receiving.Consumers;
    using Contour.Validation;

    using Moq;

    //using Ninject;

    using NUnit.Framework;
    using Contour.Testing;
    using FluentAssertions.Extensions;

    /// <summary>
    /// The bus configuration specs.
    /// </summary>
    // ReSharper disable InconsistentNaming
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Reviewed. Suppression is OK here.")]

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Reviewed. Suppression is OK here.")]

    public class BusConfigurationSpecs
    {
        public class BooMessage
        {
            public int Num { get; set; }
        }

        public class FooMessage
        {
            public int Num { get; set; }
        }

        /// <summary>
        /// The boo payload validator.
        /// </summary>
        public class BooPayloadValidator : IMessageValidatorOf<BooMessage>
        {
            public ValidationResult Validate(Message<BooMessage> message)
            {
                return Validate(message.Payload);
            }

            public ValidationResult Validate(IMessage message)
            {
                if (message.Payload is BooMessage)
                {
                    return Validate((BooMessage)message.Payload);
                }
                else
                {
                    return new ValidationResult(new BrokenRule("should be BooMessage"));
                }
            }

            private ValidationResult Validate(BooMessage booMessage)
            {
                if (booMessage.Num > 100)
                {
                    return ValidationResult.Valid;
                }
                else
                {
                    return new ValidationResult(new BrokenRule("should be greater than 100"));
                }
            }
        }

        /// <summary>
        /// The bus dependent handler.
        /// </summary>
        public class BusDependentHandler : IConsumerOf<BooMessage>
        {
            /// <summary>
            /// The build count.
            /// </summary>
            public static int BuildCount;

            /// <summary>
            /// The wait event.
            /// </summary>
            public static CountdownEvent WaitEvent;

            /// <summary>
            /// Инициализирует новый экземпляр класса <see cref="BusDependentHandler"/>.
            /// </summary>
            /// <param name="bus">
            /// The bus.
            /// </param>
            public BusDependentHandler(IBus bus)
            {
                this.Bus = bus;

                BuildCount++;
            }

            /// <summary>
            /// Gets the bus.
            /// </summary>
            public IBus Bus { get; private set; }

            /// <summary>
            /// The reset.
            /// </summary>
            public static void Reset()
            {
                WaitEvent = new CountdownEvent(3);
                BuildCount = 0;
            }

            /// <summary>
            /// The handle.
            /// </summary>
            /// <param name="context">
            /// The context.
            /// </param>
            public void Handle(IConsumingContext<BooMessage> context)
            {
                WaitEvent.Signal();
            }
        }

        /// <summary>
        /// The bus dependent transformer.
        /// </summary>
        public class BusDependentTransformer : IMessageOperator
        {
            /// <summary>
            /// The build count.
            /// </summary>
            public static int BuildCount;

            /// <summary>
            /// The wait event.
            /// </summary>
            public static CountdownEvent WaitEvent;

            /// <summary>
            /// Инициализирует новый экземпляр класса <see cref="BusDependentTransformer"/>.
            /// </summary>
            /// <param name="bus">
            /// The bus.
            /// </param>
            public BusDependentTransformer(IBus bus)
            {
                this.Bus = bus;

                BuildCount++;
            }

            /// <summary>
            /// Gets the bus.
            /// </summary>
            public IBus Bus { get; private set; }

            /// <summary>
            /// The reset.
            /// </summary>
            public static void Reset()
            {
                WaitEvent = new CountdownEvent(3);
                BuildCount = 0;
            }

            public IEnumerable<IMessage> Apply(IMessage message)
            {
                WaitEvent.Signal();
                yield break;
            }
        }

        /// <summary>
        /// The concrete handler of.
        /// </summary>
        /// <typeparam name="T">
        /// </typeparam>
        public class ConcreteHandlerOf<T> : IConsumerOf<T>
            where T : class
        {
            /// <summary>
            /// The _received.
            /// </summary>
            private readonly ManualResetEvent _received = new ManualResetEvent(false);

            /// <summary>
            /// Gets the received.
            /// </summary>
            public WaitHandle Received
            {
                get
                {
                    return this._received;
                }
            }

            /// <summary>
            /// The handle.
            /// </summary>
            /// <param name="context">
            /// The context.
            /// </param>
            public void Handle(IConsumingContext<T> context)
            {
                this._received.Set();
            }
        }

        /// <summary>
        /// The concrete transformer of.
        /// </summary>
        /// <typeparam name="T">
        /// </typeparam>
        public class ConcreteTransformerOf<T> : IMessageOperator
            where T : class
        {
            /// <summary>
            /// The _received.
            /// </summary>
            private readonly ManualResetEvent received = new ManualResetEvent(false);

            /// <summary>
            /// Gets the received.
            /// </summary>
            public WaitHandle Received
            {
                get
                {
                    return this.received;
                }
            }

            public IEnumerable<IMessage> Apply(IMessage message)
            {
                this.received.Set();
                yield break;
            }
        }

        /// <summary>
        /// The foo payload validator.
        /// </summary>
        public class FooPayloadValidator : IMessageValidatorOf<FooMessage>
        {
            public ValidationResult Validate(Message<FooMessage> message)
            {
                return Validate(message.Payload);
            }

            public ValidationResult Validate(IMessage message)
            {
                if (message.Payload is FooMessage)
                {
                    return Validate((FooMessage)message.Payload);
                }
                else
                {
                    return new ValidationResult(new BrokenRule("should be BooMessage"));
                }
            }

            private ValidationResult Validate(FooMessage booMessage)
            {
                if (booMessage.Num < 100)
                {
                    return ValidationResult.Valid;
                }
                else
                {
                    return new ValidationResult(new BrokenRule("should be less than 100"));
                }
            }
        }

        internal class ServiceLocator
        {
            private readonly Dictionary<Type, Dictionary<string, Func<ServiceLocator, object>>> creators = new Dictionary<Type, Dictionary<string, Func<ServiceLocator, object>>>();

            public void Add(Type type, string name, Func<ServiceLocator, object> creator)
            {
                if (!this.creators.ContainsKey(type))
                {
                    this.creators[type] = new Dictionary<string, Func<ServiceLocator, object>>();
                }
                this.creators[type][name] = creator;
            }

            public object Get(Type type, string name)
            {
                if (this.creators.ContainsKey(type))
                {
                    if (this.creators[type].ContainsKey(name))
                    {
                        return this.creators[type][name](this);
                    }
                }
                return null;
            }

        }

        /// <summary>
        /// The when_configuring_endpoint_with_lifecycle_handler.
        /// </summary>
        [TestFixture]
        [Category("Integration")]
        public class when_configuring_endpoint_with_lifecycle_handler : RabbitMqFixture
        {
            /// <summary>
            /// The should_handle_state_changes.
            /// </summary>
            [Test]
            public void should_handle_state_changes()
            {
                string producerConfig = string.Format(
                    @"<endpoints>
                        <endpoint name=""producer"" connectionString=""{0}{1}"" lifecycleHandler=""ProducerHandler"">
                            <outgoing>
                                <route key=""a"" label=""msg.a"" />
                            </outgoing>
                        </endpoint>
                    </endpoints>",
                    this.Url,
                    this.VhostName);

                var handler = new Mock<IBusLifecycleHandler>();

                var serviceLocator = new ServiceLocator();
                serviceLocator.Add(
                        typeof(IBusLifecycleHandler),
                        "ProducerHandler",
                        sc => handler.Object);

                DependencyResolverFunc dependencyResolver = (name, type) => serviceLocator.Get(type, name);

                IBus producer = this.StartBus(
                    "producer", 
                    cfg =>
                        {
                            var section = new XmlEndpointsSection(producerConfig);
                            new AppConfigConfigurator(section, dependencyResolver).Configure("producer", cfg);
                        });

                handler.Verify(h => h.OnStarting(It.IsAny<IBus>(), It.IsAny<EventArgs>()), Times.Once);
                handler.Verify(h => h.OnStarted(It.IsAny<IBus>(), It.IsAny<EventArgs>()), Times.Once);
                handler.Verify(h => h.OnStopping(It.IsAny<IBus>(), It.IsAny<EventArgs>()), Times.Never);
                handler.Verify(h => h.OnStopped(It.IsAny<IBus>(), It.IsAny<EventArgs>()), Times.Never);

                producer.Stop();

                handler.Verify(h => h.OnStopping(It.IsAny<IBus>(), It.IsAny<EventArgs>()), Times.Once);
                handler.Verify(h => h.OnStopped(It.IsAny<IBus>(), It.IsAny<EventArgs>()), Times.Once);
            }
        }

        /// <summary>
        /// The when_declaring_delegated_consumer.
        /// </summary>
        [TestFixture]
        [Category("Integration")]
        public class when_declaring_delegated_consumer : RabbitMqFixture
        {
            /// <summary>
            /// The should_build_consumer_each_time.
            /// </summary>
            [Test]
            public void should_build_consumer_each_time()
            {
                string producerConfig = string.Format(
                    @"<endpoints>
                        <endpoint name=""producer"" connectionString=""{0}{1}"">
                            <outgoing>
                                <route key=""a"" label=""msg.a"" />
                                <route key=""b"" label=""msg.b"" />
                            </outgoing>
                        </endpoint>
                    </endpoints>",
                    this.Url,
                    this.VhostName);

                string consumerConfig = string.Format(
                    @"<endpoints>
                            <endpoint name=""consumer"" connectionString=""{0}{1}"">
                                <incoming>
                                    <on key=""a"" label=""msg.a"" react=""BooHandler"" type=""BooMessage"" lifestyle=""Delegated"" />
                                    <on key=""b"" label=""msg.b"" react=""BooTransformer"" type=""BooMessage"" lifestyle=""Delegated"" />
                                </incoming>
                            </endpoint>
                        </endpoints>",
                    this.Url,
                    this.VhostName);

                BusDependentHandler.Reset();
                BusDependentTransformer.Reset();

                var serviceLocator = new ServiceLocator();
                DependencyResolverFunc dependencyResolver = (name, type) => serviceLocator.Get(type, name);

                IBus consumer = this.StartBus(
                    "consumer",
                    cfg =>
                    {
                        var section = new XmlEndpointsSection(consumerConfig);
                        new AppConfigConfigurator(section, dependencyResolver).Configure("consumer", cfg);
                    });

                serviceLocator.Add(
                    typeof(IConsumerOf<BooMessage>),
                    "BooHandler",
                    sc => new BusDependentHandler((IBus)sc.Get(typeof(IBus), "consumer")));

                serviceLocator.Add(
                    typeof(IMessageOperator),
                    "IMessageOperator",
                    sc => new BusDependentTransformer((IBus)sc.Get(typeof(IBus), "consumer")));

                serviceLocator.Add(
                    typeof(IConsumerOf<BooMessage>),
                    "BooTransformer",
                    sc => new OperatorConsumerOf<BooMessage>((IMessageOperator) sc.Get(typeof(IMessageOperator), "IMessageOperator")));

                IBus producer = this.StartBus(
                    "producer", 
                    cfg =>
                        {
                            var section = new XmlEndpointsSection(producerConfig);
                            new AppConfigConfigurator(section, dependencyResolver).Configure("producer", cfg);
                        });

                producer.Emit("msg.a", new { Num = 13 });
                producer.Emit("msg.a", new { Num = 13 });
                producer.Emit("msg.a", new { Num = 13 });

                BusDependentHandler.WaitEvent.Wait(5.Seconds()).Should().BeTrue();
                BusDependentHandler.BuildCount.Should().Be(3);

                producer.Emit("msg.b", new { Num = 13 });
                producer.Emit("msg.b", new { Num = 13 });
                producer.Emit("msg.b", new { Num = 13 });

                BusDependentTransformer.WaitEvent.Wait(5.Seconds()).Should().BeTrue();
                BusDependentTransformer.BuildCount.Should().Be(3);
            }
        }

        /// <summary>
        /// The when_declaring_lazy_consumer.
        /// </summary>
        [TestFixture]
        [Category("Integration")]
        public class when_declaring_lazy_consumer : RabbitMqFixture
        {
            /// <summary>
            /// The should_build_consumer_once.
            /// </summary>
            [Test]
            public void should_build_consumer_once()
            {
                string producerConfig = string.Format(
                    @"<endpoints>
                            <endpoint name=""producer"" connectionString=""{0}{1}"">
                                <outgoing>
                                    <route key=""a"" label=""msg.a"" />
                                    <route key=""b"" label=""msg.b"" />
                                </outgoing>
                            </endpoint>
                        </endpoints>",
                    this.Url,
                    this.VhostName);

                string consumerConfig = string.Format(
                    @"<endpoints>
                            <endpoint name=""consumer"" connectionString=""{0}{1}"">
                                <incoming>
                                    <on key=""a"" label=""msg.a"" react=""BooHandler"" type=""BooMessage"" lifestyle=""Lazy"" />
                                    <on key=""b"" label=""msg.b"" react=""BooTransformer"" type=""BooMessage"" lifestyle=""Lazy"" />
                                </incoming>
                            </endpoint>
                        </endpoints>",
                    this.Url,
                    this.VhostName);

                BusDependentHandler.Reset();
                BusDependentTransformer.Reset();

                var serviceLocator = new ServiceLocator();
                DependencyResolverFunc dependencyResolver = (name, type) => serviceLocator.Get(type, name);

                IBus consumer = this.StartBus(
                    "consumer",
                    cfg =>
                    {
                        var section = new XmlEndpointsSection(consumerConfig);
                        new AppConfigConfigurator(section, dependencyResolver).Configure("consumer", cfg);
                    });

                serviceLocator.Add(
                        typeof(IBus),
                        "consumer",
                        sc => consumer);

                serviceLocator.Add(
                    typeof(IConsumerOf<BooMessage>),
                    "BooHandler",
                    sc => new BusDependentHandler((IBus)sc.Get(typeof(IBus), "consumer")));

                serviceLocator.Add(
                    typeof(IMessageOperator),
                    "IMessageOperator",
                    sc => new BusDependentTransformer((IBus)sc.Get(typeof(IBus), "consumer")));

                serviceLocator.Add(
                    typeof(IConsumerOf<BooMessage>),
                    "BooTransformer",
                    sc => new OperatorConsumerOf<BooMessage>((IMessageOperator)sc.Get(typeof(IMessageOperator), "IMessageOperator")));

                IBus producer = this.StartBus(
                    "producer", 
                    cfg =>
                        {
                            var section = new XmlEndpointsSection(producerConfig);
                            new AppConfigConfigurator(section, dependencyResolver).Configure("producer", cfg);
                        });

                producer.Emit("msg.a", new { Num = 13 });
                producer.Emit("msg.a", new { Num = 13 });
                producer.Emit("msg.a", new { Num = 13 });

                BusDependentHandler.WaitEvent.Wait(5.Seconds()).
                    Should().
                    BeTrue();
                BusDependentHandler.BuildCount.Should().
                    Be(1);

                producer.Emit("msg.b", new { Num = 13 });
                producer.Emit("msg.b", new { Num = 13 });
                producer.Emit("msg.b", new { Num = 13 });

                BusDependentTransformer.WaitEvent.Wait(5.Seconds()).
                    Should().
                    BeTrue();
                BusDependentTransformer.BuildCount.Should().
                    Be(1);
            }
        }

        /// <summary>
        /// The when_receiving_with_configured_concrete_consumer.
        /// </summary>
        [TestFixture]
        [Category("Integration")]
        public class when_receiving_with_configured_concrete_consumer : RabbitMqFixture
        {
            /// <summary>
            /// The should_receive.
            /// </summary>
            [Test]
            public void should_receive()
            {
                string producerConfig = string.Format(
                    @"<endpoints>
                            <endpoint name=""producer"" connectionString=""{0}{1}"">
                                <outgoing>
                                    <route key=""a"" label=""msg.a"" />
                                </outgoing>
                            </endpoint>
                        </endpoints>",
                    this.Url,
                    this.VhostName);

                string consumerConfig = string.Format(
                    @"<endpoints>
                            <endpoint name=""consumer"" connectionString=""{0}{1}"">
                                <incoming>
                                    <on key=""a"" label=""msg.a"" react=""BooHandler"" type=""BooMessage"" />
                                </incoming>
                            </endpoint>
                        </endpoints>",
                    this.Url,
                    this.VhostName);

                var handler = new ConcreteHandlerOf<BooMessage>();

                var serviceLocator = new ServiceLocator();

                
                serviceLocator.Add(typeof(IConsumerOf<BooMessage>), "BooHandler", sc => handler);

                DependencyResolverFunc dependencyResolver = (name, type) => serviceLocator.Get(type, name);

                this.StartBus(
                    "consumer", 
                    cfg =>
                        {
                            var section = new XmlEndpointsSection(consumerConfig);
                            new AppConfigConfigurator(section, dependencyResolver).Configure("consumer", cfg);
                        });

                IBus producer = this.StartBus(
                    "producer", 
                    cfg =>
                        {
                            var section = new XmlEndpointsSection(producerConfig);
                            new AppConfigConfigurator(section, dependencyResolver).Configure("producer", cfg);
                        });

                producer.Emit("msg.a", new { Num = 13 });
                handler.Received.WaitOne(5.Seconds()).Should().BeTrue();
            }
        }

        /// <summary>
        /// The when_receiving_with_configured_dynamic_consumer.
        /// </summary>
        [TestFixture]
        [Category("Integration")]
        public class when_receiving_with_configured_dynamic_consumer : RabbitMqFixture
        {
            /// <summary>
            /// The should_receive.
            /// </summary>
            [Test]
            public void should_receive()
            {
                string producerConfig = string.Format(
                        @"<endpoints>
                            <endpoint name=""producer"" connectionString=""{0}{1}"">
                                <outgoing>
                                    <route key=""a"" label=""msg.a"" />
                                </outgoing>
                            </endpoint>
                        </endpoints>",
                        this.Url,
                        this.VhostName);

                string consumerConfig = string.Format(
                        @"<endpoints>
                            <endpoint name=""consumer"" connectionString=""{0}{1}"">
                                <incoming>
                                    <on key=""a"" label=""msg.a"" react=""DynamicHandler"" />
                                </incoming>
                            </endpoint>
                        </endpoints>",
                        this.Url,
                        this.VhostName);

                var handler = new ConcreteHandlerOf<ExpandoObject>();

                var serviceLocator = new ServiceLocator();

                serviceLocator.Add(typeof(IConsumerOf<ExpandoObject>), "DynamicHandler", sc => handler);

                DependencyResolverFunc dependencyResolver = (name, type) => serviceLocator.Get(type, name);

                this.StartBus(
                    "consumer", 
                    cfg =>
                        {
                            var section = new XmlEndpointsSection(consumerConfig);
                            new AppConfigConfigurator(section, dependencyResolver).Configure("consumer", cfg);
                        });

                IBus producer = this.StartBus(
                    "producer", 
                    cfg =>
                        {
                            var section = new XmlEndpointsSection(producerConfig);
                            new AppConfigConfigurator(section, dependencyResolver).Configure("producer", cfg);
                        });

                producer.Emit("msg.a", new { This = "That" });

                handler.Received.WaitOne(5.Seconds()).
                    Should().
                    BeTrue();
            }
        }

        /// <summary>
        /// The when_receiving_with_configured_validator_on_global_declaration.
        /// </summary>
        [TestFixture]
        [Category("Integration")]

        // [Ignore("WIP")]
        public class when_receiving_with_configured_validator_on_global_declaration : RabbitMqFixture
        {
            /// <summary>
            /// The should_validate.
            /// </summary>
            [Test]
            public void should_validate()
            {
                string producerConfig = string.Format(
                        @"<endpoints>
                            <endpoint name=""producer"" connectionString=""{0}{1}"">
                                <outgoing>
                                    <route key=""a"" label=""msg.a"" />
                                </outgoing>
                            </endpoint>
                        </endpoints>",
                        this.Url,
                        this.VhostName);

                string consumerConfig = string.Format(
                        @"<endpoints>
                            <endpoint name=""consumer"" connectionString=""{0}{1}"">
                                <validators>
                                    <add name=""ValidatorGroup"" group=""true"" />
                                </validators>
                                <incoming>
                                    <on key=""a"" label=""msg.a"" react=""BooHandler"" type=""BooMessage"" />
                                </incoming>
                            </endpoint>
                        </endpoints>",
                        this.Url,
                        this.VhostName);

                var handler = new ConcreteHandlerOf<BooMessage>();

                var serviceLocator = new ServiceLocator();
                
                serviceLocator.Add(typeof(IConsumerOf<BooMessage>), "BooHandler", sc => handler);
                serviceLocator.Add(typeof(IMessageValidator), "BooMessageValidator", sc => new BooPayloadValidator());
                serviceLocator.Add(typeof(IMessageValidator), "FooMessageValidator", sc => new FooPayloadValidator());
                serviceLocator.Add(
                    typeof(MessageValidatorGroup), 
                    "ValidatorGroup", 
                    sc => new MessageValidatorGroup(
                        new List<IMessageValidator> {
                            (IMessageValidator)sc.Get(typeof(IMessageValidator), "BooMessageValidator"),
                            (IMessageValidator)sc.Get(typeof(IMessageValidator), "FooMessageValidator")
                        }));

                DependencyResolverFunc dependencyResolver = (name, type) => serviceLocator.Get(type, name);

                this.StartBus(
                    "consumer", 
                    cfg =>
                        {
                            var section = new XmlEndpointsSection(consumerConfig);
                            new AppConfigConfigurator(section, dependencyResolver).Configure("consumer", cfg);
                        });

                IBus producer = this.StartBus(
                    "producer", 
                    cfg =>
                        {
                            var section = new XmlEndpointsSection(producerConfig);
                            new AppConfigConfigurator(section, dependencyResolver).Configure("producer", cfg);
                        });

                producer.Emit("msg.a", new { Num = 13 });

                handler.Received.WaitOne(3.Seconds()).
                    Should().
                    BeFalse();
            }
        }

        /// <summary>
        /// The when_receiving_with_configured_validator_on_incoming_declaration.
        /// </summary>
        [TestFixture]
        [Category("Integration")]
        public class when_receiving_with_configured_validator_on_incoming_declaration : RabbitMqFixture
        {
            /// <summary>
            /// The should_validate.
            /// </summary>
            [Test]
            public void should_validate()
            {
                string producerConfig = string.Format(
                        @"<endpoints>
                            <endpoint name=""producer"" connectionString=""{0}{1}"">
                                <outgoing>
                                    <route key=""a"" label=""msg.a"" />
                                </outgoing>
                            </endpoint>
                        </endpoints>",
                        this.Url,
                        this.VhostName);

                string consumerConfig = string.Format(
                        @"<endpoints>
                            <endpoint name=""consumer"" connectionString=""{0}{1}"">
                                <incoming>
                                    <on key=""a"" label=""msg.a"" react=""BooHandler"" type=""BooMessage"" validate=""BooValidator"" />
                                </incoming>
                            </endpoint>
                        </endpoints>",
                        this.Url,
                        this.VhostName);

                var handler = new ConcreteHandlerOf<BooMessage>();

                var serviceLocator = new ServiceLocator();
                serviceLocator.Add(typeof(IConsumerOf<BooMessage>), "BooHandler", sc => handler);
                serviceLocator.Add(typeof(IMessageValidatorOf<BooMessage>), "BooValidator", sc => new BooPayloadValidator());

                DependencyResolverFunc dependencyResolver = (name, type) => serviceLocator.Get(type, name);

                this.StartBus(
                    "consumer", 
                    cfg =>
                        {
                            var section = new XmlEndpointsSection(consumerConfig);
                            new AppConfigConfigurator(section, dependencyResolver).Configure("consumer", cfg);
                        });

                IBus producer = this.StartBus(
                    "producer", 
                    cfg =>
                        {
                            var section = new XmlEndpointsSection(producerConfig);
                            new AppConfigConfigurator(section, dependencyResolver).Configure("producer", cfg);
                        });

                producer.Emit("msg.a", new { Num = 13 });

                handler.Received.WaitOne(3.Seconds())
                    .Should()
                    .BeFalse();
            }
        }

        /// <summary>
        /// The when_transforming_with_configured_concrete_transformer.
        /// </summary>
        [TestFixture]
        [Category("Integration")]
        public class when_transforming_with_configured_concrete_transformer : RabbitMqFixture
        {
            /// <summary>
            /// The should_receive.
            /// </summary>
            [Test]
            public void should_receive()
            {
                string producerConfig = string.Format(
                        @"<endpoints>
                            <endpoint name=""producer"" connectionString=""{0}{1}"">
                                <outgoing>
                                    <route key=""a"" label=""msg.a"" />
                                </outgoing>
                            </endpoint>
                        </endpoints>",
                        this.Url,
                        this.VhostName);

                string consumerConfig = string.Format(
                        @"<endpoints>
                            <endpoint name=""consumer"" connectionString=""{0}{1}"">
                                <incoming>
                                    <on key=""a"" label=""msg.a"" react=""BooTransformer"" type=""BooMessage"" />
                                </incoming>
                            </endpoint>
                        </endpoints>",
                        this.Url,
                        this.VhostName);

                var handler = new ConcreteTransformerOf<BooMessage>();
                var serviceLocator = new ServiceLocator();
                serviceLocator.Add(typeof(IConsumerOf<BooMessage>),
                    "BooTransformer", 
                    sc => new OperatorConsumerOf<BooMessage>(handler));

                object dependencyResolver(string name, Type type) => serviceLocator.Get(type, name);

                this.StartBus(
                    "consumer", 
                    cfg =>
                        {
                            var section = new XmlEndpointsSection(consumerConfig);
                            new AppConfigConfigurator(section, dependencyResolver).Configure("consumer", cfg);
                        });

                IBus producer = this.StartBus(
                    "producer", 
                    cfg =>
                        {
                            var section = new XmlEndpointsSection(producerConfig);
                            new AppConfigConfigurator(section, dependencyResolver).Configure("producer", cfg);
                        });

                producer.Emit("msg.a", new { Num = 13 });

                handler.Received.WaitOne(5.Seconds()).Should().BeTrue();
            }
        }

        [TestFixture]
        [Category("Integration")]
        public class when_configuring_message_headers : RabbitMqFixture
        {
            /// <summary>
            /// The should_receive.
            /// </summary>
            [Test]
            public void should_not_copy_incoming_message_headers_on_send()
            {
                var producerMessage = "excluded.msg.a";
                var consumerMessage = "excluded.msg.b";

                var excludedHeaders = new[] { "bad-header", "x-bad-header" };

                var bus = new BusFactory().Create(
                    c =>
                    {
                        c.UsePayloadConverter(new JsonNetPayloadConverter());
                        c.SetConnectionString(this.ConnectionString);
                        c.SetExcludedIncomingHeaders(excludedHeaders);
                        c.SetEndpoint("consumer");
                        c.Route(consumerMessage);
                        c.On<ExpandoObject>(producerMessage)
                            .ReactWith((m, ctx) => ctx.Bus.Emit(consumerMessage, m));
                    });
                bus.WhenReady.WaitOne(5.Seconds());


                IBus producer = this.ConfigureBus("producer", cfg => cfg.Route(MessageLabel.From(producerMessage)));
                producer.Start();
                producer.WhenReady.WaitOne(5.Seconds());
                
                var handler = new ManualResetEventSlim();
                IDictionary<string, object> headers = null;

                IBus trap = this.ConfigureBus(
                    "trap",
                    cfg => cfg.On<ExpandoObject>(consumerMessage)
                               .ReactWith(
                                   (eo, ctx) =>
                                       {
                                           headers = ctx.Message.Headers;
                                           handler.Set();
                                       }));
                trap.Start();
                trap.WhenReady.WaitOne(10.Seconds());

                producer.Emit(
                    MessageLabel.From(producerMessage),
                    new
                        {
                            Num = 13
                        },
                    new Dictionary<string, object>
                        {
                            ["bad-header"] = "bad-header",
                            ["x-bad-header"] = "x-bad-header",
                            ["good-header"] = "good-header"
                        }).Wait(5.Seconds());

                Assert.IsTrue(handler.Wait(5.Seconds()));
                Assert.IsNotNull(headers);
                Assert.IsFalse(headers.ContainsKey("bad-header"));
                Assert.IsFalse(headers.ContainsKey("x-bad-header"));
                Assert.IsTrue(headers.ContainsKey("good-header"));
            }
        }

        /// <summary>
        /// При указании конечной точки.
        /// </summary>
        [TestFixture]
        [Category("Unit")]
        public class when_configuring_endpoint
        {
            /// <summary>
            /// Можно установить значение QoS.
            /// </summary>
            [Test]
            public void should_set_qos()
            {
                const string ProducerConfig = 
                        @"<endpoints>
                            <endpoint name=""producer"" connectionString=""amqp://localhost/integration"" lifecycleHandler=""ProducerHandler"">
                                <qos prefetchCount=""8"" />
                            </endpoint>
                        </endpoints>";

                Mock<IDependencyResolver> dependencyResoverMock = new Mock<IDependencyResolver>();
                var busConfigurator = new BusConfiguration();
                busConfigurator.UseRabbitMq();

                var section = new XmlEndpointsSection(ProducerConfig);
                var sut = new AppConfigConfigurator(section, dependencyResoverMock.Object);
                var result = sut.Configure("producer", busConfigurator);

                RabbitReceiverOptions rabbitReceiverOptions = ((BusConfiguration)result).ReceiverDefaults as RabbitReceiverOptions;
                Assert.IsNotNull(rabbitReceiverOptions, "Долны быть установлены настройки получателя.");
                Maybe<QoSParams> qosMaybe = rabbitReceiverOptions.GetQoS();
                Assert.IsTrue(qosMaybe.HasValue, "QoS должен быть установлен.");
                Assert.AreEqual(8, qosMaybe.Value.PrefetchCount, "Должно быть установлено количество потоков.");
            }

            /// <summary>
            /// Можно использовать значение по умолчанию.
            /// </summary>
            [Test]
            public void should_be_default()
            {
                const string ProducerConfig =
                        @"<endpoints>
                            <endpoint name=""producer"" connectionString=""amqp://localhost/integration"" lifecycleHandler=""ProducerHandler"">
                            </endpoint>
                        </endpoints>";

                Mock<IDependencyResolver> dependencyResoverMock = new Mock<IDependencyResolver>();

                var section = new XmlEndpointsSection(ProducerConfig);
                var sut = new AppConfigConfigurator(section, dependencyResoverMock.Object);

                using (var bus = new BusFactory().Create(cfg => 
                {
                    cfg.UsePayloadConverter(new JsonNetPayloadConverter());
                    sut.Configure("producer", cfg);
                }, false))
                {
                    RabbitReceiverOptions rabbitReceiverOptions = ((BusConfiguration)bus.Configuration).ReceiverDefaults as RabbitReceiverOptions;
                    Assert.IsNotNull(rabbitReceiverOptions, "Долны быть установлены настройки получателя.");
                    Maybe<QoSParams> qosMaybe = rabbitReceiverOptions.GetQoS();
                    Assert.IsTrue(qosMaybe.HasValue, "QoS должен быть установлен.");
                    Assert.AreEqual(50, qosMaybe.Value.PrefetchCount, "Должно быть установлено количество потоков.");
                }
            }

            [Test]
            public void should_set_excluded_headers()
            {
                var headers = new[] { "bad-header", "banned-header" };
                string ProducerConfig =
                        $@"<endpoints>
                            <endpoint name=""producer"" connectionString=""amqp://localhost/integration"" excludedHeaders=""{string.Join(",", headers)}"">
                            </endpoint>
                        </endpoints>";

                Mock<IDependencyResolver> dependencyResoverMock = new Mock<IDependencyResolver>();
                var busConfiguratorMoq = new Mock<IBusConfigurator>();

                var section = new XmlEndpointsSection(ProducerConfig);
                var sut = new AppConfigConfigurator(section, dependencyResoverMock.Object);
                sut.Configure("producer", busConfiguratorMoq.Object);
                
                busConfiguratorMoq.Verify(
                    bcf => bcf.SetExcludedIncomingHeaders(
                        It.Is<IEnumerable<string>>(
                            e => e.OrderBy(_ => _)
                                     .SequenceEqual(headers.OrderBy(_ => _)))), 
                    Times.Once);
            }

            [Test]
            public void should_set_message_header_storage()
            {
                var configuration = new BusConfiguration();
                var storage = new MessageHeaderStorage(Enumerable.Empty<string>());
                configuration.UseIncomingMessageHeaderStorage(storage);

                var endpointStorage = configuration.EndpointOptions.GetIncomingMessageHeaderStorage();
                endpointStorage.HasValue.Should().BeTrue();
                endpointStorage.Value.Should().Be(storage);
            }
        }

        /// <summary>
        /// Если в конфигурации установлено количество обработчиков сообщений из очередей конечной точки.
        /// </summary>
        [TestFixture]
        [Category("Unit")]
        public class when_configuring_endpoint_with_parallelism_level
        {
            /// <summary>
            /// Тогда это значение должно быть использовано при конфигурации.
            /// </summary>
            [Test]
            public void should_set_parallelismLevel()
            {
                const string ProducerConfig =
                        @"<endpoints>
                            <endpoint name=""producer"" connectionString=""amqp://localhost/integration"" parallelismLevel=""8"">
                                <qos prefetchCount=""8"" />
                            </endpoint>
                        </endpoints>";

                Mock<IDependencyResolver> dependencyResoverMock = new Mock<IDependencyResolver>();
                var busConfigurator = new BusConfiguration();
                busConfigurator.UseRabbitMq();

                var section = new XmlEndpointsSection(ProducerConfig);
                var sut = new AppConfigConfigurator(section, dependencyResoverMock.Object);
                var result = sut.Configure("producer", busConfigurator);

                ReceiverOptions receiverOptions = ((BusConfiguration)result).ReceiverDefaults;
                Assert.IsTrue(receiverOptions.GetParallelismLevel().HasValue, "Должно быть установлено количество обработчиков.");
                Assert.AreEqual(8, receiverOptions.GetParallelismLevel().Value, "Должно быть установлено количество обработчиков.");
            }
        }

        /// <summary>
        /// Если в конфигурации время хранения сообщений в Fault очереди.
        /// </summary>
        [TestFixture]
        [Category("Unit")]
        public class when_configuring_endpoint_with_fault_queue_ttl
        {
            /// <summary>
            /// Тогда это значение должно быть использовано при конфигурации.
            /// </summary>
            [Test]
            public void should_set_queue_ttl()
            {
                const string ProducerConfig =
                        @"<endpoints>
                            <endpoint name=""producer"" connectionString=""amqp://localhost/integration"" faultQueueTtl=""10:10:00"">
                            </endpoint>
                        </endpoints>";

                Mock<IDependencyResolver> dependencyResoverMock = new Mock<IDependencyResolver>();
                var busConfigurator = new BusConfiguration();

                var section = new XmlEndpointsSection(ProducerConfig);
                var sut = new AppConfigConfigurator(section, dependencyResoverMock.Object);
                var result = sut.Configure("producer", busConfigurator);

                ReceiverOptions receiverOptions = ((BusConfiguration)result).ReceiverDefaults;
                Assert.IsTrue(receiverOptions.GetFaultQueueTtl().HasValue, "Должно быть установлено время хранения сообщений.");
                Assert.AreEqual(TimeSpan.Parse("10:10:00"), receiverOptions.GetFaultQueueTtl().Value, "Должно быть устрановлено корректное время хранения.");
            }

            /// <summary>
            /// Тогда это значение не должно быть установлено по умолчанию.
            /// </summary>
            [Test]
            public void should_not_be_set_by_default()
            {
                const string ProducerConfig =
                        @"<endpoints>
                            <endpoint name=""producer"" connectionString=""amqp://localhost/integration"">
                            </endpoint>
                        </endpoints>";

                Mock<IDependencyResolver> dependencyResoverMock = new Mock<IDependencyResolver>();
                var busConfigurator = new BusConfiguration();

                var section = new XmlEndpointsSection(ProducerConfig);
                var sut = new AppConfigConfigurator(section, dependencyResoverMock.Object);
                var result = sut.Configure("producer", busConfigurator);

                ReceiverOptions receiverOptions = ((BusConfiguration)result).ReceiverDefaults;
                Assert.IsFalse(receiverOptions.GetFaultQueueTtl().HasValue, "Не должно быть установлено время хранения сообщений.");
            }
        }

        /// <summary>
        /// Если в конфигурации ограничено количество сообщений в Fault очереди.
        /// </summary>
        [TestFixture]
        [Category("Unit")]
        public class when_configuring_endpoint_with_fault_queue_limit
        {
            /// <summary>
            /// Тогда это значение должно быть использовано при конфигурации.
            /// </summary>
            [Test]
            public void should_set_queue_limit()
            {
                const int queueLimit = 100;
                string producerConfig = string.Format(
                        @"<endpoints>
                            <endpoint name=""producer"" connectionString=""amqp://localhost/integration"" faultQueueLimit=""{0}"">
                            </endpoint>
                        </endpoints>", 
                        queueLimit);

                Mock<IDependencyResolver> dependencyResoverMock = new Mock<IDependencyResolver>();
                var busConfigurator = new BusConfiguration();

                var section = new XmlEndpointsSection(producerConfig);
                var sut = new AppConfigConfigurator(section, dependencyResoverMock.Object);
                var result = sut.Configure("producer", busConfigurator);

                ReceiverOptions receiverOptions = ((BusConfiguration)result).ReceiverDefaults;
                Assert.IsTrue(receiverOptions.GetFaultQueueLimit().HasValue, "Должно быть установлено максимальное количество сообщений.");
                Assert.AreEqual(queueLimit, receiverOptions.GetFaultQueueLimit().Value, "Должно быть устрановлено корректное максимальное количество сообщений.");
            }

            /// <summary>
            /// Тогда это значение не должно быть установлено по умолчанию.
            /// </summary>
            [Test]
            public void should_not_be_set_by_default()
            {
                string producerConfig = 
                        @"<endpoints>
                            <endpoint name=""producer"" connectionString=""amqp://localhost/integration"">
                            </endpoint>
                        </endpoints>";

                Mock<IDependencyResolver> dependencyResoverMock = new Mock<IDependencyResolver>();
                var busConfigurator = new BusConfiguration();

                var section = new XmlEndpointsSection(producerConfig);
                var sut = new AppConfigConfigurator(section, dependencyResoverMock.Object);
                var result = sut.Configure("producer", busConfigurator);

                ReceiverOptions receiverOptions = ((BusConfiguration)result).ReceiverDefaults;
                Assert.IsFalse(receiverOptions.GetFaultQueueLimit().HasValue, "Не должно быть установлено максимальное количество сообщений.");

            }
        }

        /// <summary>
        /// Если в конфигурации динамическая исходящая маршрутизация.
        /// </summary>
        [TestFixture]
        [Category("Unit")]
        public class when_configuring_endpoint_with_dynamic_outgoing_routing
        {
            /// <summary>
            /// Тогда это значение должно быть использовано при конфигурации.
            /// </summary>
            [Test]
            public void should_route_any()
            {
                const string ProducerConfig =
                        @"<endpoints>
                            <endpoint name=""producer"" connectionString=""amqp://localhost/integration"" >
                                <dynamic outgoing=""true"" />
                            </endpoint>
                        </endpoints>";

                Mock<IDependencyResolver> dependencyResoverMock = new Mock<IDependencyResolver>();
                var section = new XmlEndpointsSection(ProducerConfig);
                var sut = new AppConfigConfigurator(section, dependencyResoverMock.Object);

                using (var bus = new BusFactory().Create(cfg =>
                {
                    cfg.UsePayloadConverter(new JsonNetPayloadConverter());
                    sut.Configure("producer", cfg);
                }, false))
                {
                    Assert.IsTrue(bus.CanRoute(MessageLabel.Any), "Должна быть включена динамическая маршрутизация.");
                }
            }
        }

        [TestFixture]
        [Category("Unit")]
        public class when_configuring_endpoint_with_connection_string
        {
            [Test]
            public void should_not_throw_on_validate_if_not_present()
            {
                const string name = "name";
                string Config = $@"<endpoints>
                                       <endpoint name=""{name}"" />
                                   </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var busConfigurator = new BusConfiguration();

                var section = new XmlEndpointsSection(Config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);
                var configuration = (BusConfiguration)configurator.Configure(name, busConfigurator);

                busConfigurator.BuildBusUsing(bc => new Mock<IBus>().Object);
                busConfigurator.UsePayloadConverter(new Mock<IPayloadConverter>().Object);
                busConfigurator.Route("label");

                Action validate = () => configuration.Validate();
                validate.Should().NotThrow("Connection string may not be specified");
            }

            [Test]
            public void should_set_connection_string_if_present()
            {
                const string name = "name";
                string Config = $@"<endpoints>
                                       <endpoint name=""{name}"" connectionString=""amqp://localhost/integration"" />
                                   </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var busConfigurator = new BusConfiguration();

                var section = new XmlEndpointsSection(Config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);
                var configuration = (BusConfiguration)configurator.Configure(name, busConfigurator);

                configuration.ConnectionString.Should().NotBeNullOrEmpty();
                configuration.EndpointOptions.GetConnectionString().Should().NotBeNull();
            }
        }

        [TestFixture]
        [Category("Unit")]
        public class when_configuring_endpoint_with_connection_string_provider
        {
            [Test]
            public void should_load_connection_string_provider_if_present()
            {
                const string name = "name";
                const string provider = "provider";
                string Config = $@"<endpoints>
                                       <endpoint name=""{name}"" connectionStringProvider=""{provider}"" />
                                   </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var busConfigurator = new BusConfiguration();

                var section = new XmlEndpointsSection(Config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);
                var configuration = (BusConfiguration)configurator.Configure(name, busConfigurator);

                resolverMock.Verify(
                    rm => rm.Resolve(
                        It.Is<string>(value => value == provider),
                        It.Is<Type>(type => type == typeof(IConnectionStringProvider))),
                    Times.Once,
                    "Should use a dependency resolver to load the connection string provider implementation.");
            }

            [Test]
            public void should_not_load_connection_string_provider_if_not_present()
            {
                const string name = "name";
                string Config = $@"<endpoints>
                                       <endpoint name=""{name}"" />
                                   </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var busConfigurator = new BusConfiguration();

                var section = new XmlEndpointsSection(Config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);
                var configuration = (BusConfiguration)configurator.Configure(name, busConfigurator);

                resolverMock.Verify(
                    rm => rm.Resolve(
                        It.IsAny<string>(),
                        It.Is<Type>(type => type == typeof(IConnectionStringProvider))),
                    Times.Never,
                    "Should not use a dependency resolver to load the connection string provider implementation.");
            }

            [Test]
            public void should_use_endpoint_connection_string_even_if_provider_is_present()
            {
                const string Name = "name";
                const string SomeString = "someString";
                string Config = 
                    $@"<endpoints>
                        <endpoint 
                            name=""{Name}"" 
                            connectionString=""{SomeString}"" 
                            connectionStringProvider=""provider"" />
                    </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var busConfigurator = new BusConfiguration();

                var section = new XmlEndpointsSection(Config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);
                var configuration = (BusConfiguration)configurator.Configure(Name, busConfigurator);

                Assert.AreEqual(SomeString, configuration.EndpointOptions.GetConnectionString().Value, "Should use a connection string from the endpoint connectionstring attribute.");
            }

            [Test]
            public void should_set_connection_string_to_outgoing_label_from_provider_if_present()
            {
                const string Name = "name";
                const string SomeString = "someString";
                const string AnotherString = "another string";
                const string Provider = "provider";
                const string Label = "msg.a";
                string Config =
                    $@"<endpoints>
                        <endpoint 
                            name=""{Name}"" 
                            connectionStringProvider=""{Provider}"" >
                            <outgoing>
                                <route key=""a"" label=""{Label}"" />
                            </outgoing>
                        </endpoint>
                
                    </endpoints>";

                var connectionStringProviderMock = new Mock<IConnectionStringProvider>();
                connectionStringProviderMock
                    .Setup(cspm => cspm.GetConnectionString(It.Is<MessageLabel>(l => l.Name == Label)))
                    .Returns(AnotherString);

                var resolverMock = new Mock<IDependencyResolver>();
                resolverMock.Setup(
                    rm => rm.Resolve(
                        It.Is<string>(value => value == Provider),
                        It.Is<Type>(t => t == typeof(IConnectionStringProvider))))
                    .Returns(connectionStringProviderMock.Object);


                var section = new XmlEndpointsSection(Config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq();

                var configuration = (BusConfiguration)configurator.Configure(Name, busConfiguration);

                var senderConfiguration = configuration.SenderConfigurations.First(sc => sc.Label.Equals(MessageLabel.From(Label)));

                var senderOptions = (RabbitSenderOptions)senderConfiguration.Options;
                senderOptions.GetConnectionString().Value.Should().Be(AnotherString, "Should use a connection string from the provider for outgoing label.");
            }

            [Test]
            public void should_override_connection_string_to_outgoing_label_from_provider_if_present()
            {
                const string Name = "name";
                const string AnotherString = "another string";
                const string Provider = "provider";
                const string Label = "msg.a";
                string Config =
                    $@"<endpoints>
                        <endpoint 
                            name=""{Name}"" 
                            connectionStringProvider=""{Provider}"" >
                            <outgoing>
                                <route key=""a"" label=""{Label}"" connectionString=""outgoing connection string"" />
                            </outgoing>
                        </endpoint>
                
                    </endpoints>";

                var connectionStringProviderMock = new Mock<IConnectionStringProvider>();
                connectionStringProviderMock
                    .Setup(cspm => cspm.GetConnectionString(It.Is<MessageLabel>(l => l.Name == Label)))
                    .Returns(AnotherString);

                var resolverMock = new Mock<IDependencyResolver>();
                resolverMock.Setup(
                    rm => rm.Resolve(
                        It.Is<string>(value => value == Provider),
                        It.Is<Type>(t => t == typeof(IConnectionStringProvider))))
                    .Returns(connectionStringProviderMock.Object);


                var section = new XmlEndpointsSection(Config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq();

                var configuration = (BusConfiguration)configurator.Configure(Name, busConfiguration);
                var senderConfiguration = configuration.SenderConfigurations.First(sc => sc.Label.Equals(MessageLabel.From(Label)));

                var senderOptions = (RabbitSenderOptions)senderConfiguration.Options;
                senderOptions.GetConnectionString().Value.Should().Be(AnotherString, "Should use a connection string from the provider for outgoing label.");
            }

            [Test]
            public void should_use_connection_string_from_outgoing_label_if_provider_returns_null()
            {
                const string Name = "name";
                const string LabelString = "outgoing connection string";
                const string Provider = "provider";
                const string Label = "msg.a";
                string Config =
                    $@"<endpoints>
                        <endpoint 
                            name=""{Name}"" 
                            connectionStringProvider=""{Provider}"" >
                            <outgoing>
                                <route key=""a"" label=""{Label}"" connectionString=""{LabelString}"" />
                            </outgoing>
                        </endpoint>
                
                    </endpoints>";

                var connectionStringProviderMock = new Mock<IConnectionStringProvider>();
                connectionStringProviderMock
                    .Setup(cspm => cspm.GetConnectionString(It.Is<MessageLabel>(l => l.Name == Label)))
                    .Returns((string)null);

                var resolverMock = new Mock<IDependencyResolver>();
                resolverMock.Setup(
                    rm => rm.Resolve(
                        It.Is<string>(value => value == Provider),
                        It.Is<Type>(t => t == typeof(IConnectionStringProvider))))
                    .Returns(connectionStringProviderMock.Object);


                var section = new XmlEndpointsSection(Config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq();

                var configuration = (BusConfiguration)configurator.Configure(Name, busConfiguration);

                var senderConfiguration = configuration.SenderConfigurations.First(sc => sc.Label.Equals(MessageLabel.From(Label)));

                var senderOptions = (RabbitSenderOptions)senderConfiguration.Options;
                senderOptions.GetConnectionString().Value.Should().Be(LabelString, "Should use a connection string from the outgoing label.");
            }


            [Test]
            public void should_set_connection_string_on_incoming_label_from_provider_if_present()
            {
                const string Name = "name";
                const string SomeString = "someString";
                const string AnotherString = "another string";
                const string Provider = "provider";
                const string Label = "msg.a";
                string Config =
                    $@"<endpoints>
                        <endpoint 
                            name=""{Name}"" 
                            connectionStringProvider=""{Provider}"" >
                            <incoming>
                                <on key=""a"" label=""{Label}"" react=""BooHandler"" type=""BooMessage"" lifestyle=""Delegated"" />
                            </incoming>
                        </endpoint>
                
                    </endpoints>";

                var connectionStringProviderMock = new Mock<IConnectionStringProvider>();
                connectionStringProviderMock
                    .Setup(cspm => cspm.GetConnectionString(It.Is<MessageLabel>(l => l.Name == Label)))
                    .Returns(AnotherString);

                var resolverMock = new Mock<IDependencyResolver>();
                resolverMock.Setup(
                    rm => rm.Resolve(
                        It.Is<string>(value => value == Provider),
                        It.Is<Type>(t => t == typeof(IConnectionStringProvider))))
                    .Returns(connectionStringProviderMock.Object);


                var section = new XmlEndpointsSection(Config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq();

                var configuration = (BusConfiguration)configurator.Configure(Name, busConfiguration);

                var receiverConfiguration = configuration.ReceiverConfigurations.First(sc => sc.Label.Equals(MessageLabel.From(Label)));

                var receiverOptions = (RabbitReceiverOptions)receiverConfiguration.Options;
                receiverOptions.GetConnectionString().Value.Should().Be(AnotherString, "Should use a connection string from the provider for incoming label.");
            }

            [Test]
            public void should_override_connection_string_on_incoming_label_from_provider_if_present()
            {
                const string Name = "name";
                const string AnotherString = "another string";
                const string Provider = "provider";
                const string Label = "msg.a";
                string Config =
                    $@"<endpoints>
                        <endpoint 
                            name=""{Name}"" 
                            connectionStringProvider=""{Provider}"" >
                            <incoming>
                                <on key=""a"" label=""{Label}"" connectionString=""incoming connection string"" react=""BooHandler"" type=""BooMessage"" lifestyle=""Delegated"" />
                            </incoming>
                        </endpoint>
                
                    </endpoints>";

                var connectionStringProviderMock = new Mock<IConnectionStringProvider>();
                connectionStringProviderMock
                    .Setup(cspm => cspm.GetConnectionString(It.Is<MessageLabel>(l => l.Name == Label)))
                    .Returns(AnotherString);

                var resolverMock = new Mock<IDependencyResolver>();
                resolverMock.Setup(
                    rm => rm.Resolve(
                        It.Is<string>(value => value == Provider),
                        It.Is<Type>(t => t == typeof(IConnectionStringProvider))))
                    .Returns(connectionStringProviderMock.Object);


                var section = new XmlEndpointsSection(Config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq();

                var configuration = (BusConfiguration)configurator.Configure(Name, busConfiguration);

                var receiverConfiguration = configuration.ReceiverConfigurations.First(sc => sc.Label.Equals(MessageLabel.From(Label)));

                var receiverOptions = (RabbitReceiverOptions)receiverConfiguration.Options;
                receiverOptions.GetConnectionString().Value.Should().Be(AnotherString, "Should use a connection string from the provider for incoming label.");
            }

            [Test]
            public void should_use_connection_string_from_incoming_label_if_provider_returns_null()
            {
                const string Name = "name";
                const string LabelString = "incoming connection string";
                const string Provider = "provider";
                const string Label = "msg.a";
                string Config =
                    $@"<endpoints>
                        <endpoint 
                            name=""{Name}"" 
                            connectionStringProvider=""{Provider}"" >
                            <incoming>
                                <on key=""a"" label=""{Label}"" connectionString=""{LabelString}"" react=""BooHandler"" type=""BooMessage"" lifestyle=""Delegated"" />
                            </incoming>
                        </endpoint>
                
                    </endpoints>";

                var connectionStringProviderMock = new Mock<IConnectionStringProvider>();
                connectionStringProviderMock
                    .Setup(cspm => cspm.GetConnectionString(It.Is<MessageLabel>(l => l.Name == Label)))
                    .Returns((string)null);

                var resolverMock = new Mock<IDependencyResolver>();
                resolverMock.Setup(
                    rm => rm.Resolve(
                        It.Is<string>(value => value == Provider),
                        It.Is<Type>(t => t == typeof(IConnectionStringProvider))))
                    .Returns(connectionStringProviderMock.Object);


                var section = new XmlEndpointsSection(Config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq();

                var configuration = (BusConfiguration)configurator.Configure(Name, busConfiguration);

                var receiverConfiguration = configuration.ReceiverConfigurations.First(sc => sc.Label.Equals(MessageLabel.From(Label)));

                var receiverOptions = (RabbitReceiverOptions)receiverConfiguration.Options;
                receiverOptions.GetConnectionString().Value.Should().Be(LabelString, "Should use a connection string from the outgoing label.");
            }
        }


        [TestFixture]
        [Category("Unit")]
        public class when_configuring_endpoint_with_connection_reuse
        {
            [Test]
            public void should_set_connection_reuse_if_present()
            {
                const string name = "name";
                string Config = $@"<endpoints>
                                       <endpoint name=""{name}"" connectionString=""amqp://localhost/integration"" reuseConnection=""true""/>
                                   </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var busConfigurator = new BusConfiguration();

                var section = new XmlEndpointsSection(Config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);
                var configuration = (BusConfiguration)configurator.Configure(name, busConfigurator);

                var property = configuration.EndpointOptions.GetReuseConnection();
                property.HasValue.Should().BeTrue();
                property.Value.Should().BeTrue();
            }

            [Test]
            public void should_use_default_if_not_present()
            {
                const string name = "name";
                string Config = $@"<endpoints>
                                        <endpoint name=""{name}"" connectionString=""amqp://localhost/integration""/>
                                   </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var busConfigurator = new BusConfiguration();

                var section = new XmlEndpointsSection(Config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);
                var configuration = (BusConfiguration)configurator.Configure(name, busConfigurator);

                var property = configuration.EndpointOptions.GetReuseConnection();
                property.Should().BeTrue();
            }
        }

        [TestFixture]
        [Category("Unit")]
        public class when_configuring_endpoint_incoming
        {
            [Test]
            public void should_set_qos_prefetch_count_if_present()
            {
                const string endpointName = "ep";
                const int prefetchCount = 5;
                const string onKeyName = "key";

                string config =
                    $@"<endpoints>
                        <endpoint name=""{endpointName}"" connectionString=""amqp://localhost:666"">
                                <incoming>
                                    <on key=""{onKeyName}"" label=""msg.a"" react=""DynamicHandler"" requiresAccept=""true"">
                                        <qos prefetchCount=""{prefetchCount}"" />
                                    </on>
                                </incoming>
                        </endpoint>
                    </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var section = new XmlEndpointsSection(config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);
                
                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq(); //Basic receiver configurator and receiver options are actually unaware of any QoS settings; so these tests are not really Contour specific

                var result = configurator.Configure(endpointName, busConfiguration);
                var busConfigurationResult = (BusConfiguration) result;
                var receiverConfiguration = busConfigurationResult.ReceiverConfigurations.First();
                var receiverOptions = (RabbitReceiverOptions) receiverConfiguration.Options;
                
                var qos = receiverOptions.GetQoS();
                var value = qos.Value;

                value.PrefetchCount.Should().Be(prefetchCount, "Incoming QoS prefetch count should be set");
            }

            [Test]
            public void should_set_qos_prefetch_size_if_present()
            {
                const string endpointName = "ep";
                const int prefetchSize = 6;
                const string onKeyName = "key";

                string config =
                    $@"<endpoints>
                        <endpoint name=""{endpointName}"" connectionString=""amqp://localhost:666"">
                                <incoming>
                                    <on key=""{onKeyName}"" label=""msg.a"" react=""DynamicHandler"" requiresAccept=""true"">
                                        <qos prefetchCount="""" prefetchSize=""{prefetchSize}"" />
                                    </on>
                                </incoming>
                        </endpoint>
                    </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var section = new XmlEndpointsSection(config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq(); //Basic receiver configurator and receiver options are actually unaware of any QoS settings; so these tests are not really Contour specific

                var result = configurator.Configure(endpointName, busConfiguration);
                var busConfigurationResult = (BusConfiguration)result;
                var receiverConfiguration = busConfigurationResult.ReceiverConfigurations.First();
                var receiverOptions = (RabbitReceiverOptions)receiverConfiguration.Options;

                var qos = receiverOptions.GetQoS();
                var value = qos.Value;

                value.PrefetchSize.Should().Be(prefetchSize, "Incoming QoS prefetch size should be set");
            }

            [Test]
            public void should_use_endpoint_qos_prefetch_count_if_not_present()
            {
                const string endpointName = "ep";
                const int prefetchCount = 5;
                const string onKeyName = "key";

                string config =
                    $@"<endpoints>
                        <endpoint name=""{endpointName}"" connectionString=""amqp://localhost:666"">
                            <qos prefetchCount=""{prefetchCount}"" />
                                <incoming>
                                    <on key=""{onKeyName}"" label=""msg.a"" react=""DynamicHandler"" requiresAccept=""true"" />
                                </incoming>
                        </endpoint>
                    </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var section = new XmlEndpointsSection(config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq(); //Basic receiver configurator and receiver options are actually unaware of any QoS settings; so these tests are not really Contour specific

                var result = configurator.Configure(endpointName, busConfiguration);
                var busConfigurationResult = (BusConfiguration)result;
                var receiverConfiguration = busConfigurationResult.ReceiverConfigurations.First();

                var receiverOptions = (RabbitReceiverOptions)receiverConfiguration.Options;

                var qos = receiverOptions.GetQoS();
                var value = qos.Value;

                value.PrefetchCount.Should().Be(prefetchCount, "Endpoint QoS prefetch count should be used");
            }

            [Test]
            public void should_use_endpoint_qos_prefetch_size_if_not_present()
            {
                const string endpointName = "ep";
                const int prefetchSize = 6;
                const string onKeyName = "key";

                string config =
                    $@"<endpoints>
                        <endpoint name=""{endpointName}"" connectionString=""amqp://localhost:666"">
                            <qos prefetchCount="""" prefetchSize=""{prefetchSize}"" />
                                <incoming>
                                    <on key=""{onKeyName}"" label=""msg.a"" react=""DynamicHandler"" requiresAccept=""true"" />
                                </incoming>
                        </endpoint>
                    </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var section = new XmlEndpointsSection(config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq(); //Basic receiver configurator and receiver options are actually unaware of any QoS settings; so these tests are not really Contour specific

                var result = configurator.Configure(endpointName, busConfiguration);
                var busConfigurationResult = (BusConfiguration)result;
                var receiverConfiguration = busConfigurationResult.ReceiverConfigurations.First();

                var receiverOptions = (RabbitReceiverOptions)receiverConfiguration.Options;

                var qos = receiverOptions.GetQoS();
                var value = qos.Value;

                value.PrefetchSize.Should().Be(prefetchSize, "Endpoint QoS prefetch size should be used");
            }

            [Test]
            public void should_use_qos_default_prefetch_count_if_no_incoming_and_endpoint_settings_are_present()
            {
                const string endpointName = "ep";
                const string onKeyName = "key";

                string config =
                    $@"<endpoints>
                        <endpoint name=""{endpointName}"" connectionString=""amqp://localhost:666"">
                            <incoming>
                                <on key=""{onKeyName}"" label=""msg.a"" react=""DynamicHandler"" requiresAccept=""true"" />
                            </incoming>
                        </endpoint>
                    </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var section = new XmlEndpointsSection(config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq(); //Basic receiver configurator and receiver options are actually unaware of any QoS settings; so these tests are not really Contour specific

                var result = configurator.Configure(endpointName, busConfiguration);
                var busConfigurationResult = (BusConfiguration)result;
                var receiverConfiguration = busConfigurationResult.ReceiverConfigurations.First();

                var receiverOptions = (RabbitReceiverOptions)receiverConfiguration.Options;
                var qos = receiverOptions.GetQoS().Value;

                var receiverDefaults = (RabbitReceiverOptions)busConfiguration.ReceiverDefaults;
                var defaultQos = receiverDefaults.GetQoS().Value;
                
                qos.PrefetchCount.Should().Be(defaultQos.PrefetchCount, "Default QoS prefetch count should be used");
            }

            [Test]
            public void should_use_qos_default_prefetch_size_if_no_incoming_and_endpoint_settings_are_present()
            {
                const string endpointName = "ep";
                const string onKeyName = "key";

                string config =
                    $@"<endpoints>
                        <endpoint name=""{endpointName}"" connectionString=""amqp://localhost:666"">
                            <incoming>
                                <on key=""{onKeyName}"" label=""msg.a"" react=""DynamicHandler"" requiresAccept=""true"" />
                            </incoming>
                        </endpoint>
                    </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var section = new XmlEndpointsSection(config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq(); //Basic receiver configurator and receiver options are actually unaware of any QoS settings; so these tests are not really Contour specific

                var result = configurator.Configure(endpointName, busConfiguration);
                var busConfigurationResult = (BusConfiguration)result;
                var receiverConfiguration = busConfigurationResult.ReceiverConfigurations.First();

                var receiverOptions = (RabbitReceiverOptions)receiverConfiguration.Options;
                var qos = receiverOptions.GetQoS().Value;

                var receiverDefaults = (RabbitReceiverOptions)busConfiguration.ReceiverDefaults;
                var defaultQos = receiverDefaults.GetQoS().Value;

                qos.PrefetchSize.Should().Be(defaultQos.PrefetchSize, "Default QoS prefetch size should be used");
            }

            [Test]
            public void should_set_parallelism_level_if_present()
            {
                const string endpointName = "ep";
                const uint parallelismLevel = 99;
                const string onKeyName = "key";

                string config =
                    $@"<endpoints>
                        <endpoint name=""{endpointName}"" connectionString=""amqp://localhost:666"" parallelismLevel=""123"">
                                <incoming>
                                    <on key=""{onKeyName}"" label=""msg.a"" react=""DynamicHandler"" requiresAccept=""true"" parallelismLevel=""{parallelismLevel}""/>
                                </incoming>
                        </endpoint>
                    </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var section = new XmlEndpointsSection(config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq(); //Basic receiver configurator and receiver options are actually unaware of any QoS settings; so these tests are not really Contour specific

                var result = configurator.Configure(endpointName, busConfiguration);
                var busConfigurationResult = (BusConfiguration)result;
                var receiverConfiguration = busConfigurationResult.ReceiverConfigurations.First();

                var receiverOptions = (RabbitReceiverOptions)receiverConfiguration.Options;
                receiverOptions.GetParallelismLevel().Value.Should().Be(parallelismLevel, "Incoming parallelism level should be set");
            }

            [Test]
            public void should_use_endpoint_parallelism_level_if_not_present()
            {
                const string endpointName = "ep";
                const uint parallelismLevel = 99;
                const string onKeyName = "key";

                string config =
                    $@"<endpoints>
                        <endpoint name=""{endpointName}"" connectionString=""amqp://localhost:666"" parallelismLevel=""{parallelismLevel}"">
                                <incoming>
                                    <on key=""{onKeyName}"" label=""msg.a"" react=""DynamicHandler"" requiresAccept=""true"" />
                                </incoming>
                        </endpoint>
                    </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var section = new XmlEndpointsSection(config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq(); //Basic receiver configurator and receiver options are actually unaware of any QoS settings; so these tests are not really Contour specific

                var result = configurator.Configure(endpointName, busConfiguration);
                var busConfigurationResult = (BusConfiguration)result;
                var receiverConfiguration = busConfigurationResult.ReceiverConfigurations.First();

                var receiverOptions = (RabbitReceiverOptions)receiverConfiguration.Options;
                receiverOptions.GetParallelismLevel().Value.Should().Be(parallelismLevel, "Endpoint parallelism level should be used");
            }

            [Test]
            public void should_use_parallelism_level_defaults_if_no_incoming_and_endpoint_settings_are_present()
            {
                const string endpointName = "ep";
                const string onKeyName = "key";

                string config =
                    $@"<endpoints>
                        <endpoint name=""{endpointName}"" connectionString=""amqp://localhost:666"" >
                            <incoming>
                                <on key=""{onKeyName}"" label=""msg.a"" react=""DynamicHandler"" requiresAccept=""true"" />
                            </incoming>
                        </endpoint>
                    </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var section = new XmlEndpointsSection(config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq(); //Basic receiver configurator and receiver options are actually unaware of any QoS settings; so these tests are not really Contour specific

                var result = configurator.Configure(endpointName, busConfiguration);
                var busConfigurationResult = (BusConfiguration)result;
                var receiverConfiguration = busConfigurationResult.ReceiverConfigurations.First();

                var receiverOptions = (RabbitReceiverOptions)receiverConfiguration.Options;
                var receiverDefaults = busConfiguration.ReceiverDefaults;

                receiverOptions.GetParallelismLevel().Value.Should().Be(receiverDefaults.GetParallelismLevel().Value, "Default parallelism level should be used");
            }

            [Test]
            public void should_set_connection_string_if_present()
            {
                const string endpointName = "ep";
                const string onKeyName = "key";
                var conString = "123";

                string config =
                    $@"<endpoints>
                        <endpoint name=""{endpointName}"" connectionString=""amqp://localhost:666"" >
                                <incoming>
                                    <on key=""{onKeyName}"" label=""msg.a"" react=""DynamicHandler"" requiresAccept=""true"" connectionString=""{conString}""/>
                                </incoming>
                        </endpoint>
                    </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var section = new XmlEndpointsSection(config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq();

                var result = configurator.Configure(endpointName, busConfiguration);
                var busConfigurationResult = (BusConfiguration)result;
                var receiverConfiguration = busConfigurationResult.ReceiverConfigurations.First();

                var receiverOptions = (RabbitReceiverOptions)receiverConfiguration.Options;
                receiverOptions.GetConnectionString().Value.Should().Be(conString, "Connection string should be set");
            }

            [Test]
            public void should_use_endpoint_connection_string_if_not_present()
            {
                const string endpointName = "ep";
                const string onKeyName = "key";
                var incomingString = "123";
                var endpointString = "456";

                string config =
                    $@"<endpoints>
                        <endpoint name=""{endpointName}"" connectionString=""{endpointString}"" >
                                <incoming>
                                    <on key=""{onKeyName}"" label=""msg.a"" react=""DynamicHandler"" requiresAccept=""true""/>
                                </incoming>
                        </endpoint>
                    </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var section = new XmlEndpointsSection(config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq();

                var result = configurator.Configure(endpointName, busConfiguration);
                var busConfigurationResult = (BusConfiguration)result;
                var receiverConfiguration = busConfigurationResult.ReceiverConfigurations.First();

                var receiverOptions = (RabbitReceiverOptions)receiverConfiguration.Options;
                receiverOptions.GetConnectionString().Value.Should().Be(endpointString, "Connection string should be set");
            }

            [Test]
            public void should_set_connection_reuse_if_present()
            {
                const string endpointName = "ep";
                const string onKeyName = "key";
                var incomingString = "123";
                var endpointString = "456";
                var reuseConnection = true;

                string config =
                    $@"<endpoints>
                        <endpoint name=""{endpointName}"" connectionString=""{endpointString}"" >
                                <incoming>
                                    <on key=""{onKeyName}"" label=""msg.a"" react=""DynamicHandler"" requiresAccept=""true"" reuseConnection=""{reuseConnection}""/>
                                </incoming>
                        </endpoint>
                    </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var section = new XmlEndpointsSection(config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq();

                var result = configurator.Configure(endpointName, busConfiguration);
                var busConfigurationResult = (BusConfiguration)result;
                var receiverConfiguration = busConfigurationResult.ReceiverConfigurations.First();

                var receiverOptions = (RabbitReceiverOptions)receiverConfiguration.Options;
                receiverOptions.GetReuseConnection().Value.Should().Be(reuseConnection, "Connection reuse should be set");
            }

            [Test]
            public void should_use_endpoint_connection_reuse_if_not_present()
            {
                const string endpointName = "ep";
                const string onKeyName = "key";
                var incomingString = "123";
                var endpointString = "456";
                var reuseConnection = true;

                string config =
                    $@"<endpoints>
                        <endpoint name=""{endpointName}"" connectionString=""{endpointString}"" reuseConnection=""{reuseConnection}"">
                                <incoming>
                                    <on key=""{onKeyName}"" label=""msg.a"" react=""DynamicHandler"" requiresAccept=""true"" />
                                </incoming>
                        </endpoint>
                    </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var section = new XmlEndpointsSection(config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq();

                var result = configurator.Configure(endpointName, busConfiguration);
                var busConfigurationResult = (BusConfiguration)result;
                var receiverConfiguration = busConfigurationResult.ReceiverConfigurations.First();

                var receiverOptions = (RabbitReceiverOptions)receiverConfiguration.Options;
                receiverOptions.GetReuseConnection().Value.Should().Be(reuseConnection, "Connection reuse should be set");
            }

            [Test]
            public void should_use_default_connection_reuse_if_no_incoming_and_endpoint_settings_are_set()
            {
                const string endpointName = "ep";
                const string onKeyName = "key";
                var endpointString = "456";

                string config =
                    $@"<endpoints>
                        <endpoint name=""{endpointName}"" connectionString=""{endpointString}"" >
                                <incoming>
                                    <on key=""{onKeyName}"" label=""msg.a"" react=""DynamicHandler"" requiresAccept=""true"" />
                                </incoming>
                        </endpoint>
                    </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var section = new XmlEndpointsSection(config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq();

                var result = configurator.Configure(endpointName, busConfiguration);
                var busConfigurationResult = (BusConfiguration)result;
                var receiverConfiguration = busConfigurationResult.ReceiverConfigurations.First();

                var receiverOptions = (RabbitReceiverOptions)receiverConfiguration.Options;
                receiverOptions.GetReuseConnection().Value.Should().Be(true, "Connection reuse should be set to default");
            }
        }

        [TestFixture]
        [Category("Unit")]
        public class when_configuring_endpoint_outgoing
        {
            [Test]
            public void should_set_connection_string_if_present()
            {
                const string endpointName = "ep";
                const string onKeyName = "key";
                var outgoingString = "123";
                var label = "label";

                string config =
                    $@"<endpoints>
                        <endpoint name=""{endpointName}"" connectionString=""amqp://localhost:666"" >
                                <outgoing>
                                    <route key=""{onKeyName}"" label=""{label}"" connectionString=""{outgoingString}""/>
                                </outgoing>
                        </endpoint>
                    </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var section = new XmlEndpointsSection(config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq();

                var result = configurator.Configure(endpointName, busConfiguration);
                var busConfigurationResult = (BusConfiguration)result;
                
                // Need to find sender by label because two more senders are registered by default
                var senderConfiguration = busConfigurationResult.SenderConfigurations.First(sc => sc.Label.Equals(MessageLabel.From(label)));

                var senderOptions = (RabbitSenderOptions)senderConfiguration.Options;
                senderOptions.GetConnectionString().Value.Should().Be(outgoingString, "Connection string should be set");
            }

            [Test]
            public void should_use_endpoint_connection_string_if_not_present()
            {
                const string endpointName = "ep";
                const string onKeyName = "key";
                var endpointString = "amqp://localhost:666";
                var outgoingString = "123";
                var label = "label";

                string config =
                    $@"<endpoints>
                        <endpoint name=""{endpointName}"" connectionString=""{endpointString}"" >
                                <outgoing>
                                    <route key=""{onKeyName}"" label=""{label}"" />
                                </outgoing>
                        </endpoint>
                    </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var section = new XmlEndpointsSection(config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq();

                var result = configurator.Configure(endpointName, busConfiguration);
                var busConfigurationResult = (BusConfiguration)result;

                // Need to find sender by label because two more senders are registered by default
                var senderConfiguration = busConfigurationResult.SenderConfigurations.First(sc => sc.Label.Equals(MessageLabel.From(label)));

                var senderOptions = (RabbitSenderOptions)senderConfiguration.Options;
                senderOptions.GetConnectionString().Value.Should().Be(endpointString, "Connection string should be set");
            }
            
            [Test]
            public void should_set_connection_reuse_if_present()
            {
                const string endpointName = "ep";
                const string onKeyName = "key";
                var endpointString = "amqp://localhost:666";
                var outgoingString = "123";
                var label = "label";
                var reuseConnection = true;

                string config =
                    $@"<endpoints>
                        <endpoint name=""{endpointName}"" connectionString=""{endpointString}"" >
                                <outgoing>
                                    <route key=""{onKeyName}"" label=""{label}"" reuseConnection=""{reuseConnection}""/>
                                </outgoing>
                        </endpoint>
                    </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var section = new XmlEndpointsSection(config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq();

                var result = configurator.Configure(endpointName, busConfiguration);
                var busConfigurationResult = (BusConfiguration)result;

                // Need to find sender by label because two more senders are registered by default
                var senderConfiguration = busConfigurationResult.SenderConfigurations.First(sc => sc.Label.Equals(MessageLabel.From(label)));

                var senderOptions = (RabbitSenderOptions)senderConfiguration.Options;
                senderOptions.GetReuseConnection().Value.Should().Be(reuseConnection, "Connection reuse should be set");
            }

            [Test]
            public void should_use_endpoint_connection_reuse_if_not_present()
            {
                const string endpointName = "ep";
                const string onKeyName = "key";
                var endpointString = "amqp://localhost:666";
                var outgoingString = "123";
                var label = "label";
                var reuseConnection = true;

                string config =
                    $@"<endpoints>
                        <endpoint name=""{endpointName}"" connectionString=""{endpointString}"" reuseConnection=""{reuseConnection}"">
                                <outgoing>
                                    <route key=""{onKeyName}"" label=""{label}"" />
                                </outgoing>
                        </endpoint>
                    </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var section = new XmlEndpointsSection(config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq();

                var result = configurator.Configure(endpointName, busConfiguration);
                var busConfigurationResult = (BusConfiguration)result;

                // Need to find sender by label because two more senders are registered by default
                var senderConfiguration = busConfigurationResult.SenderConfigurations.First(sc => sc.Label.Equals(MessageLabel.From(label)));

                var senderOptions = (RabbitSenderOptions)senderConfiguration.Options;
                senderOptions.GetReuseConnection().Value.Should().Be(reuseConnection, "Connection reuse should be set");
            }

            [Test]
            public void should_use_default_connection_reuse_if_no_outgoing_and_endpoint_settings_are_set()
            {
                const string endpointName = "ep";
                const string onKeyName = "key";
                var endpointString = "amqp://localhost:666";
                var label = "label";

                string config =
                    $@"<endpoints>
                        <endpoint name=""{endpointName}"" connectionString=""{endpointString}"" >
                                <outgoing>
                                    <route key=""{onKeyName}"" label=""{label}"" />
                                </outgoing>
                        </endpoint>
                    </endpoints>";

                var resolverMock = new Mock<IDependencyResolver>();
                var section = new XmlEndpointsSection(config);
                var configurator = new AppConfigConfigurator(section, resolverMock.Object);

                var busConfiguration = new BusConfiguration();
                busConfiguration.UseRabbitMq();

                var result = configurator.Configure(endpointName, busConfiguration);
                var busConfigurationResult = (BusConfiguration)result;

                // Need to find sender by label because two more senders are registered by default
                var senderConfiguration = busConfigurationResult.SenderConfigurations.First(sc => sc.Label.Equals(MessageLabel.From(label)));

                var senderOptions = (RabbitSenderOptions)senderConfiguration.Options;
                senderOptions.GetReuseConnection().Value.Should().Be(true, "Connection reuse should be set to default");
            }
        }
    }
}
