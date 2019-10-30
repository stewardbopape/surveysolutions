﻿using System;

namespace WB.Core.BoundedContexts.Designer.Views.Questionnaire.ChangeHistory
{
    public class QuestionnaireRevision
    {
        public QuestionnaireRevision(Guid questionnaireId, Guid? revision = null)
        {
            this.QuestionnaireId = questionnaireId;
            this.Revision = revision;
        }

        public QuestionnaireRevision(string questionnaireId)
        {
            this.QuestionnaireId = Guid.Parse(questionnaireId);
        }

        public Guid QuestionnaireId { get; }
        public Guid? Revision { get; }

        public override string ToString() => Revision.HasValue ? $"{QuestionnaireId:N}${Revision:N}" : $"{QuestionnaireId:N}";
    }
}
