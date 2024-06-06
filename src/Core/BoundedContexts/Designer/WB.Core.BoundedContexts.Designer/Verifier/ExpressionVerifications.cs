﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WB.Core.BoundedContexts.Designer.Implementation.Services;
using WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration;
using WB.Core.BoundedContexts.Designer.Resources;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.Services.CodeGeneration;
using WB.Core.BoundedContexts.Designer.ValueObjects;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.ExpressionStorage;
using WB.Core.SharedKernels.Questionnaire.Documents;
using WB.Core.SharedKernels.QuestionnaireEntities;
using WB.Core.SharedKernels.SurveySolutions.Documents;

namespace WB.Core.BoundedContexts.Designer.Verifier
{
    public class ExpressionVerifications : AbstractVerifier, IPartialVerifier
    {
        private readonly IMacrosSubstitutionService macrosSubstitutionService;
        private readonly IExpressionProcessor expressionProcessor;
        private readonly IDynamicCompilerSettingsProvider compilerSettings;

        
        private static string WrapToClass(string expression) => $"using System; class __0c6e6226bbc84e43aae9324a93cd594f {{ bool __b1d0447f51874e3b83f145683aeec643() {{ return ({expression}); }} }} ";

        public ExpressionVerifications(IMacrosSubstitutionService macrosSubstitutionService,
            IExpressionProcessor expressionProcessor,
            IDynamicCompilerSettingsProvider compilerSettings)
        {
            this.macrosSubstitutionService = macrosSubstitutionService;
            this.expressionProcessor = expressionProcessor;
            this.compilerSettings = compilerSettings;
        }

        private IEnumerable<Func<MultiLanguageQuestionnaireDocument, IEnumerable<QuestionnaireVerificationMessage>>> ErrorsVerifiers => new []
        {
            ExpressionError(ExpressionUsesForbiddenDateTimeProperties, "WB0118", WB0118_ExpressionReferencingForbiddenDateTimeProperies),
            ExpressionWarning(this.BitwiseAnd, "WB0237", VerificationMessages.WB0237_BitwiseAnd),
            ExpressionWarning(this.BitwiseOr, "WB0238", VerificationMessages.WB0238_BitwiseOr),

            CriticalRuleError(CriticalRuleExpressionIsEmpty, "WB0319", VerificationMessages.WB0319_CriticalityConditionExpressionIsEmpty),
            CriticalRuleError(CriticalRuleMessageIsEmpty, "WB0320", VerificationMessages.WB0320_CriticalityConditionExpressionIsEmpty),
            CriticalRuleError(CriticalRuleUsingForbiddenClasses, "WB0321", VerificationMessages.WB0321_CriticalityConditionUsingForbiddenClasses),

            CriticalRuleError(CriticalRuleExpressionHasLengthMoreThanLimitLength, "WB0322", string.Format(VerificationMessages.WB0322_CriticalRuleExpressionIsTooLong, MaxExpressionLength)),
            CriticalRuleError(CriticalRuleMessageLengthMoreThanLimitLength, "WB0323", string.Format(VerificationMessages.WB0323_CriticalRuleMessageIsTooLong, MaxValidationMessageLength)),
            
            Critical<IVariable>(VariableExpressionHasLengthMoreThan10000Characters, "WB0005", string.Format(VerificationMessages.WB0005_VariableExpressionHasLengthMoreThan10000Characters, MaxExpressionLength)),
            Error<IComposite, ValidationCondition>(GetValidationConditionsOrEmpty, ValidationConditionUsesForbiddenDateTimeProperties, "WB0118", index => string.Format(WB0118_ExpressionReferencingForbiddenDateTimeProperies, index)),
            Error<IComposite, ValidationCondition>(GetValidationConditionsOrEmpty, ValidationConditionIsTooLong, "WB0104", index => string.Format(VerificationMessages.WB0104_ValidationConditionIsTooLong, index, MaxExpressionLength), VerificationMessageLevel.Critical),
            Error<IComposite, ValidationCondition>(GetValidationConditionsOrEmpty, ValidationConditionIsEmpty, "WB0106", index => string.Format(VerificationMessages.WB0106_ValidationConditionIsEmpty, index)),
            Error<IQuestion, IComposite>(CategoricalLinkedQuestionUsedInFilterExpression, "WB0109", VerificationMessages.WB0109_CategoricalLinkedQuestionUsedInLinkedQuestionFilterExpresssion),
            Critical<IQuestion>(OptionFilterExpressionHasLengthMoreThan10000Characters, "WB0028", string.Format(VerificationMessages.WB0028_OptionsFilterExpressionHasLengthMoreThan10000Characters, MaxExpressionLength)),
            Error<SingleQuestion>(CascadingQuestionHasEnablementCondition, "WB0091", VerificationMessages.WB0091_CascadingChildQuestionShouldNotContainCondition),
            Error<SingleQuestion>(CascadingQuestionHasValidationExpresssion, "WB0092", VerificationMessages.WB0092_CascadingChildQuesionShouldNotContainValidation),
            Critical<IComposite>(this.ConditionExpressionHasLengthMoreThan10000Characters, "WB0094", string.Format(VerificationMessages.WB0094_ConditionExpresssionHasLengthMoreThan10000Characters, MaxExpressionLength)),
            Critical<IQuestion>(LinkedQuestionFilterExpressionHasLengthMoreThan10000Characters, "WB0108", string.Format(VerificationMessages.WB0108_LinkedQuestionFilterExpresssionHasLengthMoreThan10000Characters, MaxExpressionLength)),
            Critical<IGroup>(GroupEnablementConditionReferenceChildItems,  "WB0130", VerificationMessages.WB0130_SubsectionOrRosterReferenceChildrendInCondition),
            WarningForCollection(FewQuestionsWithSameLongEnablement, "WB0235", VerificationMessages.WB0235_FewQuestionsWithSameLongEnablement),
            WarningForCollection(FewQuestionsWithSameLongValidation, "WB0236", VerificationMessages.WB0236_FewQuestionsWithSameLongValidation),
            Warning<IQuestionnaireEntity>(this.RowIndexInMultiOptionBasedRoster, "WB0220", string.Format(VerificationMessages.WB0220_RowIndexInMultiOptionBasedRoster, nameof(IRosterLevel.rowindex), nameof(IRosterLevel.rowcode))),
            this.Warning_ValidationConditionRefersToAFutureQuestion_WB0250,
            this.Warning_EnablementConditionRefersToAFutureQuestion_WB0251,
            Warning<IValidatable, IQuestionnaireEntity>(this.SupervisorQuestionInValidation, "WB0229", VerificationMessages.WB0229_SupervisorQuestionInValidation),
            Warning<IComposite>(HasLongEnablementCondition, "WB0209", VerificationMessages.WB0209_LongEnablementCondition),
            WarningForTranslation<IComposite, ValidationCondition>(GetValidationConditionsOrEmpty, ValidationMessageIsEmpty, "WB0107", index => string.Format(VerificationMessages.WB0107_ValidationMessageIsEmpty, index)),
            Warning<IQuestion, ValidationCondition>(q => q.ValidationConditions, HasLongValidationCondition, "WB0212", index => string.Format(VerificationMessages.WB0212_LongValidationCondition, index)),
            WarningForCollection(ConsecutiveQuestionsWithIdenticalEnablementConditions, "WB0218", VerificationMessages.WB0218_ConsecutiveQuestionsWithIdenticalEnablementConditions),
            WarningForCollection(ConsecutiveUnconditionalSingleChoiceQuestionsWith2Options, "WB0219", string.Format(VerificationMessages.WB0219_ConsecutiveUnconditionalSingleChoiceQuestionsWith2Options, UnconditionalSingleChoiceQuestionOptionsCount)),

            Error<IComposite>(this.ConditionUsingForbiddenClasses, "WB0272", VerificationMessages.WB0272_ConditionUsingForbiddenClasses),
            Error<IComposite, ValidationCondition>(GetValidationConditionsOrEmpty, ValidationUsingForbiddenClasses, "WB0273", 
                index => VerificationMessages.WB0273_ValidationConditionUsingForbiddenClasses, VerificationMessageLevel.Critical),
            Error<IVariable>(VariableUsingForbiddenClasses, "WB0274", VerificationMessages.WB0274_VariableUsingForbiddenClasses),
            Error<IQuestion>(FilterExpressionUsingForbiddenClasses, "WB0275", VerificationMessages.WB0275_FilterExpressionIsUsingForbiddenClasses),
            Error<IComposite>(ConditionsContainsRowname, "WB0276", VerificationMessages.WB0276_RownameIsNotSupported)
        };

