using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using MemBus;
using MemBus.Configurators;
using NUnit.Framework;
using Rhino.Mocks;

namespace MembusPoC
{
    [TestFixture]
    public class TestMemBus
    {

        public static void Main()
        {
            new TestMemBus().TestAutofacIoCAdapter();
            Console.Read();
        }

        [Test]
        public void TestAutofacIoCAdapter()
        {
            
            var builder = new ContainerBuilder();
            builder.RegisterModule(new IoCRegistration());
            var container = builder.Build();
            var _bus =
                BusSetup.StartWith<Conservative>()
                    .Apply<IoCSupport>(
                        s => s.SetAdapter(container.Resolve<IocAdapter>()).SetHandlerInterface(typeof (IHandle<>)))
                    .Construct();
            _bus.Publish(new DomainEvent1());
            Assert.IsTrue(true);
        }

        [Test]
        public void TestMockIocAdapter()
        {
            var eventHandler1 = MockRepository.GenerateMock<IHandle<DomainEvent1>>();
            var eventHandler2 = MockRepository.GenerateMock<IHandle<DomainEvent1>>();
            var _bus =
                BusSetup.StartWith<Conservative>()
                    .Apply<IoCSupport>(
                        s => s.SetAdapter(new MockIocAdapter(eventHandler1, eventHandler2)).SetHandlerInterface(typeof(IHandle<>)))
                    .Construct();
            eventHandler1.Expect(it => it.Handle(Arg<DomainEvent1>.Is.Anything));
            eventHandler2.Expect(it => it.Handle(Arg<DomainEvent1>.Is.Anything));
            _bus.Publish(new DomainEvent1());
            eventHandler1.VerifyAllExpectations();
            eventHandler2.VerifyAllExpectations();
           
        }

        [Test]
        public void TestSimpleIocAdapter()
        {
            var eventHandler = new DomainEvent1Handler3();
            
            var _bus =
                BusSetup.StartWith<Conservative>()
                    .Apply<IoCSupport>(
                        s => s.SetAdapter(new MockIocAdapter(eventHandler)).SetHandlerInterface(typeof(IHandle<>)))
                    .Construct();

            var publishedEvent = new DomainEvent1();
            Assert.AreEqual(eventHandler.EventCaptured, "NoEvent");
            _bus.Publish(publishedEvent);
            Assert.AreEqual(eventHandler.EventCaptured, publishedEvent.EventName);

        }

        [Test]
        public void TestChainedEvents()
        {
            
            var mockIocAdapter = new MockIocAdapter();
            var _bus =
                BusSetup.StartWith<Conservative>()
                    .Apply<IoCSupport>(
                        s => s.SetAdapter(mockIocAdapter).SetHandlerInterface(typeof(IHandle<>)))
                    .Construct();
            var eventHandler4 = new DomainEvent1Handler4(_bus);
            var eventHandler5 = new DomainEvent1Handler5();
            mockIocAdapter.AddEventHandlers(eventHandler4, eventHandler5);

            var publishedEvent = new DomainEvent1();
            Assert.AreEqual(eventHandler4.EventCaptured, "NoEvent");
            Assert.AreEqual(eventHandler5.EventCaptured, "NoEvent");
            _bus.Publish(publishedEvent);
            Assert.AreEqual(eventHandler4.EventCaptured, publishedEvent.EventName);
            Assert.AreEqual(eventHandler5.EventCaptured, "Event 2");
            Assert.IsTrue(eventHandler4.DateTimeCaptured < eventHandler5.DateTimeCaptured);

        }
    }

    public class MockIocAdapter : IocAdapter
    {
        private List<object> _eventHandlers = new List<object>();
        public MockIocAdapter(params object[] eventHandlers)
        {
            _eventHandlers.AddRange(eventHandlers);
        }

        public void AddEventHandlers(params object[] eventHandlers)
        {
            _eventHandlers.AddRange(eventHandlers);
        }

        public IEnumerable<object> GetAllInstances(Type desiredType)
        {

            foreach (var eventHandler in _eventHandlers)
            {
                if (desiredType.IsAssignableFrom(eventHandler.GetType()))
                yield return eventHandler;
            }
        }
    }

