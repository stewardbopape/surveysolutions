﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Main.Core.Entities.SubEntities;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities;
using WB.UI.Headquarters.Models.WebInterview;
using InterviewStaticText = WB.UI.Headquarters.Models.WebInterview.InterviewStaticText;
using WB.Core.SharedKernels.SurveySolutions.Documents;

namespace WB.UI.Headquarters.API.WebInterview
{
    public partial class WebInterview
    {
        public SectionData GetPrefilledQuestions()
        {
            var interview = this.GetCallerInterview();
            var questionirre = this.GetCallerQuestionnaire();
            var firstSection = questionirre.GetAllSections().First();

            var questions = questionirre
                .GetPrefilledQuestions()
                .Where(x => this.GetEntityType(x) != InterviewEntityType.Unsupported)
                .Select(x => new InterviewEntityWithType
                {
                    Identity = Identity.Create(x, RosterVector.Empty).ToString(),
                    EntityType = this.GetEntityType(x).ToString()
                })
                
                .ToArray();

            return new SectionData
            {
                Info = null,
                Breadcrumbs = null,
                Entities = questions,
                NavigationState = new ButtonState
                {
                    Title = "Start",
                    Status = CalculateSimpleStatus(Identity.Create(firstSection, RosterVector.Empty), interview),
                    NavigateToSection = firstSection.FormatGuid()
                }
            };
        }

        public SectionData GetSectionDetails(string sectionId)
        {
            if (sectionId == null) throw new ArgumentNullException(nameof(sectionId));

            if (sectionId == "prefilled")
            {
                return this.GetPrefilledQuestions();
            }

            Identity secitonIdentity = Identity.Parse(sectionId);
            var statefulInterview = this.GetCallerInterview();
            var ids = statefulInterview.GetUnderlyingInterviewerEntities(secitonIdentity);

            var entities = ids
                .Where(x => this.GetEntityType(x.Id) != InterviewEntityType.Unsupported)
                .Select(x => new InterviewEntityWithType
            {
                Identity = x.ToString(),
                EntityType = this.GetEntityType(x.Id).ToString()
            }).ToArray();

            return new SectionData
            {
                Info = new SectionInfo
                {
                    Id = sectionId,
                    Type = "Section",
                    Status = CalculateSimpleStatus(secitonIdentity, statefulInterview),
                    Title = statefulInterview.GetGroup(secitonIdentity).Title.Text
                },
                Breadcrumbs = this.GetBreadcrumbs(secitonIdentity, statefulInterview),
                Entities = entities,
                NavigationState = this.GetButtonsState(statefulInterview, secitonIdentity)
            };
        }

        private ButtonState GetButtonsState(IStatefulInterview statefulInterview, Identity sectionIdentity)
        {
            var parent = statefulInterview.GetParentGroup(sectionIdentity);
            if (parent != null)
            {
                var parentGroup = statefulInterview.GetGroup(parent);

                return new ButtonState
                {
                    Status = CalculateSimpleStatus(parent, statefulInterview),
                    Title = parentGroup.Title.Text,
                    NavigateToSection = parent.ToString(),
                    IsParentButton = true
                };
            }

            var sections = this.GetCallerQuestionnaire().GetAllSections().ToArray();

            var currentSectionIdx = Array.IndexOf(sections, sectionIdentity.Id);

            if (currentSectionIdx + 1 >= sections.Length)
            {
                return new ButtonState
                {
                    Title = "Complete interview",
                    Status = SimpleGroupStatus.Other,
                    NavigateToSection = sectionIdentity.ToString()
                };
            }
            else
            {
                var nextSectionId = Identity.Create(sections[currentSectionIdx + 1], RosterVector.Empty);

                return new ButtonState
                {
                    Title = statefulInterview.GetGroup(nextSectionId).Title.Text,
                    Status = CalculateSimpleStatus(nextSectionId, statefulInterview),
                    NavigateToSection = nextSectionId.ToString()
                };
            }
        }

