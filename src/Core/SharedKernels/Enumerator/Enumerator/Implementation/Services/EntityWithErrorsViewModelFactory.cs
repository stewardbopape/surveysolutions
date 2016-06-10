﻿using System;
using System.Collections.Generic;
using System.Linq;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.Enumerator.Aggregates;
using WB.Core.SharedKernels.Enumerator.Repositories;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.ViewModels;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions.State;

namespace WB.Core.SharedKernels.Enumerator.Implementation.Services
{
    internal class EntityWithErrorsViewModelFactory : IEntityWithErrorsViewModelFactory
    {
        private readonly IStatefulInterviewRepository interviewRepository;
        private readonly IPlainQuestionnaireRepository questionnaireRepository;
        private readonly IInterviewViewModelFactory interviewViewModelFactory;
        private readonly ISubstitutionService substitutionService;
        private readonly DynamicTextViewModel title;

        private readonly int maxNumberOfEntities = 30;

        public EntityWithErrorsViewModelFactory(
            IStatefulInterviewRepository interviewRepository, 
            IPlainQuestionnaireRepository questionnaireRepository, 
            IInterviewViewModelFactory interviewViewModelFactory, 
            DynamicTextViewModel title, 
            ISubstitutionService substitutionService)
        {
            this.interviewRepository = interviewRepository;
            this.questionnaireRepository = questionnaireRepository;
            this.interviewViewModelFactory = interviewViewModelFactory;
            this.title = title;
            this.substitutionService = substitutionService;
        }

        public IEnumerable<EntityWithErrorsViewModel> GetEntities(string interviewId, NavigationState navigationState)
        {
            IStatefulInterview interview = this.interviewRepository.Get(interviewId);
            var questionnaire = questionnaireRepository.GetQuestionnaire(interview.QuestionnaireIdentity);
            Identity[] invalidEntities = interview.GetInvalidEntitiesInInterview().Take(this.maxNumberOfEntities).ToArray();
           
            var entitiesWithErrors = new List<EntityWithErrorsViewModel>();
            foreach (var invalidEntity in invalidEntities)
            {
                var entityWithErrorsViewModel = interviewViewModelFactory.GetNew<EntityWithErrorsViewModel>();

                var navigationIdentity = NavigationIdentity.CreateForGroup(interview.GetParentGroup(invalidEntity),
                    invalidEntity);

                var errorTitle = questionnaire.HasQuestion(invalidEntity.Id)
                   ? questionnaire.GetQuestionTitle(invalidEntity.Id)
                   : questionnaire.GetStaticText(invalidEntity.Id);

                title.Init(interviewId, navigationIdentity.AnchoredElementIdentity, errorTitle);

                entityWithErrorsViewModel.Init(navigationIdentity, title.PlainText, navigationState);
                entitiesWithErrors.Add(entityWithErrorsViewModel);
            }
            return entitiesWithErrors;
        }

        public int MaxNumberOfEntities => this.maxNumberOfEntities;
    }
}