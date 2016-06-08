using System.Collections.Generic;
using Machine.Specifications;
using Moq;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions.State;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.Enumerator.ViewModels.FilteredSingleOptionQuestionViewModelTests
{
    /*internal class when_entering_filter_text : FilteredSingleOptionQuestionViewModelTestsContext
    {
        Establish context = () =>
        {
            var interviewId = "interviewId";
            questionStateMock = new Mock<QuestionStateViewModel<SingleOptionQuestionAnswered>> { DefaultValue = DefaultValue.Mock };
            answeringViewModelMock = new Mock<AnsweringViewModel>() { DefaultValue = DefaultValue.Mock };

            var optionsRepo = SetupOptionsRepositoryForQuestionnaire();


            viewModel = CreateFilteredSingleOptionQuestionViewModel(
                questionStateViewModel: questionStateMock.Object,
                answering: answeringViewModelMock.Object);


            viewModel.Options = new List<FilteredSingleOptionQuestionViewModel.FilteredComboboxItemViewModel>()
            {
                new FilteredSingleOptionQuestionViewModel.FilteredComboboxItemViewModel() {Text = "abc", Value = 1},
                new FilteredSingleOptionQuestionViewModel.FilteredComboboxItemViewModel() {Text = "bac", Value = 2},
                new FilteredSingleOptionQuestionViewModel.FilteredComboboxItemViewModel() {Text = "bbc", Value = 3},
                new FilteredSingleOptionQuestionViewModel.FilteredComboboxItemViewModel() {Text = "bba", Value = 4},
                new FilteredSingleOptionQuestionViewModel.FilteredComboboxItemViewModel() {Text = "ccc", Value = 5},
            };

            var navigationState = Create.Other.NavigationState();
            viewModel.Init(interviewId, questionIdentity, navigationState);
        };

        Because of = () =>
            viewModel.FilterText = "a";

        It should_update_suggestions_list = () =>
            viewModel.AutoCompleteSuggestions.Count.ShouldEqual(3);

        It should_suggestions_list_contains_only_items_after_filtering_text = () =>
        {
            viewModel.AutoCompleteSuggestions.ShouldContain(i => i.Value == 1);
            viewModel.AutoCompleteSuggestions.ShouldContain(i => i.Value == 2);
            viewModel.AutoCompleteSuggestions.ShouldContain(i => i.Value == 4);
        };

        static FilteredSingleOptionQuestionViewModel viewModel;

        private static Mock<QuestionStateViewModel<SingleOptionQuestionAnswered>> questionStateMock;

        private static Mock<AnsweringViewModel> answeringViewModelMock;
    }*/
}