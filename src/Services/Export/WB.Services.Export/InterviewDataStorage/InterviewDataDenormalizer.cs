﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using WB.Services.Export.Events.Interview;
using WB.Services.Export.Infrastructure;
using WB.Services.Export.Interview.Entities;
using WB.Services.Export.Questionnaire;
using WB.Services.Export.Questionnaire.Services;
using WB.Services.Infrastructure.EventSourcing;

namespace WB.Services.Export.InterviewDataStorage
{
    [DebuggerDisplay("{" + nameof(ToString) + "()}")]
    public class RosterInfo
    {
        public Guid InterviewId { get; set; }
        public RosterVector RosterVector { get; set; }

        public override string ToString() => InterviewId + "-" + RosterVector;

        public override bool Equals(object obj)
        {
            var item = obj as RosterInfo;
            if (item == null)
                return false;

            return this.InterviewId.Equals(item.InterviewId) && this.RosterVector.Equals(item.RosterVector);
        }

        public override int GetHashCode()
        {
            return this.InterviewId.GetHashCode() ^ this.RosterVector.GetHashCode();
        }
    }

    [DebuggerDisplay("{" + nameof(ToString) + "()}")]
    public class UpdateValueInfo
    {
        public string ColumnName { get; set; }
        public object Value { get; set; }
        public NpgsqlDbType ValueType { get; set; }

        public override string ToString() => ColumnName + "-" + Value + "-" + ValueType;

        public override bool Equals(object obj)
        {
            var item = obj as UpdateValueInfo;
            if (item == null)
                return false;

            return this.ColumnName.Equals(item.ColumnName);
        }

        public override int GetHashCode()
        {
            return this.ColumnName.GetHashCode();
        }
    }

    public class InterviewDataState
    {
        public IDictionary<string, HashSet<Guid>> InsertInterviews { get; set; } = new Dictionary<string, HashSet<Guid>>();
        public IDictionary<string, HashSet<RosterInfo>> InsertRosters { get; set; } = new Dictionary<string, HashSet<RosterInfo>>();
        public IDictionary<string, IDictionary<RosterInfo, HashSet<UpdateValueInfo>>> UpdateValues = new Dictionary<string, IDictionary<RosterInfo, HashSet<UpdateValueInfo>>>();
        public IDictionary<string, HashSet<RosterInfo>> RemoveRosters { get; set; } = new Dictionary<string, HashSet<RosterInfo>>();
        public IDictionary<string, HashSet<Guid>> RemoveInterviews { get; set; } = new Dictionary<string, HashSet<Guid>>();

        public void InsertInterviewInTable(string tableName, Guid interviewId)
        {
            if (!InsertInterviews.ContainsKey(tableName))
                InsertInterviews.Add(tableName, new HashSet<Guid>());
            InsertInterviews[tableName].Add(interviewId);
        }

        public void RemoveInterviewFromTable(string tableName, Guid interviewId)
        {
            if (!RemoveInterviews.ContainsKey(tableName))
                RemoveInterviews.Add(tableName, new HashSet<Guid>());
            RemoveInterviews[tableName].Add(interviewId);
        }

        public void InsertRosterInTable(string tableName, Guid interviewId, RosterVector rosterVector)
        {
            if (!InsertRosters.ContainsKey(tableName))
                InsertRosters.Add(tableName, new HashSet<RosterInfo>());
            InsertRosters[tableName].Add(new RosterInfo() { InterviewId = interviewId, RosterVector = rosterVector});
        }

        public void RemoveRosterFromTable(string tableName, Guid interviewId, RosterVector rosterVector)
        {
            if (!RemoveRosters.ContainsKey(tableName))
                RemoveRosters.Add(tableName, new HashSet<RosterInfo>());
            RemoveRosters[tableName].Add(new RosterInfo() { InterviewId = interviewId, RosterVector = rosterVector });
        }

        public void UpdateValueInTable(string tableName, Guid interviewId, RosterVector rosterVector, string columnName, object value, NpgsqlDbType valueType)
        {
            if (!UpdateValues.ContainsKey(tableName))
                UpdateValues.Add(tableName, new Dictionary<RosterInfo, HashSet<UpdateValueInfo>>());
            var updateValueForTable = UpdateValues[tableName];
            var rosterInfo = new RosterInfo() {InterviewId = interviewId, RosterVector = rosterVector};
            if (!updateValueForTable.ContainsKey(rosterInfo))
                updateValueForTable.Add(rosterInfo, new HashSet<UpdateValueInfo>());
            updateValueForTable[rosterInfo].Add(new UpdateValueInfo() { ColumnName = columnName, Value = value, ValueType = valueType});
        }
    }


