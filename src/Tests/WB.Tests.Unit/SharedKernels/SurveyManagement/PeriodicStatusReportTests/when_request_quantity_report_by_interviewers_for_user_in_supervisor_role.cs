﻿using System.Web.Mvc;
using Machine.Specifications;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts;
using WB.Core.SharedKernels.SurveyManagement.Web.Controllers;
using WB.Core.SharedKernels.SurveyManagement.Web.Models;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.PeriodicStatusReportTests
{
    internal class when_request_quantity_report_by_interviewers_for_user_in_supervisor_role : PeriodicStatusReportControllerTestContext
    {
        Establish context = () =>
        {
            periodicStatusReportController = CreatePeriodicStatusReportController();
        };

        Because of = () =>
            result = periodicStatusReportController.QuantityByInterviewers(null) as ViewResult;

        It should_active_page_be_NumberOfCompletedInterviews = () =>
            ((MenuItem)result.ViewBag.ActivePage).ShouldEqual(MenuItem.NumberOfCompletedInterviews);

        It should_responsible_name_be_not_a_link = () =>
            ((PeriodicStatusReportModel)result.Model).CanNavigateToQuantityByTeamMember.ShouldEqual(false);

        It should_go_back_to_supervisor_button_be_invisible = () =>
            ((PeriodicStatusReportModel)result.Model).CanNavigateToQuantityBySupervisors.ShouldEqual(false);

        It should_WebApiActionName_be_ByInterviewers = () =>
           ((PeriodicStatusReportModel)result.Model).WebApiActionName.ShouldEqual(PeriodicStatusReportWebApiActionName.ByInterviewers);

        It should_ReportName_be_Quantity = () =>
           ((PeriodicStatusReportModel)result.Model).ReportName.ShouldEqual("Quantity");
            
        private static PeriodicStatusReportController periodicStatusReportController;
        private static ViewResult result;
    }
}
