﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.AspNet.SignalR;
using WB.Core.GenericSubdomains.Portable.ServiceLocation;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Exceptions;
using WB.Core.SharedKernels.DataCollection.Repositories;

namespace WB.Enumerator.Native.WebInterview
{
    public abstract class WebInterview : Hub, IErrorDetailsProvider
    {
        private   IStatefulInterviewRepository statefulInterviewRepository => this.ServiceLocator.GetInstance<IStatefulInterviewRepository>();
        protected ICommandService commandService => this.ServiceLocator.GetInstance<ICommandService>();
        private   IQuestionnaireStorage questionnaireRepository => this.ServiceLocator.GetInstance<IQuestionnaireStorage>();
        public IServiceLocator ServiceLocator { get; private set; }

        protected string CallerInterviewId => this.Context.QueryString[@"interviewId"];
        private string CallerSectionid => this.Clients.Caller.sectionId;

        protected IStatefulInterview GetCallerInterview() => this.statefulInterviewRepository.Get(this.CallerInterviewId);

        protected IQuestionnaire GetCallerQuestionnaire()
        {
            var interview = this.GetCallerInterview();
            return this.questionnaireRepository.GetQuestionnaire(interview.QuestionnaireIdentity, interview.Language);
        }

        protected virtual bool IsReviewMode => false;

        protected virtual bool IsCurrentUserObserving => false;

        public WebInterview()
        {
        }

        public void FillExceptionData(Dictionary<string, string> data)
        {
            var interviewId = CallerInterviewId;
            if (interviewId != null) data["caller.interviewId"] = interviewId;
        }

        [Localizable(false)]
        public static string GetConnectedClientSectionKey(Identity sectionId, Guid interviewId) => $"{sectionId}x{interviewId}";

        [Localizable(false)]
        public static string GetConnectedClientPrefilledSectionKey(Guid interviewId) => $"PrefilledSectionx{interviewId}";
        

        public void SetServiceLocator(IServiceLocator serviceLocator)
        {
            this.ServiceLocator = serviceLocator;
        }
    }
}
