﻿using Machine.Specifications;
using Microsoft.Practices.ServiceLocation;
using Moq;
using Ncqrs;
using WB.Core.BoundedContexts.Designer.Implementation.Services;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.GenericSubdomains.Logging;
using WB.Core.SharedKernels.SurveySolutions.Implementation.Services;
using WB.Core.SharedKernels.SurveySolutions.Services;

namespace WB.Tests.Unit
{
    public class AssemblyContext : IAssemblyContext
    {
        public void OnAssemblyStart()
        {
            SetupServiceLocator();
        }

        public void OnAssemblyComplete() {}

        public static void SetupServiceLocator()
        {
            if (ServiceLocator.IsLocationProviderSet)
                return;

            var serviceLocator = Stub<IServiceLocator>.WithNotEmptyValues;

            ServiceLocator.SetLocatorProvider(() => serviceLocator);

            Setup.InstanceToMockedServiceLocator<ISubstitutionService>(new SubstitutionService());
            Setup.InstanceToMockedServiceLocator<IKeywordsProvider>(new KeywordsProvider(new SubstitutionService()));

            NcqrsEnvironment.SetGetter<ILogger>(Mock.Of<ILogger>);
            NcqrsEnvironment.SetGetter<IUniqueIdentifierGenerator>(Mock.Of<IUniqueIdentifierGenerator>);
            NcqrsEnvironment.SetGetter<IClock>(Mock.Of<IClock>);
        }
    }
}
