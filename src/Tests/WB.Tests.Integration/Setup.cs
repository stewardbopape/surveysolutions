﻿using Microsoft.Practices.ServiceLocation;
using Moq;
using WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration;
using WB.Core.SharedKernels.ExpressionProcessor.Services;

namespace WB.Tests.Integration
{
    internal static class Setup
    {
        public static void MockedServiceLocator()
        {
            var serviceLocatorMock = new Mock<IServiceLocator> { DefaultValue = DefaultValue.Mock };

            IExpressionProcessor roslynExpressionProcessor = new RoslynExpressionProcessor();

            serviceLocatorMock
                .Setup(locator => locator.GetInstance<IExpressionProcessor>())
                .Returns(roslynExpressionProcessor);

            ServiceLocator.SetLocatorProvider(() => serviceLocatorMock.Object);
        }
    }
}