        private bool CriticalRuleMessageLengthMoreThanLimitLength(CriticalRule rule, MultiLanguageQuestionnaireDocument questionnaire)
            => rule.Message?.Length > MaxValidationMessageLength;

        private bool CriticalRuleExpressionHasLengthMoreThanLimitLength(CriticalRule rule, MultiLanguageQuestionnaireDocument questionnaire)
            => rule.Expression?.Length > MaxExpressionLength;

        private bool ConditionsContainsRowname(IComposite node, MultiLanguageQuestionnaireDocument questionnaire)
        {
            const string RowName = "@rowname";

            if (node is IValidatable validatable && validatable.ValidationConditions.Any(vc => vc.Expression.Contains(RowName)))
                return true;

            if (node is IQuestion question
                && (
                    (question.LinkedFilterExpression?.Contains(RowName) ?? false)
                    || (question.Properties?.OptionsFilterExpression?.Contains(RowName) ?? false)
                    || (question.ConditionExpression?.Contains(RowName) ?? false))
                )
                return true;

            if (node is IVariable variable && (variable.Expression?.Contains(RowName) ?? false))
                return true;

            return false;
        }

        private bool FilterExpressionUsingForbiddenClasses(IQuestion node, MultiLanguageQuestionnaireDocument questionnaire)
        {
            var expression = string.IsNullOrEmpty(node.LinkedFilterExpression) ? node.Properties?.OptionsFilterExpression : node.LinkedFilterExpression;
            return CheckForbiddenClassesUsage(expression, questionnaire);
        }

        private bool VariableUsingForbiddenClasses(IVariable node, MultiLanguageQuestionnaireDocument questionnaire)
        {
            var expression = node.Expression;
            return CheckForbiddenClassesUsage(expression, questionnaire);
        }

        private bool ValidationUsingForbiddenClasses(IComposite node, ValidationCondition validationCondition, MultiLanguageQuestionnaireDocument questionnaire)
        {
            var validationConditionExpression = validationCondition.Expression;
            return CheckForbiddenClassesUsage(validationConditionExpression, questionnaire);
        }

        private bool ConditionUsingForbiddenClasses(IComposite item, MultiLanguageQuestionnaireDocument questionnaire)
        {
            var enablingCondition = item.GetEnablingCondition();
            return CheckForbiddenClassesUsage(enablingCondition, questionnaire);
        }

        private bool CriticalRuleUsingForbiddenClasses(CriticalRule item, MultiLanguageQuestionnaireDocument questionnaire) 
            => CheckForbiddenClassesUsage(item.Expression, questionnaire);