        private Breadcrumb[] GetBreadcrumbs(Identity group, IStatefulInterview statefulInterview)
        {
            var callerQuestionnaire = this.GetCallerQuestionnaire();
            ReadOnlyCollection<Guid> parentIds = callerQuestionnaire.GetParentsStartingFromTop(group.Id);

            var breadCrumbs = new List<Breadcrumb>();
            int metRosters = 0;

            foreach (Guid parentId in parentIds)
            {
                if (callerQuestionnaire.IsRosterGroup(parentId))
                {
                    metRosters++;
                    var itemRosterVector = group.RosterVector.Shrink(metRosters);
                    var itemIdentity = new Identity(parentId, itemRosterVector);
                    var breadCrumb = new Breadcrumb { Title = statefulInterview.GetGroup(itemIdentity).Title.Text };

                    breadCrumbs.Add(breadCrumb);
                }
                else
                {
                    var itemIdentity = new Identity(parentId, group.RosterVector.Shrink(metRosters));
                    var breadCrumb = new Breadcrumb { Title = statefulInterview.GetGroup(itemIdentity).Title.Text };

                    breadCrumbs.Add(breadCrumb);
                }
            }

            return breadCrumbs.ToArray();
        }

        private static SimpleGroupStatus CalculateSimpleStatus(Identity group, IStatefulInterview interview)
        {
            if (interview.HasEnabledInvalidQuestionsAndStaticTexts(group))
                return SimpleGroupStatus.Invalid;

            if (interview.HasUnansweredQuestions(group))
                return SimpleGroupStatus.Other;

            bool isSomeSubgroupNotCompleted = interview
                .GetEnabledSubgroups(group)
                .Select(subgroup => CalculateSimpleStatus(subgroup, interview))
                .Any(status => status != SimpleGroupStatus.Completed);

            if (isSomeSubgroupNotCompleted)
                return SimpleGroupStatus.Other;

            return SimpleGroupStatus.Completed;
        }

        public InterviewEntity GetEntityDetails(string id)
        {
            var identity = Identity.Parse(id);
            var callerInterview = this.GetCallerInterview();

            InterviewTreeQuestion question = callerInterview.GetQuestion(identity);
            if (question != null)
            {
                GenericQuestion result = new StubEntity { Id = id };

                if (question.IsSingleFixedOption)
                {
                    result = this.autoMapper.Map<InterviewSingleOptionQuestion>(question);

                    var options = callerInterview.GetTopFilteredOptionsForQuestion(identity, null, null, 200);
                    ((InterviewSingleOptionQuestion)result).Options = options;
                }
                else if (question.IsText)
                {
                    InterviewTreeQuestion textQuestion = callerInterview.GetQuestion(identity);
                    result = this.autoMapper.Map<InterviewTextQuestion>(textQuestion);
                    var textQuestionMask = this.GetCallerQuestionnaire().GetTextQuestionMask(identity.Id);
                    if (!string.IsNullOrEmpty(textQuestionMask))
                    {
                        ((InterviewTextQuestion)result).Mask = textQuestionMask;
                    }
                }
                else if (question.IsInteger)
                {
                    InterviewTreeQuestion integerQuestion = callerInterview.GetQuestion(identity);
                    var interviewIntegerQuestion = this.autoMapper.Map<InterviewIntegerQuestion>(integerQuestion);
                    var callerQuestionnaire = this.GetCallerQuestionnaire();

                    interviewIntegerQuestion.UseFormatting = callerQuestionnaire.ShouldUseFormatting(identity.Id);
                    var isRosterSize = callerQuestionnaire.ShouldQuestionSpecifyRosterSize(identity.Id);
                    interviewIntegerQuestion.IsRosterSize = isRosterSize;

                    if (isRosterSize)
                    {
                        var isRosterSizeOfLongRoster = callerQuestionnaire.IsQuestionIsRosterSizeForLongRoster(identity.Id);
                        interviewIntegerQuestion.AnswerMaxValue = isRosterSizeOfLongRoster ? Constants.MaxLongRosterRowCount : Constants.MaxRosterRowCount;
                    }

                    result = interviewIntegerQuestion;
                }
                else if (question.IsDouble)
                {
                    InterviewTreeQuestion textQuestion = callerInterview.GetQuestion(identity);
                    var interviewDoubleQuestion = this.autoMapper.Map<InterviewDoubleQuestion>(textQuestion);
                    var callerQuestionnaire = this.GetCallerQuestionnaire();
                    interviewDoubleQuestion.CountOfDecimalPlaces = callerQuestionnaire.GetCountOfDecimalPlacesAllowedByQuestion(identity.Id);
                    interviewDoubleQuestion.UseFormatting = callerQuestionnaire.ShouldUseFormatting(identity.Id);
                    result = interviewDoubleQuestion;
                }
                else if (question.IsMultiFixedOption)
                {
                    result = this.autoMapper.Map<InterviewMutliOptionQuestion>(question);

                    var options = callerInterview.GetTopFilteredOptionsForQuestion(identity, null, null, 200);
                    var typedResult = (InterviewMutliOptionQuestion)result;
                    typedResult.Options = options;
                    typedResult.Ordered = this.GetCallerQuestionnaire().ShouldQuestionRecordAnswersOrder(identity.Id);
                    typedResult.MaxSelectedAnswersCount = this.GetCallerQuestionnaire().GetMaxSelectedAnswerOptions(identity.Id);
                }

                this.PutValidationMessages(result.Validity, callerInterview, identity);
                this.PutInstructions(result, identity);
                this.PutHideIfDisabled(result, identity);

                return result;
            }

            InterviewTreeStaticText staticText = callerInterview.GetStaticText(identity);
            if (staticText != null)
            {
                InterviewStaticText result = new InterviewStaticText() { Id = id };
                result = this.autoMapper.Map<InterviewStaticText>(staticText);

                var callerQuestionnaire = this.GetCallerQuestionnaire();
                var attachment = callerQuestionnaire.GetAttachmentForEntity(identity.Id);
                if (attachment != null)
                {
                    result.AttachmentContent = attachment.ContentId;
                }

                this.PutHideIfDisabled(result, identity);
                this.PutValidationMessages(result.Validity, callerInterview, identity);

                return result;
            }

            InterviewTreeGroup @group = callerInterview.GetGroup(identity);
            if (@group != null)
            {
                var result = new InterviewGroupOrRosterInstance { Id = id };
                result = this.autoMapper.Map<InterviewGroupOrRosterInstance>(@group);

                this.PutHideIfDisabled(result, identity);

                return result;
            }

            InterviewTreeRoster @roster = callerInterview.GetRoster(identity);
            if (@roster != null)
            {
                var result = new InterviewGroupOrRosterInstance { Id = id };
                result = this.autoMapper.Map<InterviewGroupOrRosterInstance>(@roster);

                this.PutHideIfDisabled(result, identity);

                return result;
            }

            return null;
        }

