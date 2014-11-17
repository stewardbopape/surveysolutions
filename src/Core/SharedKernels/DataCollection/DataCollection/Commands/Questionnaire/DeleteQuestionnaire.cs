﻿using System;
using Ncqrs.Commanding;

namespace WB.Core.SharedKernels.DataCollection.Commands.Questionnaire
{
    [Serializable]
    public class DeleteQuestionnaire : QuestionnaireCommand
    {
        public DeleteQuestionnaire(Guid questionnaireId, long questionnaireVersion, Guid? responsibleId)
            : base(questionnaireId, questionnaireId)
        {
            this.ResponsibleId = responsibleId;
            this.QuestionnaireVersion = questionnaireVersion;
        }

        public long QuestionnaireVersion { get; set; }
        public Guid? ResponsibleId { get; set; }
    }
}