        private bool CheckForbiddenClassesUsage(string? expression, MultiLanguageQuestionnaireDocument questionnaire)
        {
            if (string.IsNullOrEmpty(expression)) return false;
            if (ExpressionContainsForbiddenTypeRef.TryGetValue(expression, out var value))
                return value;

            var expressionWithInlinedMacros = this.macrosSubstitutionService.InlineMacros(expression, questionnaire.Macros.Values);
            string code = WrapToClass(expressionWithInlinedMacros);
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(code, 
                options: new CSharpParseOptions(documentationMode:DocumentationMode.None));

            var compilation = CSharpCompilation.Create(
                "rules.dll",
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release),
                syntaxTrees: syntaxTree.ToEnumerable(),
                references: compilerSettings.GetAssembliesToReference());

            var foundUsages = new CodeSecurityChecker().FindForbiddenClassesUsage(syntaxTree, compilation);
            ExpressionContainsForbiddenTypeRef[expression] = foundUsages.Any();

            return ExpressionContainsForbiddenTypeRef[expression];
        }

        private bool GroupEnablementConditionReferenceChildItems(IGroup group, MultiLanguageQuestionnaireDocument questionnaire)
        {
            if (string.IsNullOrWhiteSpace(group.ConditionExpression))
                return false;
            var condition = this.macrosSubstitutionService.InlineMacros(group.ConditionExpression, questionnaire.Macros.Values);
            if (string.IsNullOrWhiteSpace(condition))
                return false;

            var variablesInExpression = GetIdentifiersUsedInExpression(condition, questionnaire);
            foreach (var variable in variablesInExpression)
            {
                var entity = questionnaire.Questionnaire.GetEntityByVariable(variable);
                if (entity == null)
                    continue;
                var parentIds = questionnaire.Questionnaire.GetParentGroupsIds(entity);
                if (parentIds.Contains(group.PublicKey))
                    return true;
            }
            return false;
        }
        
        private IEnumerable<IQuestion> GetReferencedQuestions(string expression, MultiLanguageQuestionnaireDocument questionnaire)
            => this
                .GetIdentifiersUsedInExpression(expression, questionnaire)
                .Select(identifier => questionnaire.GetQuestionByName(identifier))
                .Where(referencedQuestion => referencedQuestion != null)
                .Select(question => question!);

        private IEnumerable<IQuestion> GetReferencedQuestions(ValidationCondition validationCondition, MultiLanguageQuestionnaireDocument questionnaire)
            => this.GetReferencedQuestions(validationCondition.Expression, questionnaire);

        private IEnumerable<IQuestion> GetReferencedQuestions(IValidatable validatable, MultiLanguageQuestionnaireDocument questionnaire)
            => validatable
                .ValidationConditions
                .SelectMany(condition => this.GetReferencedQuestions(condition, questionnaire))
                .Distinct();

        private static IEnumerable<QuestionnaireEntityReference[]> ConsecutiveUnconditionalSingleChoiceQuestionsWith2Options(MultiLanguageQuestionnaireDocument questionnaire)
            => questionnaire
                .Find<SingleQuestion>()
                .Where(IsUnconditionalWith2Options)
                .GroupBy(question => new
                {
                    FirstConsecutiveUnconditionalSingleOptionQuestionWith2Options = question.UnwrapReferences(GetPreviousUnconditionalSingleOptionQuestionWith2Options).Last(),
                })
                .Where(grouping => grouping.Count() >= 3)
                .Select(grouping => grouping.Select(question => CreateReference(question)).ToArray());

        private static SingleQuestion? GetPreviousUnconditionalSingleOptionQuestionWith2Options(SingleQuestion question)
        {
            var previousQuestion = question.GetPrevious() as SingleQuestion;

            return previousQuestion != null && IsUnconditionalWith2Options(previousQuestion)
                ? previousQuestion
                : null;
        }

        private static bool IsUnconditionalWith2Options(SingleQuestion question)
            => string.IsNullOrWhiteSpace(question.ConditionExpression)
               && question.Answers.Count == 2;

        private static IEnumerable<QuestionnaireEntityReference[]> ConsecutiveQuestionsWithIdenticalEnablementConditions(MultiLanguageQuestionnaireDocument questionnaire)
            => questionnaire
                .Find<IQuestion>()
                .Where(question => !string.IsNullOrWhiteSpace(question.ConditionExpression))
                .GroupBy(question => new
                {
                    Enablement = question.ConditionExpression,
                    FirstConsecutiveQuestionWithSameEnablement = question.UnwrapReferences(GetPreviousQuestionWithSameEnablement).Last(),
                })
                .Where(grouping => grouping.Count() >= 3)
                .Select(grouping => grouping.Select(question => CreateReference(question)).ToArray());

        private static IQuestion? GetPreviousQuestionWithSameEnablement(IQuestion question)
        {
            var previousQuestion = question.GetPrevious() as IQuestion;

            return previousQuestion?.ConditionExpression == question.ConditionExpression
                ? previousQuestion
                : null;
        }

        private static bool HasLongValidationCondition(ValidationCondition condition)
            => !string.IsNullOrEmpty(condition.Expression) && condition.Expression.Length > 500;

        private static bool ValidationMessageIsEmpty(ValidationCondition validationCondition)
            => string.IsNullOrWhiteSpace(validationCondition.Message);

        private bool HasLongEnablementCondition(IComposite groupOrQuestion)
        {
            var customEnablementCondition = GetCustomEnablementCondition(groupOrQuestion);

            if (string.IsNullOrEmpty(customEnablementCondition))
                return false;

            var exceeded = customEnablementCondition.Length > 500;
            return exceeded;
        }

        private static string? GetCustomEnablementCondition(IComposite entity)
        {
            var entityAsIConditional = entity as IConditional;

            return entityAsIConditional?.ConditionExpression;
        }


