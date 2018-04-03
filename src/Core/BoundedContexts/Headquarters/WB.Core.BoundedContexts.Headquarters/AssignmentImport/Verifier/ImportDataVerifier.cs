﻿using System;
using System.Collections.Generic;
using System.Linq;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport.Parser;
using WB.Core.BoundedContexts.Headquarters.Services.Preloading;
using WB.Core.BoundedContexts.Headquarters.ValueObjects.PreloadedData;
using WB.Core.GenericSubdomains.Portable.Implementation.ServiceVariables;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.MaskFormatter;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.SurveySolutions.Documents;
using messages = WB.Core.BoundedContexts.Headquarters.Resources.PreloadingVerificationMessages;

namespace WB.Core.BoundedContexts.Headquarters.AssignmentImport.Verifier
{
    
    internal class ImportDataVerifier : IPreloadedDataVerifier
    {
        private readonly IQuestionnaireStorage questionnaireStorage;

        public ImportDataVerifier(IQuestionnaireStorage questionnaireStorage)
        {
            this.questionnaireStorage = questionnaireStorage;
        }

        public IEnumerable<PanelImportVerificationError> VerifyAnswers(QuestionnaireIdentity questionnaireIdentity, AssignmentRow assignmentRow)
        {
            var questionnaire = this.questionnaireStorage.GetQuestionnaire(questionnaireIdentity, null);

            foreach (var assignmentValue in assignmentRow.Answers)
            {
                foreach (var error in this.AnswerVerifiers.Select(x => x.Invoke(assignmentValue, questionnaire)))
                    if (error != null) yield return error;

                if (!(assignmentValue is AssignmentAnswers compositeValue)) continue;

                foreach (var value in compositeValue.Values)
                foreach (var error in this.AnswerVerifiers.Select(x => x.Invoke(value, questionnaire)))
                    if (error != null) yield return error;
            }
        }

        public IEnumerable<PanelImportVerificationError> VerifyRosters(
            QuestionnaireIdentity questionnaireIdentity, PreloadedFileInfo[] files)
        {
            var questionnaire = this.questionnaireStorage.GetQuestionnaire(questionnaireIdentity, null);

            foreach (var file in files)
            foreach (var error in this.RosterVerifiers.Select(x => x.Invoke(file, files, questionnaire)))
                if (error != null) yield return error;
        }

        public IEnumerable<PanelImportVerificationError> VerifyColumns(
            QuestionnaireIdentity questionnaireIdentity, PreloadedFileInfo[] files)
        {
            var questionnaire = this.questionnaireStorage.GetQuestionnaire(questionnaireIdentity, null);

            foreach (var file in files)
            {
                foreach (var columnName in file.Columns)
                foreach (var error in this.ColumnVerifiers.Select(x => x.Invoke(file, columnName, questionnaire)))
                    if (error != null) yield return error;

                var lowercaseColumnNames = file.Columns.Select(x => x.ToLower());
                foreach (var expectedServiceColumn in this.GetRequiredServiceColumns(file, questionnaire))
                {
                    if (!lowercaseColumnNames.Contains(expectedServiceColumn.ToLower()))
                        yield return ToColumnError("PL0007", messages.PL0007_ServiceColumnIsAbsent, file.FileName, expectedServiceColumn);
                }
            }
        }

        private IEnumerable<Func<PreloadedFileInfo, PreloadedFileInfo[], IQuestionnaire, PanelImportVerificationError>> RosterVerifiers => new[]
        {
            Error(RosterNotFound, "PL0004", messages.PL0004_FileWasntMappedRoster),
            Error(OrphanRoster, "PL0008", messages.PL0008_OrphanRosterRecord),
            Error(DuplicatedRosterInstances, "PL0006", messages.PL0006_IdDublication),
            Error(IdIsEmpty, "PL0042", messages.PL0042_IdIsEmpty)
        };

        private IEnumerable<Func<PreloadedFileInfo, string, IQuestionnaire, PanelImportVerificationError>> ColumnVerifiers => new[]
        {
            Error(UnknownColumn, "PL0003", messages.PL0003_ColumnWasntMappedOnQuestion),
            Error(CategoricalMultiQuestion_OptionNotFound, "PL0014", messages.PL0014_ParsedValueIsNotAllowed),
            Error(OptionalGpsPropertyAndMissingLatitudeAndLongitude, "PL0030", messages.PL0030_GpsFieldsRequired),
            Error(DuplicatedColumn, "PL0031", messages.PL0031_ColumnNameDuplicatesFound),
            Error(UnsupportedAreaQuestion, "PL0038", messages.PL0038_UnsupportedAreaQuestion),
        };