        private void PutValidationMessages(Validity validity, IStatefulInterview callerInterview, Identity identity)
        {
            validity.Messages = callerInterview.GetFailedValidationMessages(identity).ToArray();
        }

        private void PutHideIfDisabled(InterviewEntity result, Identity identity)
        {
            result.HideIfDisabled = this.GetCallerQuestionnaire().ShouldBeHiddenIfDisabled(identity.Id);
        }

        private void PutInstructions(GenericQuestion result, Identity id)
        {
            var callerQuestionnaire = this.GetCallerQuestionnaire();

            result.Instructions = callerQuestionnaire.GetQuestionInstruction(id.Id);
            result.HideInstructions = callerQuestionnaire.GetHideInstructions(id.Id);
        }

        private InterviewEntityType GetEntityType(Guid entityId)
        {
            var callerQuestionnaire = this.GetCallerQuestionnaire();

            if (callerQuestionnaire.IsVariable(entityId)) return InterviewEntityType.Unsupported;
            if (callerQuestionnaire.HasGroup(entityId) || callerQuestionnaire.IsRosterGroup(entityId))
                return InterviewEntityType.Group;
            if (callerQuestionnaire.IsStaticText(entityId)) return InterviewEntityType.StaticText;

            switch (callerQuestionnaire.GetQuestionType(entityId))
            {
                case QuestionType.DateTime:
                    return InterviewEntityType.DateTime;
                case QuestionType.GpsCoordinates:
                    return InterviewEntityType.Gps;
                case QuestionType.Multimedia:
                    return InterviewEntityType.Multimedia;
                case QuestionType.MultyOption:
                    return InterviewEntityType.CategoricalMulti;
                case QuestionType.SingleOption:
                    return InterviewEntityType.CategoricalSingle;
                case QuestionType.Numeric:
                    return callerQuestionnaire.IsQuestionInteger(entityId)
                        ? InterviewEntityType.Integer
                        : InterviewEntityType.Double;
                case QuestionType.Text:
                    return InterviewEntityType.TextQuestion;
                default:
                    return InterviewEntityType.Unsupported;
            }
        }
    }
}