        private EntityVerificationResult<IQuestionnaireEntity> SupervisorQuestionInValidation(IValidatable validatable, MultiLanguageQuestionnaireDocument questionnaire)
        {
            var supervisorQuestions = this
                .GetReferencedQuestions(validatable, questionnaire)
                .Where(question => question.QuestionScope == QuestionScope.Supervisor)
                .ToList();

            return supervisorQuestions.Count == 0
                ? EntityVerificationResult.NoProblems()
                : EntityVerificationResult.Problems(validatable, supervisorQuestions);
        }

        private IEnumerable<QuestionnaireVerificationMessage> Warning_EnablementConditionRefersToAFutureQuestion_WB0251(MultiLanguageQuestionnaireDocument questionnaire)
        {
            var result = new List<QuestionnaireVerificationMessage>();
            Guid[] questionnairePlainStructure = questionnaire.GetAllEntitiesIdAndTypePairsInQuestionnaireFlowOrder().Select(e => e.Id).ToArray();
            var enitiesWithConditions =
                questionnaire.Find<IComposite>(
                    c => !string.IsNullOrEmpty((c as IConditional)?.ConditionExpression));

            foreach (var enitiesWithCondition in enitiesWithConditions)
            {
                var entityIndex = Array.IndexOf(questionnairePlainStructure, enitiesWithCondition.PublicKey);
                var conditionExpression = ((IConditional)enitiesWithCondition).ConditionExpression;

                var referencedQuestions = this.GetReferencedQuestions(conditionExpression, questionnaire);

                foreach (var referencedQuestion in referencedQuestions)
                {
                    var indexOfReferencedQuestion =
                         Array.IndexOf(questionnairePlainStructure, referencedQuestion.PublicKey);

                    if (indexOfReferencedQuestion > entityIndex)
                    {
                        result.Add(QuestionnaireVerificationMessage.Warning("WB0251",
                            VerificationMessages.WB0251_EnablementConditionRefersToAFutureQuestion,
                            CreateReference(enitiesWithCondition),
                            CreateReference(referencedQuestion)));
                    }
                }
            }
            return result;
        }

        private IEnumerable<QuestionnaireVerificationMessage> Warning_ValidationConditionRefersToAFutureQuestion_WB0250(
            MultiLanguageQuestionnaireDocument questionnaire)
        {
            var result = new List<QuestionnaireVerificationMessage>();
            var questionnairePlainStructure = questionnaire.GetAllEntitiesIdAndTypePairsInQuestionnaireFlowOrder().Select(e => e.Id).ToArray();

            var enitiesWithValidations =
                questionnaire.Find<IComposite>(
                    c => c is IValidatable && ((IValidatable)c).ValidationConditions.Count > 0);

            foreach (var enitiesWithValidation in enitiesWithValidations)
            {
                var entityIndex = Array.IndexOf(questionnairePlainStructure, enitiesWithValidation.PublicKey);
                var validationIndex = 1;
                var validationConditions = ((IValidatable)enitiesWithValidation).ValidationConditions;
                foreach (var validationCondition in validationConditions)
                {
                    var referencedQuestions = this.GetReferencedQuestions(validationCondition, questionnaire);
                    foreach (var referencedQuestion in referencedQuestions)
                    {
                        var indexOfReferencedQuestion =
                            Array.IndexOf(questionnairePlainStructure, referencedQuestion.PublicKey);

                        if (indexOfReferencedQuestion > entityIndex)
                        {
                            result.Add(QuestionnaireVerificationMessage.Warning("WB0250",
                                string.Format(VerificationMessages.WB0250_ValidationConditionRefersToAFutureQuestion, validationIndex),
                                CreateReference(enitiesWithValidation, validationIndex, QuestionnaireVerificationReferenceProperty.ValidationExpression),
                                CreateReference(referencedQuestion)));
                        }
                    }
                    validationIndex++;
                }
            }
            return result;
        }

        private static bool IsInsideMultiOptionBasedRoster(IQuestionnaireEntity entity, MultiLanguageQuestionnaireDocument questionnaire)
            => entity.UnwrapReferences(x => x.GetParent())
                .Any(parent => questionnaire.Questionnaire.IsMultiRoster(parent as IGroup));

        private bool RowIndexInMultiOptionBasedRoster(IQuestionnaireEntity entity, MultiLanguageQuestionnaireDocument questionnaire)
            => this.UsesRowIndex(entity)
               && IsInsideMultiOptionBasedRoster(entity, questionnaire);

        private bool UsesRowIndex(IQuestionnaireEntity entity) => entity.GetAllExpressions().Any(this.UsesRowIndex);

        private bool UsesRowIndex(string expression)
        {
            var identifiers = this.expressionProcessor.GetIdentifiersUsedInExpression(expression);
            return identifiers.Contains("rowindex") || identifiers.Contains("@rowindex");
        }

        private bool BitwiseAnd(string expression, MultiLanguageQuestionnaireDocument questionnaire) => this.expressionProcessor.ContainsBitwiseAnd(expression);
        private bool BitwiseOr(string expression, MultiLanguageQuestionnaireDocument questionnaire) => this.expressionProcessor.ContainsBitwiseOr(expression);

        private static IEnumerable<QuestionnaireEntityReference[]> FewQuestionsWithSameLongValidation(MultiLanguageQuestionnaireDocument questionnaire)
            => questionnaire
                .Find<IQuestion>()
                .Where(question => question.ValidationConditions != null)
                .SelectMany(question => question.ValidationConditions.Select(validation => new { validation, question }))
                .Where(x => !string.IsNullOrWhiteSpace(x.validation.Expression))
                .Where(x => x.validation.Expression.Length >= 100)
                .GroupBy(x => x.validation.Expression)
                .Where(grouping => grouping.Select(x => x.question).Distinct().Count() >= 2)
                .Select(grouping => grouping.Select(x => CreateReference(x.question)).ToArray());