        private IEnumerable<Func<AssignmentValue, IQuestionnaire, PanelImportVerificationError>> AnswerVerifiers => new[]
        {
            Error<AssignmentRosterInstanceCode>(RosterInstanceCode_NoParsed, "PL0009", messages.PL0009_RosterIdIsInconsistantWithRosterSizeQuestion),
            Error<AssignmentRosterInstanceCode>(RosterInstanceCode_InvalidCode, "PL0009", messages.PL0009_RosterIdIsInconsistantWithRosterSizeQuestion),
            Error<AssignmentTextAnswer>(Text_HasInvalidMask, "PL0014", messages.PL0014_ParsedValueIsNotAllowed),
            Error<AssignmentDoubleAnswer>(CategoricalSingle_OptionNotFound, "PL0014", messages.PL0014_ParsedValueIsNotAllowed),
            Error<AssignmentDateTimeAnswer>(DateTime_NotParsed, "PL0016", messages.PL0016_ExpectedDateTimeNotParsed),
            Error<AssignmentGpsAnswer>(Gps_NotParsed, "PL0017", messages.PL0017_ExpectedGpsNotParsed),
            Error<AssignmentIntegerAnswer>(Interger_NotParsed, "PL0018", messages.PL0018_ExpectedIntNotParsed),
            Error<AssignmentDoubleAnswer>(Double_NotParsed, "PL0019", messages.PL0019_ExpectedDecimalNotParsed),
            Error<AssignmentIntegerAnswer>(Integer_IsNegativeRosterSize, "PL0022", messages.PL0022_AnswerIsIncorrectBecauseIsRosterSizeAndNegative),
            Error<AssignmentResponsible>(Responsible_IsEmpty, "PL0025", messages.PL0025_ResponsibleNameIsEmpty),
            Error<AssignmentResponsible>(Responsible_NotFound, "PL0026", messages.PL0026_ResponsibleWasNotFound),
            Error<AssignmentResponsible>(Responsible_IsLocked, "PL0027", messages.PL0027_ResponsibleIsLocked),
            Error<AssignmentResponsible>(Responsible_HasInvalidRole, "PL0028", messages.PL0028_UserIsNotSupervisorOrInterviewer),
            Error<AssignmentIntegerAnswer>(Integer_ExceededRosterSize, "PL0029", string.Format(messages.PL0029_AnswerIsIncorrectBecauseIsRosterSizeAndMoreThan40, Constants.MaxRosterRowCount)),
            Error<AssignmentIntegerAnswer>(Integer_ExceededLongRosterSize, "PL0029", string.Format(messages.PL0029_AnswerIsIncorrectBecauseIsRosterSizeAndMoreThan40, Constants.MaxLongRosterRowCount)),
            Error<AssignmentGpsAnswer>(Gps_DontHaveLongitudeOrLatitude, "PL0030", messages.PL0030_GpsMandatoryFilds),
            Error<AssignmentGpsAnswer>(Gps_LatitudeMustBeGeaterThenN90AndLessThen90, "PL0032", messages.PL0032_LatitudeMustBeGeaterThenN90AndLessThen90),
            Error<AssignmentGpsAnswer>(Gps_LongitudeMustBeGeaterThenN180AndLessThen180, "PL0033", messages.PL0033_LongitudeMustBeGeaterThenN180AndLessThen180),
            Error<AssignmentGpsAnswer>(Gps_CommaSymbolIsNotAllowed, "PL0034", messages.PL0034_CommaSymbolIsNotAllowedInNumericAnswer),
            Error<AssignmentIntegerAnswer>(Integer_CommaSymbolIsNotAllowed, "PL0034", messages.PL0034_CommaSymbolIsNotAllowedInNumericAnswer),
            Error<AssignmentQuantity>(Quantity_IsNotInteger, "PL0035", messages.PL0035_QuantityNotParsed),
            Error<AssignmentQuantity>(Quantity_IsNegative, "PL0036", messages.PL0036_QuantityShouldBeGreaterThanMinus1),
            Error<AssignmentCategoricalMultiAnswer>(CategoricalMulti_AnswerExceedsMaxAnswersCount, "PL0041", messages.PL0041_AnswerExceedsMaxAnswersCount),
        };

