using System;
using System.Collections.Generic;
using System.Linq;

namespace WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration.Model
{
    public class RosterScopeTemplateModel : RosterScopeBaseModel
    {
        public RosterScopeTemplateModel(KeyValuePair<string, List<RosterTemplateModel>> rosterScope,
            QuestionnaireExecutorTemplateModel executorModel)
            : base(executorModel.GenerateEmbeddedExpressionMethods, rosterScope.Value.First().ParentScope, String.Empty, rosterScope.Key,
            rosterScope.Value.SelectMany(r => r.Groups).ToList(), rosterScope.Value.SelectMany(r => r.Questions).ToList(),
            rosterScope.Value.SelectMany(r => r.Rosters).ToList(), new List<Guid>())
        {
            this.RostersInScope = rosterScope.Value;
            this.ParentTypeName = rosterScope.Value[0].ParentScope.GeneratedTypeName;
            this.ExecutorModel = executorModel;
            this.Version = executorModel.Version;
        }

        public QuestionnaireExecutorTemplateModel ExecutorModel { private set; get; }

        public string ParentTypeName { set; get; }

        public List<RosterTemplateModel> RostersInScope { set; get; }
    }
}