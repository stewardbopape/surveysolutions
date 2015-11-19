﻿using System;
using System.Collections.Generic;
using System.Linq;
using AppDomainToolkit;
using Machine.Specifications;
using Ncqrs.Spec;
using WB.Core.SharedKernels.DataCollection.Events.Interview;

namespace WB.Tests.Integration.InterviewTests.EnablementAndValidness
{
    internal class when_answering_gps_question_A_and_that_answer_disables_question_B_and_disabled_B_disables_question_C_and_B_was_a___ : InterviewTestsContext
    {
        Establish context = () =>
        {
            appDomainContext = AppDomainContext.Create();
        };

        Because of = () =>
            results = Execute.InStandaloneAppDomain(appDomainContext.Domain, () =>
            {
                var emptyRosterVector = new decimal[] { };
                var userId = Guid.Parse("11111111111111111111111111111111");
                var questionnaireId = Guid.Parse("22222222222222222222222222222222");
                var answerTime = new DateTime(2014, 1, 1);

                var questionAId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
                var questionBId = Guid.Parse("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
                var questionCId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");

                Setup.MockedServiceLocator();

                var questionnaireDocument = Create.QuestionnaireDocument(questionnaireId,
                    Create.GpsCoordinateQuestion(questionAId, "gps"),
                    Create.NumericRealQuestion(questionBId, "b", enablementCondition: "gps.Latitude < 0"),
                    Create.NumericRealQuestion(questionCId, "c", enablementCondition: "gps.Latitude < 0")
                );

                var interview = SetupInterview(questionnaireDocument, new List<object>
                {
                    Create.Event.QuestionsEnabled(new []
                    {
                        Create.Identity(questionAId),
                        Create.Identity(questionBId),
                        Create.Identity(questionCId) 
                    }),
                    Create.Event.NumericRealQuestionAnswered(questionBId, 4.2m)
                });

                using (var eventContext = new EventContext())
                {
                    interview.AnswerGeoLocationQuestion(userId, questionAId, emptyRosterVector, answerTime, 10, 10, 10, 10, DateTimeOffset.Now);

                    return new InvokeResults
                    {
                        QuestionBDisabled = GetFirstEventByType<QuestionsDisabled>(eventContext.Events).Questions.FirstOrDefault(q => q.Id == questionBId) != null,
                        QuestionCDisabled = GetFirstEventByType<QuestionsDisabled>(eventContext.Events).Questions.FirstOrDefault(q => q.Id == questionCId) != null,
                    };
                }
            });

        It should_disable_question_B = () =>
            results.QuestionBDisabled.ShouldBeTrue();

        It should_disable_question_C = () =>
            results.QuestionCDisabled.ShouldBeTrue();

        Cleanup stuff = () =>
        {
            appDomainContext.Dispose();
            appDomainContext = null;
        };

        private static InvokeResults results;
        private static AppDomainContext appDomainContext;

        [Serializable]
        internal class InvokeResults
        {
            public bool QuestionBDisabled { get; set; }
            public bool QuestionCDisabled { get; set; }
        }
    }
}