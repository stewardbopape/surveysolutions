﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Main.Core.Documents;
using Main.Core.Domain;
using Main.Core.View;
using Ncqrs;
using Ncqrs.Commanding.CommandExecution;
using Ncqrs.Domain;
using Ncqrs.Eventing;
using Ncqrs.Eventing.ServiceModel.Bus;
using Ncqrs.Eventing.Sourcing.Snapshotting;
using Ncqrs.Eventing.Storage;
using Ncqrs.Restoring.EventStapshoot;
using WB.Core.Questionnaire.ImportService.Commands;

namespace WB.Core.Questionnaire.ImportService
{
    public class DefaultImportService : CommandExecutorBase<ImportQuestionnaireCommand>
    {
        protected override void ExecuteInContext(IUnitOfWorkContext context, ImportQuestionnaireCommand command)
        {
            var document = command.Source as QuestionnaireDocument;
            if (document == null)
                throw new ArgumentException("only QuestionnaireDocuments are supported for now");

            var questionnsire = context.GetById<QuestionnaireAR>(command.CommandIdentifier) ?? new QuestionnaireAR(command.CommandIdentifier);

            document.CreatedBy = command.CreatedBy;
            questionnsire.CreateNewSnapshot(document);

            context.Accept();
        }

    }
}
