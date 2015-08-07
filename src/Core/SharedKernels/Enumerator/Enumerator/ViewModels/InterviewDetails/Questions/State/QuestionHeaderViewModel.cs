using System;
using System.Linq;
using Cirrious.MvvmCross.ViewModels;
using WB.Core.BoundedContexts.Tester.Implementation.Aggregates;
using WB.Core.BoundedContexts.Tester.Implementation.Entities;
using WB.Core.BoundedContexts.Tester.Implementation.Entities.QuestionModels;
using WB.Core.BoundedContexts.Tester.Repositories;
using WB.Core.BoundedContexts.Tester.Services;
using WB.Core.Infrastructure.EventBus.Lite;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.SurveySolutions.Services;

namespace WB.Core.BoundedContexts.Tester.ViewModels.Questions.State
{
    public class QuestionHeaderViewModel : MvxNotifyPropertyChanged,
        ILiteEventHandler<SubstitutionTitlesChanged>
    {
        public string Instruction { get; set; }
        private string title;
        public string Title
        {
            get { return this.title; }
            set { this.title = value; this.RaisePropertyChanged(); }
        }

        private readonly IPlainKeyValueStorage<QuestionnaireModel> questionnaireRepository;
        private readonly IStatefulInterviewRepository interviewRepository;
        private readonly ILiteEventRegistry registry;
        private readonly ISubstitutionService substitutionService;
        private readonly IAnswerToUIStringService answerToUIStringService;
        private readonly IRosterTitleSubstitutionService rosterTitleSubstitutionService;
        private Identity questionIdentity;
        private string interviewId;

        public void Init(string interviewId, Identity questionIdentity)
        {
            if (interviewId == null) throw new ArgumentNullException("interviewId");
            if (questionIdentity == null) throw new ArgumentNullException("questionIdentity");

            var interview = this.interviewRepository.Get(interviewId);
            QuestionnaireModel questionnaire = this.questionnaireRepository.GetById(interview.QuestionnaireId);
            var questionModel = questionnaire.Questions[questionIdentity.Id];

            this.Title = questionModel.Title;
            this.Instruction = questionModel.Instructions;
            this.questionIdentity = questionIdentity;
            this.interviewId = interviewId;

            this.CalculateSubstitutions(questionnaire, interview);

            this.registry.Subscribe(this, interviewId);
        }

        protected QuestionHeaderViewModel() { }

        public QuestionHeaderViewModel(
            IPlainKeyValueStorage<QuestionnaireModel> questionnaireRepository,
            IStatefulInterviewRepository interviewRepository,
            ILiteEventRegistry registry,
            ISubstitutionService substitutionService,
            IAnswerToUIStringService answerToUIStringService,
            IRosterTitleSubstitutionService rosterTitleSubstitutionService)
        {
            this.questionnaireRepository = questionnaireRepository;
            this.interviewRepository = interviewRepository;
            this.registry = registry;
            this.substitutionService = substitutionService;
            this.answerToUIStringService = answerToUIStringService;
            this.rosterTitleSubstitutionService = rosterTitleSubstitutionService;
        }

        public void Handle(SubstitutionTitlesChanged @event)
        {
            bool thisQuestionChanged = @event.Questions.Any(x => this.questionIdentity.ToIdentityForEvents().Equals(x));

            if (thisQuestionChanged)
            {
                var interview = this.interviewRepository.Get(this.interviewId);
                QuestionnaireModel questionnaire = this.questionnaireRepository.GetById(interview.QuestionnaireId);

                this.CalculateSubstitutions(questionnaire, interview);
            }
        }

        private void CalculateSubstitutions(QuestionnaireModel questionnaire, IStatefulInterview interview)
        {
            BaseQuestionModel questionModel = questionnaire.Questions[this.questionIdentity.Id];

            string questionTitle = questionModel.Title;
            if (this.substitutionService.ContainsRosterTitle(questionTitle))
            {
                questionTitle = this.rosterTitleSubstitutionService.Substitute(questionModel.Title,
                    this.questionIdentity, this.interviewId);
            }
            string[] variablesToReplace = this.substitutionService.GetAllSubstitutionVariableNames(questionTitle);

            foreach (var variable in variablesToReplace)
            {
                BaseQuestionModel substitutedQuestionModel = questionnaire.QuestionsByVariableNames[variable];

                var baseInterviewAnswer = interview.FindBaseAnswerByOrDeeperRosterLevel(substitutedQuestionModel.Id, this.questionIdentity.RosterVector);
                string answerString = baseInterviewAnswer != null ? this.answerToUIStringService.AnswerToUIString(substitutedQuestionModel, baseInterviewAnswer) : null;

                questionTitle = this.substitutionService.ReplaceSubstitutionVariable(
                    questionTitle, variable, answerString ?? this.substitutionService.DefaultSubstitutionText);
            }

            this.Title = questionTitle;
        }
    }
}