        private IEnumerable<string> GetRequiredServiceColumns(PreloadedFileInfo file, IQuestionnaire questionnaire)
        {
            if (IsQuestionnaireFile(file, questionnaire))
                yield break;

            yield return ServiceColumns.InterviewId;

            var rosterId = questionnaire.GetRosterIdByVariableName(file.QuestionnaireOrRosterName, true);

            var parentRosterIds = questionnaire.GetRostersFromTopToSpecifiedGroup(rosterId.Value);

            foreach (var parentRosterId in parentRosterIds)
                yield return string.Format(ServiceColumns.IdSuffixFormat, questionnaire.GetRosterVariableName(parentRosterId));
        }

        private bool IdIsEmpty(PreloadedFileInfo file, PreloadedFileInfo[] files, IQuestionnaire questionnaire)
        {
            return false;
            //var parentDataFile = preloadedDataService.GetParentDataFile(levelData.FileName, allLevels);

            //if (parentDataFile != null)
            //    yield break;

            //var idColumnIndex = preloadedDataService.GetIdColumnIndex(levelData);
            //if (idColumnIndex < 0)
            //    yield break;

            //for (int y = 0; y < levelData.Content.Length; y++)
            //{
            //    var idValue = levelData.Content[y][idColumnIndex];
            //    if (string.IsNullOrEmpty(idValue))
            //    {
            //        yield return new InterviewImportReference(idColumnIndex, y, PreloadedDataVerificationReferenceType.Cell, "", levelData.FileName);
            //    }
            //}
        }

        private bool DuplicatedRosterInstances(PreloadedFileInfo file, PreloadedFileInfo[] allFiles, IQuestionnaire questionnaire)
        {
            return false;
            //this.Verifier(this.IdDuplication, "PL0006", messages.PL0006_IdDublication),   
            //private IEnumerable<PreloadedDataVerificationReference> IdDuplication(PreloadedDataByFile levelData, PreloadedDataByFile[] allLevels,
            //    IPreloadedDataService preloadedDataService)
            //{
            //    var idColumnIndex = preloadedDataService.GetIdColumnIndex(levelData);
            //    var parentIdColumnIndexes = preloadedDataService.GetParentIdColumnIndexes(levelData);

            //    if (idColumnIndex < 0 || parentIdColumnIndexes == null)
            //        yield break;

            //    var idAndParentContainer = new HashSet<string>();
            //    for (int y = 0; y < levelData.Content.Length; y++)
            //    {
            //        var idValue = levelData.Content[y][idColumnIndex];
            //        if (string.IsNullOrEmpty(idValue))
            //        {
            //            yield return
            //                new PreloadedDataVerificationReference(idColumnIndex, y, PreloadedDataVerificationReferenceType.Cell, "",
            //                    levelData.FileName);
            //            continue;
            //        }
            //        var parentIdValue = string.Join(",", parentIdColumnIndexes.Select(parentidIndex => levelData.Content[y][parentidIndex]));
            //        string itemKey = String.Format("{0}\t{1}", idValue, parentIdValue);
            //        if (idAndParentContainer.Contains(itemKey))
            //        {
            //            yield return
            //                new PreloadedDataVerificationReference(idColumnIndex, y, PreloadedDataVerificationReferenceType.Cell,
            //                    string.Format("id:{0}, parentId: {1}", idValue, parentIdValue), levelData.FileName);
            //            continue;
            //        }
            //        idAndParentContainer.Add(itemKey);
            //    }
            //}
        }