    public class InterviewDataDenormalizer :
        IFunctionalHandler,
        IAsyncEventHandler<InterviewCreated>,
        IAsyncEventHandler<InterviewHardDeleted>,
        IAsyncEventHandler<TextQuestionAnswered>,
        IAsyncEventHandler<NumericIntegerQuestionAnswered>,
        IAsyncEventHandler<NumericRealQuestionAnswered>,
        IAsyncEventHandler<TextListQuestionAnswered>,
        IAsyncEventHandler<MultipleOptionsLinkedQuestionAnswered>,
        IAsyncEventHandler<MultipleOptionsQuestionAnswered>,
        IAsyncEventHandler<SingleOptionQuestionAnswered>,
        IAsyncEventHandler<SingleOptionLinkedQuestionAnswered>,
        IAsyncEventHandler<AreaQuestionAnswered>,
        IAsyncEventHandler<AudioQuestionAnswered>,
        IAsyncEventHandler<DateTimeQuestionAnswered>,
        IAsyncEventHandler<GeoLocationQuestionAnswered>,
        IAsyncEventHandler<PictureQuestionAnswered>,
        IAsyncEventHandler<QRBarcodeQuestionAnswered>,
        IAsyncEventHandler<YesNoQuestionAnswered>,
        
        IAsyncEventHandler<AnswerRemoved>,
        IAsyncEventHandler<AnswersRemoved>,
        IAsyncEventHandler<QuestionsDisabled>,
        IAsyncEventHandler<QuestionsEnabled>,
        IAsyncEventHandler<AnswersDeclaredInvalid>,
        IAsyncEventHandler<AnswersDeclaredValid>,
        IAsyncEventHandler<VariablesChanged>,
        IAsyncEventHandler<VariablesDisabled>,
        IAsyncEventHandler<VariablesEnabled>,
        IAsyncEventHandler<RosterInstancesAdded>,
        IAsyncEventHandler<RosterInstancesRemoved>
        //IAsyncEventHandler<GroupsDisabled>,
        //IAsyncEventHandler<GroupsEnabled>
    {

        private readonly ITenantContext tenantContext;
        private readonly IQuestionnaireStorage questionnaireStorage;
        private readonly IMemoryCache memoryCache;

        private readonly InterviewDataState state;

        public InterviewDataDenormalizer(ITenantContext tenantContext, IQuestionnaireStorage questionnaireStorage,
            IMemoryCache memoryCache)
        {
            this.tenantContext = tenantContext;
            this.questionnaireStorage = questionnaireStorage;
            this.memoryCache = memoryCache;

            state = new InterviewDataState();
        }

        public Task Handle(PublishedEvent<InterviewCreated> @event, CancellationToken token = default)
        {
            return AddInterview(@event.EventSourceId, token);
        }

        public Task Handle(PublishedEvent<InterviewHardDeleted> @event, CancellationToken token = default)
        {
            return RemoveInterview(@event.EventSourceId, token);
        }

        public Task Handle(PublishedEvent<TextQuestionAnswered> @event, CancellationToken token = default)
        {
            return UpdateQuestionValue(
                interviewId: @event.EventSourceId,
                entityId: @event.Event.QuestionId,
                rosterVector: @event.Event.RosterVector,
                value: @event.Event.Answer,
                valueType: NpgsqlDbType.Text, token: token);
        }

        public Task Handle(PublishedEvent<NumericIntegerQuestionAnswered> @event, CancellationToken token = default)
        {
            return UpdateQuestionValue(
                interviewId: @event.EventSourceId,
                entityId: @event.Event.QuestionId,
                rosterVector: @event.Event.RosterVector,
                value: @event.Event.Answer,
                valueType: NpgsqlDbType.Integer, token: token);
        }

        public Task Handle(PublishedEvent<NumericRealQuestionAnswered> @event, CancellationToken token = default)
        {
            double answer = (double)@event.Event.Answer;
            return UpdateQuestionValue(
                interviewId: @event.EventSourceId,
                entityId: @event.Event.QuestionId,
                rosterVector: @event.Event.RosterVector,
                value: double.IsNaN(answer) ? null : (object)answer,
                valueType: NpgsqlDbType.Double, token: token);
        }

        public Task Handle(PublishedEvent<TextListQuestionAnswered> @event, CancellationToken token = default)
        {
            return UpdateQuestionValue(
                interviewId: @event.EventSourceId,
                entityId: @event.Event.QuestionId,
                rosterVector: @event.Event.RosterVector,
                value: SerializeToJson(@event.Event.Answers),
                valueType: NpgsqlDbType.Json, token: token);
        }

        public Task Handle(PublishedEvent<MultipleOptionsLinkedQuestionAnswered> @event, CancellationToken token = default)
        {
            return UpdateQuestionValue(
                interviewId: @event.EventSourceId,
                entityId: @event.Event.QuestionId,
                rosterVector: @event.Event.RosterVector,
                value: @event.Event.SelectedRosterVectors.Select(c => c.Select(i => (int)i).ToArray()).ToArray(),
                valueType: NpgsqlDbType.Array | NpgsqlDbType.Array | NpgsqlDbType.Integer, token: token);
        }

        public Task Handle(PublishedEvent<MultipleOptionsQuestionAnswered> @event, CancellationToken token = default)
        {
            return UpdateQuestionValue(
                interviewId: @event.EventSourceId,
                entityId: @event.Event.QuestionId,
                rosterVector: @event.Event.RosterVector,
                value: @event.Event.SelectedValues.Select(c => (int)c).ToArray(),
                valueType: NpgsqlDbType.Array | NpgsqlDbType.Integer, token: token);
        }

        public Task Handle(PublishedEvent<SingleOptionQuestionAnswered> @event, CancellationToken token = default)
        {
            return UpdateQuestionValue(
                interviewId: @event.EventSourceId,
                entityId: @event.Event.QuestionId,
                rosterVector: @event.Event.RosterVector,
                value: (int)@event.Event.SelectedValue,
                valueType: NpgsqlDbType.Integer, token: token);
        }

        public Task Handle(PublishedEvent<SingleOptionLinkedQuestionAnswered> @event, CancellationToken token = default)
        {
            return UpdateQuestionValue(
                interviewId: @event.EventSourceId,
                entityId: @event.Event.QuestionId,
                rosterVector: @event.Event.RosterVector,
                value: @event.Event.SelectedRosterVector.Select(c => (int)c).ToArray(),
                valueType: NpgsqlDbType.Array | NpgsqlDbType.Integer, token: token);
        }

        public Task Handle(PublishedEvent<AreaQuestionAnswered> @event, CancellationToken token = default)
        {
            var area = new Area(@event.Event.Geometry, @event.Event.MapName, @event.Event.NumberOfPoints,
                @event.Event.AreaSize, @event.Event.Length, @event.Event.Coordinates, @event.Event.DistanceToEditor);
            return UpdateQuestionValue(
                interviewId: @event.EventSourceId,
                entityId: @event.Event.QuestionId,
                rosterVector: @event.Event.RosterVector,
                value: SerializeToJson(area),
                valueType: NpgsqlDbType.Json, token: token);
        }

        public Task Handle(PublishedEvent<AudioQuestionAnswered> @event, CancellationToken token = default)
        {
            var audioAnswer = AudioAnswer.FromString(@event.Event.FileName, @event.Event.Length);
            return UpdateQuestionValue(
                interviewId: @event.EventSourceId,
                entityId: @event.Event.QuestionId,
                rosterVector: @event.Event.RosterVector,
                value: SerializeToJson(audioAnswer),
                valueType: NpgsqlDbType.Json, token: token);
        }

        public Task Handle(PublishedEvent<DateTimeQuestionAnswered> @event, CancellationToken token = default)
        {
            return UpdateQuestionValue(
                interviewId: @event.EventSourceId,
                entityId: @event.Event.QuestionId,
                rosterVector: @event.Event.RosterVector,
                value: @event.Event.Answer,
                valueType: NpgsqlDbType.Date, token: token);
        }

        public Task Handle(PublishedEvent<GeoLocationQuestionAnswered> @event, CancellationToken token = default)
        {
            GeoPosition geoPosition = new GeoPosition(@event.Event.Latitude,
                @event.Event.Longitude,
                @event.Event.Accuracy,
                @event.Event.Altitude,
                @event.Event.Timestamp);
            return UpdateQuestionValue(
                interviewId: @event.EventSourceId,
                entityId: @event.Event.QuestionId,
                rosterVector: @event.Event.RosterVector,
                value: SerializeToJson(geoPosition),
                valueType: NpgsqlDbType.Json, token: token);
        }

        public Task Handle(PublishedEvent<PictureQuestionAnswered> @event, CancellationToken token = default)
        {
            return UpdateQuestionValue(
                interviewId: @event.EventSourceId,
                entityId: @event.Event.QuestionId,
                rosterVector: @event.Event.RosterVector,
                value: @event.Event.PictureFileName,
                valueType: NpgsqlDbType.Text, token: token);
        }

        public Task Handle(PublishedEvent<QRBarcodeQuestionAnswered> @event, CancellationToken token = default)
        {
            return UpdateQuestionValue(
                interviewId: @event.EventSourceId,
                entityId: @event.Event.QuestionId,
                rosterVector: @event.Event.RosterVector,
                value: SerializeToJson(@event.Event.Answer),
                valueType: NpgsqlDbType.Text, token: token);
        }

        public Task Handle(PublishedEvent<YesNoQuestionAnswered> @event, CancellationToken token = default)
        {
            return UpdateQuestionValue(
                interviewId: @event.EventSourceId,
                entityId: @event.Event.QuestionId,
                rosterVector: @event.Event.RosterVector,
                value: SerializeToJson(@event.Event.AnsweredOptions),
                valueType: NpgsqlDbType.Json, token: token);
        }

        public Task Handle(PublishedEvent<AnswerRemoved> @event, CancellationToken token = default)
        {
            return UpdateQuestionValue(
                interviewId: @event.EventSourceId,
                entityId: @event.Event.QuestionId,
                rosterVector: @event.Event.RosterVector,
                value: null,
                valueType: NpgsqlDbType.Unknown, token: token);
        }

        public async Task Handle(PublishedEvent<AnswersRemoved> @event, CancellationToken token = default)
        {
            foreach (var question in @event.Event.Questions)
            {
                await UpdateQuestionValue(
                    interviewId: @event.EventSourceId,
                    entityId: question.Id,
                    rosterVector: question.RosterVector,
                    value: null,
                    valueType: NpgsqlDbType.Unknown, token: token);
            }
        }

        public async Task Handle(PublishedEvent<QuestionsDisabled> @event, CancellationToken token = default)
        {
            foreach (var question in @event.Event.Questions)
            {
                await UpdateEnablementValue(
                    interviewId: @event.EventSourceId,
                    entityId: question.Id,
                    rosterVector: question.RosterVector,
                    isEnabled: false, token: token);
            }
        }

        public async Task Handle(PublishedEvent<QuestionsEnabled> @event, CancellationToken token = default)
        {
            foreach (var question in @event.Event.Questions)
            {
                await UpdateEnablementValue(
                    interviewId: @event.EventSourceId,
                    entityId: question.Id,
                    rosterVector: question.RosterVector,
                    isEnabled: true, token: token);
            }
        }

        public async Task Handle(PublishedEvent<AnswersDeclaredInvalid> @event, CancellationToken token = default)
        {
            var failedValidationConditions = @event.Event.FailedValidationConditions;
            foreach (var question in @event.Event.Questions)
            {
                await UpdateValidityValue(
                    interviewId: @event.EventSourceId,
                    entityId: question.Id,
                    rosterVector: question.RosterVector,
                    validityValue: failedValidationConditions[question].Select(c => c.FailedConditionIndex).ToArray(), token: token); 
            }
        }

        public async Task Handle(PublishedEvent<AnswersDeclaredValid> @event, CancellationToken token = default)
        {
            foreach (var question in @event.Event.Questions)
            {
                await UpdateValidityValue(
                    interviewId: @event.EventSourceId,
                    entityId: question.Id,
                    rosterVector: question.RosterVector,
                    validityValue: null, token: token);
            }
        }

        public async Task Handle(PublishedEvent<VariablesChanged> @event, CancellationToken token = default)
        {
            foreach (var variable in @event.Event.ChangedVariables)
            {
                await UpdateVariableValue(
                    interviewId: @event.EventSourceId,
                    entityId: variable.Identity.Id,
                    rosterVector: variable.Identity.RosterVector,
                    value: variable.NewValue, 
                    token: token);
            }
        }

        public async Task Handle(PublishedEvent<VariablesDisabled> @event, CancellationToken token = default)
        {
            foreach (var identity in @event.Event.Variables)
            {
                await UpdateEnablementValue(
                    interviewId: @event.EventSourceId,
                    entityId: identity.Id,
                    rosterVector: identity.RosterVector,
                    isEnabled: false, token: token);
            }
        }

        public async Task Handle(PublishedEvent<VariablesEnabled> @event, CancellationToken token = default)
        {
            foreach (var identity in @event.Event.Variables)
            {
                await UpdateEnablementValue(
                    interviewId: @event.EventSourceId,
                    entityId: identity.Id,
                    rosterVector: identity.RosterVector,
                    isEnabled: true, token: token);
            }
        }

        public async Task Handle(PublishedEvent<RosterInstancesAdded> @event, CancellationToken token = default)
        {
            foreach (var rosterInstance in @event.Event.Instances)
            {
                var rosterVector = rosterInstance.OuterRosterVector.Append(rosterInstance.RosterInstanceId);
                await AddRoster(@event.EventSourceId, rosterInstance.GroupId, rosterVector, token);
            }
        }

        public async Task Handle(PublishedEvent<RosterInstancesRemoved> @event, CancellationToken token = default)
        {
            foreach (var rosterInstance in @event.Event.Instances)
            {
                var rosterVector = rosterInstance.OuterRosterVector.Append(rosterInstance.RosterInstanceId).ToArray();
                await RemoveRoster(@event.EventSourceId, rosterInstance.GroupId, rosterVector, token);
            }
        }

        /*public Task Handle(PublishedEvent<GroupsDisabled> @event, CancellationToken token = default)
        {
            /*foreach (var identity in @event.Event.Groups)
            {
                state.Commands.Add(InterviewDataStateChangeCommand.Disable(
                    interviewId: @event.EventSourceId,
                    entityId: identity.Id,
                    rosterVector: identity.RosterVector.Coordinates.ToArray()
                ));
            }#1#
            return Task.CompletedTask;
        }

        public Task Handle(PublishedEvent<GroupsEnabled> @event, CancellationToken token = default)
        {
            /*foreach (var identity in @event.Event.Groups)
            {
                state.Commands.Add(InterviewDataStateChangeCommand.Enable(
                    interviewId: @event.EventSourceId,
                    entityId: identity.Id,
                    rosterVector: identity.RosterVector.Coordinates.ToArray()
                ));
            }#1#
            return Task.CompletedTask;
        }*/

        private async Task AddInterview(Guid interviewId, CancellationToken token = default)
        {
            var questionnaire = await GetQuestionnaireByInterviewIdAsync(interviewId, token);
            if (questionnaire == null)
                return;

            var topLevelGroups = questionnaire.GetInterviewLevelGroupsWithQuestionOrVariables();
            foreach (var topLevelGroup in topLevelGroups)
            {
                state.InsertInterviewInTable(topLevelGroup.TableName, interviewId);
                state.InsertInterviewInTable(topLevelGroup.EnablementTableName, interviewId);
                state.InsertInterviewInTable(topLevelGroup.ValidityTableName, interviewId);
            }
        }

        private async Task AddRoster(Guid interviewId, Guid groupId, RosterVector rosterVector, CancellationToken token = default)
        {
            var questionnaire = await GetQuestionnaireByInterviewIdAsync(interviewId, token);
            if (questionnaire == null)
                return;

            var @group = questionnaire.Find<Group>(groupId);
            if (@group.Children.Any(e => e is Question || e is Variable))
            {
                state.InsertRosterInTable(@group.TableName, interviewId, rosterVector);
                state.InsertRosterInTable(@group.EnablementTableName, interviewId, rosterVector);
                state.InsertRosterInTable(@group.ValidityTableName, interviewId, rosterVector);
            }
        }

        public async Task UpdateQuestionValue(Guid interviewId, Guid entityId, RosterVector rosterVector, object value, NpgsqlDbType valueType, CancellationToken token = default)
        {
            var questionnaire = await GetQuestionnaireByInterviewIdAsync(interviewId, token);
            if (questionnaire == null)
                return;

            var entity = questionnaire.Find<IQuestionnaireEntity>(entityId);
            var parentGroup = (Group)entity.GetParent();
            var columnName = ((Question)entity).ColumnName;
            state.UpdateValueInTable(parentGroup.TableName, interviewId, rosterVector, columnName, value, valueType);
        }

        public async Task UpdateVariableValue(Guid interviewId, Guid entityId, RosterVector rosterVector, object value, CancellationToken token = default)
        {
            var questionnaire = await GetQuestionnaireByInterviewIdAsync(interviewId, token);
            if (questionnaire == null)
                return;

            var variable = questionnaire.Find<Variable>(entityId);
            var parentGroup = (Group)variable.GetParent();
            var columnName = variable.ColumnName;
            var columnType = GetPostgresSqlTypeForVariable(variable);

            if (columnType == NpgsqlDbType.Double && value is string sValue && sValue == "NaN")
                value = double.NaN;

            state.UpdateValueInTable(parentGroup.TableName, interviewId, rosterVector, columnName, value, columnType);
        }

        public async Task UpdateEnablementValue(Guid interviewId, Guid entityId, RosterVector rosterVector, bool isEnabled, CancellationToken token = default)
        {
            var questionnaire = await GetQuestionnaireByInterviewIdAsync(interviewId, token);
            if (questionnaire == null)
                return;

            var entity = questionnaire.Find<IQuestionnaireEntity>(entityId);
            var tableName = ResolveGroupForEnablementOrValidity(entity).EnablementTableName;
            var columnName = ResolveColumnNameForEnablementOrValidity(entity);
            state.UpdateValueInTable(tableName, interviewId, rosterVector, columnName, isEnabled, NpgsqlDbType.Boolean);
        }

        public async Task UpdateValidityValue(Guid interviewId, Guid entityId, RosterVector rosterVector, int[] validityValue, CancellationToken token = default)
        {
            var questionnaire = await GetQuestionnaireByInterviewIdAsync(interviewId, token);
            if (questionnaire == null)
                return;
            var entity = questionnaire.Find<IQuestionnaireEntity>(entityId);
            var tableName = ResolveGroupForEnablementOrValidity(entity).ValidityTableName;
            var columnName = ResolveColumnNameForEnablementOrValidity(entity);
            state.UpdateValueInTable(tableName, interviewId, rosterVector, columnName, validityValue, NpgsqlDbType.Array | NpgsqlDbType.Integer);
        }

        public async Task RemoveRoster(Guid interviewId, Guid groupId, RosterVector rosterVector, CancellationToken token = default)
        {
            var questionnaire = await GetQuestionnaireByInterviewIdAsync(interviewId, token);
            if (questionnaire == null)
                return;

            var @group = questionnaire.Find<Group>(groupId);
            if (@group.Children.Any(e => e is Question || e is Variable))
            {
                state.RemoveRosterFromTable(@group.TableName, interviewId, rosterVector);
                state.RemoveRosterFromTable(@group.EnablementTableName, interviewId, rosterVector);
                state.RemoveRosterFromTable(@group.ValidityTableName, interviewId, rosterVector);
            }
        }

        public async Task RemoveInterview(Guid interviewId, CancellationToken token = default)
        {
            var questionnaire = await GetQuestionnaireByInterviewIdAsync(interviewId, token);
            if (questionnaire == null)
                return;

            var topLevelGroups = questionnaire.GetInterviewLevelGroupsWithQuestionOrVariables();
            foreach (var topLevelGroup in topLevelGroups)
            {
                state.RemoveInterviewFromTable(topLevelGroup.TableName, interviewId);
                state.RemoveInterviewFromTable(topLevelGroup.EnablementTableName, interviewId);
                state.RemoveInterviewFromTable(topLevelGroup.ValidityTableName, interviewId);
            }
        }

        public async Task SaveStateAsync(CancellationToken cancellationToken)
        {
            var commands = GenerateSqlCommandsAsync();
            foreach (var sqlCommand in commands)
            {
                sqlCommand.Connection = tenantContext.DbContext.Database.GetDbConnection();
                await sqlCommand.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        private List<DbCommand> GenerateSqlCommandsAsync()
        {
            var commands = new List<DbCommand>();

            foreach (var tableWithAddInterviews in state.InsertInterviews)
                commands.Add(CreateInsertCommandForTable(tableWithAddInterviews.Key, tableWithAddInterviews.Value));

            foreach (var tableWithAddRosters in state.InsertRosters)
                commands.Add(CreateAddRosterInstanceForTable(tableWithAddRosters.Key, tableWithAddRosters.Value));

            foreach (var updateValueInfo in state.UpdateValues)
            {
                foreach (var groupedByInterviewAndRoster in updateValueInfo.Value)
                {
                    var updateValueCommand = CreateUpdateValueForTable(updateValueInfo.Key, 
                        groupedByInterviewAndRoster.Key,
                        groupedByInterviewAndRoster.Value);
                    commands.Add(updateValueCommand);
                }
            }

            foreach (var tableWithRemoveRosters in state.RemoveRosters)
                commands.Add(CreateRemoveRosterInstanceForTable(tableWithRemoveRosters.Key, tableWithRemoveRosters.Value));

            foreach (var tableWithRemoveInterviews in state.RemoveInterviews)
                commands.Add(CreateDeleteCommandForTable(tableWithRemoveInterviews.Key, tableWithRemoveInterviews.Value));

            return commands;
        }

        private Task<QuestionnaireDocument> GetQuestionnaireByInterviewIdAsync(Guid interviewId, CancellationToken token = default)
        {
            var key = $"{nameof(InterviewDataDenormalizer)}:{tenantContext.Tenant.Name}:{interviewId}";
            return memoryCache.GetOrCreateAsync(key,
                async entry =>
                {
                    entry.SlidingExpiration = TimeSpan.FromMinutes(3);

                    var questionnaireId = await this.tenantContext.DbContext.InterviewReferences.FindAsync(interviewId);
                    if (questionnaireId == null)
                        return null;
                    var questionnaire = await questionnaireStorage.GetQuestionnaireAsync(tenantContext.Tenant, new QuestionnaireId(questionnaireId.QuestionnaireId), token);
                    return questionnaire;
                });
        }

        private DbCommand CreateInsertCommandForTable(string tableName, HashSet<Guid> interviewIds)
        {
            var text = $"INSERT INTO \"{tenantContext.Tenant.Name}\".\"{tableName}\" ({InterviewDatabaseConstants.InterviewId})" +
                       $"           VALUES ";
            NpgsqlCommand insertCommand = new NpgsqlCommand();

            int index = 0;
            foreach (var interviewId in interviewIds)
            {
                index++;
                text += $" (@interviewId{index}),";
                insertCommand.Parameters.AddWithValue($"@interviewId{index}", NpgsqlDbType.Uuid, interviewId);
            }

            text = text.TrimEnd(',');
            text += ";";

            insertCommand.CommandText = text;
            return insertCommand;
        }

        private DbCommand CreateDeleteCommandForTable(string tableName, HashSet<Guid> interviewIds)
        {
            var text = $"DELETE FROM \"{tenantContext.Tenant.Name}\".\"{tableName}\" " +
                       $"      WHERE {InterviewDatabaseConstants.InterviewId} = ANY(@interviewIds);";
            NpgsqlCommand deleteCommand = new NpgsqlCommand(text);
            deleteCommand.Parameters.AddWithValue("@interviewIds", NpgsqlDbType.Array | NpgsqlDbType.Uuid, interviewIds.ToArray());
            return deleteCommand;
        }

        private DbCommand CreateAddRosterInstanceForTable(string tableName, IEnumerable<RosterInfo> rosterInfos)
        {
            var text = $"INSERT INTO \"{tenantContext.Tenant.Name}\".\"{tableName}\" ({InterviewDatabaseConstants.InterviewId}, {InterviewDatabaseConstants.RosterVector})" +
                       $"           VALUES";

            NpgsqlCommand insertCommand = new NpgsqlCommand();
            int index = 0;
            foreach (var rosterInfo in rosterInfos)
            {
                index++;
                text += $"       (@interviewId{index}, @rosterVector{index}),";
                insertCommand.Parameters.AddWithValue($"@interviewId{index}", NpgsqlDbType.Uuid, rosterInfo.InterviewId);
                insertCommand.Parameters.AddWithValue($"@rosterVector{index}", NpgsqlDbType.Array | NpgsqlDbType.Integer, rosterInfo.RosterVector.Coordinates.ToArray());
            }

            text = text.TrimEnd(',');
            text += ";";

            insertCommand.CommandText = text;
            return insertCommand;
        }

        private DbCommand CreateRemoveRosterInstanceForTable(string tableName, IEnumerable<RosterInfo> rosterInfos)
        {
            var text = $"DELETE FROM \"{tenantContext.Tenant.Name}\".\"{tableName}\" " +
                       $"      WHERE ";
            NpgsqlCommand deleteCommand = new NpgsqlCommand();

            int index = 0;
            foreach (var rosterInfo in rosterInfos)
            {
                index++;
                text += $" (" +
                        $"   {InterviewDatabaseConstants.InterviewId} = @interviewId{index}" +
                        $"   AND {InterviewDatabaseConstants.RosterVector} = @rosterVector{index}" +
                        $" ) " +
                        $" OR";
                deleteCommand.Parameters.AddWithValue($"@interviewId{index}", NpgsqlDbType.Uuid, rosterInfo.InterviewId);
                deleteCommand.Parameters.AddWithValue($"@rosterVector{index}", NpgsqlDbType.Array | NpgsqlDbType.Integer, rosterInfo.RosterVector.Coordinates.ToArray());
            }

            text = text.TrimEnd('O', 'R');
            text += ";";

            deleteCommand.CommandText = text;

            return deleteCommand;
        }

        private Group ResolveGroupForEnablementOrValidity(IQuestionnaireEntity entity)
        {
            return (entity as Group) ?? ((Group)entity.GetParent());
        }

        private string ResolveColumnNameForEnablementOrValidity(IQuestionnaireEntity entity)
        {
            switch (entity)
            {
//                case Group group:
//                    return InterviewDatabaseConstants.InstanceValue;
                case Question question:
                    return question.ColumnName;
                case Variable variable:
                    return variable.ColumnName;
                default:
                    throw new ArgumentException("Unsupported entity type: " + entity.GetType().Name);
            }
        }

        private DbCommand CreateUpdateValueForTable(string tableName, RosterInfo rosterInfo, IEnumerable<UpdateValueInfo> updateValueInfos)
        {
            bool isTopLevel = rosterInfo.RosterVector == null || rosterInfo.RosterVector.Length == 0;

            NpgsqlCommand updateCommand = new NpgsqlCommand();

            var setValues = string.Empty;

            int index = 0;
            foreach (var updateValueInfo in updateValueInfos)
            {
                index++;
                setValues += $"   \"{updateValueInfo.ColumnName}\" = @answer{index},";

                if (updateValueInfo.Value == null)
                    updateCommand.Parameters.AddWithValue($"@answer{index}", DBNull.Value);
                else
                    updateCommand.Parameters.AddWithValue($"@answer{index}", updateValueInfo.ValueType, updateValueInfo.Value);
            }
            setValues = setValues.TrimEnd(',');

            var text = $"UPDATE \"{tenantContext.Tenant.Name}\".\"{tableName}\" " +
                       $"   SET {setValues}" +
                       $" WHERE {InterviewDatabaseConstants.InterviewId} = @interviewId";

            updateCommand.Parameters.AddWithValue("@interviewId", NpgsqlDbType.Uuid, rosterInfo.InterviewId);

            if (!isTopLevel)
            {
                text += $"   AND {InterviewDatabaseConstants.RosterVector} = @rosterVector;";
                updateCommand.Parameters.AddWithValue("@rosterVector", NpgsqlDbType.Array | NpgsqlDbType.Integer, rosterInfo.RosterVector.Coordinates.ToArray());
            }

            updateCommand.CommandText = text;
            return updateCommand;
        }

        private NpgsqlDbType GetPostgresSqlTypeForVariable(Variable variable)
        {
            switch (variable.Type)
            {
                case VariableType.Boolean: return NpgsqlDbType.Boolean;
                case VariableType.DateTime: return NpgsqlDbType.Date;
                case VariableType.Double: return NpgsqlDbType.Double;
                case VariableType.LongInteger: return NpgsqlDbType.Bigint;
                case VariableType.String: return NpgsqlDbType.Text;
                default:
                    throw new ArgumentException("Unknown variable type: " + variable.Type);
            }
        }

        private string SerializeToJson(object value) => JsonConvert.SerializeObject(value);
    }
}
