using Machine.Specifications;
using Moq;
using WB.Core.Infrastructure.EventBus;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.Infrastructure.EventBusMobileTests
{
    internal class subscribe_all_method_register_all_event_handlers_for_class : EventBusTestsContext
    {
        Establish context = () =>
        {
            value = 10;
            
            var eventRegistry = CreateEventRegistry();
            eventBus = CreateEventBus(eventRegistry);

            handlersClass = CreateClassWithEventHandlers();
            eventRegistry.Subscribe(handlersClass);
        };

        Because of = () =>
            eventBus.Publish(value);

        It should_doesnot_call_Handle_for_int = () =>
            Mock.Get(handlersClass).Verify(s => s.Handle(Moq.It.IsAny<int>()), Times.Never);

        It should_call_Handle_for_long = () =>
            Mock.Get(handlersClass).Verify(s => s.Handle(value), Times.Once());

        It should_doesnot_call_Handle_for_string = () =>
            Mock.Get(handlersClass).Verify(s => s.Handle(Moq.It.IsAny<string>()), Times.Never);


        private static ILiteEventBus eventBus;
        private static long value;
        private static DumyEventHandlers handlersClass;
    }
}