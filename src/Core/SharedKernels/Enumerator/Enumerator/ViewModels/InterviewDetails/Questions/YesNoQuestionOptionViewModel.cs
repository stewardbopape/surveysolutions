﻿using System;
using System.Linq;
using Cirrious.MvvmCross.ViewModels;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions.State;


namespace WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions
{
    public class YesNoQuestionOptionViewModel : MvxNotifyPropertyChanged
    {
        public YesNoQuestionViewModel QuestionViewModel { get; private set; }
        public QuestionStateViewModel<YesNoQuestionAnswered> QuestionState { get; set; }

        public YesNoQuestionOptionViewModel(YesNoQuestionViewModel questionViewModel,
            QuestionStateViewModel<YesNoQuestionAnswered> questionState)
        {
            this.QuestionViewModel = questionViewModel;
            this.QuestionState = questionState;
        }

        public decimal Value { get; set; }

        private string title;
        public string Title
        {
            get { return this.title; }
            set { this.title = value; this.RaisePropertyChanged(); }
        }

        private bool? selected;

        public bool? Selected
        {
            get { return this.selected; }
            set
            {
                if (this.selected == value)
                    return;

                this.selected = value;

                this.RaisePropertyChanged();
                this.RaisePropertyChanged(() => YesSelected);
                this.RaisePropertyChanged(() => NoSelected);
            }
        }

        public bool YesSelected
        {
            get { return this.Selected.HasValue && this.Selected.Value; }
            set
            {
                if (this.YesSelected == value)
                    return;

                this.Selected = value;
                this.RaiseAnswerCommand.Execute();
            }
        }

        public bool NoSelected
        {
            get { return this.Selected.HasValue && !this.Selected.Value; }
            set
            {
                if (this.NoSelected == value)
                    return;

                this.Selected = !value;
                this.RaiseAnswerCommand.Execute();
            }
        }

        private int? yesCheckedOrder;

        public int? YesCheckedOrder
        {
            get { return this.yesCheckedOrder; }
            set
            {
                if (this.yesCheckedOrder == value)
                    return;

                this.yesCheckedOrder = value;
                this.RaisePropertyChanged();
            }
        }

        public int? AnswerCheckedOrder { get; set; }

        public IMvxCommand RaiseAnswerCommand
        {
            get { return new MvxCommand(async () => await this.QuestionViewModel.ToggleAnswerAsync(this)); }
        }

        public IMvxCommand RemoveAnswerCommand
        {
            get
            {
                return new MvxCommand(() => {
                    this.Selected = null;
                    this.RaiseAnswerCommand.Execute();
                });
            }
        }
    }
}