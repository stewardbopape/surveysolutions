﻿using System;
using Main.Core.Domain;
using Main.Core.Entities.SubEntities;
using Main.Core.Events.Questionnaire;
using Microsoft.Practices.ServiceLocation;
using Moq;
using Ncqrs.Spec;
using NUnit.Framework;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.BoundedContexts.Designer.Exceptions;

namespace WB.Core.BoundedContexts.Designer.Tests.QuestionnaireTests
{
    [TestFixture]
    public class UpdateGroupTests : QuestionnaireTestsContext
    {
        [SetUp]
        public void SetUp()
        {
            var serviceLocatorMock = new Mock<IServiceLocator> { DefaultValue = DefaultValue.Mock };
            ServiceLocator.SetLocatorProvider(() => serviceLocatorMock.Object);
        }

        [Test]
        [Ignore("TLK KP-2834")]
        public void NewUpdateGroup_When_new_propagation_kind_of_group_without_subgroups_is_AutoPropagate_Then_throws_DomainException_with_type_GroupCantBecomeAutoPropagateIfHasAnyChildGroup()
        {
            using (var eventContext = new EventContext())
            {
                // arrange
                var groupId = Guid.Parse("11111111-1111-1111-1111-111111111111");
                var newPropagationKind = Propagate.AutoPropagated;
                Guid responsibleId = Guid.NewGuid();
                Questionnaire questionnaire = CreateQuestionnaireWithOneGroupAndQuestionInIt(
                    questionId: Guid.NewGuid(), groupId: groupId, groupPropagationKind: Propagate.None,
                    responsibleId: responsibleId);

                // act
                questionnaire.UpdateGroup(groupId, responsibleId: responsibleId, title: "New title", rosterSizeQuestionId: null, description: null, condition: null);

                // assert
                Assert.That(GetSingleEvent<GroupUpdated>(eventContext).Propagateble, Is.EqualTo(newPropagationKind));
            }
        }

        [Test]
        [Ignore("TLK KP-2834")]
        public void NewUpdateGroup_When_new_propagation_kind_of_group_with_subgroups_is_AutoPropagate_Then_throws_DomainException_with_type_GroupCantBecomeAutoPropagateIfHasAnyChildGroup()
        {
            // arrange
            Guid groupId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithRegularGroupAndRegularGroupInIt(groupId: groupId, responsibleId: responsibleId);

            // act
            TestDelegate act = () => questionnaire.UpdateGroup(groupId, responsibleId: responsibleId, title: "New title", rosterSizeQuestionId: null, description: null, condition: null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.GroupCantBecomeAutoPropagateIfHasAnyChildGroup));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t")]
        public void NewUpdateGroup_When_groups_new_title_is_empty_or_whitespaces_Then_throws_DomainException(string emptyTitle)
        {
            // arrange
            var groupPublicKey = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(questionnaireId: Guid.NewGuid(), groupId: groupPublicKey, responsibleId: responsibleId);

            // act
            TestDelegate act = () => questionnaire.UpdateGroup(groupPublicKey, responsibleId: responsibleId, title: emptyTitle, rosterSizeQuestionId: null, description: null, condition: null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.GroupTitleRequired));
        }

        [Test]
        public void NewUpdateGroup_When_groups_new_title_is_not_empty_Then_raised_GroupUpdated_event_contains_the_same_group_title()
        {
            using (var eventContext = new EventContext())
            {
                // arrange
                var groupPublicKey = Guid.NewGuid();
                Guid responsibleId = Guid.NewGuid();
                Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(questionnaireId: Guid.NewGuid(), groupId: groupPublicKey, responsibleId: responsibleId);
                string notEmptyNewTitle = "Some new title";

                // act
                questionnaire.UpdateGroup(groupPublicKey, responsibleId: responsibleId, title: notEmptyNewTitle, rosterSizeQuestionId: null, description: null, condition: null);

                // assert
                Assert.That(GetSingleEvent<GroupUpdated>(eventContext).GroupText, Is.EqualTo(notEmptyNewTitle));
            }
        }

