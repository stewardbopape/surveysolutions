﻿using Machine.Specifications;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts.Factories;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts.InputModels;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts.Views;
using WB.Core.Infrastructure.Transactions;

namespace WB.Tests.Integration.SupervisorTeamsAndStatusesReportTests
{
    [Ignore("Fix in KP-9839")]
    internal class when_empty_data_base : SupervisorTeamsAndStatusesReportContext
    {
        Establish context = () =>
        {
            reportFactory = CreateTeamsAndStatusesReport();
        };

        private Because of = () => report = postgresTransactionManager.ExecuteInQueryTransaction(() => reportFactory.Load(new TeamsAndStatusesInputModel()));

        private It should_return_0_records = () => report.TotalCount.ShouldEqual(0);

        private static SupervisorTeamsAndStatusesReport reportFactory;
        private static TeamsAndStatusesReportView report;
    }
}

