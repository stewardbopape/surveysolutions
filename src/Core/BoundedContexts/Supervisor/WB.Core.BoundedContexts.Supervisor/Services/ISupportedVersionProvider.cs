﻿using WB.Core.SharedKernels.QuestionnaireVerification.ValueObjects;

namespace WB.Core.BoundedContexts.Supervisor.Services
{
    //TODO: Remove when HQ part is separated
    public interface ISupportedVersionProvider
    {
         QuestionnaireVersion GetSupportedQuestionnaireVersion();
    }
}
