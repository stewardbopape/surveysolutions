﻿using System;
using System.Collections.Generic;
using System.Linq;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.Commands.Interview.Base;
using WB.UI.Headquarters.Models.WebInterview;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Dtos;

namespace WB.UI.Headquarters.API.WebInterview
{
    public partial class WebInterview
    {
        private Guid commandResponsibleId => this.webInterviewConfigProvider.Get(this.GetCallerInterview().QuestionnaireIdentity).ResponsibleId;

        private void ExecuteQuestionCommand(QuestionCommand command)
        {
            try
            {
                this.commandService.Execute(command);
            }
            catch (Exception e)
            {
                this.Clients.Caller.markAnswerAsNotSaved(command.QuestionId.FormatGuid(), e.Message);
            }
        }
        
        public void ChangeLanguage(ChangeLanguageReuqest request)
            => this.commandService.Execute(new SwitchTranslation(this.GetCallerInterview().Id, request.Language,
                this.commandResponsibleId));

        public void AnswerTextQuestion(string questionIdenty, string text)
        {
            var identity = Identity.Parse(questionIdenty);
            this.ExecuteQuestionCommand(new AnswerTextQuestionCommand(this.GetCallerInterview().Id,
                this.commandResponsibleId, identity.Id, identity.RosterVector, DateTime.UtcNow, text));
        }

        public void AnswerTextListQuestion(string questionIdenty, TextListAnswerRow[] rows)
        {
            var identity = Identity.Parse(questionIdenty);
            this.ExecuteQuestionCommand(new AnswerTextListQuestionCommand(this.GetCallerInterview().Id,
                this.commandResponsibleId, identity.Id, identity.RosterVector, DateTime.UtcNow,
                rows.Select(row => new Tuple<decimal, string>(row.Value, row.Text)).ToArray()));
        }

        public void AnswerGpsQuestion(string questionIdenty, GpsAnswer answer)
        {
            var identity = Identity.Parse(questionIdenty);
            this.ExecuteQuestionCommand(new AnswerGeoLocationQuestionCommand(this.GetCallerInterview().Id,
                this.commandResponsibleId, identity.Id, identity.RosterVector, DateTime.UtcNow, answer.Latitude, answer.Longitude,
                answer.Accuracy ?? 0, answer.Altitude ?? 0, DateTimeOffset.FromUnixTimeMilliseconds(answer.Timestamp ?? 0)));
        }

        public void AnswerDateQuestion(string questionIdenty, DateTime answer)
        {
            var identity = Identity.Parse(questionIdenty);
            this.ExecuteQuestionCommand(new AnswerDateTimeQuestionCommand(this.GetCallerInterview().Id,
                this.commandResponsibleId, identity.Id, identity.RosterVector, DateTime.UtcNow, answer));
        }

        public void AnswerSingleOptionQuestion(int answer, string questionId)
        {
            Identity identity = Identity.Parse(questionId);
            this.ExecuteQuestionCommand(new AnswerSingleOptionQuestionCommand(this.GetCallerInterview().Id, commandResponsibleId,
                identity.Id, identity.RosterVector, DateTime.UtcNow, answer));
        }

        public void AnswerLinkedSingleOptionQuestion(string questionIdentity, int[] answer)
        {
            Identity identity = Identity.Parse(questionIdentity);
            decimal[] decimalAnswer = answer.Select(Convert.ToDecimal).ToArray();
            this.ExecuteQuestionCommand(new AnswerSingleOptionLinkedQuestionCommand(this.GetCallerInterview().Id, commandResponsibleId,
                identity.Id, identity.RosterVector, DateTime.UtcNow, decimalAnswer));
        }

        public void AnswerLinkedToListSingleQuestion(string questionIdentity, int answer)
        {
            Identity identity = Identity.Parse(questionIdentity);
            this.ExecuteQuestionCommand(new AnswerSingleOptionQuestionCommand(this.GetCallerInterview().Id, commandResponsibleId,
                identity.Id, identity.RosterVector, DateTime.UtcNow, answer));
        }

        public void AnswerLinkedMultiOptionQuestion(string questionIdentity, int[][] answer)
        {
            Identity identity = Identity.Parse(questionIdentity);
            decimal[][] decimalAnswer = answer.Select(x => x.Select(Convert.ToDecimal).ToArray()).ToArray();
            this.ExecuteQuestionCommand(new AnswerMultipleOptionsLinkedQuestionCommand(this.GetCallerInterview().Id, commandResponsibleId,
                identity.Id, identity.RosterVector, DateTime.UtcNow, decimalAnswer));
        }

        public void AnswerLinkedToListMultiQuestion(string questionIdentity, int[] answer)
        {
            Identity identity = Identity.Parse(questionIdentity);
            this.ExecuteQuestionCommand(new AnswerMultipleOptionsQuestionCommand(this.GetCallerInterview().Id, commandResponsibleId,
                identity.Id, identity.RosterVector, DateTime.UtcNow, answer));
        }

        public void AnswerMultiOptionQuestion(int[] answer, string questionId)
        {
            Identity identity = Identity.Parse(questionId);
            this.ExecuteQuestionCommand(new AnswerMultipleOptionsQuestionCommand(this.GetCallerInterview().Id, commandResponsibleId,
                identity.Id, identity.RosterVector, DateTime.UtcNow, answer));
        }

        public void AnswerYesNoQuestion(string questionId, InterviewYesNoAnswer[] answerDto)
        {
            Identity identity = Identity.Parse(questionId);
            var answer = answerDto.Select(a => new AnsweredYesNoOption(a.Value, a.Yes)).ToArray();
            this.ExecuteQuestionCommand(new AnswerYesNoQuestion(this.GetCallerInterview().Id, commandResponsibleId,
                identity.Id, identity.RosterVector, DateTime.UtcNow, answer));
        }

        public void AnswerIntegerQuestion(string questionIdenty, int answer)
        {
            Identity identity = Identity.Parse(questionIdenty);
            this.ExecuteQuestionCommand(new AnswerNumericIntegerQuestionCommand(this.GetCallerInterview().Id, this.commandResponsibleId, identity.Id, identity.RosterVector, DateTime.UtcNow, answer));
        }

        public void AnswerDoubleQuestion(string questionIdenty, double answer)
        {
            Identity identity = Identity.Parse(questionIdenty);
            this.ExecuteQuestionCommand(new AnswerNumericRealQuestionCommand(this.GetCallerInterview().Id, this.commandResponsibleId, identity.Id, identity.RosterVector, DateTime.UtcNow, answer));
        }

        public void RemoveAnswer(string questionId)
        {
            Identity identity = Identity.Parse(questionId);
            this.ExecuteQuestionCommand(new RemoveAnswerCommand(this.GetCallerInterview().Id, commandResponsibleId, identity, DateTime.UtcNow));
        }
    }
}