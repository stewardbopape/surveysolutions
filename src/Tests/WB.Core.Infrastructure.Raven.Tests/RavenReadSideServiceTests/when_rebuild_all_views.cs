﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Machine.Specifications;
using Moq;
using Ncqrs.Eventing;
using Ncqrs.Eventing.Storage;
using NUnit.Framework;
using WB.Core.Infrastructure.EventBus;
using WB.Core.Infrastructure.FunctionalDenormalization;
using WB.Core.Infrastructure.ReadSide;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.Infrastructure.Storage.Raven.Implementation.ReadSide;
using It = Machine.Specifications.It;

namespace WB.Core.Infrastructure.Raven.Tests.RavenReadSideServiceTests
{
    internal class when_rebuild_all_views : RavenReadSideServiceTestContext
    {
        Establish context = () =>
        {
            readSideRepositoryCleanerMock=new Mock<IReadSideRepositoryCleaner>();
            readSideRepositoryWriterMock=new Mock<IReadSideRepositoryWriter>();
            readSideRepositoryWriterMock.Setup(x => x.ViewType).Returns(typeof (object));

            eventHandlerMock=new Mock<IEventHandler>();

            eventHandlerMock.Setup(x => x.Writers).Returns(new object[] { readSideRepositoryCleanerMock.Object, readSideRepositoryWriterMock.Object });

            eventDispatcherMock=new Mock<IEventDispatcher>();
            eventDispatcherMock.Setup(x => x.GetAllRegistredEventHandlers()).Returns(new[] { eventHandlerMock.Object });

            committedEvent = new CommittedEvent(Guid.NewGuid(), "test", Guid.NewGuid(), Guid.NewGuid(), 1, DateTime.Now, new object(),
                new Version(1, 2));
            streamableEventStoreMock=new Mock<IStreamableEventStore>();
            streamableEventStoreMock.Setup(x => x.GetAllEvents(Moq.It.IsAny<int>(), Moq.It.IsAny<int>()))
                .Returns(new[] { new[] { committedEvent } });
            ravenReadSideService = CreateRavenReadSideService(eventDispatcher: eventDispatcherMock.Object, streamableEventStore: streamableEventStoreMock.Object);
        };

        Because of = () => WaitRebuildReadsideFinish();

        It should_rebuild_all_view = () =>
            ravenReadSideService.AreViewsBeingRebuiltNow().ShouldEqual(false);

        It should_call_clean_method_for_registered_writers_once = () =>
            readSideRepositoryCleanerMock.Verify(x=>x.Clear(), Times.Once);

        It should_enable_cache_for_registered_writers_once = () =>
            readSideRepositoryWriterMock.Verify(x=>x.EnableCache(), Times.Once);

        It should_disable_cache_for_registered_writers_once = () =>
           readSideRepositoryWriterMock.Verify(x => x.DisableCache(), Times.Once);

        It should_publish_one_event_on_event_dispatcher = () =>
            eventDispatcherMock.Verify(x=>x.PublishEventToHandlers(committedEvent, Moq.It.IsAny<IEnumerable<IEventHandler>>()), Times.Once);

        private static RavenReadSideService ravenReadSideService;
        private static Mock<IEventDispatcher> eventDispatcherMock;
        private static Mock<IStreamableEventStore> streamableEventStoreMock;
        private static Mock<IEventHandler> eventHandlerMock;
        private static Mock<IReadSideRepositoryCleaner> readSideRepositoryCleanerMock;
        private static Mock<IReadSideRepositoryWriter> readSideRepositoryWriterMock;

        private static CommittedEvent committedEvent;

        protected static void WaitRebuildReadsideFinish()
        {
            ravenReadSideService.RebuildAllViewsAsync(0);

            Thread.Sleep(1000);

            while (ravenReadSideService.AreViewsBeingRebuiltNow())
            {
                Thread.Sleep(1000);
            }
        }
    }
}
