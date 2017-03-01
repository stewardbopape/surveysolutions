using System;
using System.Collections.Generic;
using System.Linq;
using AppDomainToolkit;
using Machine.Specifications;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using Ncqrs.Spec;
using NUnit.Framework;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Events.Interview;

namespace WB.Tests.Integration.InterviewTests.EnablementAndValidness
{
    [TestFixture]
    internal class when_answering_question_that_enables_section_with_nested_rosters_inside_it : InterviewTestsContext
    {
        Establish context = () =>
        {
            appDomainContext = AppDomainContext.Create();
        };

        Because of = () =>
            results = Execute.InStandaloneAppDomain(appDomainContext.Domain, () =>
            {
                Setup.MockedServiceLocator();

                Guid userId = Guid.NewGuid();

                var questionnaireDocument = Abc.Create.Entity.QuestionnaireDocument(questionnaireId,
                    Abc.Create.Entity.Group(Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC"), children: new IComposite[]
                    {
                        Abc.Create.Entity.NumericIntegerQuestion(numId, variable: "x1")
                    }),
                    Abc.Create.Entity.Group(Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD"), enablementCondition: "x1 == 1", children: new IComposite[]
                    {
                        Abc.Create.Entity.TextListQuestion(questionId: list1Id, variable: "l1"),
                        Abc.Create.Entity.Roster(roster1Id, rosterSizeQuestionId: list1Id, variable: "r1", rosterSizeSourceType:RosterSizeSourceType.Question, children: new IComposite[]
                        {
                            Abc.Create.Entity.TextListQuestion(questionId: list2Id, variable: "l2"),
                            Abc.Create.Entity.Roster(roster2Id, rosterSizeQuestionId: list2Id, variable: "r2", rosterSizeSourceType:RosterSizeSourceType.Question, children: new IComposite[]
                            {
                                Abc.Create.Entity.TextQuestion(questionId: textId, variable: null)
                            })
                        })
                    }));

                var interview = SetupStatefullInterview(questionnaireDocument);
                interview.AnswerNumericIntegerQuestion(Abc.Create.Command.AnswerNumericIntegerQuestionCommand(interview.Id, userId, questionId: numId, answer: 1));
                interview.AnswerTextListQuestion(userId, list1Id, RosterVector.Empty, DateTime.Now, new[] { Tuple.Create(1m, "Hello") });
                interview.AnswerTextListQuestion(userId, list2Id, IntegrationCreate.RosterVector(1), DateTime.Now, new[] { Tuple.Create(1m, "World") });

                var invokeResults = new InvokeResults();
                using (var eventContext = new EventContext())
                {
                    interview.AnswerNumericIntegerQuestion(Abc.Create.Command.AnswerNumericIntegerQuestionCommand(numId, answer: 2));
                    invokeResults.SubGroupGotEnablementEvents = eventContext.AnyEvent<GroupsDisabled>(x => x.Groups.Any(y => y.Id == roster2Id));
                }


                using (new EventContext())
                {
                    interview.AnswerNumericIntegerQuestion(Guid.NewGuid(), numId, RosterVector.Empty, DateTime.Now, 1);
                    invokeResults.TopRosterIsEnabled = interview.IsEnabled(IntegrationCreate.Identity(roster1Id, IntegrationCreate.RosterVector(1)));
                    invokeResults.NestedRosterIsEnabled = interview.IsEnabled(IntegrationCreate.Identity(roster2Id, IntegrationCreate.RosterVector(1, 1)));
                    return invokeResults;
                }
            });

        It should_not_raise_enablement_events_for_subgroups = () => results.SubGroupGotEnablementEvents.ShouldBeFalse();

        It should_mark_nested_roster_as_enabled = () => results.NestedRosterIsEnabled.ShouldBeTrue();

        It should_mark_top_level_roster_as_enabled = () => results.TopRosterIsEnabled.ShouldBeTrue();


        private Cleanup stuff = () =>
        {
            appDomainContext.Dispose();
            appDomainContext = null;
        };

        private static InvokeResults results;
        private static AppDomainContext<AssemblyTargetLoader, PathBasedAssemblyResolver> appDomainContext;
        private static Guid questionnaireId = Guid.Parse("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
        private static Guid roster1Id = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        private static Guid roster2Id = Guid.Parse("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
        private static Guid numId = Guid.Parse("11111111111111111111111111111111");
        private static Guid list1Id = Guid.Parse("22222222222222222222222222222222");
        private static Guid list2Id = Guid.Parse("33333333333333333333333333333333");
        private static Guid textId = Guid.Parse("44444444444444444444444444444444");

        [Serializable]
        internal class InvokeResults
        {
            public bool TopRosterIsEnabled { get; set; }
            public bool NestedRosterIsEnabled { get; set; }
            public bool SubGroupGotEnablementEvents { get; set; }
        }
    }
}