        private static IEnumerable<QuestionnaireEntityReference[]> FewQuestionsWithSameLongEnablement(MultiLanguageQuestionnaireDocument questionnaire)
            => questionnaire
                .Find<IQuestion>()
                .Where(question => !string.IsNullOrWhiteSpace(question.ConditionExpression))
                .Where(question => question.ConditionExpression.Length >= 100)
                .GroupBy(question => question.ConditionExpression)
                .Where(grouping => grouping.Count() >= 2)
                .Select(grouping => grouping.Select(question => CreateReference(question)).ToArray());

        private static string WB0118_ExpressionReferencingForbiddenDateTimeProperies
            => string.Format(VerificationMessages.WB0118_ExpressionReferencingForbiddenDateTimeProperies,
                $"{nameof(DateTime)}.{nameof(DateTime.Now)}", $"{nameof(DateTime)}.{nameof(DateTime.UtcNow)}",
                $"{nameof(DateTime)}.{nameof(DateTime.Today)}");

        private static IEnumerable<ValidationCondition> GetValidationConditionsOrEmpty(IComposite entity)
        {
            var entityAsIConditional = entity as IValidatable;

            return entityAsIConditional != null
                ? entityAsIConditional.ValidationConditions
                : Enumerable.Empty<ValidationCondition>();
        }

        private EntityVerificationResult<IComposite> CategoricalLinkedQuestionUsedInFilterExpression(IQuestion question, MultiLanguageQuestionnaireDocument questionnaire)
        {
            if (!(question.LinkedToQuestionId.HasValue || question.LinkedToRosterId.HasValue))
               return new EntityVerificationResult<IComposite> { HasErrors = false };

            if (string.IsNullOrEmpty(question.LinkedFilterExpression))
                return new EntityVerificationResult<IComposite> { HasErrors = false };

            return this.VerifyWhetherEntityExpressionReferencesIncorrectQuestions(question,
                question.LinkedFilterExpression,
                questionnaire, isReferencedQuestionIncorrect: (q) => q.PublicKey == question.PublicKey);
        }

        private bool LinkedQuestionFilterExpressionHasLengthMoreThan10000Characters(IQuestion question, MultiLanguageQuestionnaireDocument questionnaire)
        {
            if (!(question.LinkedToQuestionId.HasValue || question.LinkedToRosterId.HasValue))
                return false;

            return this.DoesExpressionExceed1000CharsLimit(questionnaire, question.LinkedFilterExpression);
        }

        private bool ConditionExpressionHasLengthMoreThan10000Characters(IComposite entity, MultiLanguageQuestionnaireDocument questionnaire)
        {
            return this.DoesExpressionExceed1000CharsLimit(questionnaire, (entity as IConditional)?.ConditionExpression);
        }

        private bool OptionFilterExpressionHasLengthMoreThan10000Characters(IQuestion question, MultiLanguageQuestionnaireDocument questionnaire)
            => this.DoesExpressionExceed1000CharsLimit(questionnaire, question.Properties?.OptionsFilterExpression);

        private static bool CascadingQuestionHasValidationExpresssion(SingleQuestion question, MultiLanguageQuestionnaireDocument questionnaire)
        {
            return question.CascadeFromQuestionId.HasValue && !string.IsNullOrWhiteSpace(question.ValidationExpression);
        }

        private static bool CascadingQuestionHasEnablementCondition(SingleQuestion question, MultiLanguageQuestionnaireDocument questionnaire)
        {
            return question.CascadeFromQuestionId.HasValue && !string.IsNullOrWhiteSpace(question.ConditionExpression);
        }

        private bool ValidationConditionUsesForbiddenDateTimeProperties(IComposite question, ValidationCondition validationCondition, MultiLanguageQuestionnaireDocument questionnaire)
            => ExpressionUsesForbiddenDateTimeProperties(validationCondition.Expression, questionnaire);

        private static bool ValidationConditionIsEmpty(IComposite question, ValidationCondition validationCondition, MultiLanguageQuestionnaireDocument questionnaire)
            => string.IsNullOrWhiteSpace(validationCondition.Expression);

        private static bool CriticalRuleExpressionIsEmpty(CriticalRule criticalRule, MultiLanguageQuestionnaireDocument questionnaire)
            => string.IsNullOrWhiteSpace(criticalRule.Expression);

        private static bool CriticalRuleMessageIsEmpty(CriticalRule criticalRule, MultiLanguageQuestionnaireDocument questionnaire)
            => string.IsNullOrWhiteSpace(criticalRule.Message);
        

        private static bool ValidationConditionIsTooLong(IComposite question, ValidationCondition validationCondition, MultiLanguageQuestionnaireDocument questionnaire)
            => validationCondition.Expression?.Length > MaxExpressionLength;
        


        private EntityVerificationResult<IComposite> VerifyWhetherEntityExpressionReferencesIncorrectQuestions(
            IComposite entity, string? expression, MultiLanguageQuestionnaireDocument questionnaire, Func<IComposite, bool> isReferencedQuestionIncorrect)
        {
            if (string.IsNullOrEmpty(expression))
                return new EntityVerificationResult<IComposite> { HasErrors = false };

            IEnumerable<IComposite> incorrectReferencedQuestions = this
                .GetIdentifiersUsedInExpression(expression, questionnaire)
                .Select(identifier => questionnaire.Questionnaire.GetEntityByVariable(identifier))
                .Where(referencedQuestion => referencedQuestion != null)
                .Select(question => question!)
                .Where(isReferencedQuestionIncorrect)
                .ToList();

            if (!incorrectReferencedQuestions.Any())
                return new EntityVerificationResult<IComposite> { HasErrors = false };

            var referencedEntities =
                Enumerable.Concat(entity.ToEnumerable(), incorrectReferencedQuestions).Distinct().ToArray();

            return new EntityVerificationResult<IComposite>
            {
                HasErrors = true,
                ReferencedEntities = referencedEntities
            };
        }
        