        [Test]
        public void NewUpdateGroup_When_group_does_not_exist_Then_throws_DomainException()
        {
            // arrange
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaire(responsibleId: responsibleId);
            Guid notExistingGroupPublicKey = Guid.NewGuid();

            // act
            TestDelegate act = () =>
                {
                    questionnaire.UpdateGroup(notExistingGroupPublicKey, responsibleId: responsibleId, title: null, rosterSizeQuestionId: null, description: null, condition: null);
                };

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.GroupNotFound));
        }

        [Test]
        public void NewUpdateGroup_When_group_exists_Then_raised_GroupUpdated_event_contains_group_public_key()
        {
            using (var eventContext = new EventContext())
            {
                // arrange
                var groupPublicKey = Guid.NewGuid();
                Guid responsibleId = Guid.NewGuid();
                Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(groupId: groupPublicKey, responsibleId: responsibleId);

                // act
                questionnaire.UpdateGroup(groupPublicKey, responsibleId: responsibleId, title: "group text", rosterSizeQuestionId: null, description: null, condition: null);

                // assert
                Assert.That(GetSingleEvent<GroupUpdated>(eventContext).GroupPublicKey, Is.EqualTo(groupPublicKey));
            }
        }

        [Test]
        public void NewUpdateGroup_When_group_exists_and_group_text_specified_Then_raised_GroupUpdated_event_with_same_group_text()
        {
            using (var eventContext = new EventContext())
            {
                // arrange
                var groupPublicKey = Guid.NewGuid();
                Guid responsibleId = Guid.NewGuid();
                Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(groupId: groupPublicKey, responsibleId: responsibleId);
                var groupText = "new group text";

                // act
                questionnaire.UpdateGroup(groupPublicKey, responsibleId: responsibleId, title: groupText, rosterSizeQuestionId: null, description: null, condition: null);

                // assert
                Assert.That(GetSingleEvent<GroupUpdated>(eventContext).GroupText, Is.EqualTo(groupText));
            }
        }

        [Test]
        public void NewUpdateGroup_When_group_exists_and_condition_expression_specified_Then_raised_GroupUpdated_event_with_same_condition_expression()
        {
            using (var eventContext = new EventContext())
            {
                // arrange
                var groupPublicKey = Guid.NewGuid();
                Guid responsibleId = Guid.NewGuid();
                Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(groupId: groupPublicKey, responsibleId: responsibleId);
                var conditionExpression = "2 < 7";

                // act
                questionnaire.UpdateGroup(groupPublicKey, responsibleId: responsibleId, title: "text of a group", rosterSizeQuestionId: null, description: null, condition: conditionExpression);

                // assert
                Assert.That(GetSingleEvent<GroupUpdated>(eventContext).ConditionExpression, Is.EqualTo(conditionExpression));
            }
        }

        [Test]
        public void NewUpdateGroup_When_group_exists_and_description_specified_Then_raised_GroupUpdated_event_with_same_description()
        {
            using (var eventContext = new EventContext())
            {
                // arrange
                var groupPublicKey = Guid.NewGuid();
                Guid responsibleId = Guid.NewGuid();
                Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(groupId: groupPublicKey, responsibleId: responsibleId);
                var description = "hardest questionnaire in the world";

                // act
                questionnaire.UpdateGroup(groupPublicKey, responsibleId: responsibleId, title: "Title", rosterSizeQuestionId: null, description: description, condition: null);

                // assert
                Assert.That(GetSingleEvent<GroupUpdated>(eventContext).Description, Is.EqualTo(description));
            }
        }

        [Test]
        public void NewUpdateGroup_When_User_Doesnot_Have_Permissions_For_Edit_Questionnaire_Then_DomainException_should_be_thrown()
        {
            // arrange
            var groupPublicKey = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(groupId: groupPublicKey, responsibleId: Guid.NewGuid());
            var description = "hardest questionnaire in the world";

            // act
            TestDelegate act = () => questionnaire.UpdateGroup(groupPublicKey, responsibleId: Guid.NewGuid(), title: "Title", rosterSizeQuestionId: null, description: description, condition: null);
            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.DoesNotHavePermissionsForEdit));
        }

        [Test]
        public void NewUpdateGroup_When_Group_Have_Condition_With_Reference_To_Existing_Question_Then_DomainException_should_NOT_be_thrown()
        {
            // arrange
            Guid groupId = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(responsibleId: responsibleId,
                groupId: groupId);
            string aliasForExistingQuestion = "q2";
            string expression = string.Format("[{0}] > 0", aliasForExistingQuestion);

            RegisterExpressionProcessorMock(expression, new[] { aliasForExistingQuestion });

            AddQuestion(questionnaire, Guid.NewGuid(), groupId, responsibleId, QuestionType.Text, aliasForExistingQuestion);

            // act
            TestDelegate act =
                () =>
                    questionnaire.UpdateGroup(
                        groupId: groupId,
                        responsibleId: responsibleId, title: "Title", rosterSizeQuestionId: null, description: null, condition: expression);

            // assert
            Assert.DoesNotThrow(act);
        }

        [Test]
        public void NewUpdateGroup_When_Group_Have_Condition_With_Reference_To_Not_Existing_Question_Then_DomainException_should_be_thrown()
        {
            // arrange
            Guid groupId = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(responsibleId: responsibleId,
                groupId: groupId);
            string aliasForNotExistingQuestion = "q2";
            string expression = string.Format("[{0}] > 0", aliasForNotExistingQuestion);

            RegisterExpressionProcessorMock(expression, new[] { aliasForNotExistingQuestion });

            AddQuestion(questionnaire, Guid.NewGuid(), groupId, responsibleId, QuestionType.Text, "q1");

            // act
            TestDelegate act =
                () =>
                    questionnaire.UpdateGroup(
                        groupId: groupId,
                        responsibleId: responsibleId, title: "Title", rosterSizeQuestionId: null, description: null, condition: expression);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.ExpressionContainsNotExistingQuestionReference));
        }
    }
}