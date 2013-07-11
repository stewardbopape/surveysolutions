using System;
using Main.Core.Documents;
using Main.Core.Domain;
using Ncqrs.Commanding;
using Ncqrs.Commanding.CommandExecution.Mapping.Attributes;

namespace Main.Core.Commands.Questionnaire
{
    [MapsToAggregateRootMethodOrConstructor(typeof(QuestionnaireAR), "ImportQuestionnaire")]
    public class ImportQuestionnaireCommand : CommandBase
    {
        public ImportQuestionnaireCommand(Guid createdBy, IQuestionnaireDocument source)
            : base(source.PublicKey)
        {
            CreatedBy = createdBy;
            Source = source;
            QuestionnaireId = source.PublicKey;
        }

        /// <summary>
        ///     Gets or sets the created by.
        /// </summary>
        public Guid CreatedBy { get; private set; }
        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        public IQuestionnaireDocument Source { get; private set; }

        [AggregateRootId]
        public Guid QuestionnaireId { get; private set; }
    }
}
