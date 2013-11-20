﻿using System;
using System.Collections.Generic;
using Main.Core.Documents;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using Main.Core.Events.Questionnaire;
using Microsoft.Practices.ServiceLocation;
using Moq;
using Ncqrs.Eventing;
using Ncqrs.Eventing.ServiceModel.Bus;
using NUnit.Framework;
using WB.Core.BoundedContexts.Designer.Implementation.Factories;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Document;
using WB.Core.GenericSubdomains.Logging;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;

namespace WB.Core.BoundedContexts.Designer.Tests.QuestionnaireDenormalizerTests
{
    [TestFixture]
    // ReSharper disable RedundantArgumentName
    public class QuestionnaireDenormalizerTests
    {
        [SetUp]
        public void SetUp()
        {
            ServiceLocator.SetLocatorProvider(() => new Mock<IServiceLocator> { DefaultValue = DefaultValue.Mock }.Object);
        }

        [TestCase(Propagate.AutoPropagated, Propagate.AutoPropagated)]
        [TestCase(Propagate.None, Propagate.AutoPropagated)]
        [TestCase(Propagate.None, Propagate.None)]
        [Ignore("Will be removed after merge next branch on default")]
        public void HandleGroupUpdated_When_group_new_and_old_propagation_kind_do_not_imply_trigger_cleaning_Then_all_triggers_in_autoptopagate_questions_should_intact(Propagate oldPropagationKind, Propagate newPropagationKind)
        {
            // Arrange
            var questionnaireId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var updatedGroupId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var autoQuestionId = Guid.Parse("33333333-3333-3333-3333-333333333333");

            var document = CreateQuestionnaireDocument(questionnaireId);

            document
                .AddChapter(Guid.NewGuid())
                .AddGroup(updatedGroupId, propagationKind: oldPropagationKind);

            var autoPropagateQuestion = document
                .AddChapter(Guid.NewGuid())
                .AddQuestion(autoQuestionId, type: QuestionType.AutoPropagate, triggers: new List<Guid> { updatedGroupId }) as AutoPropagateQuestion;

            var storageStub = CreateQuestionnaireDenormalizerStorageStub(document);

            var denormalizer = CreateQuestionnaireDenormalizer(storageStub);

            var groupUpdatedEvent = CreateGroupUpdatedEvent(updatedGroupId, propagationKind: newPropagationKind);

            var evnt = CreatePublishedEvent(questionnaireId, groupUpdatedEvent);

            // Act
            denormalizer.Handle(evnt);

            // Assert
            Assert.That(autoPropagateQuestion.Triggers, Contains.Item(updatedGroupId));
        }

        [Test]
        public void HandleQuestionChanged_When_QuestionUpdate_event_is_come_Then_all_abstractQuestion_fields_are_updated()
        {
            // Arrange
            var questionnaireId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var questionId = Guid.Parse("22222222-2222-2222-2222-222222222222");

            var innerDocument = CreateQuestionnaireDocument(questionnaireId);

           innerDocument
                .AddChapter(Guid.NewGuid())
                .AddQuestion(questionId);

            var storageStub = CreateQuestionnaireDenormalizerStorageStub(innerDocument);

            var denormalizer = CreateQuestionnaireDenormalizer(storageStub);

            QuestionChanged evnt = CreateQuestionChangedEvent(questionId);

            // Act
            denormalizer.Handle(CreatePublishedEvent(questionnaireId, evnt));

            // Assert
            #warning: updated question is a new entity, that's why we should search for it by it's id
            var question = innerDocument.Find<IQuestion>(questionId);

            Assert.That(evnt.QuestionText, Is.EqualTo(question.QuestionText));
            Assert.That(evnt.QuestionType, Is.EqualTo(question.QuestionType));
            Assert.That(evnt.Featured, Is.EqualTo(question.Featured));
            Assert.That(evnt.AnswerOrder, Is.EqualTo(question.AnswerOrder));
            Assert.That(evnt.ConditionExpression, Is.EqualTo(question.ConditionExpression));
            Assert.That(evnt.Instructions, Is.EqualTo(question.Instructions));
            Assert.That(evnt.StataExportCaption, Is.EqualTo(question.StataExportCaption));
            Assert.That(evnt.ValidationExpression, Is.EqualTo(question.ValidationExpression));
            Assert.That(evnt.ValidationMessage, Is.EqualTo(question.ValidationMessage));
        }

        private static QuestionnaireDenormalizer CreateQuestionnaireDenormalizer(Mock<IReadSideRepositoryWriter<QuestionnaireDocument>> storageStub)
        {
            #warning: we shouldn't use CompleteQuestionFactory here?
            var denormalizer = new QuestionnaireDenormalizer(storageStub.Object, new QuestionFactory(), Mock.Of<ILogger>(), Mock.Of<IQuestionnaireDocumentUpgrader>());

            return denormalizer;
        }

        private static Mock<IReadSideRepositoryWriter<QuestionnaireDocument>> CreateQuestionnaireDenormalizerStorageStub(QuestionnaireDocument document)
        {
            var storageStub = new Mock<IReadSideRepositoryWriter<QuestionnaireDocument>>();

            storageStub.Setup(d => d.GetById(document.PublicKey)).Returns(document);

            return storageStub;
        }

        private static QuestionnaireDocument CreateQuestionnaireDocument(Guid questionnaireId)
        {
            var innerDocument = new QuestionnaireDocument
            {
                Title = string.Format("Questionnaire {0}", questionnaireId),
                PublicKey = questionnaireId
            };
            return innerDocument;
        }

        private static QuestionChanged CreateQuestionChangedEvent(Guid questionId, QuestionType type = QuestionType.Text, int maxValue = 0, List<Guid> triggers = null)
        {
            return new QuestionChanged
                {
                    QuestionText = "What is your name",
                    QuestionType = type,
                    PublicKey = questionId,
                    Featured = true,
                    AnswerOrder = Order.AsIs,
                    ConditionExpression = string.Empty,
                    Answers = null,
                    Instructions = "Answer this question, please",
                    StataExportCaption = "name",
                    ValidationExpression = "[this]!=''",
                    ValidationMessage = "Empty names is invalid answer",
                    Triggers = triggers,
                    MaxValue = maxValue
                };
        }

        private static GroupUpdated CreateGroupUpdatedEvent(Guid groupId, Propagate propagationKind = Propagate.None)
        {
            return new GroupUpdated
                {
                    GroupPublicKey = groupId
                };
        }

        private static IPublishedEvent<T> CreatePublishedEvent<T>(Guid questionnaireId, T evnt)
        {
            IPublishedEvent<T> e = new PublishedEvent<T>(new UncommittedEvent(Guid.NewGuid(),
                                                                              questionnaireId,
                                                                              1,
                                                                              1,
                                                                              DateTime.Now,
                                                                              evnt,
                                                                              new Version(1, 0))
                );
            return e;
        }
    }
    // ReSharper restore RedundantArgumentName
}