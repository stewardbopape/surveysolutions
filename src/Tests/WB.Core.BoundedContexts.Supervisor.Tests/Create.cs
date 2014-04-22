﻿using System;
using System.Net.Http;
using Moq;
using Ncqrs.Commanding.ServiceModel;
using NSubstitute;
using WB.Core.BoundedContexts.Supervisor.Synchronization;
using WB.Core.BoundedContexts.Supervisor.Synchronization.Implementation;
using WB.Core.BoundedContexts.Supervisor.Users;
using WB.Core.BoundedContexts.Supervisor.Users.Implementation;
using WB.Core.GenericSubdomains.Logging;
using WB.Core.Infrastructure.PlainStorage;

namespace WB.Core.BoundedContexts.Supervisor.Tests
{
    internal static class Create
    {
        internal static HeadquartersLoginService HeadquartersLoginService(IHeadquartersUserReader headquartersUserReader = null, 
            HttpMessageHandler messageHandler = null, 
            ILogger logger = null, 
            ICommandService commandService = null, 
            HeadquartersSettings headquartersSettings = null)
        {
            return new HeadquartersLoginService(logger ?? Substitute.For<ILogger>(), 
                commandService ?? Substitute.For<ICommandService>(), 
                messageHandler ?? Substitute.For<HttpMessageHandler>(),
                headquartersSettings ?? HeadquartersSettings(),
                headquartersUserReader ?? Substitute.For<IHeadquartersUserReader>());
        }

        public static UserChangedFeedReader UserChangedFeedReader(HeadquartersSettings settings = null, HttpMessageHandler messageHandler = null)
        {
            return new UserChangedFeedReader(settings ?? HeadquartersSettings(), 
                messageHandler ?? Substitute.For<HttpMessageHandler>(),
                new SynchronizationContext(Substitute.For<IPlainStorageAccessor<SynchronizationStatus>>()));
        }

        private static HeadquartersSettings HeadquartersSettings()
        {
            return new HeadquartersSettings(new Uri("http://localhost/"), new Uri("http://localhost/"), new Uri("http://localhost/"), "", string.Empty);
        }
    }
}