        private bool OrphanRoster(PreloadedFileInfo file, PreloadedFileInfo[] files, IQuestionnaire questionnaire)
        {
            if (IsQuestionnaireFile(file, questionnaire)) return false;

            return false;
            //var parentDataFile = preloadedDataService.GetParentDataFile(levelData.FileName, allLevels);

            //if (parentDataFile == null)
            //    yield break;

            //var parentIdColumnIndexes = preloadedDataService.GetParentIdColumnIndexes(levelData);
            //var parentIdColumnIndexesForParentDataFile = preloadedDataService.GetParentIdColumnIndexes(parentDataFile);
            //var idColumnIndexInParentFile = preloadedDataService.GetIdColumnIndex(parentDataFile);

            //if (idColumnIndexInParentFile < 0 || parentIdColumnIndexes == null || parentIdColumnIndexes.Length == 0 ||
            //    parentIdColumnIndexesForParentDataFile == null)
            //    yield break;

            //var parentIds =
            //    parentDataFile.Content.Select(
            //        row =>
            //            this.JoinRowIdWithParentIdsInParentIdsVector(row[idColumnIndexInParentFile],
            //                this.CreateParentIdsVector(row, parentIdColumnIndexesForParentDataFile))).ToList();

            //for (int y = 0; y < levelData.Content.Length; y++)
            //{

            //    var parentIdValues = this.CreateParentIdsVector(levelData.Content[y], parentIdColumnIndexes);
            //    if (!parentIds.Any(p => p.SequenceEqual(parentIdValues)))
            //        yield return
            //            new PreloadedDataVerificationReference(parentIdColumnIndexes.First(), y, PreloadedDataVerificationReferenceType.Cell,
            //                string.Join(",", parentIdValues), levelData.FileName);
            //}

            //private string[] JoinRowIdWithParentIdsInParentIdsVector(string id, string[] parentIds)
            //{
            //    var result = new string[parentIds.Length + 1];
            //    result[0] = id;
            //    for (int i = 1; i < result.Length; i++)
            //    {
            //        result[i] = parentIds[i - 1];
            //    }
            //    return result;
            //}

            //private string[] CreateParentIdsVector(string[] content, int[] parentIdColumnIndexes)
            //{
            //    return parentIdColumnIndexes.Select(parentIdColumnIndex => content[parentIdColumnIndex]).ToArray();
            //}
        }

        private bool IsQuestionnaireFile(PreloadedFileInfo file, IQuestionnaire questionnaire)
            => string.Equals(file.QuestionnaireOrRosterName, questionnaire.Title, StringComparison.InvariantCultureIgnoreCase);

        private bool RosterNotFound(PreloadedFileInfo file, PreloadedFileInfo[] allFiles, IQuestionnaire questionnaire)
            => !IsQuestionnaireFile(file, questionnaire) && !questionnaire.HasRoster(file.QuestionnaireOrRosterName);

        private bool UnknownColumn(PreloadedFileInfo file, string columnName, IQuestionnaire questionnaire)
        {
            if (string.IsNullOrWhiteSpace(columnName)) return true;
            if (ServiceColumns.AllSystemVariables.Contains(columnName)) return false;
            if (GetRequiredServiceColumns(file, questionnaire).Select(x => x.ToLower()).Contains(columnName)) return false;

            var compositeColumnValues = columnName.Split(new[] { QuestionDataParser.ColumnDelimiter },
                StringSplitOptions.RemoveEmptyEntries);

            var questionOrVariableName = compositeColumnValues[0].ToLower();

            var rosterId = questionnaire.GetRosterIdByVariableName(file.QuestionnaireOrRosterName, true);
            if (rosterId.HasValue)
            {
                var rosterSizeQuestionId = questionnaire.GetRosterSizeQuestion(rosterId.Value);

                if (questionnaire.GetQuestionType(rosterSizeQuestionId) == QuestionType.TextList &&
                    questionnaire.GetQuestionVariableName(rosterSizeQuestionId).ToLower() == questionOrVariableName) return false;
            }

            foreach (var variableId in questionnaire.GetAllUnderlyingVariablesOutsideRosters(rosterId))
                if (questionnaire.GetVariableName(variableId).ToLower() == questionOrVariableName) return false;

            foreach (var questionId in questionnaire.GetAllUnderlyingQuestionsOutsideRosters(rosterId))
                if (questionnaire.GetQuestionVariableName(questionId).ToLower() == questionOrVariableName) return false;

            return true;
        }

        private bool OptionalGpsPropertyAndMissingLatitudeAndLongitude(PreloadedFileInfo file, string columnName, IQuestionnaire questionnaire)
        {
            var compositeColumnValues = columnName.Split(new[] { QuestionDataParser.ColumnDelimiter },
                StringSplitOptions.RemoveEmptyEntries);

            if (compositeColumnValues.Length < 2) return false;

            var questionVariableName = compositeColumnValues[0].ToLower();

            var questionId = questionnaire.GetQuestionIdByVariable(questionVariableName);
            if (!questionId.HasValue) return false;

            if (questionnaire.GetQuestionType(questionId.Value) != QuestionType.GpsCoordinates) return false;

            var lowercaseColumnNames = file.Columns.Select(x => x.ToLower());

            return !lowercaseColumnNames.Contains($"{questionVariableName}{QuestionDataParser.ColumnDelimiter}{nameof(GeoPosition.Latitude).ToLower()}") || 
                   !lowercaseColumnNames.Contains($"{questionVariableName}{QuestionDataParser.ColumnDelimiter}{nameof(GeoPosition.Longitude).ToLower()}");
        }