    public class AutofacIocAdapter : IocAdapter
    {
        private IComponentContext _componentContext;

        public AutofacIocAdapter(IComponentContext componentContext)
        {
            _componentContext = componentContext;
        }

        public IEnumerable<object> GetAllInstances(Type desiredType)
        {
            var ienumarableOfGenericType = typeof(IEnumerable<>).MakeGenericType(desiredType);
            var result = _componentContext.Resolve(ienumarableOfGenericType) ;
            return result as IEnumerable<object>;
        }
    }

    public class IoCRegistration: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DomainEvent1Handler>().As<IHandle<DomainEvent1>>();
            builder.RegisterType<DomainEvent1Handler2>().As<IHandle<DomainEvent1>>();
            builder.RegisterType<Independence1>().As<IIndependence1>();
            builder.RegisterType<Independence2>().As<IIndependence2>();
            builder.RegisterType<AutofacIocAdapter>().As<IocAdapter>();
        }
    }



    public class DomainEvent1
    {
        public string EventName { get; set; }
        public DateTime EventDateTime { get; set; }

        public DomainEvent1()
        {
            EventName = "Event 1";
            EventDateTime = DateTime.Now;
        }
    }

    public class DomainEvent2
    {
        public string EventName { get; set; }
        public DateTime EventDateTime { get; set; }

        public DomainEvent2()
        {
            EventName = "Event 2";
            EventDateTime = DateTime.Now;
        }
    }

    public interface IHandle<in T>
    {
        void Handle(T @event);
    }

    public class DomainEvent1Handler : IHandle<DomainEvent1>
    {
        private readonly IIndependence1 _independence1;
        private readonly IIndependence2 _independence2;

        public DomainEvent1Handler(IIndependence1 independence1, IIndependence2 independence2)
        {
            _independence1 = independence1;
            _independence2 = independence2;
        }

        public void Handle(DomainEvent1 @event)
        {
            
            _independence1.ProcessSomething();
            _independence2.ProcessSomeOthers();
        }
    }

    public class DomainEvent1Handler2 : IHandle<DomainEvent1>
    {
        private readonly IIndependence1 _independence1;
        private readonly IIndependence2 _independence2;

        public DomainEvent1Handler2(IIndependence1 independence1, IIndependence2 independence2)
        {
            _independence1 = independence1;
            _independence2 = independence2;
        }

        public void Handle(DomainEvent1 @event)
        {
            _independence1.ProcessSomething();
            _independence2.ProcessSomeOthers();
        }
    }

    public class DomainEvent1Handler3 : IHandle<DomainEvent1>
    {
        public string EventCaptured = "NoEvent";
        

        public void Handle(DomainEvent1 @event)
        {
            EventCaptured = @event.EventName;
            
        }
    }

    public class DomainEvent1Handler4 : IHandle<DomainEvent1>
    {
        public string EventCaptured = "NoEvent";
        public DateTime DateTimeCaptured = DateTime.MinValue;
        private IBus _bus;

        public DomainEvent1Handler4(IBus bus)
        {
            _bus = bus;
        }

        public void Handle(DomainEvent1 @event)
        {
            EventCaptured = @event.EventName;
            DateTimeCaptured = @event.EventDateTime;
            _bus.Publish(new DomainEvent2());
        }
    }

    public class DomainEvent1Handler5 : IHandle<DomainEvent2>
    {
        public string EventCaptured = "NoEvent";
        public DateTime DateTimeCaptured = DateTime.MinValue;

        public void Handle(DomainEvent2 @event)
        {
            EventCaptured = @event.EventName;
            DateTimeCaptured = @event.EventDateTime;
        }
    }


    public interface IIndependence1
    {
        void ProcessSomething();
    }

    public class Independence1 : IIndependence1
    {
        public void ProcessSomething()
        {

        }
    }

    public interface IIndependence2
    {
        void ProcessSomeOthers();
    }

    public class Independence2 : IIndependence2
    {
        public void ProcessSomeOthers()
        {
            
        }
    }
}
