using System;
using System.Collections.Generic;

using AppDomainToolkit;

using Machine.Specifications;

using Main.Core.Entities.SubEntities;

using Ncqrs.Spec;

using WB.Core.SharedKernels.DataCollection.Events.Interview;

namespace WB.Tests.Integration.InterviewTests.EnablementAndValidness
{
    internal class when_answering_multiple_options_question_which_is_roster_size_question_and_roster_was_disabled_and_deleted : InterviewTestsContext
    {
        Establish context = () =>
        {
            appDomainContext = AppDomainContext.Create();
        };

        Because of = () =>
            results = Execute.InStandaloneAppDomain(appDomainContext.Domain, () =>
            {
                Setup.MockedServiceLocator();

                var userId = Guid.Parse("11111111111111111111111111111111");

                var questionnaireId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
                var idOfQuestionInRoster = Guid.Parse("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
                var rosterId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
                var multiOptionQuestionId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");

                var questionnaireDocument = Create.QuestionnaireDocument(questionnaireId,
                    Create.MultyOptionsQuestion(multiOptionQuestionId, variable:"q1",
                        answers: new List<Answer>{ Create.Option(text:"Hello", value: "1"), Create.Option(text:"World", value: "2") }),
                    Create.Roster(rosterId, 
                        rosterSizeQuestionId: multiOptionQuestionId,
                        rosterSizeSourceType: RosterSizeSourceType.Question,
                        enablementCondition: "!q1.Contains(2)",
                        children: new[]
                                  {
                                      Create.Question(idOfQuestionInRoster, variable:"q2")
                                  })
                    );

                var emptyVector = new decimal[] {};
                var interview = SetupInterview(questionnaireDocument, new object[]
                    {
                        // This is unsupportable mess and I don't like idea to write test this way!

                        //Create.Event.MultipleOptionsQuestionAnswered(multiOptionQuestionId, selectedValues: new []{ 1m }),
                        //Create.Event.RosterInstancesAdded(Create.AddedRosterInstance(rosterId, null, 1)),
                        //Create.Event.MultipleOptionsQuestionAnswered(multiOptionQuestionId, selectedValues: new []{ 1m, 2m }),
                        //Create.Event.RosterInstancesAdded(Create.AddedRosterInstance(rosterId, null, 2m)),
                        //Create.Event.GroupsDisabled(Create.Identity(rosterId, new [] {1m}), Create.Identity(rosterId,  new [] {2m})),
                        //Create.Event.QuestionsDisabled(Create.Identity(idOfQuestionInRoster, new [] {1m}), Create.Identity(idOfQuestionInRoster,  new [] {2m})),
                        //Create.Event.MultipleOptionsQuestionAnswered(multiOptionQuestionId, selectedValues: new []{ 2m }),
                        //Create.Event.RosterInstancesRemoved(Create.RosterInstance(rosterId, null, 1)),
                        //Create.Event.GroupsEnabled(Create.Identity(rosterId, new [] {1m})),
                        //Create.Event.QuestionsEnabled(Create.Identity(idOfQuestionInRoster, new [] {1m})),
                        //Create.Event.MultipleOptionsQuestionAnswered(multiOptionQuestionId, selectedValues: emptyVector),
                        //Create.Event.RosterInstancesRemoved(Create.RosterInstance(rosterId, null, 2)),
                        //Create.Event.GroupsEnabled(Create.Identity(rosterId, new [] {2m})),
                        //Create.Event.QuestionsEnabled(Create.Identity(idOfQuestionInRoster, new [] {2m})),
                        //Create.Event.MultipleOptionsQuestionAnswered(multiOptionQuestionId, selectedValues: new []{ 1m }),
                        //Create.Event.RosterInstancesAdded(Create.AddedRosterInstance(rosterId, emptyVector, 1))
                    });

                interview.AnswerMultipleOptionsQuestion(userId, multiOptionQuestionId, Empty.RosterVector, DateTime.Now, new decimal[] { 1 });
                interview.AnswerMultipleOptionsQuestion(userId, multiOptionQuestionId, Empty.RosterVector, DateTime.Now, new decimal[] { 1, 2 });
                interview.AnswerMultipleOptionsQuestion(userId, multiOptionQuestionId, Empty.RosterVector, DateTime.Now, new decimal[] { 2 });
                interview.AnswerMultipleOptionsQuestion(userId, multiOptionQuestionId, Empty.RosterVector, DateTime.Now, emptyVector);
                interview.AnswerMultipleOptionsQuestion(userId, multiOptionQuestionId, Empty.RosterVector, DateTime.Now, new decimal[] { 1 });

                using (var eventContext = new EventContext())
                {
                    interview.AnswerTextQuestion(userId, idOfQuestionInRoster, new decimal[] { 1 }, DateTime.Now, "Hello World!");

                    return new InvokeResults()
                           {
                               WasTextQuestionAnswered = HasEvent<TextQuestionAnswered>(eventContext.Events)
                           };
                }
            });

        It should_raise_TextQuestionAnswered_event = () =>
            results.WasTextQuestionAnswered.ShouldBeTrue();

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
            public bool WasTextQuestionAnswered { get; set; }
        }
    }
}