        private bool DuplicatedColumn(PreloadedFileInfo file, string columnName, IQuestionnaire questionnaire)
            => !string.IsNullOrWhiteSpace(columnName) && file.Columns.Select(x => x.ToLower()).Count(x => x == columnName) > 1;

        private bool UnsupportedAreaQuestion(PreloadedFileInfo file, string columnName, IQuestionnaire questionnaire) 
            => questionnaire.GetQuestionByVariable(columnName)?.QuestionType == QuestionType.Area;

        private bool CategoricalMultiQuestion_OptionNotFound(PreloadedFileInfo file, string columnName, IQuestionnaire questionnaire)
        {
            var compositeColumn = columnName.Split(new[] {QuestionDataParser.ColumnDelimiter},
                StringSplitOptions.RemoveEmptyEntries);

            if (compositeColumn.Length < 2) return false;

            var question = questionnaire.GetQuestionByVariable(compositeColumn[0]);

            return question?.QuestionType == QuestionType.MultyOption &&
                   question.Answers.All(x => x.AnswerValue != compositeColumn[1]);
        }

        private bool RosterInstanceCode_InvalidCode(AssignmentRosterInstanceCode answer, IQuestionnaire questionnaire)
        {
            if (!answer.Code.HasValue) return false;

            var rosterId = questionnaire.GetRosterIdByVariableName(answer.VariableName, true);
            if (!rosterId.HasValue) return false;

            if (questionnaire.IsFixedRoster(rosterId.Value))
                return !questionnaire.GetFixedRosterCodes(rosterId.Value).Contains(answer.Code.Value);

            var rosterSizeQuestionId = questionnaire.GetRosterSizeQuestion(rosterId.Value);

            var questionType = questionnaire.GetQuestionType(rosterSizeQuestionId);
            switch (questionType)
            {
                case QuestionType.MultyOption:
                    return !questionnaire.GetMultiSelectAnswerOptionsAsValues(rosterSizeQuestionId).Contains(answer.Code.Value);
                case QuestionType.Numeric:
                case QuestionType.TextList:
                    return answer.Code < 0;
            }

            return false;
        }

        private bool RosterInstanceCode_NoParsed(AssignmentRosterInstanceCode answer)
            => !string.IsNullOrWhiteSpace(answer.Value) && !answer.Code.HasValue;

        private bool CategoricalSingle_OptionNotFound(AssignmentDoubleAnswer answer, IQuestionnaire questionnaire)
        {
            if (string.IsNullOrEmpty(answer.Value)) return false;
            if (Double_NotParsed(answer)) return false;

            var questionId = questionnaire.GetQuestionIdByVariable(answer.VariableName);
            if (!questionId.HasValue) return false;

            if (questionnaire.GetQuestionType(questionId.Value) != QuestionType.SingleOption) return false;

            return questionnaire.GetQuestionByVariable(answer.VariableName)?.Answers
                ?.All(x => x.AnswerValue != answer.Value) ?? false;
        }

        private bool Text_HasInvalidMask(AssignmentTextAnswer answer, IQuestionnaire questionnaire)
        {
            if (string.IsNullOrEmpty(answer.Value)) return false;

            var questionId = questionnaire.GetQuestionIdByVariable(answer.VariableName);
            if (!questionId.HasValue) return false;

            var textMask = questionnaire.GetTextQuestionMask(questionId.Value);
            if (string.IsNullOrWhiteSpace(textMask)) return false;

            return !new MaskedFormatter(textMask).IsTextMaskMatched(answer.Value);
        }

        private bool DateTime_NotParsed(AssignmentDateTimeAnswer answer)
            => !string.IsNullOrWhiteSpace(answer.Value) && !answer.Answer.HasValue;

        private bool Double_NotParsed(AssignmentDoubleAnswer answer)
            => !string.IsNullOrWhiteSpace(answer.Value) && !answer.Answer.HasValue;

