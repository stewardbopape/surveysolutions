﻿using System;
using Machine.Specifications;
using Moq;
using Ncqrs.Eventing.ServiceModel.Bus;
using WB.Core.GenericSubdomains.Utils;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.SurveyManagement.EventHandler;
using WB.Core.SharedKernels.SurveyManagement.Views.Interview;
using It = Machine.Specifications.It;

namespace WB.Core.SharedKernels.SurveyManagement.Tests.EventHandlers.Interview.InterviewsChartDenormalizerTests
{
    internal class when_handling_interview_created_event_in_chart_denormalizer : InterviewsChartDenormalizerTestContext
    {
        Establish context = () =>
        {
            var interviewDetailsForChart = new InterviewDetailsForChart
            {
                QuestionnaireId = questionnaireId,
                QuestionnaireVersion = 1,
                Status = InterviewStatus.ApprovedBySupervisor
            };

            var interviewDetailsStorage =
                Mock.Of<IReadSideRepositoryWriter<InterviewDetailsForChart>>(x => x.GetById(interviewId.FormatGuid()) == interviewDetailsForChart);

            var questionnaireDetailsForChart = new QuestionnaireDetailsForChart
            {
                QuestionnaireId = questionnaireId,
                QuestionnaireVersion = 1
            };

            var questionnaireDetailsStorage =
                Mock.Of<IReadSideRepositoryWriter<QuestionnaireDetailsForChart>>(x => x.GetById(Moq.It.IsAny<string>()) == questionnaireDetailsForChart);

            var statisticsMock = new StatisticsLineGroupedByDateAndTemplate
            {
                QuestionnaireId = questionnaireId,
                ApprovedBySupervisorCount = 1
            };

            statisticsStorage = new Mock<IReadSideRepositoryWriter<StatisticsLineGroupedByDateAndTemplate>>();
            statisticsStorage.Setup(x => x.GetById(Moq.It.IsAny<string>())).Returns(statisticsMock);
            statisticsStorage.Setup(x => x.Store(Moq.It.IsAny<StatisticsLineGroupedByDateAndTemplate>(), Moq.It.IsAny<string>()))
                .Callback((StatisticsLineGroupedByDateAndTemplate stats, string id) => statistics = stats);

            denormalizer = CreateStatisticsDenormalizer(statisticsStorage.Object, interviewDetailsStorage, questionnaireDetailsStorage);

            evnt = CreateInterviewCreatedEvent(questionnaireId, questionnaireId, 1, interviewId);
        };

        Because of = () =>
            denormalizer.Handle(evnt);

        It should_statistics_storage_stores_new_state = () =>
            statisticsStorage.Verify(x => x.Store(Moq.It.IsAny<StatisticsLineGroupedByDateAndTemplate>(), Moq.It.IsAny<string>()),
                Times.Never);

        private static Guid questionnaireId = Guid.Parse("11111111111111111111111111111111");
        private static Guid interviewId = Guid.Parse("22222222222222222222222222222222");

        private static StatisticsLineGroupedByDateAndTemplate statistics;
        private static Mock<IReadSideRepositoryWriter<StatisticsLineGroupedByDateAndTemplate>> statisticsStorage;
        private static InterviewsChartDenormalizer denormalizer;
        private static IPublishedEvent<InterviewCreated> evnt;

    }
}
