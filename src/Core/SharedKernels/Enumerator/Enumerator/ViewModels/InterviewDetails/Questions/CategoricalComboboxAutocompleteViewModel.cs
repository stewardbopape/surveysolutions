﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.Enumerator.Properties;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions.State;

namespace WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions
{
    public class CategoricalComboboxAutocompleteViewModel : MvxNotifyPropertyChanged,
        IInterviewEntityViewModel,
        ICompositeQuestion,
        IDisposable
    {
        public virtual event Func<object, int, Task> OnItemSelected;
        public virtual event Func<object, EventArgs, Task> OnAnswerRemoved;
        public virtual event Func<object, EventArgs, Task> OnShowErrorIfNoAnswer;
        private readonly FilteredOptionsViewModel filteredOptionsViewModel;
        private readonly bool displaySelectedValue;

        public CategoricalComboboxAutocompleteViewModel(IQuestionStateViewModel questionState,
            FilteredOptionsViewModel filteredOptionsViewModel,
            bool displaySelectedValue)
        {
            this.QuestionState = questionState;
            this.filteredOptionsViewModel = filteredOptionsViewModel;
            this.displaySelectedValue = displaySelectedValue;
        }

        public void Init(string interviewId, Identity entityIdentity, NavigationState navigationState)
        {
            this.Identity = entityIdentity;
            this.UpdateFilter(null);
        }

        private int[] excludedOptions = Array.Empty<int>();
        public Identity Identity { get; private set; }

        public string FilterText { get; set; }

        private List<OptionWithSearchTerm> autoCompleteSuggestions = new List<OptionWithSearchTerm>();
        public List<OptionWithSearchTerm> AutoCompleteSuggestions
        {
            get => this.autoCompleteSuggestions;
            set => this.RaiseAndSetIfChanged(ref this.autoCompleteSuggestions, value);
        }

        public IMvxCommand<string> FilterCommand => new MvxAsyncCommand<string>(this.UpdateFilter);
        public IMvxCommand RemoveAnswerCommand => new MvxAsyncCommand(async () =>
        {
            await this.UpdateFilter(null);
            if (this.OnAnswerRemoved == null)
                return;

            await InvokeAllHandlers<EventArgs>(this.OnAnswerRemoved, EventArgs.Empty);
        });

     
        public IMvxCommand<OptionWithSearchTerm> SaveAnswerBySelectedOptionCommand => new MvxAsyncCommand<OptionWithSearchTerm>(this.SaveAnswerBySelectedOption);
        public IMvxCommand ShowErrorIfNoAnswerCommand => new MvxAsyncCommand(this.ShowErrorIfNoAnswer);

        private async Task ShowErrorIfNoAnswer()
        {
            await InvokeAllHandlers<EventArgs>(this.OnShowErrorIfNoAnswer, EventArgs.Empty);

            if (string.IsNullOrEmpty(this.FilterText)) return;

            var selectedOption = this.filteredOptionsViewModel.GetOptions(this.FilterText).FirstOrDefault(x => !this.excludedOptions.Contains(x.Value));

            if (selectedOption?.Title.Equals(this.FilterText, StringComparison.CurrentCultureIgnoreCase) == true)
                await this.SaveAnswerBySelectedOption(ToOptionWithSearchTerm(string.Empty, selectedOption));
            else
            {
                var errorMessage = UIResources.Interview_Question_Filter_MatchError.FormatString(this.FilterText);
                await this.QuestionState.Validity.MarkAnswerAsNotSavedWithMessage(errorMessage);
            }
        }

        private async Task SaveAnswerBySelectedOption(OptionWithSearchTerm option)
        {
            await InvokeAllHandlers<int>(this.OnItemSelected, option.Value);
            await this.UpdateFilter(displaySelectedValue ? option.Title : null);
        }

        private async Task InvokeAllHandlers<T>(Func<object, T, Task> handler, T value)
        {
            Delegate[] invocationList = handler.GetInvocationList();
            Task[] handlerTasks = new Task[invocationList.Length];

            for (int i = 0; i < invocationList.Length; i++)
            {
                handlerTasks[i] = ((Func<object, T, Task>) invocationList[i])(this, value);
            }

            await Task.WhenAll(handlerTasks);
        }

        public async Task UpdateFilter(string filter) => await this.InvokeOnMainThreadAsync(async () =>
        {
            this.AutoCompleteSuggestions = this.GetSuggestions(filter).ToList();
            this.FilterText = filter;
            await this.RaisePropertyChanged(() => this.FilterText);
        });

        private IEnumerable<OptionWithSearchTerm> GetSuggestions(string filter)
        {
            List<CategoricalOption> filteredOptions = this.filteredOptionsViewModel.GetOptions(filter);

            foreach (var model in filteredOptions.Count == 1 && displaySelectedValue
                ? filteredOptions
                : filteredOptions.Where(x => !this.excludedOptions.Contains(x.Value)))
            {
                if (model.Title.IsNullOrEmpty())
                    continue;

                yield return ToOptionWithSearchTerm(filter, model);
            }
        }

        private static OptionWithSearchTerm ToOptionWithSearchTerm(string filter, CategoricalOption model) => new OptionWithSearchTerm
        {
            Value = model.Value,
            Title = model.Title,
            SearchTerm = filter
        };

        public void ExcludeOptions(int[] optionsToExclude) => this.excludedOptions = optionsToExclude ?? Array.Empty<int>();

        public void Dispose() { }

        public QuestionInstructionViewModel InstructionViewModel => null;
        public IQuestionStateViewModel QuestionState { get; protected set; }
        public AnsweringViewModel Answering => null;
    }
}
