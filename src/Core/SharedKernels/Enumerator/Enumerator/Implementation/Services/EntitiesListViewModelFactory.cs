﻿using System;
using System.Collections.Generic;
using System.Linq;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.ViewModels;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails;

namespace WB.Core.SharedKernels.Enumerator.Implementation.Services
{
    internal class EntitiesListViewModelFactory : IEntitiesListViewModelFactory
    {
        private readonly IStatefulInterviewRepository interviewRepository;
        private readonly IInterviewViewModelFactory interviewViewModelFactory;
        private readonly IDynamicTextViewModelFactory dynamicTextViewModelFactory;

        private readonly int maxNumberOfEntities = 30;

        public EntitiesListViewModelFactory(
            IStatefulInterviewRepository interviewRepository, 
            IInterviewViewModelFactory interviewViewModelFactory, 
            IDynamicTextViewModelFactory dynamicTextViewModelFactory)
        {
            this.interviewRepository = interviewRepository;
            this.interviewViewModelFactory = interviewViewModelFactory;
            this.dynamicTextViewModelFactory = dynamicTextViewModelFactory;
        }

        public IEnumerable<EntityWithErrorsViewModel> GetTopEntitiesWithErrors(string interviewId, NavigationState navigationState)
        {
            IStatefulInterview interview = this.interviewRepository.Get(interviewId);
            Identity[] invalidEntities = interview.GetInvalidEntitiesInInterview().Take(this.maxNumberOfEntities).ToArray();
           
            return this.EntityWithErrorsViewModels<EntityWithErrorsViewModel>(interviewId, navigationState, invalidEntities, interview);
        }

        public IEnumerable<EntityWithErrorsViewModel> GetTopUnansweredQuestions(string interviewId, NavigationState navigationState)
        {
            IStatefulInterview interview = this.interviewRepository.Get(interviewId);
            Identity[] invalidEntities = interview.GetAllUnansweredQuestions().Take(this.maxNumberOfEntities).ToArray();
            var viewModels = this.EntityWithErrorsViewModels<EntityWithErrorsViewModel>(interviewId, navigationState, invalidEntities, interview);
            return viewModels.Select(vm =>
                {
                    vm.IsError = false;
                    return vm;
                });
        }

        public IEnumerable<EntityWithCommentsViewModel> GetTopEntitiesWithComments(string interviewId, NavigationState navigationState)
        {
            IStatefulInterview interview = this.interviewRepository.Get(interviewId);
            Identity[] commentedBySupervisorEntities = interview.GetCommentedBySupervisorQuestionsVisibleToInterviewer().Take(this.maxNumberOfEntities).ToArray();

            return this.EntityWithErrorsViewModels<EntityWithCommentsViewModel>(interviewId, navigationState, commentedBySupervisorEntities, interview);
        }

        public IEnumerable<EntityWithErrorsViewModel> GetTopUnansweredCriticalQuestions(string interviewId, NavigationState navigationState)
        {
            IStatefulInterview interview = this.interviewRepository.GetOrThrow(interviewId);
            Identity[] invalidEntities = interview.GetAllUnansweredCriticalQuestions().Take(this.maxNumberOfEntities).ToArray();

            var viewModels = this.EntityWithErrorsViewModels<EntityWithErrorsViewModel>(interviewId, navigationState, invalidEntities, interview);
            return viewModels;
        }

        public IEnumerable<FailedCriticalRuleViewModel> GetTopFailedCriticalRules(string interviewId, NavigationState navigationState)
        {
            IStatefulInterview interview = this.interviewRepository.GetOrThrow(interviewId);
            Guid[] ids = interview.CollectInvalidCriticalRules().Take(this.maxNumberOfEntities).ToArray();
           
            var criticalRules = new List<FailedCriticalRuleViewModel>();
            foreach (var id in ids)
            {
                var criticalityConditionMessage = interview.GetCriticalRuleMessage(id);
                var failedCriticalRuleViewModel = this.interviewViewModelFactory.GetNew<FailedCriticalRuleViewModel>();
                
                using (var title = this.dynamicTextViewModelFactory.CreateDynamicTextViewModel())
                {
                    title.InitAsStatic(criticalityConditionMessage);
                    failedCriticalRuleViewModel.Init(title.PlainText);
                }

                criticalRules.Add(failedCriticalRuleViewModel);
            }
            
            return criticalRules;
        }

        private IEnumerable<T> EntityWithErrorsViewModels<T>(string interviewId, NavigationState navigationState,
            Identity[] invalidEntities, IStatefulInterview interview) where T : ListEntityViewModel
        {
            var entitiesWithErrors = new List<T>();
            foreach (var invalidEntity in invalidEntities)
            {
                var entityWithErrorsViewModel = this.interviewViewModelFactory.GetNew<T>();

                var navigationIdentity = interview.IsQuestionPrefilled(invalidEntity)
                    ? NavigationIdentity.CreateForPrefieldScreen()
                    : NavigationIdentity.CreateForGroup(interview.GetParentGroup(invalidEntity), invalidEntity);

                //remove registration from bus
                using (var title = this.dynamicTextViewModelFactory.CreateDynamicTextViewModel())
                {
                    title.Init(interviewId, invalidEntity);
                    entityWithErrorsViewModel.Init(navigationIdentity, title.PlainText, navigationState);
                }

                entitiesWithErrors.Add(entityWithErrorsViewModel);
            }
            return entitiesWithErrors;
        }

        public int MaxNumberOfEntities => this.maxNumberOfEntities;
    }
}