        private bool DoesExpressionExceed1000CharsLimit(MultiLanguageQuestionnaireDocument questionnaire, string? expression)
        {
            if (string.IsNullOrEmpty(expression))
                return false;

            var expressionWithInlinedMacroses =
                this.macrosSubstitutionService.InlineMacros(expression, questionnaire.Macros.Values);

            return expressionWithInlinedMacroses.Length > MaxExpressionLength;
        }

        private bool VariableExpressionHasLengthMoreThan10000Characters(IVariable variable,
            MultiLanguageQuestionnaireDocument questionnaire)
            => this.DoesExpressionExceed1000CharsLimit(questionnaire, variable.Expression);

        
        protected bool ExpressionUsesForbiddenDateTimeProperties(string? expression,
            MultiLanguageQuestionnaireDocument questionnaire)
        {
            if (string.IsNullOrWhiteSpace(expression)) return false;
            return GetIdentifiersUsedInExpression(expression, questionnaire)
                .Contains(RoslynExpressionProcessor.ForbiddenDatetimeNow);
        }

        protected IReadOnlyCollection<string> GetIdentifiersUsedInExpression(string expression,
            MultiLanguageQuestionnaireDocument questionnaire)
        {
            string expressionWithInlinedMacros =
                this.macrosSubstitutionService.InlineMacros(expression, questionnaire.Macros.Values);

            return this.expressionProcessor.GetIdentifiersUsedInExpression(expressionWithInlinedMacros);
        }

        private static Func<MultiLanguageQuestionnaireDocument, IEnumerable<QuestionnaireVerificationMessage>> Error<TEntity, TReferencedEntity>(
            Func<TEntity, MultiLanguageQuestionnaireDocument, EntityVerificationResult<TReferencedEntity>> verifyEntity, string code, string message)
            where TEntity : class, IComposite
            where TReferencedEntity : class, IComposite
        {
            return questionnaire =>
                from entity in questionnaire.Find<TEntity>(_ => true)
                let verificationResult = verifyEntity(entity, questionnaire)
                where verificationResult.HasErrors
                select QuestionnaireVerificationMessage.Error(code, message, verificationResult.ReferencedEntities.Select(x => CreateReference(x)).ToArray());
        }

        private static Func<MultiLanguageQuestionnaireDocument, IEnumerable<QuestionnaireVerificationMessage>> Error<TEntity>(
            Func<TEntity, MultiLanguageQuestionnaireDocument, bool> hasError, string code, string message)
            where TEntity : class, IComposite
        {
            return questionnaire =>
                questionnaire
                    .Find<TEntity>(entity => hasError(entity, questionnaire))
                    .Select(entity => QuestionnaireVerificationMessage.Error(code, message, CreateReference(entity)));
        }

        private Func<MultiLanguageQuestionnaireDocument, IEnumerable<QuestionnaireVerificationMessage>> ExpressionError(
            Func<string, MultiLanguageQuestionnaireDocument, bool> hasError, string code, string message)
        {
            return questionnaire => ExpressionCheckImpl(questionnaire, VerificationMessageLevel.General, hasError, code, message);
        }

        private Func<MultiLanguageQuestionnaireDocument, IEnumerable<QuestionnaireVerificationMessage>> ExpressionWarning(
            Func<string, MultiLanguageQuestionnaireDocument, bool> hasError, string code, string message)
        {
            return questionnaire => ExpressionCheckImpl(questionnaire, VerificationMessageLevel.Warning, hasError, code, message);
        }