        private bool CategoricalMulti_AnswerExceedsMaxAnswersCount(AssignmentCategoricalMultiAnswer answer, IQuestionnaire questionnaire)
        {
            var questionId = questionnaire.GetQuestionIdByVariable(answer.VariableName);
            if (!questionId.HasValue) return false;

            var maxAnswersCount = questionnaire.GetMaxSelectedAnswerOptions(questionId.Value);

            return maxAnswersCount.HasValue &&
                   answer.Values.OfType<AssignmentIntegerAnswer>().Count(x => x.Answer.HasValue) > maxAnswersCount;
        }

        private bool Gps_CommaSymbolIsNotAllowed(AssignmentGpsAnswer answer)
        {
            var latitude = answer.Values.FirstOrDefault(x => x.VariableName == nameof(GeoPosition.Latitude).ToLower())?.Value;
            var longitude = answer.Values.FirstOrDefault(x => x.VariableName == nameof(GeoPosition.Longitude).ToLower())?.Value;

            return !string.IsNullOrWhiteSpace(latitude) && latitude.Contains(",") ||
                   !string.IsNullOrWhiteSpace(longitude) && longitude.Contains(",");
        }

        private bool Gps_LongitudeMustBeGeaterThenN180AndLessThen180(AssignmentGpsAnswer answer)
        {
            if (this.Gps_DontHaveLongitudeOrLatitude(answer)) return false;

            var longitude = answer.Values.OfType<AssignmentDoubleAnswer>()
                .FirstOrDefault(x => x.VariableName == nameof(GeoPosition.Longitude).ToLower()).Answer;

            return longitude < -180 || longitude > 180;
        }

        private bool Gps_LatitudeMustBeGeaterThenN90AndLessThen90(AssignmentGpsAnswer answer)
        {
            if (this.Gps_DontHaveLongitudeOrLatitude(answer)) return false;

            var latitude = answer.Values.OfType<AssignmentDoubleAnswer>()
                .FirstOrDefault(x => x.VariableName == nameof(GeoPosition.Latitude).ToLower()).Answer;

            return latitude < -90 || latitude > 90;
        }

        private bool Gps_DontHaveLongitudeOrLatitude(AssignmentGpsAnswer answer)
        {
            if (this.Gps_NotParsed(answer)) return false;

            var latitude = answer.Values.OfType<AssignmentDoubleAnswer>()
                .FirstOrDefault(x => x.VariableName == nameof(GeoPosition.Latitude).ToLower())?.Answer;
            var longitude = answer.Values.OfType<AssignmentDoubleAnswer>()
                .FirstOrDefault(x => x.VariableName == nameof(GeoPosition.Longitude).ToLower())?.Answer;

            return (!latitude.HasValue && longitude.HasValue) || (latitude.HasValue && !longitude.HasValue);
        }

        private bool Gps_NotParsed(AssignmentGpsAnswer answer)
        {
            var latitudeAnswer = answer.Values.OfType<AssignmentDoubleAnswer>().FirstOrDefault(x => x.VariableName == nameof(GeoPosition.Latitude).ToLower());
            var longitudeValueAnswer = answer.Values.OfType<AssignmentDoubleAnswer>().FirstOrDefault(x => x.VariableName == nameof(GeoPosition.Longitude).ToLower());

            return !string.IsNullOrWhiteSpace(latitudeAnswer.Value) && !latitudeAnswer.Answer.HasValue ||
                   !string.IsNullOrWhiteSpace(longitudeValueAnswer.Value) && !longitudeValueAnswer.Answer.HasValue;
        }

        private bool Interger_NotParsed(AssignmentIntegerAnswer answer)
            => !string.IsNullOrWhiteSpace(answer.Value) && !answer.Answer.HasValue;

        private bool Integer_CommaSymbolIsNotAllowed(AssignmentIntegerAnswer answer)
            => !string.IsNullOrWhiteSpace(answer.Value) && answer.Value.Contains(",");

        private bool Integer_ExceededRosterSize(AssignmentIntegerAnswer answer, IQuestionnaire questionnaire)
        {
            var questionId = questionnaire.GetQuestionIdByVariable(answer.VariableName);
            if (!questionId.HasValue) return false;
            if (!questionnaire.IsRosterSizeQuestion(questionId.Value)) return false;

            return !questionnaire.IsQuestionIsRosterSizeForLongRoster(questionId.Value) &&
                answer.Answer.HasValue && answer.Answer > questionnaire.GetMaxRosterRowCount();
        }

