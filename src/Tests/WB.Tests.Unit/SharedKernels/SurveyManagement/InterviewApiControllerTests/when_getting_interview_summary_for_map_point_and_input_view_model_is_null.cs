﻿using Machine.Specifications;
using WB.Core.SharedKernels.SurveyManagement.Views.Interview;
using WB.Core.SharedKernels.SurveyManagement.Web.Controllers;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.InterviewApiControllerTests
{
    internal class when_getting_interview_summary_for_map_point_and_input_view_model_is_null : InterviewApiControllerTestsContext
    {
        private Establish context = () =>
        {
            controller = CreateController();
        };

        Because of = () =>
            viewModel = controller.InterviewSummaryForMapPoint(null);

        It should_view_model_be_null = () =>
            viewModel.ShouldBeNull();

        private static InterviewApiController controller;
        private static InterviewSummaryForMapPointView viewModel;
    }
}