        private IEnumerable<QuestionnaireVerificationMessage> ExpressionCheckImpl(MultiLanguageQuestionnaireDocument questionnaire,
            VerificationMessageLevel messageLevel,
                Func<string, MultiLanguageQuestionnaireDocument, bool> hasError, string code, string message)
        {
            var entitiesWithConditions =
                    questionnaire.Find<IComposite>(c => !string.IsNullOrEmpty((c as IConditional)?.ConditionExpression));

            foreach (var entitiesWithCondition in entitiesWithConditions)
            {
                if (entitiesWithCondition is IConditional conditional)
                {
                    var expressionWithInlinedMacros = this.macrosSubstitutionService.InlineMacros(conditional.ConditionExpression, questionnaire.Macros.Values);
                    if (hasError(expressionWithInlinedMacros, questionnaire)) 
                        yield return QuestionnaireVerificationMessage.VerificationMessage(messageLevel, code, message, CreateReference(entitiesWithCondition, property: QuestionnaireVerificationReferenceProperty.EnablingCondition));
                }
            }

            var questionsWithFilter = questionnaire.Find<IQuestion>(c => !string.IsNullOrEmpty(c.Properties?.OptionsFilterExpression));
            foreach (var question in questionsWithFilter)
            {
                var filter = question.Properties?.OptionsFilterExpression;
                var filterWithInlinedMacros = this.macrosSubstitutionService.InlineMacros(filter, questionnaire.Macros.Values);
                if (filter != null && hasError(filterWithInlinedMacros, questionnaire))
                    yield return QuestionnaireVerificationMessage.VerificationMessage(messageLevel, code, message, CreateReference(question, property: QuestionnaireVerificationReferenceProperty.OptionsFilter)); 
            }

            var questionsWithLinkedFilter = questionnaire.Find<IQuestion>(c => !string.IsNullOrEmpty(c.LinkedFilterExpression));
            foreach (var question in questionsWithLinkedFilter)
            {
                var filter = question.LinkedFilterExpression;
                var filterWithInlinedMacros = this.macrosSubstitutionService.InlineMacros(filter, questionnaire.Macros.Values);
                if (filter != null && hasError(filterWithInlinedMacros, questionnaire))
                    yield return QuestionnaireVerificationMessage.VerificationMessage(messageLevel, code, message, CreateReference(question)); 
            }
            
            var entitiesWithValidationConditions =
                questionnaire
                    .Find<IComposite>(c => (c as IValidatable)?.ValidationConditions.Count > 0)
                    .Select(e => (IValidatable)e);

            foreach (var entity in entitiesWithValidationConditions)
            {
                for(var index = 0; index < entity.ValidationConditions.Count; index++)
                {
                    var validationCondition = entity.ValidationConditions[index];
                    var validationWithInlinedMacros = this.macrosSubstitutionService.InlineMacros(validationCondition.Expression, questionnaire.Macros.Values);
                    if (!string.IsNullOrWhiteSpace(validationWithInlinedMacros) && hasError(validationCondition.Expression, questionnaire))
                        yield return QuestionnaireVerificationMessage.VerificationMessage(messageLevel, code, message, CreateReference(entity, index, QuestionnaireVerificationReferenceProperty.ValidationExpression)); 
                }
            }
            
            var variables = questionnaire.Find<IVariable>(v => !string.IsNullOrEmpty(v.Expression));
            foreach (var variable in variables)
            {
                if (!string.IsNullOrWhiteSpace(variable.Expression))
                {
                    var variableWithInlinedMacros = this.macrosSubstitutionService.InlineMacros(variable.Expression, questionnaire.Macros.Values);
                    if (hasError(variableWithInlinedMacros, questionnaire)) 
                        yield return QuestionnaireVerificationMessage.VerificationMessage(messageLevel, code, message, QuestionnaireEntityReference.CreateForVariable(variable.PublicKey));
                }
            }

            foreach (var criticalRule in questionnaire.CriticalRules)
            {
                if (!string.IsNullOrEmpty(criticalRule.Expression))
                {
                    var criticalRuleWithInlinedMacros = this.macrosSubstitutionService.InlineMacros(criticalRule.Expression, questionnaire.Macros.Values);
                    if (hasError(criticalRuleWithInlinedMacros, questionnaire))
                        yield return QuestionnaireVerificationMessage.VerificationMessage(messageLevel, code, message, 
                            QuestionnaireEntityReference.CreateForCriticalRule(criticalRule.Id));
                }
            }
        }
        
        private static Func<MultiLanguageQuestionnaireDocument, IEnumerable<QuestionnaireVerificationMessage>> CriticalRuleError(
            Func<CriticalRule, MultiLanguageQuestionnaireDocument, bool> hasError, string code, string message)
        {
            return questionnaire =>
                questionnaire.CriticalRules
                    .Where(cc => hasError(cc, questionnaire))
                    .Select(cc => QuestionnaireVerificationMessage.Error(code, message,
                        QuestionnaireEntityReference.CreateForCriticalRule(cc.Id)));
        }

        private static Func<MultiLanguageQuestionnaireDocument, IEnumerable<QuestionnaireVerificationMessage>> Error<TEntity, TSubEntity>(
            Func<TEntity, IEnumerable<TSubEntity>> getSubEntities, 
            Func<TEntity, TSubEntity, MultiLanguageQuestionnaireDocument, bool> hasError, string code, 
            Func<int, string> getMessageBySubEntityIndex, VerificationMessageLevel level = VerificationMessageLevel.General)
            where TEntity : class, IComposite
        {
            return questionnaire =>
                questionnaire
                    .Find<TEntity>(entity => true)
                    .SelectMany(entity => getSubEntities(entity).Select((subEntity, index) => new { Entity = entity, SubEntity = subEntity, Index = index }))
                    .Where(descriptor => hasError(descriptor.Entity, descriptor.SubEntity, questionnaire))
                    .Select(descriptor => level == VerificationMessageLevel.General 
                        ? QuestionnaireVerificationMessage.Error(code, getMessageBySubEntityIndex(descriptor.Index + 1), CreateReference(descriptor.Entity, descriptor.Index))
                        : QuestionnaireVerificationMessage.Critical(code, getMessageBySubEntityIndex(descriptor.Index + 1), CreateReference(descriptor.Entity, descriptor.Index)));
        }

        private static Func<MultiLanguageQuestionnaireDocument, IEnumerable<QuestionnaireVerificationMessage>> Critical<TEntity>(
            Func<TEntity, MultiLanguageQuestionnaireDocument, bool> hasError, string code, string message)
            where TEntity : class, IComposite
        {
            return questionnaire =>
                questionnaire
                    .Find<TEntity>(entity => hasError(entity, questionnaire))
                    .Select(entity => QuestionnaireVerificationMessage.Critical(code, message, CreateReference(entity)));
        }

        private static Func<MultiLanguageQuestionnaireDocument, IEnumerable<QuestionnaireVerificationMessage>> WarningForTranslation<TEntity, TSubEntity>(
            Func<TEntity, IEnumerable<TSubEntity>> getSubEnitites, Func<TSubEntity, bool> hasError, string code, Func<int, string> getMessageBySubEntityIndex)
            where TEntity : class, IComposite
        {
            return WarningForTranslation(getSubEnitites, (entity, subEntity, questionnaire) => hasError(subEntity), code, getMessageBySubEntityIndex);
        }

