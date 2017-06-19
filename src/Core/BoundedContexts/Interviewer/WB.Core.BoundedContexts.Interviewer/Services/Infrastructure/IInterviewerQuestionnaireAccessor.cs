﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Main.Core.Documents;
using WB.Core.BoundedContexts.Interviewer.Views;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.Enumerator.Views;
using WB.Core.SharedKernels.Questionnaire.Translations;

namespace WB.Core.BoundedContexts.Interviewer.Services.Infrastructure
{
    public interface IInterviewerQuestionnaireAccessor
    {
        void StoreQuestionnaire(QuestionnaireIdentity questionnaireIdentity, string questionnaireDocument,
            bool census, List<TranslationDto> translationDtos);

        void RemoveQuestionnaire(QuestionnaireIdentity questionnaireIdentity);

        Task StoreQuestionnaireAssemblyAsync(QuestionnaireIdentity questionnaireIdentity, byte[] assembly);

        QuestionnaireDocument GetQuestionnaire(QuestionnaireIdentity questionnaireIdentity);

        List<QuestionnaireIdentity> GetCensusQuestionnaireIdentities();

        bool IsQuestionnaireExists(QuestionnaireIdentity questionnaireIdentity);

        bool IsQuestionnaireAssemblyExists(QuestionnaireIdentity questionnaireIdentity);

        IReadOnlyCollection<QuestionnaireDocumentView> LoadAll();

        void StoreTranslations(QuestionnaireIdentity questionnaireIdentity, List<TranslationInstance> translationInstances);
    }
}