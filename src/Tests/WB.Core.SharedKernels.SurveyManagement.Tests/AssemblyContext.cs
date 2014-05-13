﻿using Machine.Specifications;
using Microsoft.Practices.ServiceLocation;
using Moq;
using WB.Core.SharedKernels.ExpressionProcessor.Implementation.Services;
using WB.Core.SharedKernels.ExpressionProcessor.Services;

namespace WB.Core.SharedKernels.SurveyManagement.Tests
{
    public class AssemblyContext : IAssemblyContext
    {
        public void OnAssemblyStart()
        {
            var serviceLocatorMock = new Mock<IServiceLocator> { DefaultValue = DefaultValue.Mock };

            ServiceLocator.SetLocatorProvider(() =>
            {
                return serviceLocatorMock.Object;
            });
        }

        public void OnAssemblyComplete() {}
    }
}
