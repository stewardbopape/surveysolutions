﻿using System;
using System.Linq;
using Machine.Specifications;
using Moq;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Dtos;
using WB.Core.SharedKernels.Enumerator.Aggregates;
using WB.Core.SharedKernels.Enumerator.Models.Questionnaire;
using WB.Core.SharedKernels.Enumerator.Models.Questionnaire.Questions;
using WB.Core.SharedKernels.Enumerator.Repositories;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.Enumerator.ViewModels.YesNoQuestionViewModelTests
{
    internal class when_handling_question_answered_event : YesNoQuestionViewModelTestsContext
    {
        Establish context = () =>
        {
            questionGuid = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            questionId = Create.Identity(questionGuid, Empty.RosterVector);

            var questionnaire = BuildDefaultQuestionnaire(questionId);
            ((YesNoQuestionModel) questionnaire.Questions.First().Value).AreAnswersOrdered = true;

            var yesNoAnswer = Create.YesNoAnswer(questionGuid, Empty.RosterVector);
            yesNoAnswer.SetAnswers(new[]
            {
                new AnsweredYesNoOption(5, true),  
                new AnsweredYesNoOption(2, false),  
            });

            var interview = Mock.Of<IStatefulInterview>(x => x.GetYesNoAnswer(questionId) == yesNoAnswer);

            var questionnaireStorage = new Mock<IPlainKeyValueStorage<QuestionnaireModel>>();
            var interviewRepository = new Mock<IStatefulInterviewRepository>();

            questionnaireStorage.SetReturnsDefault(questionnaire);
            interviewRepository.SetReturnsDefault(interview);

            viewModel = CreateViewModel(questionnaireStorage: questionnaireStorage.Object,
                interviewRepository: interviewRepository.Object);

            viewModel.Init("blah", questionId, Create.NavigationState());
        };

        Because of = () =>
        {
            viewModel.Handle(new YesNoQuestionAnswered(Guid.NewGuid(), questionGuid, Empty.RosterVector, DateTime.Now, new []
            {
                new AnsweredYesNoOption(5, true), 
                new AnsweredYesNoOption(2, false), 
                new AnsweredYesNoOption(1, true), 
            }));
        };

        It should_set_checked_order_to_yes_options = () =>
        {
            viewModel.Options[4].YesCheckedOrder.ShouldEqual(1);
            viewModel.Options[1].YesCheckedOrder.ShouldEqual(null);
            viewModel.Options[0].YesCheckedOrder.ShouldEqual(2);
        };

        It should_mark_options_as_checked = () => viewModel.Options.Count(x => x.YesSelected).ShouldEqual(2);


        static YesNoQuestionViewModel viewModel;
        static Identity questionId;
        private static Guid questionGuid;
    }
}