        private bool Integer_ExceededLongRosterSize(AssignmentIntegerAnswer answer, IQuestionnaire questionnaire)
        {
            var questionId = questionnaire.GetQuestionIdByVariable(answer.VariableName);
            if (!questionId.HasValue) return false;

            return questionnaire.IsQuestionIsRosterSizeForLongRoster(questionId.Value) &&
                   answer.Answer.HasValue && answer.Answer > questionnaire.GetMaxLongRosterRowCount();
        }

        private bool Integer_IsNegativeRosterSize(AssignmentIntegerAnswer answer, IQuestionnaire questionnaire)
        {
            var questionId = questionnaire.GetQuestionIdByVariable(answer.VariableName);
            if (!questionId.HasValue) return false;

            return questionnaire.IsRosterSizeQuestion(questionId.Value) && answer.Answer.HasValue && answer.Answer < 0;
        }

        private bool Responsible_HasInvalidRole(AssignmentResponsible responsible)
            => !this.Responsible_NotFound(responsible) && responsible.Responsible.IsSupervisorOrInterviewer;

        private bool Responsible_IsLocked(AssignmentResponsible responsible) 
            => !this.Responsible_NotFound(responsible) && responsible.Responsible.IsLocked;

        private bool Responsible_NotFound(AssignmentResponsible responsible) 
            => !this.Responsible_IsEmpty(responsible) && responsible.Responsible == null;

        private bool Responsible_IsEmpty(AssignmentResponsible responsible) 
            => string.IsNullOrWhiteSpace(responsible.Value);
        
        private bool Quantity_IsNegative(AssignmentQuantity quantity)
            => !Quantity_IsNotInteger(quantity) && quantity.Quantity != -1 && quantity.Quantity < 1;

        private bool Quantity_IsNotInteger(AssignmentQuantity quantity)
            => !string.IsNullOrWhiteSpace(quantity.Value) && !quantity.Quantity.HasValue;


        private static Func<PreloadedFileInfo, PreloadedFileInfo[], IQuestionnaire, PanelImportVerificationError> Error(
            Func<PreloadedFileInfo, PreloadedFileInfo[], IQuestionnaire, bool> hasError, string code, string message) => (file, allFiles, questionnaire) =>
            hasError(file, allFiles, questionnaire) ? ToFileError(code, message, file) : null;

        private static Func<PreloadedFileInfo, string, IQuestionnaire, PanelImportVerificationError> Error(
            Func<PreloadedFileInfo, string, IQuestionnaire, bool> hasError, string code, string message) => (file, columnName, questionnaire) =>
            hasError(file, columnName?.ToLower(), questionnaire) ? ToColumnError(code, message, file.FileName, columnName) : null;

        private static Func<AssignmentValue, IQuestionnaire, PanelImportVerificationError> Error<TValue>(
            Func<TValue, bool> hasError, string code, string message) where TValue : AssignmentValue => (value, questionnaire) =>
            value is TValue && hasError((TValue)value) ? ToCellError(code, message, value) : null;

        private static Func<AssignmentValue, IQuestionnaire, PanelImportVerificationError> Error<TValue>(
            Func<TValue, IQuestionnaire, bool> hasError, string code, string message) where TValue : AssignmentValue => (value, questionnaire) =>
            value is TValue && hasError((TValue)value, questionnaire) ? ToCellError(code, message, value) : null;

        private static PanelImportVerificationError ToFileError(string code, string message, PreloadedFileInfo fileInfo)
            => new PanelImportVerificationError(code, message, new InterviewImportReference(PreloadedDataVerificationReferenceType.File, fileInfo.FileName, fileInfo.FileName));
        private static PanelImportVerificationError ToColumnError(string code, string message, string fileName, string columnName)
            => new PanelImportVerificationError(code, message, new InterviewImportReference(PreloadedDataVerificationReferenceType.Column, columnName, fileName));

        private static PanelImportVerificationError ToCellError(string code, string message, AssignmentValue assignmentValue)
            => new PanelImportVerificationError(code, message, new InterviewImportReference(assignmentValue.Row, 0, PreloadedDataVerificationReferenceType.Cell,
                assignmentValue.Value, assignmentValue.FileName));
    }
}
