﻿using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Moq;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.Services;
using WB.Tests.Abc;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.Enumerator.StatefulInterviewTests
{
    internal class when_getting_question_otions : StatefulInterviewTestsContext
    {
        Establish context = () =>
        {
            options = new List<CategoricalOption>()
            {
                new CategoricalOption() {Value = 1, Title = "1"},
                new CategoricalOption() {Value = 2, Title = "2"}
            };

            IQuestionnaireStorage questionnaireRepository = Setup.QuestionnaireRepositoryWithOneQuestionnaire(questionnaireId, 
                _ => _.GetOptionsForQuestion(questionId, null, string.Empty) == options);

            statefulInterview = Create.AggregateRoot.StatefulInterview(questionnaireId: questionnaireId, questionnaireRepository: questionnaireRepository);
        };

        Because of = () =>
        {
            categoricalOptions =
                statefulInterview.GetFirstTopFilteredOptionsForQuestion(questionIdentity, null, string.Empty, 200).ToList();
        };

        It should_contains_2_elements = () =>
            categoricalOptions.Count().ShouldEqual(2);

        It should_question_options = () =>
            categoricalOptions.ShouldEqual(options.ToList());

        static StatefulInterview statefulInterview;

        static readonly Guid questionId = Guid.Parse("11111111111111111111111111111113");
        static readonly Identity questionIdentity = Create.Identity(questionId);
        static IEnumerable<CategoricalOption> options;

        static List<CategoricalOption> categoricalOptions;
        static readonly Guid questionnaireId = Guid.Parse("11111111111111111111111111111112");
        
    }
}
