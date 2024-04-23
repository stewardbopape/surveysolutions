﻿using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.ValueObjects;

namespace WB.Core.SharedKernels.DataCollection.WebApi;

public class QuestionnaireSettingsApiView
{
    public QuestionnaireIdentity QuestionnaireIdentity { get; set; }
    public CriticalityLevel? CriticalityLevel { get; set; }
    public bool? IsSwitchableToWeb { set; get; }
}
