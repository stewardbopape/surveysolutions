using System;
using Main.Core.Entities.SubEntities;
using Main.Core.Events.Questionnaire;
using Main.Core.Utility;
using Ncqrs.Eventing.ServiceModel.Bus;
using WB.Core.BoundedContexts.Supervisor.Views.Questionnaire;
using WB.Core.Infrastructure.FunctionalDenormalization;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;

namespace WB.Core.BoundedContexts.Supervisor.EventHandler
{
    public class QuestionnaireQuestionsInfoDenormalizer : IEventHandler, IEventHandler<TemplateImported>
    {
        private readonly IReadSideRepositoryWriter<QuestionnaireQuestionsInfo> questionnaires;

        public QuestionnaireQuestionsInfoDenormalizer(IReadSideRepositoryWriter<QuestionnaireQuestionsInfo> questionnaires)
        {
            this.questionnaires = questionnaires;
        }

        public string Name
        {
            get { return this.GetType().Name; }
        }

        public Type[] UsesViews
        {
            get { return new Type[0]; }
        }

        public Type[] BuildsViews
        {
            get { return new[] { typeof(QuestionnaireQuestionsInfo) }; }
        }

        public void Handle(IPublishedEvent<TemplateImported> evnt)
        {
            var map = new QuestionnaireQuestionsInfo(evnt.Payload.Source);
         
            this.questionnaires.Store(map, RepositoryKeysHelper.GetVersionedKey(evnt.EventSourceId, evnt.EventSequence));
        }
    }
}