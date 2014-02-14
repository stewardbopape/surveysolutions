﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Machine.Specifications;
using WB.Core.BoundedContexts.Supervisor.Implementation.Services.TabletInformation;
using WB.Core.BoundedContexts.Supervisor.Views.TabletInformation;

namespace WB.Core.BoundedContexts.Supervisor.Tests.ServiceTests.FileBasedTabletInformationServiceTests
{
    internal class when_requesting_list_of_packages_and_storage_is_empty : FileBasedTabletInformationServiceTestContext
    {
        Establish context = () =>
        {
            fileBasedTabletInformationService = CreateFileBasedTabletInformationService();
        };

        Because of = () => returnedPackages = fileBasedTabletInformationService.GetAllTabletInformationPackages();

        It should_return_empty_list = () => returnedPackages.Count.ShouldEqual(0);

        private static FileBasedTabletInformationService fileBasedTabletInformationService;

        private static List<TabletInformationView> returnedPackages;
    }
}