        private static Func<MultiLanguageQuestionnaireDocument, IEnumerable<QuestionnaireVerificationMessage>> WarningForTranslation<TEntity, TSubEntity>(
            Func<TEntity, IEnumerable<TSubEntity>> getSubEnitites, Func<TEntity, TSubEntity, MultiLanguageQuestionnaireDocument, bool> hasError, string code, Func<int, string> getMessageBySubEntityIndex)
            where TEntity : class, IComposite
        {
            return questionnaire =>
                questionnaire
                    .FindWithTranslations<TEntity>(entity => true)
                    .SelectMany(translatedEntity => getSubEnitites(translatedEntity.Entity).Select((subEntity, index) => new { Entity = translatedEntity, SubEntity = subEntity, Index = index }))
                    .Where(descriptor => hasError(descriptor.Entity.Entity, descriptor.SubEntity, questionnaire))
                    .Select(descriptor =>
                        QuestionnaireVerificationMessage.Warning(
                            code,
                            descriptor.Entity.TranslationName == null
                                ? getMessageBySubEntityIndex(descriptor.Index + 1)
                                : descriptor.Entity.TranslationName + ": " + getMessageBySubEntityIndex(descriptor.Index + 1),
                            CreateReference(descriptor.Entity.Entity, descriptor.Index))
                    );
        }

        private static Func<MultiLanguageQuestionnaireDocument, IEnumerable<QuestionnaireVerificationMessage>> Warning<TEntity, TSubEntity>(
            Func<TEntity, IEnumerable<TSubEntity>> getSubEnitites, Func<TSubEntity, bool> hasError, string code, Func<int, string> getMessageBySubEntityIndex)
            where TEntity : class, IComposite
        {
            return Warning(getSubEnitites, (entity, subEntity, questionnaire) => hasError(subEntity), code, getMessageBySubEntityIndex);
        }

        private static Func<MultiLanguageQuestionnaireDocument, IEnumerable<QuestionnaireVerificationMessage>> Warning<TEntity, TSubEntity>(
            Func<TEntity, IEnumerable<TSubEntity>> getSubEnitites, Func<TEntity, TSubEntity, MultiLanguageQuestionnaireDocument, bool> hasError, string code, Func<int, string> getMessageBySubEntityIndex)
            where TEntity : class, IComposite
        {
            return questionnaire =>
                questionnaire
                    .Find<TEntity>(entity => true)
                    .SelectMany(entity => getSubEnitites(entity).Select((subEntity, index) => new { Entity = entity, SubEntity = subEntity, Index = index }))
                    .Where(descriptor => hasError(descriptor.Entity, descriptor.SubEntity, questionnaire))
                    .Select(descriptor => QuestionnaireVerificationMessage.Warning(code, getMessageBySubEntityIndex(descriptor.Index + 1), CreateReference(descriptor.Entity, descriptor.Index)));
        }

        private static Func<MultiLanguageQuestionnaireDocument, IEnumerable<QuestionnaireVerificationMessage>> Warning<TEntity>(
            Func<TEntity, MultiLanguageQuestionnaireDocument, bool> hasError, string code, string message)
            where TEntity : class, IQuestionnaireEntity
        {
            return questionnaire =>
                questionnaire
                    .Find<TEntity>(x => hasError(x, questionnaire))
                    .Select(entity => QuestionnaireVerificationMessage.Warning(code, message, CreateReference(entity)));
        }

        private static Func<MultiLanguageQuestionnaireDocument, IEnumerable<QuestionnaireVerificationMessage>> Warning<TEntity, TReferencedEntity>(
            Func<TEntity, MultiLanguageQuestionnaireDocument, EntityVerificationResult<TReferencedEntity>> verifyEntity, string code, string message)
            where TEntity : class, IQuestionnaireEntity
            where TReferencedEntity : class, IQuestionnaireEntity
        {
            return questionnaire =>
                from entity in questionnaire.Find<TEntity>(_ => true)
                let verificationResult = verifyEntity(entity, questionnaire)
                where verificationResult.HasErrors
                select QuestionnaireVerificationMessage.Warning(code, message, 
                    verificationResult.ReferencedEntities.Select(entity => CreateReference(entity)).ToArray());
        }

        private static Func<MultiLanguageQuestionnaireDocument, IEnumerable<QuestionnaireVerificationMessage>> Warning<TEntity>(
            Func<TEntity, bool> hasError, string code, string message)
            where TEntity : class, IQuestionnaireEntity
        {
            return questionnaire =>
                questionnaire
                    .Find<TEntity>(hasError)
                    .Select(entity => QuestionnaireVerificationMessage.Warning(code, message, CreateReference(entity)));
        }

        private static Func<MultiLanguageQuestionnaireDocument, IEnumerable<QuestionnaireVerificationMessage>> WarningForCollection(
            Func<MultiLanguageQuestionnaireDocument, IEnumerable<QuestionnaireEntityReference[]>> getReferences, string code, string message)
        {
            return questionnaire
                => getReferences(questionnaire)
                    .Select(references => QuestionnaireVerificationMessage.Warning(code, message, references));
        }

        public IEnumerable<QuestionnaireVerificationMessage> Verify(MultiLanguageQuestionnaireDocument multiLanguageQuestionnaireDocument)
        {
            var verificationMessagesByQuestionnaire = new List<QuestionnaireVerificationMessage>();
            ExpressionContainsForbiddenTypeRef = new Dictionary<string, bool>();
            
            foreach (var verifier in ErrorsVerifiers.AsParallel())
            {
                verificationMessagesByQuestionnaire.AddRange(verifier.Invoke(multiLanguageQuestionnaireDocument));
            }

            return verificationMessagesByQuestionnaire;
        }

        private Dictionary<string, bool> ExpressionContainsForbiddenTypeRef = new Dictionary<string, bool>();
    }
}
