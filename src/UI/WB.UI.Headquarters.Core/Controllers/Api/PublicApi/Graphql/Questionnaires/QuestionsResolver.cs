using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Resolvers;
using WB.Core.BoundedContexts.Headquarters.Factories;
using WB.Core.BoundedContexts.Headquarters.Implementation.Factories;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.BoundedContexts.Headquarters.Views.Questionnaire;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;

namespace WB.UI.Headquarters.Controllers.Api.PublicApi.Graphql.Questionnaires
{
    public class QuestionsResolver
    {
        public IEnumerable<QuestionnaireCompositeItem> Questions(string variable, 
            long version,
            string language,
            [Service] IQuestionnaireStorage storage, 
            [Service] IQuestionnaireBrowseViewFactory browseViewFactory,
            [Service] IResolverContext resolverContext)
        {
            var questionnaireBrowseItem = browseViewFactory.GetByVariableName(variable, version);
            var questionnaire = storage.GetQuestionnaire(questionnaireBrowseItem.Identity(), language);
            var featured = questionnaire.GetPrefilledQuestions().ToHashSet();

            resolverContext.ScopedContextData = resolverContext.ScopedContextData.SetItem("language", language);

            return from q in questionnaire.GetAllQuestions()
                select new QuestionnaireCompositeItem
                {
                    Id = questionnaire.GetEntityIdMapValue(q),
                    EntityId = q,
                    EntityType = EntityType.Question,
                    Featured = featured.Contains(q),
                    StataExportCaption = questionnaire.GetQuestionVariableName(q),
                    VariableLabel = questionnaire.GetQuestionExportDescription(q),
                    QuestionType = questionnaire.GetQuestionType(q),
                    QuestionText = questionnaire.GetQuestionTitle(q),
                    QuestionScope = questionnaire.GetQuestionScope(q)
                };
        }
    }
}
