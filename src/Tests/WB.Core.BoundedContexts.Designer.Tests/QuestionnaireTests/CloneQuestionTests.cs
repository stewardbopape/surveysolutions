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
    public class CloneQuestionTests : QuestionnaireTestsContext
    {
        [SetUp]
        public void SetUp()
        {
            var serviceLocatorMock = new Mock<IServiceLocator> { DefaultValue = DefaultValue.Mock };
            ServiceLocator.SetLocatorProvider(() => serviceLocatorMock.Object);
        }

        [Test]
        public void CloneQuestion_When_title_is_not_empty_Then_QuestionChanged_event_contains_the_same_title_caption()
        {
            using (var eventContext = new EventContext())
            {
                // arrange
                Guid newQuestionId = Guid.Parse("00000000-1111-0000-1111-000000000000");
                Guid sourceQuestionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
                Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
                Guid responsibleId = Guid.NewGuid();

                Questionnaire questionnaire = CreateQuestionnaireWithOneGroupAndQuestionInIt(questionId: sourceQuestionId, groupId: groupId, responsibleId: responsibleId);

                string notEmptyTitle = "not empty :)";

                // act
                questionnaire.CloneQuestion(newQuestionId, groupId, notEmptyTitle, QuestionType.Text, "test_clone", false, false,
                                                QuestionScope.Interviewer, string.Empty, string.Empty,
                                                string.Empty,
                                                string.Empty, new Option[0], Order.AZ, sourceQuestionId, 1, responsibleId, null, false, null);

                // assert
                var risedEvent = GetSingleEvent<QuestionCloned>(eventContext);
                Assert.That(risedEvent.QuestionText, Is.EqualTo(notEmptyTitle));
            }
        }

        [Test]
        public void CloneQuestion_When_title_is_empty_Then_DomainException_should_be_thrown()
        {
            // arrange
            Guid newQuestionId = Guid.Parse("00000000-1111-0000-1111-000000000000");
            Guid sourceQuestionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
            Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneGroupAndQuestionInIt(questionId: sourceQuestionId, groupId: groupId, responsibleId: responsibleId);

            // act
            var emptyTitle = string.Empty;
            TestDelegate act = () =>
                               questionnaire.CloneQuestion(newQuestionId, groupId, emptyTitle, QuestionType.Text, "test", false, false,
                                                               QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
                                                               string.Empty, new Option[0], Order.AZ, sourceQuestionId, 1, responsibleId, null, false, null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.QuestionTitleRequired));
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void CloneQuestion_When_answer_title_is_absent_Then_DomainException_should_be_thrown(QuestionType questionType)
        {
            // arrange
            Guid newQuestionId = Guid.Parse("00000000-1111-0000-1111-000000000000");
            Guid sourceQuestionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
            Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
            Guid responsibleId = Guid.NewGuid();

            Questionnaire questionnaire = CreateQuestionnaireWithOneQuestionInTypeAndOptions(sourceQuestionId, questionType, CreateTwoOptions(), responsibleId: responsibleId, groupId: groupId);
            var optionsWithEmptyTitles = new Option[2] { new Option(Guid.NewGuid(), "1", string.Empty), new Option(Guid.NewGuid(), "2", string.Empty) };
            // act
            TestDelegate act =
                () =>
                questionnaire.CloneQuestion(newQuestionId, groupId, "test", questionType, "test_clone", false, false, 
                                            QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
                                            string.Empty, optionsWithEmptyTitles, Order.AsIs, sourceQuestionId, 1, responsibleId, null, false, null);
            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.SelectorTextRequired));
        }

        [Test]
        [TestCase(QuestionType.Numeric)]
        [TestCase(QuestionType.AutoPropagate)]
        public void CloneQuestion_When_question_type_is_handled_by_type_specific_command_Then_DomainException_should_be_thrown(
            QuestionType questionType)
        {
            Guid newQuestionId = Guid.Parse("00000000-1111-0000-1111-000000000000");
            Guid sourceQuestionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
            Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
            Guid responsibleId = Guid.NewGuid();

            Questionnaire questionnaire = CreateQuestionnaireWithOneQuestionInTypeAndOptions(sourceQuestionId, questionType, CreateTwoOptions(), responsibleId: responsibleId, groupId: groupId);

            TestDelegate act = () =>
                questionnaire.CloneQuestion(
                    questionId: newQuestionId,
                    groupId: groupId,
                    title: "What is your last name?",
                    type: questionType,
                    alias: "name",
                    isMandatory: false,
                    isFeatured: false,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    instructions: string.Empty,
                    optionsOrder: Order.AsIs,
                    options: new Option[0],
                    responsibleId: responsibleId,
                    linkedToQuestionId: null, 
                    sourceQuestionId: sourceQuestionId, 
                    targetIndex: 1, 
                    areAnswersOrdered: false, 
                    maxAllowedAnswers:null);

            // Assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.QuestionTypeIsReroutedOnQuestionTypeSpecificCommand));
        }

        [Test]
        [TestCase(20)]
        [TestCase(0)]
        [TestCase(-1)]
        public void CloneNumericQuestion_When_countOfDecimalPlaces_is_incorrect_Then_DomainException_should_be_thrown(int countOfDecimalPlaces)
        {
            Guid newQuestionId = Guid.Parse("00000000-1111-0000-1111-000000000000");
            Guid sourceQuestionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
            Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
            Guid responsibleId = Guid.NewGuid();

            Questionnaire questionnaire = CreateQuestionnaireWithOneQuestionInTypeAndOptions(sourceQuestionId, QuestionType.Numeric, CreateTwoOptions(), responsibleId: responsibleId, groupId: groupId);


            TestDelegate act = () =>
                questionnaire.CloneNumericQuestion(
                    questionId: newQuestionId,
                    groupId: groupId,
                    title: "What is your last name?",
                    isAutopropagating:false, 
                    alias: "name",
                    isMandatory: false,
                    isFeatured: false,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    instructions: string.Empty,
                    responsibleId: responsibleId, sourceQuestionId: sourceQuestionId, targetIndex: 1, maxValue: null,
                    triggeredGroupIds: new Guid[0], isInteger: false, countOfDecimalPlaces: countOfDecimalPlaces);

            // Assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.CountOfDecimalPlacesValueIsIncorrect));
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void CloneQuestion_When_answer_title_is_not_empty_Then_event_contains_the_same_answer_title(QuestionType questionType)
        {
            using (var eventContext = new EventContext())
            {
                Guid newQuestionId = Guid.Parse("00000000-1111-0000-1111-000000000000");
                Guid sourceQuestionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
                Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
                Guid responsibleId = Guid.NewGuid();

                var notEmptyAnswerOptionTitle1 = "title";
                var notEmptyAnswerOptionTitle2 = "title1";
                Option[] newOptionsWithNotEmptyTitles = new Option[2] { new Option(Guid.NewGuid(), "1", notEmptyAnswerOptionTitle1), new Option(Guid.NewGuid(), "2", notEmptyAnswerOptionTitle2) };
                // arrange
                Questionnaire questionnaire = CreateQuestionnaireWithOneQuestionInTypeAndOptions(
                    sourceQuestionId, questionType, new[]
                        {
                            new Option(Guid.NewGuid(), "1", "option text"),
                            new Option(Guid.NewGuid(), "2", "option text1"),
                        }, responsibleId: responsibleId, groupId: groupId);


                // act
                questionnaire.CloneQuestion(newQuestionId, groupId,"test", questionType, "test", false, false, 
                                                QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
                                                string.Empty, newOptionsWithNotEmptyTitles, Order.AsIs, sourceQuestionId, 1, responsibleId, null, false, null);
                // assert
                var risedEvent = GetSingleEvent<QuestionCloned>(eventContext);
                Assert.AreEqual(notEmptyAnswerOptionTitle1, risedEvent.Answers[0].AnswerText);
                Assert.AreEqual(notEmptyAnswerOptionTitle2, risedEvent.Answers[1].AnswerText);
            }
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void CloneQuestion_When_User_Doesnot_Have_Permissions_For_Edit_Questionnaire_Then_DomainException_should_be_thrown(QuestionType questionType)
        {
            // arrange
            Guid newQuestionId = Guid.Parse("00000000-1111-0000-1111-000000000000");
            Guid sourceQuestionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
            Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");

            var notEmptyAnswerOptionTitle1 = "title";
            var notEmptyAnswerOptionTitle2 = "title1";
            Option[] newOptionsWithNotEmptyTitles = new Option[2] { new Option(Guid.NewGuid(), "1", notEmptyAnswerOptionTitle1), new Option(Guid.NewGuid(), "2", notEmptyAnswerOptionTitle2) };
            // arrange
            Questionnaire questionnaire = CreateQuestionnaireWithOneQuestionInTypeAndOptions(
                sourceQuestionId, questionType, new[]
                        {
                            new Option(Guid.NewGuid(), "1", "option text"),
                            new Option(Guid.NewGuid(), "2", "option text1"),
                        }, responsibleId: Guid.NewGuid(), groupId: groupId);

            // act
            TestDelegate act = () => questionnaire.CloneQuestion(newQuestionId, groupId, "test", questionType, "test", false, false, 
                                            QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
                                            string.Empty, newOptionsWithNotEmptyTitles, Order.AsIs, sourceQuestionId, 1, Guid.NewGuid(), null, false, null);
            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.DoesNotHavePermissionsForEdit));
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void CloneQuestion_When_categorical_question_with_linked_question_that_does_not_exist_in_questionnaire_questions_scope_Then_DomainException_should_be_thrown(QuestionType questionType)
        {
            // arrange
            Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
            Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
            Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
            Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire =
                CreateQuestionnaireWithAutoGroupAndRegularGroupAndQuestionsInThem(
                    autoGroupPublicKey: autoGroupId,
                    secondGroup: groupId,
                    autoQuestionId: autoQuestionId,
                    questionId: questionId,
                    responsibleId: responsibleId,
                    questionType: questionType);

            // act
            TestDelegate act =
                () =>
                questionnaire.CloneQuestion(Guid.NewGuid(), groupId, "test", questionType, "test", false, false, 
                                            QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
                                            string.Empty, null, Order.AsIs, questionId, 1, responsibleId, Guid.NewGuid(), false, null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.LinkedQuestionDoesNotExist));
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void CloneQuestion_When_categorical_question_with_linked_question_that_exist_in_autopropagated_group_questions_scope(QuestionType questionType)
        {
            using (var eventContext = new EventContext())
            {
                // arrange
                Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
                Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
                Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
                Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
                Guid responsibleId = Guid.NewGuid();

                Questionnaire questionnaire =
                    CreateQuestionnaireWithAutoGroupAndRegularGroupAndQuestionsInThem(
                        autoGroupPublicKey: autoGroupId,
                        secondGroup: groupId,
                        autoQuestionId: autoQuestionId,
                        questionId: questionId,
                        responsibleId: responsibleId,
                        questionType: questionType);

                // act
                questionnaire.CloneQuestion(Guid.NewGuid(), groupId, "test", questionType, "test", false, false, 
                                            QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
                                            string.Empty, null, Order.AsIs, questionId, 1, responsibleId, autoQuestionId, areAnswersOrdered: false, maxAllowedAnswers: null);

                // assert
                var risedEvent = GetSingleEvent<QuestionCloned>(eventContext);
                Assert.AreEqual(autoQuestionId, risedEvent.LinkedToQuestionId);
            }
        }

        [Test]
        [TestCase(QuestionType.DateTime)]
        [TestCase(QuestionType.GpsCoordinates)]
        [TestCase(QuestionType.Text)]
        public void CloneQuestion_When_non_categorical_question_with_linked_question_Then_DomainException_should_be_thrown(QuestionType questionType)
        {
            // arrange
            Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
            Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
            Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
            Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire =
                CreateQuestionnaireWithAutoGroupAndRegularGroupAndQuestionsInThem(
                    autoGroupPublicKey: autoGroupId,
                    secondGroup: groupId,
                    autoQuestionId: autoQuestionId,
                    questionId: questionId,
                    responsibleId: responsibleId,
                    questionType: QuestionType.MultyOption);

            // act
            TestDelegate act =
                () =>
                questionnaire.CloneQuestion(Guid.NewGuid(), groupId, "test", questionType, "test", false, false, 
                                            QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
                                            string.Empty, null, Order.AsIs, questionId, 1, responsibleId, autoQuestionId, false, null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.NotCategoricalQuestionLinkedToAnoterQuestion));
        }

        [Test]
        [TestCase(QuestionType.DateTime)]
        [TestCase(QuestionType.Numeric)]
        [TestCase(QuestionType.Text)]
        public void CloneQuestion_When_categorical_question_with_linked_question_with_number_or_text_or_datetime_type(QuestionType questionType)
        {
            using (var eventContext = new EventContext())
            {
                // arrange
                Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
                Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
                Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
                Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
                Guid responsibleId = Guid.NewGuid();

                Questionnaire questionnaire =
                    CreateQuestionnaireWithAutoGroupAndRegularGroupAndQuestionsInThem(
                        autoGroupPublicKey: autoGroupId,
                        secondGroup: groupId,
                        autoQuestionId: autoQuestionId,
                        questionId: questionId,
                        responsibleId: responsibleId,
                        questionType: QuestionType.MultyOption,
                        autoQuestionType: questionType);


                // act
                questionnaire.CloneQuestion(Guid.NewGuid(), groupId, "test", QuestionType.MultyOption, "test", false, false, 
                                            QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
                                            string.Empty, null, Order.AsIs, questionId, 1, responsibleId, autoQuestionId, false, null);

                // assert
                var risedEvent = GetSingleEvent<QuestionCloned>(eventContext);
                Assert.AreEqual(autoQuestionId, risedEvent.LinkedToQuestionId);
            }
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void CloneQuestion_When_categorical_question_with_linked_question_that_not_of_type_text_or_number_or_datetime_Then_DomainException_should_be_thrown(QuestionType questionType)
        {
            // arrange
            Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
            Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
            Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
            Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire =
                CreateQuestionnaireWithAutoGroupAndRegularGroupAndQuestionsInThem(
                    autoGroupPublicKey: autoGroupId,
                    secondGroup: groupId,
                    autoQuestionId: autoQuestionId,
                    questionId: questionId,
                    responsibleId: responsibleId,
                    questionType: questionType,
                    autoQuestionType: QuestionType.GpsCoordinates);

            // act
            TestDelegate act =
                () =>
                questionnaire.CloneQuestion(Guid.NewGuid(), groupId, "test", questionType, "test", false, false, 
                                            QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
                                            string.Empty, null, Order.AsIs, questionId, 1, responsibleId, autoQuestionId, false, null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.NotSupportedQuestionForLinkedQuestion));
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void CloneQuestion_When_categorical_question_have_answers_and_linked_question_in_the_same_time_Then_DomainException_should_be_thrown(QuestionType questionType)
        {
            // arrange
            Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
            Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
            Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
            Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire =
                CreateQuestionnaireWithAutoGroupAndRegularGroupAndQuestionsInThem(
                    autoGroupPublicKey: autoGroupId,
                    secondGroup: groupId,
                    autoQuestionId: autoQuestionId,
                    questionId: questionId,
                    responsibleId: responsibleId,
                    questionType: questionType);

            // act
            TestDelegate act =
                () =>
                questionnaire.CloneQuestion(Guid.NewGuid(), groupId, "test", questionType, "test", false, false,
                                            QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
                                            string.Empty, new Option[] {new Option(Guid.NewGuid(), "1", "auto"),},
                                            Order.AsIs, questionId, 1, responsibleId, autoQuestionId, false, null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.ConflictBetweenLinkedQuestionAndOptions));
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void CloneQuestion_When_categorical_question_with_linked_question_that_does_not_exist_in_questions_scope_from_autopropagate_groups_Then_DomainException_should_be_thrown(QuestionType questionType)
        {
            // arrange
            Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
            Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
            Guid questionThatLinkedButNotFromPropagateGroupId = Guid.Parse("00000000-1111-0000-2222-222000000000");
            Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
            Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire =
                CreateQuestionnaireWithAutoAndRegularGroupsAnd1QuestionInAutoGroupAnd2QuestionsInRegular(
                    autoGroupPublicKey: autoGroupId,
                    secondGroup: groupId,
                    autoQuestionId: autoQuestionId,
                    questionId: questionId,
                    responsibleId: responsibleId,
                    questionType: questionType,
                    questionThatLinkedButNotFromPropagateGroup: questionThatLinkedButNotFromPropagateGroupId);

            // act
            TestDelegate act =
                () =>
                questionnaire.CloneQuestion(Guid.NewGuid(), groupId, "test", questionType, "test", false, false, 
                                            QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
                                            string.Empty, null, Order.AsIs, questionId, 1,
                                            responsibleId, questionThatLinkedButNotFromPropagateGroupId, false, null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.LinkedQuestionIsNotInPropagateGroup));
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void CloneQuestion_When_categorical_question_that_has_featured_status_Then_DomainException_should_be_thrown(QuestionType questionType)
        {
            // arrange
            Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
            Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
            Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
            Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire =
                CreateQuestionnaireWithAutoGroupAndRegularGroupAndQuestionsInThem(
                    autoGroupPublicKey: autoGroupId,
                    secondGroup: groupId,
                    autoQuestionId: autoQuestionId,
                    questionId: questionId,
                    responsibleId: responsibleId,
                    questionType: questionType);

            // act
            TestDelegate act =
                () =>
                questionnaire.CloneQuestion(Guid.NewGuid(), groupId, "test", questionType, "test", false, true, 
                                            QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
                                            string.Empty, null, Order.AsIs, questionId, 1, responsibleId, autoQuestionId, false, null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.QuestionWithLinkedQuestionCanNotBeFeatured));
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void CloneQuestion_When_categorical_question_with_linked_question_that_has_head_status_Then_DomainException_should_be_thrown(QuestionType questionType)
        {
            // arrange
            Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
            Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
            Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
            Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire =
                CreateQuestionnaireWithAutoGroupAndRegularGroupAndQuestionsInThem(
                    autoGroupPublicKey: autoGroupId,
                    secondGroup: groupId,
                    autoQuestionId: autoQuestionId,
                    questionId: questionId,
                    responsibleId: responsibleId,
                    questionType: questionType);

            // act
            TestDelegate act =
                () =>
                questionnaire.CloneQuestion(Guid.NewGuid(), autoGroupId, "test", questionType, "test", false, false, true,
                                            QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
                                            string.Empty, null, Order.AsIs, questionId, 1, responsibleId, autoQuestionId, false, null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.QuestionWithLinkedQuestionCanNotBeHead));
        }
        [Test]
        [TestCase(QuestionType.MultyOption)]
        public void CloneQuestion_When_Categorical_Not_Linked_Multi_Question_That_Ordered_and_MaxAnswer_Are_Set_Then_event_contains_values(QuestionType questionType)
        {
            using (var eventContext = new EventContext())
            {
                var sourceQuestionId = Guid.NewGuid();
                var groupKey = Guid.NewGuid();
                var areAnswersOrdered = true;
                var maxAllowedAnswers = 1;
                Guid responsibleId = Guid.NewGuid();
                // arrange
                Questionnaire questionnaire = CreateQuestionnaireWithOneQuestionInTypeAndOptions(sourceQuestionId, questionType, CreateTwoOptions(), responsibleId: responsibleId, groupId: groupKey);

                // act
                questionnaire.CloneQuestion(Guid.NewGuid(), groupKey, "test", questionType, "test", false, false,
                                            QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
                                            string.Empty, CreateTwoOptions(), Order.AsIs, sourceQuestionId, 1, responsibleId, null, areAnswersOrdered, maxAllowedAnswers);
                // assert
                var risedEvent = GetSingleEvent<QuestionCloned>(eventContext);
                Assert.AreEqual(areAnswersOrdered, risedEvent.AreAnswersOrdered);
                Assert.AreEqual(maxAllowedAnswers, risedEvent.MaxAllowedAnswers);
            }
        }

        [Test]
        public void CloneQuestion_When_MaxAllowedAnswers_For_MultiQuestion_Is_Negative_Then_DomainException_of_type_MaxAllowedAnswersIsNotPositive_should_be_thrown()
        {
            var sourceQuestionId = Guid.NewGuid();
            var groupKey = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            // arrange
            Questionnaire questionnaire = CreateQuestionnaireWithOneQuestionInTypeAndOptions(sourceQuestionId, QuestionType.MultyOption, CreateTwoOptions(), responsibleId: responsibleId, groupId: groupKey);

            // act
            TestDelegate act = () => questionnaire.CloneQuestion(Guid.NewGuid(), groupKey, "test", QuestionType.MultyOption, "test", false, false,
                QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
                string.Empty, CreateTwoOptions(), Order.AsIs, sourceQuestionId, 1, responsibleId, null, false, -1);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.MaxAllowedAnswersIsNotPositive));
        }

        [Test]
        public void CloneQuestion_When_MaxAllowedAnswers_For_MultiQuestion_More_Than_Options_Then_DomainException_of_type_MaxAllowedAnswersMoreThanOptions_should_be_thrown()
        {
            var sourceQuestionId = Guid.NewGuid();
            var groupKey = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            // arrange
            Questionnaire questionnaire = CreateQuestionnaireWithOneQuestionInTypeAndOptions(sourceQuestionId, QuestionType.MultyOption, CreateTwoOptions(), responsibleId: responsibleId, groupId: groupKey);

            // act
            TestDelegate act = () => questionnaire.CloneQuestion(Guid.NewGuid(), groupKey, "test", QuestionType.MultyOption, "test", false, false,
                QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
                string.Empty, CreateTwoOptions(), Order.AsIs, sourceQuestionId, 1, responsibleId, null, false, 3);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.MaxAllowedAnswersMoreThanOptions));
        }

        [Test]
        public void CloneQuestion_When_categorical_multi_question_with_linked_question_that_has_max_allowed_answers_Then_DomainException_should_NOT_be_thrown()
        {
            // arrange
            const int maxAllowedAnswers = 5;
            Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
            Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
            Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
            Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire =
                CreateQuestionnaireWithAutoGroupAndRegularGroupAndQuestionsInThem(
                    autoGroupPublicKey: autoGroupId,
                    secondGroup: groupId,
                    autoQuestionId: autoQuestionId,
                    questionId: questionId,
                    responsibleId: responsibleId,
                    questionType: QuestionType.MultyOption);

            // act
            TestDelegate act = () => questionnaire.CloneQuestion(
                questionId: Guid.NewGuid(),
                groupId: autoGroupId,
                title: "Question",
                type: QuestionType.MultyOption,
                alias: "test",
                isMandatory: false,
                isFeatured: false,
                scope: QuestionScope.Interviewer,
                condition: string.Empty,
                validationExpression: string.Empty,
                validationMessage: string.Empty,
                instructions: string.Empty,
                options: null,
                optionsOrder: Order.AZ,
                responsibleId: responsibleId,
                linkedToQuestionId: autoQuestionId,
                areAnswersOrdered: false,
                maxAllowedAnswers: maxAllowedAnswers,
                sourceQuestionId: questionId,
                targetIndex: 1);

            // assert
            Assert.DoesNotThrow(act);
        }

        [Test]
        public void CloneQuestion_When_categorical_multi_question_with_linked_question_that_has_max_allowed_answers_Then_QuestionCloned_event_with_max_allowed_answers_value_should_be_raised()
        {
            using (var eventContext = new EventContext())
            {
                // arrange
                const int maxAllowedAnswers = 5;
                Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
                Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
                Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
                Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
                Guid responsibleId = Guid.NewGuid();
                Questionnaire questionnaire =
                    CreateQuestionnaireWithAutoGroupAndRegularGroupAndQuestionsInThem(
                        autoGroupPublicKey: autoGroupId,
                        secondGroup: groupId,
                        autoQuestionId: autoQuestionId,
                        questionId: questionId,
                        responsibleId: responsibleId,
                        questionType: QuestionType.MultyOption);

                // act
                questionnaire.CloneQuestion(
                    questionId: Guid.NewGuid(),
                    groupId: autoGroupId,
                    title: "Question",
                    type: QuestionType.MultyOption,
                    alias: "test",
                    isMandatory: false,
                    isFeatured: false,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    instructions: string.Empty,
                    options: null,
                    optionsOrder: Order.AZ,
                    responsibleId: responsibleId,
                    linkedToQuestionId: autoQuestionId,
                    areAnswersOrdered: false,
                    maxAllowedAnswers: maxAllowedAnswers,
                    sourceQuestionId: questionId,
                    targetIndex: 1);

                // assert
                var risedEvent = GetSingleEvent<QuestionCloned>(eventContext);
                Assert.AreEqual(maxAllowedAnswers, risedEvent.MaxAllowedAnswers);
            }
        }

        [Test]
        public void CloneQuestion_When_categorical_multi_question_with_linked_question_ordered_Then_QuestionCloned_event_with_ordered_value_should_be_raised()
        {
            using (var eventContext = new EventContext())
            {
                // arrange
                const bool areAnswersOrdered = true;
                Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
                Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
                Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
                Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
                Guid responsibleId = Guid.NewGuid();
                Questionnaire questionnaire =
                    CreateQuestionnaireWithAutoGroupAndRegularGroupAndQuestionsInThem(
                        autoGroupPublicKey: autoGroupId,
                        secondGroup: groupId,
                        autoQuestionId: autoQuestionId,
                        questionId: questionId,
                        responsibleId: responsibleId,
                        questionType: QuestionType.MultyOption);

                // act
                questionnaire.CloneQuestion(
                    questionId: Guid.NewGuid(),
                    groupId: autoGroupId,
                    title: "Question",
                    type: QuestionType.MultyOption,
                    alias: "test",
                    isMandatory: false,
                    isFeatured: false,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    instructions: string.Empty,
                    options: null,
                    optionsOrder: Order.AZ,
                    responsibleId: responsibleId,
                    linkedToQuestionId: autoQuestionId,
                    areAnswersOrdered: areAnswersOrdered,
                    maxAllowedAnswers: null,
                    sourceQuestionId: questionId,
                    targetIndex: 1);

                // assert
                var risedEvent = GetSingleEvent<QuestionCloned>(eventContext);
                Assert.AreEqual(areAnswersOrdered, risedEvent.AreAnswersOrdered);
            }
        }

        [Test]
        public void CloneQuestion_When_Question_Have_Condition_With_Variable_Name_Reference_To_Existing_Question_Then_DomainException_should_NOT_be_thrown()
        {
            // arrange
            Guid question1Id = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(responsibleId: responsibleId,
                groupId: groupId);
            string aliasForExistingQuestion = "q2";
            string expression = string.Format("[{0}] > 0", aliasForExistingQuestion);

            RegisterExpressionProcessorMock(expression, new[] { aliasForExistingQuestion });

            AddQuestion(questionnaire, question1Id, groupId, responsibleId, QuestionType.Text, "q1");
            AddQuestion(questionnaire, Guid.NewGuid(), groupId, responsibleId, QuestionType.Text, aliasForExistingQuestion);

            // act
            TestDelegate act = () => questionnaire.CloneQuestion(
                    questionId: Guid.NewGuid(),
                    groupId: groupId,
                    title: "Question",
                    type: QuestionType.Text,
                    alias: "test",
                    isMandatory: false,
                    isFeatured: false,
                    scope: QuestionScope.Interviewer,
                    condition: expression,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    instructions: string.Empty,
                    options: null,
                    optionsOrder: Order.AZ,
                    responsibleId: responsibleId,
                    linkedToQuestionId: null,
                    areAnswersOrdered: false,
                    maxAllowedAnswers: null,
                    sourceQuestionId: question1Id,
                    targetIndex: 1);

            // assert
            Assert.DoesNotThrow(act);
        }

        [Test]
        public void CloneQuestion_When_Question_Have_Condition_With_Question_Id_Reference_To_Existing_Question_Then_DomainException_should_NOT_be_thrown()
        {
            // arrange
            Guid question1Id = Guid.NewGuid();
            Guid question2Id = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(responsibleId: responsibleId,
                groupId: groupId);
            string expression = string.Format("[{0}] > 0", question2Id);

            RegisterExpressionProcessorMock(expression, new[] { question2Id.ToString() });

            AddQuestion(questionnaire, question1Id, groupId, responsibleId, QuestionType.Text, "q1");
            AddQuestion(questionnaire, question2Id, groupId, responsibleId, QuestionType.Text, "q2");

            // act
            TestDelegate act = () => questionnaire.CloneQuestion(
                    questionId: Guid.NewGuid(),
                    groupId: groupId,
                    title: "Question",
                    type: QuestionType.Text,
                    alias: "test",
                    isMandatory: false,
                    isFeatured: false,
                    scope: QuestionScope.Interviewer,
                    condition: expression,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    instructions: string.Empty,
                    options: null,
                    optionsOrder: Order.AZ,
                    responsibleId: responsibleId,
                    linkedToQuestionId: null,
                    areAnswersOrdered: false,
                    maxAllowedAnswers: null,
                    sourceQuestionId: question1Id,
                    targetIndex: 1);

            // assert
            Assert.DoesNotThrow(act);
        }

        [Test]
        public void CloneQuestion_When_Question_Have_Validation_With_Variable_Name_Reference_To_Existing_Question_Then_DomainException_should_NOT_be_thrown()
        {
            // arrange
            Guid question1Id = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(responsibleId: responsibleId,
                groupId: groupId);
            string aliasForExistingQuestion = "q2";
            string expression = string.Format("[{0}] > 0", aliasForExistingQuestion);

            RegisterExpressionProcessorMock(expression, new[] { aliasForExistingQuestion });

            AddQuestion(questionnaire, question1Id, groupId, responsibleId, QuestionType.Text, "q1");
            AddQuestion(questionnaire, Guid.NewGuid(), groupId, responsibleId, QuestionType.Text, aliasForExistingQuestion);

            // act
            TestDelegate act = () => questionnaire.CloneQuestion(
                    questionId: Guid.NewGuid(),
                    groupId: groupId,
                    title: "Question",
                    type: QuestionType.Text,
                    alias: "test",
                    isMandatory: false,
                    isFeatured: false,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: expression,
                    validationMessage: string.Empty,
                    instructions: string.Empty,
                    options: null,
                    optionsOrder: Order.AZ,
                    responsibleId: responsibleId,
                    linkedToQuestionId: null,
                    areAnswersOrdered: false,
                    maxAllowedAnswers: null,
                    sourceQuestionId: question1Id,
                    targetIndex: 1);

            // assert
            Assert.DoesNotThrow(act);
        }

        [Test]
        public void CloneQuestion_When_Question_Have_Validation_With_Question_Id_Reference_To_Existing_Question_Then_DomainException_should_NOT_be_thrown()
        {
            // arrange
            Guid question1Id = Guid.NewGuid();
            Guid question2Id = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(responsibleId: responsibleId,
                groupId: groupId);
            string expression = string.Format("[{0}] > 0", question2Id);

            RegisterExpressionProcessorMock(expression, new[] { question2Id.ToString() });

            AddQuestion(questionnaire, question1Id, groupId, responsibleId, QuestionType.Text, "q1");
            AddQuestion(questionnaire, question2Id, groupId, responsibleId, QuestionType.Text, "q2");

            // act
            TestDelegate act = () => questionnaire.CloneQuestion(
                    questionId: Guid.NewGuid(),
                    groupId: groupId,
                    title: "Question",
                    type: QuestionType.Text,
                    alias: "test",
                    isMandatory: false,
                    isFeatured: false,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: expression,
                    validationMessage: string.Empty,
                    instructions: string.Empty,
                    options: null,
                    optionsOrder: Order.AZ,
                    responsibleId: responsibleId,
                    linkedToQuestionId: null,
                    areAnswersOrdered: false,
                    maxAllowedAnswers: null,
                    sourceQuestionId: question1Id,
                    targetIndex: 1);

            // assert
            Assert.DoesNotThrow(act);
        }

        [Test]
        public void CloneQuestion_When_Question_Have_Condition_With_Reference_To_Not_Existing_Question_Then_DomainException_should_be_thrown()
        {
            // arrange
            Guid question1Id = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(responsibleId: responsibleId,
                groupId: groupId);
            string aliasForNotExistingQuestion = "q3";
            string expression = string.Format("[{0}] > 0", aliasForNotExistingQuestion);

            RegisterExpressionProcessorMock(expression, new[] { aliasForNotExistingQuestion });

            AddQuestion(questionnaire, question1Id, groupId, responsibleId, QuestionType.Text, "q1");
            AddQuestion(questionnaire, Guid.NewGuid(), groupId, responsibleId, QuestionType.Text, "q2");

            // act
            TestDelegate act = () => questionnaire.CloneQuestion(
                    questionId: Guid.NewGuid(),
                    groupId: groupId,
                    title: "Question",
                    type: QuestionType.Text,
                    alias: "test",
                    isMandatory: false,
                    isFeatured: false,
                    scope: QuestionScope.Interviewer,
                    condition: expression,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    instructions: string.Empty,
                    options: null,
                    optionsOrder: Order.AZ,
                    responsibleId: responsibleId,
                    linkedToQuestionId: null,
                    areAnswersOrdered: false,
                    maxAllowedAnswers: null,
                    sourceQuestionId: question1Id,
                    targetIndex: 1);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.ExpressionContainsNotExistingQuestionReference));
        }

        [Test]
        public void CloneQuestion_When_Question_Have_Condition_With_2_References_And_Second_To_Not_Existing_Question_Then_DomainException_should_be_thrown()
        {
            // arrange
            Guid question1Id = Guid.NewGuid();
            Guid question12d = Guid.NewGuid();
            Guid idForNotExistingQuestion = Guid.NewGuid();
            string idForNotExistingQuestionAsString = idForNotExistingQuestion.ToString();
            Guid groupId = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(responsibleId: responsibleId,
                groupId: groupId);
            string expression = string.Format("[{0}] > 0 AND [{1}] > 1", question1Id, question12d);

            RegisterExpressionProcessorMock(expression, new[] { question1Id.ToString(), idForNotExistingQuestionAsString });

            AddQuestion(questionnaire, question1Id, groupId, responsibleId, QuestionType.Text, "q1");
            AddQuestion(questionnaire, question12d, groupId, responsibleId, QuestionType.Text, "q2");

            // act
            TestDelegate act = () => questionnaire.CloneQuestion(
                    questionId: Guid.NewGuid(),
                    groupId: groupId,
                    title: "Question",
                    type: QuestionType.Text,
                    alias: "test",
                    isMandatory: false,
                    isFeatured: false,
                    scope: QuestionScope.Interviewer,
                    condition: expression,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    instructions: string.Empty,
                    options: null,
                    optionsOrder: Order.AZ,
                    responsibleId: responsibleId,
                    linkedToQuestionId: null,
                    areAnswersOrdered: false,
                    maxAllowedAnswers: null,
                    sourceQuestionId: question1Id,
                    targetIndex: 1);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.ExpressionContainsNotExistingQuestionReference));
            Assert.That(domainException.Message, Is.StringContaining(idForNotExistingQuestionAsString));
        }

        [Test]
        public void CloneQuestion_When_Question_Have_Validation_With_Reference_To_Not_Existing_Question_Then_DomainException_should_be_thrown()
        {
            // arrange
            Guid question1Id = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(responsibleId: responsibleId,
                groupId: groupId);
            string aliasForNotExistingQuestion = "q3";
            string expression = string.Format("[{0}] > 0", aliasForNotExistingQuestion);

            RegisterExpressionProcessorMock(expression, new[] { aliasForNotExistingQuestion });

            AddQuestion(questionnaire, question1Id, groupId, responsibleId, QuestionType.Text, "q1");
            AddQuestion(questionnaire, Guid.NewGuid(), groupId, responsibleId, QuestionType.Text, "q2");

            // act
            TestDelegate act = () => questionnaire.CloneQuestion(
                    questionId: Guid.NewGuid(),
                    groupId: groupId,
                    title: "Question",
                    type: QuestionType.Text,
                    alias: "test",
                    isMandatory: false,
                    isFeatured: false,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: expression,
                    validationMessage: string.Empty,
                    instructions: string.Empty,
                    options: null,
                    optionsOrder: Order.AZ,
                    responsibleId: responsibleId,
                    linkedToQuestionId: null,
                    areAnswersOrdered: false,
                    maxAllowedAnswers: null,
                    sourceQuestionId: question1Id,
                    targetIndex: 1);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.ExpressionContainsNotExistingQuestionReference));
        }

        [Test]
        public void CloneQuestion_When_Question_Have_Validation_With_2_References_And_Second_To_Not_Existing_Question_Then_DomainException_should_be_thrown()
        {
            // arrange
            Guid question1Id = Guid.NewGuid();
            Guid question2d = Guid.NewGuid();
            Guid idForNotExistingQuestion = Guid.NewGuid();
            string idForNotExistingQuestionAsString = idForNotExistingQuestion.ToString();
            Guid groupId = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(responsibleId: responsibleId,
                groupId: groupId);
            string expression = string.Format("[{0}] > 0 AND [{1}] > 1", question1Id, question2d);

            RegisterExpressionProcessorMock(expression, new[] { question1Id.ToString(), idForNotExistingQuestion.ToString() });

            AddQuestion(questionnaire, question1Id, groupId, responsibleId, QuestionType.Text, "q1");
            AddQuestion(questionnaire, Guid.NewGuid(), groupId, responsibleId, QuestionType.Text, "q2");

            // act
            TestDelegate act = () => questionnaire.CloneQuestion(
                    questionId: Guid.NewGuid(),
                    groupId: groupId,
                    title: "Question",
                    type: QuestionType.Text,
                    alias: "test",
                    isMandatory: false,
                    isFeatured: false,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: expression,
                    validationMessage: string.Empty,
                    instructions: string.Empty,
                    options: null,
                    optionsOrder: Order.AZ,
                    responsibleId: responsibleId,
                    linkedToQuestionId: null,
                    areAnswersOrdered: false,
                    maxAllowedAnswers: null,
                    sourceQuestionId: question1Id,
                    targetIndex: 1);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.ExpressionContainsNotExistingQuestionReference));
            Assert.That(domainException.Message, Is.StringContaining(idForNotExistingQuestionAsString));
        }

#warning following tests are commented because they were copy pasted from add command tests but Slava had not enough time to uncomment them and make them test for Clone feature (when he wrote such tests in his spare time)


//        [Test]
//        [TestCase(QuestionType.SingleOption)]
//        [TestCase(QuestionType.MultyOption)]
//        public void NewUpdateQuestion_When_AnswerTitle_is_not_unique_Then_DomainException_should_be_thrown(QuestionType questionType)
//        {
//            Guid questionKey = Guid.NewGuid();
//            // arrange
//            Questionnaire questionnaire = CreateQuestionnaireWithOneQuestionnInTypeAndOptions(questionKey, questionType, options: new[] { new Option(Guid.NewGuid(), "12", "title"), new Option(Guid.NewGuid(), "125", "title1") });
//            Option[] options = new Option[] { new Option(Guid.NewGuid(), "1", "title"), new Option(Guid.NewGuid(), "2", "title") };
//            // act
//            TestDelegate act =
//                () =>
//                questionnaire.CloneQuestion(questionKey, "test", questionType, "test", false, false, false,
//                                                QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
//                                                string.Empty, options, Order.AsIs, null, new Guid[0]);
//            // assert
//            var domainException = Assert.Throws<DomainException>(act);
//            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.SelectorTextNotUnique));
//        }

//        [Test]
//        [TestCase(QuestionType.SingleOption)]
//        [TestCase(QuestionType.MultyOption)]
//        public void NewUpdateQuestion_When_AnswerTitle_is_unique_Then_event_contains_the_same_answer_titles(QuestionType questionType)
//        {
//            using (var eventContext = new EventContext())
//            {
//                // arrange
//                var firstAnswerOptionTitle = "title1";
//                var secondAnswerOptionTitleThatNotEqualsFirstOne = firstAnswerOptionTitle + "1";

//                Guid questionKey = Guid.NewGuid();
//                Questionnaire questionnaire = CreateQuestionnaireWithOneQuestionnInTypeAndOptions(questionKey, questionType, options: new[] { new Option(Guid.NewGuid(), "121", "title"), new Option(Guid.NewGuid(), "12", "title1") });
//                Option[] options = new Option[] { new Option(Guid.NewGuid(), "1", firstAnswerOptionTitle), new Option(Guid.NewGuid(), "2", secondAnswerOptionTitleThatNotEqualsFirstOne) };
                
//                // act
//                questionnaire.CloneQuestion(questionKey, "test", questionType, "test", false, false, false,
//                                                QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
//                                                string.Empty, options, Order.AsIs, null, new Guid[0]);
//                // assert
//                var risedEvent = GetSingleEvent<QuestionCloned>(eventContext);

//                Assert.That(risedEvent.Answers[0].AnswerText, Is.EqualTo(firstAnswerOptionTitle));
//                Assert.That(risedEvent.Answers[1].AnswerText, Is.EqualTo(secondAnswerOptionTitleThatNotEqualsFirstOne));
                
//            }
//        }

//        [Test]
//        public void NewUpdateQuestion_When_qustion_in_propagated_group_is_featured_Then_DomainException_should_be_thrown()
//        {
//            // Arrange
//            Guid updatedQuestion = Guid.NewGuid();
//            bool isFeatured = true;
//            Questionnaire questionnaire = CreateQuestionnaireWithOneAutoGroupAndQuestionInIt(updatedQuestion);

//            // Act
//            TestDelegate act = () => questionnaire.CloneQuestion(updatedQuestion, "What is your last name?", QuestionType.Text, "name", false,
//                                                                     isFeatured,
//                                                                     false, QuestionScope.Interviewer, "", "", "", "", new Option[0], Order.AsIs, 0, new Guid[0]);

//            // assert
//            var domainException = Assert.Throws<DomainException>(act);
//            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.QuestionIsFeaturedButNotInsideNonPropagateGroup));
//        }

//        [Test]
//        public void NewUpdateQuestion_When_question_inside_non_propagated_group_is_featured_Then_raised_QuestionChanged_event_contains_the_same_featured_field()
//        {
//            using (var eventContext = new EventContext())
//            {
//                // Arrange
//                Guid updatedQuestion = Guid.NewGuid();
//                bool isFeatured = true;
//                Questionnaire questionnaire = CreateQuestionnaireWithOneGroupAndQuestionInIt(updatedQuestion);

//                // Act
//                questionnaire.CloneQuestion(updatedQuestion, "What is your last name?", QuestionType.Text, "name", false,
//                                                isFeatured,
//                                                false, QuestionScope.Interviewer, "", "", "", "", new Option[0], Order.AsIs, 0, new Guid[0]);

//                // Assert
//                Assert.That(GetSingleEvent<QuestionCloned>(eventContext).Featured, Is.EqualTo(isFeatured));
//            }
//        }

//        [Test]
//        public void NewUpdateQuestion_When_question_is_head_of_propagated_group_but_inside_non_propagated_group_Then_DomainException_should_be_thrown()
//        {
//            // Arrange
//            Guid updatedQuestion = Guid.NewGuid();
//            bool isHeadOfPropagatedGroup = true;
//            Questionnaire questionnaire = CreateQuestionnaireWithOneGroupAndQuestionInIt(updatedQuestion);

//            // Act
//            TestDelegate act = () => questionnaire.CloneQuestion(updatedQuestion, "What is your last name?", QuestionType.Text, "name", false, false,
//                                                                     isHeadOfPropagatedGroup,
//                                                                     QuestionScope.Interviewer, "", "", "", "", new Option[0], Order.AsIs, 0, new Guid[0]);

//            // assert
//            var domainException = Assert.Throws<DomainException>(act);
//            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.QuestionIsHeadOfGroupButNotInsidePropagateGroup));
//        }

//        [Test]
//        public void NewUpdateQuestion_When_question_is_head_of_propagated_group_and_inside_propagated_group_Then_raised_QuestionChanged_event_contains_the_same_header_field()
//        {
//            using (var eventContext = new EventContext())
//            {
//                // Arrange
//                Guid updatedQuestion = Guid.NewGuid();
//                bool isHeadOfPropagatedGroup = true;
//                Questionnaire questionnaire = CreateQuestionnaireWithOneAutoGroupAndQuestionInIt(updatedQuestion);

//                // Act
//                questionnaire.CloneQuestion(updatedQuestion, "What is your last name?", QuestionType.Text, "name", false, false,
//                                                isHeadOfPropagatedGroup,
//                                                QuestionScope.Interviewer, "", "", "", "", new Option[0], Order.AsIs, 0, new Guid[0]);

//                // Assert
//                Assert.That(GetSingleEvent<QuestionCloned>(eventContext).Capital, Is.EqualTo(isHeadOfPropagatedGroup));
//            }
//        }

//        [Test]
//        [TestCase(QuestionType.SingleOption)]
//        [TestCase(QuestionType.MultyOption)]
//        public void NewUpdateQuestion_When_QuestionType_is_option_type_and_answer_options_list_is_empty_Then_DomainException_should_be_thrown(QuestionType questionType)
//        {
//            // Arrange
//            var emptyAnswersList = new Option[] { };

//            Guid targetQuestionPublicKey = Guid.NewGuid();
//            var questionnaire = CreateQuestionnaireWithOneQuestion(targetQuestionPublicKey);

//            // Act
//            TestDelegate act = () =>
//                               questionnaire.CloneQuestion(targetQuestionPublicKey, "Title", questionType, "name",
//                                                               false, false, false, QuestionScope.Interviewer, "", "", "",
//                                                               "", emptyAnswersList, Order.AZ, 0, new List<Guid>().ToArray());

//            // assert
//            var domainException = Assert.Throws<DomainException>(act);
//            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.SelectorEmpty));
//        }

//        [Test]
//        public void NewUpdateQuestion_When_capital_parameter_is_true_Then_in_QuestionChanged_event_capital_property_should_be_set_in_true_too()
//        {
//            using (var eventContext = new EventContext())
//            {
//                // Arrange
//                Guid targetQuestionPublicKey = Guid.NewGuid();
//                var questionnaire = CreateQuestionnaireWithOneAutoGroupAndQuestionInIt(targetQuestionPublicKey);

//                bool capital = true;

//                // Act
//                questionnaire.CloneQuestion(targetQuestionPublicKey, "Title", QuestionType.Text, "title",
//                                                false, false, capital, QuestionScope.Interviewer, "", "", "",
//                                                "", new Option[] { }, Order.AZ, 0, new List<Guid>().ToArray());

//                // Assert
//                var risedEvent = GetSingleEvent<QuestionCloned>(eventContext);
//                Assert.AreEqual(capital, risedEvent.Capital);
//            }
//        }

//        [TestCase("ma_name38")]
//        [TestCase("__")]
//        [TestCase("_123456789012345678901234567890_")]
//        public void NewUpdateQuestion_When_variable_name_is_valid_Then_rised_QuestionChanged_event_contains_the_same_stata_caption(string validVariableName)
//        {
//            using (var eventContext = new EventContext())
//            {
//                // Arrange
//                Guid targetQuestionPublicKey = Guid.NewGuid();
//                var questionnaire = CreateQuestionnaireWithOneQuestion(targetQuestionPublicKey);

//                // Act
//                questionnaire.CloneQuestion(targetQuestionPublicKey, "Title", QuestionType.Text,
//                                                validVariableName,
//                                                false, false, false, QuestionScope.Interviewer, "", "", "", "",
//                                                new Option[0], Order.AZ, 0, new Guid[0]);

//                // Assert
//                Assert.That(GetSingleEvent<QuestionCloned>(eventContext).Alias, Is.EqualTo(validVariableName));
//            }
//        }

//        [Test]
//        public void NewUpdateQuestion_When_we_updating_absent_question_Then_DomainException_should_be_thrown()
//        {
//            // Arrange
//            Questionnaire questionnaire = CreateQuestionnaire();

//            // Act
//            TestDelegate act = () =>
//                               questionnaire.CloneQuestion(Guid.NewGuid(), "Title", QuestionType.Text, "valid",
//                                                               false, false, false, QuestionScope.Interviewer, "", "", "",
//                                                               "", new Option[] { }, Order.AZ, 0, new List<Guid>().ToArray());

//            // assert
//            var domainException = Assert.Throws<DomainException>(act);
//            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.QuestionNotFound));
//        }

//        [Test]
//        public void NewUpdateQuestion_When_variable_name_has_33_chars_Then_DomainException_should_be_thrown()
//        {
//            // Arrange
//            Guid targetQuestionPublicKey = Guid.NewGuid();
//            var questionnaire = CreateQuestionnaireWithOneQuestion(targetQuestionPublicKey);
//            string longVariableName = "".PadRight(33, 'A');

//            // Act
//            TestDelegate act = () => questionnaire.CloneQuestion(targetQuestionPublicKey, "Title", QuestionType.Text,
//                                                                     longVariableName,
//                                                                     false, false, false, QuestionScope.Interviewer, "", "", "", "",
//                                                                     new Option[0], Order.AZ, 0, new Guid[0]);

//            // assert
//            var domainException = Assert.Throws<DomainException>(act);
//            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.VariableNameMaxLength));
//        }

//        [Test]
//        public void NewUpdateQuestion_When_variable_name_starts_with_digit_Then_DomainException_should_be_thrown()
//        {
//            // Arrange
//            Guid targetQuestionPublicKey = Guid.NewGuid();
//            var questionnaire = CreateQuestionnaireWithOneQuestion(targetQuestionPublicKey);

//            string stataExportCaptionWithFirstDigit = "1aaaa";

//            // Act
//            TestDelegate act = () => questionnaire.CloneQuestion(targetQuestionPublicKey, "Title", QuestionType.Text,
//                                                                     stataExportCaptionWithFirstDigit,
//                                                                     false, false, false, QuestionScope.Interviewer, "", "", "", "",
//                                                                     new Option[0], Order.AZ, 0, new Guid[0]);

//            // assert
//            var domainException = Assert.Throws<DomainException>(act);
//            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.VariableNameStartWithDigit));
//        }

//        [Test]
//        public void NewUpdateQuestion_When_variable_name_has_trailing_spaces_and_is_valid_Then_rised_QuestionChanged_evend_should_contains_trimed_stata_caption()
//        {
//            using (var eventContext = new EventContext())
//            {
//                // Arrange
//                Guid targetQuestionPublicKey = Guid.NewGuid();
//                var questionnaire = CreateQuestionnaireWithOneQuestion(targetQuestionPublicKey);
//                string variableNameWithTrailingSpaces = " my_name38  ";

//                // Act
//                questionnaire.CloneQuestion(targetQuestionPublicKey, "Title", QuestionType.Text,
//                                                variableNameWithTrailingSpaces,
//                                                false, false, false, QuestionScope.Interviewer, "", "", "", "",
//                                                new Option[0], Order.AZ, 0, new Guid[0]);


//                // Assert
//                var risedEvent = GetSingleEvent<QuestionCloned>(eventContext);
//                Assert.AreEqual(variableNameWithTrailingSpaces.Trim(), risedEvent.Alias);
//            }
//        }

//        [Test]
//        public void NewUpdateQuestion_When_variable_name_is_empty_Then_DomainException_should_be_thrown()
//        {
//            // Arrange
//            Guid targetQuestionPublicKey = Guid.NewGuid();
//            var questionnaire = CreateQuestionnaireWithOneQuestion(targetQuestionPublicKey);

//            string emptyVariableName = string.Empty;

//            // Act
//            TestDelegate act = () => questionnaire.CloneQuestion(targetQuestionPublicKey, "Title", QuestionType.Text,
//                                                                     emptyVariableName,
//                                                                     false, false, false, QuestionScope.Interviewer, "", "", "", "",
//                                                                     new Option[0], Order.AZ, 0, new Guid[0]);


//            // assert
//            var domainException = Assert.Throws<DomainException>(act);
//            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.VariableNameRequired));
//        }

//        [Test]
//        public void NewUpdateQuestion_When_variable_name_contains_any_non_underscore_letter_or_digit_character_Then_DomainException_should_be_thrown()
//        {
//            // Arrange
//            Guid targetQuestionPublicKey = Guid.NewGuid();
//            var questionnaire = CreateQuestionnaireWithOneQuestion(targetQuestionPublicKey);

//            string nonValidVariableNameWithBannedSymbols = "aaa:_&b";

//            // Act
//            TestDelegate act = () => questionnaire.CloneQuestion(targetQuestionPublicKey, "Title", QuestionType.Text,
//                                                                     nonValidVariableNameWithBannedSymbols,
//                                                                     false, false, false, QuestionScope.Interviewer, "", "", "", "",
//                                                                     new Option[0], Order.AZ, 0, new Guid[0]);

//            // assert
//            var domainException = Assert.Throws<DomainException>(act);
//            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.VariableNameSpecialCharacters));
//        }

//        [Test]
//        public void NewUpdateQuestion_When_questionnaire_has_another_question_with_same_variable_name_Then_DomainException_should_be_thrown()
//        {
//            // Arrange
//            Guid targetQuestionPublicKey = Guid.NewGuid();
//            string duplicateVariableName = "text";
//            var questionnaire = CreateQuestionnaireWithTwoQuestions(targetQuestionPublicKey);

//            // Act
//            TestDelegate act = () => questionnaire.CloneQuestion(targetQuestionPublicKey, "Title", QuestionType.Text,
//                                                                     duplicateVariableName,
//                                                                     false, false, false, QuestionScope.Interviewer, "", "", "", "",
//                                                                     new Option[0], Order.AZ, 0, new Guid[0]);

//            // assert
//            var domainException = Assert.Throws<DomainException>(act);
//            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.VarialbeNameNotUnique));
//        }

//        [TestCase(QuestionType.SingleOption)]
//        [TestCase(QuestionType.MultyOption)]
//        public void NewUpdateQuestion_When_there_is_only_one_option_in_categorical_question_Then_DomainException_should_be_thrown(QuestionType questionType)
//        {
//            Guid targetQuestionPublicKey = Guid.NewGuid();
//            var questionnaire = CreateQuestionnaireWithOneQuestionnInTypeAndOptions(targetQuestionPublicKey, questionType: questionType, options: new Option[2]
//                    {
//                        new Option(id: Guid.NewGuid(), title: "text1", value: "1") ,
//                        new Option(id: Guid.NewGuid(), title: "text2", value: "2") 
//                    });

//            Option[] oneOption = new Option[1] { new Option(Guid.NewGuid(), "1", "title") };
//            // act
//            TestDelegate act =
//                () =>
//                questionnaire.CloneQuestion(questionId: targetQuestionPublicKey,
//                                   title: "What is your last name?",
//                                   alias: "name",
//                                   type: questionType,
//                                   scope: QuestionScope.Interviewer,
//                                   condition: string.Empty,
//                                   validationExpression: string.Empty,
//                                   validationMessage: string.Empty,
//                                   isFeatured: false,
//                                   isMandatory: false,
//                                   isHeaderOfPropagatableGroup: false,
//                                   optionsOrder: Order.AZ,
//                                   instructions: string.Empty,
//                                   triggedGroupIds: new Guid[0],
//                                   maxValue: 0,
//                                   options: oneOption);

//            // assert
//            var domainException = Assert.Throws<DomainException>(act);
//            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.TooFewOptionsInCategoryQuestion));
//        }

//        [TestCase(QuestionType.SingleOption)]
//        [TestCase(QuestionType.MultyOption)]
//        public void NewUpdateQuestion_When_there_are_two_options_in_categorical_question_Then_raised_QuestionCloned_event_contains_the_same_options_count(QuestionType questionType)
//        {
//            using (var eventContext = new EventContext())
//            {
//                // arrange
//                Guid targetQuestionPublicKey = Guid.NewGuid();
//                var questionnaire = CreateQuestionnaireWithOneQuestionnInTypeAndOptions(targetQuestionPublicKey, questionType: questionType, options: new Option[2]
//                    {
//                        new Option(id: Guid.NewGuid(), title: "text1", value: "1") ,
//                        new Option(id: Guid.NewGuid(), title: "text2", value: "2") 
//                    });

//                const int answerOptionsCount = 2;

//                Option[] options = new Option[answerOptionsCount] { new Option(Guid.NewGuid(), "1", "title"), new Option(Guid.NewGuid(), "2", "title1") };
//                // act
//                questionnaire.CloneQuestion(questionId: targetQuestionPublicKey,
//                                   title: "What is your last name?",
//                                   alias: "name",
//                                   type: questionType,
//                                   scope: QuestionScope.Interviewer,
//                                   condition: string.Empty,
//                                   validationExpression: string.Empty,
//                                   validationMessage: string.Empty,
//                                   isFeatured: false,
//                                   isMandatory: false,
//                                   isHeaderOfPropagatableGroup: false,
//                                   optionsOrder: Order.AZ,
//                                   instructions: string.Empty,
//                                   triggedGroupIds: new Guid[0],
//                                   maxValue: 0,
//                                   options: options);

//                // assert
//                var raisedEvent = GetSingleEvent<QuestionCloned>(eventContext);
//                Assert.That(raisedEvent.Answers.Length, Is.EqualTo(answerOptionsCount));
//            }
//        }

//        [Test]
//        [TestCase(QuestionType.SingleOption)]
//        [TestCase(QuestionType.MultyOption)]
//#warning Roma: when part is incorrect should be something like when answer option value contains not number
//        public void NewUpdateQuestion_When_answer_option_value_allows_only_numbers_Then_DomainException_should_be_thrown(QuestionType questionType)
//        {
//            // arrange
//            Guid targetQuestionPublicKey = Guid.NewGuid();
//            var questionnaire = CreateQuestionnaireWithOneQuestion(targetQuestionPublicKey);

//            // Act
//            TestDelegate act = () =>
//                               questionnaire.CloneQuestion(
//                                   questionId: targetQuestionPublicKey,
//                                   title: "What is your last name?",
//                                   alias: "name",
//                                   type: QuestionType.MultyOption,
//                                   scope: QuestionScope.Interviewer,
//                                   condition: string.Empty,
//                                   validationExpression: string.Empty,
//                                   validationMessage: string.Empty,
//                                   isFeatured: false,
//                                   isMandatory: false,
//                                   isHeaderOfPropagatableGroup: false,
//                                   optionsOrder: Order.AZ,
//                                   instructions: string.Empty,
//                                   triggedGroupIds: new Guid[0],
//                                   maxValue: 0,
//                                   options: new Option[1] { new Option(id: Guid.NewGuid(), title: "text", value: "some text") });

//            // Assert
//            var domainException = Assert.Throws<DomainException>(act);
//            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.SelectorValueSpecialCharacters));
//        }

//        [Test]
//        [TestCase(QuestionType.SingleOption)]
//        [TestCase(QuestionType.MultyOption)]
//        public void NewUpdateQuestion_When_answer_option_value_contains_only_numbers_Then_raised_QuestionChanged_event_contains_question_answer_with_the_same_answe_values(
//            QuestionType questionType)
//        {
//            using (var eventContext = new EventContext())
//            {
//                // arrange
//                Guid targetQuestionPublicKey = Guid.NewGuid();
//                string answerValue1 = "10";
//                string answerValue2 = "100";
//                var questionnaire = CreateQuestionnaireWithOneQuestion(targetQuestionPublicKey);

//                // act
//                questionnaire.CloneQuestion(
//                    questionId: targetQuestionPublicKey,
//                    title: "What is your last name?",
//                    alias: "name",
//                    type: questionType,
//                    scope: QuestionScope.Interviewer,
//                    condition: string.Empty,
//                    validationExpression: string.Empty,
//                    validationMessage: string.Empty,
//                    isFeatured: false,
//                    isMandatory: false,
//                    isHeaderOfPropagatableGroup: false,
//                    optionsOrder: Order.AZ,
//                    instructions: string.Empty,
//                    triggedGroupIds: new Guid[0],
//                    maxValue: 0,
//                    options: new Option[2] { 
//                        new Option(id: Guid.NewGuid(), title: "text1", value: answerValue1),
//                        new Option(id: Guid.NewGuid(), title: "text2", value: answerValue2)});


//                // assert
//                Assert.That(GetSingleEvent<QuestionCloned>(eventContext).Answers[0].AnswerValue, Is.EqualTo(answerValue1));
//                Assert.That(GetSingleEvent<QuestionCloned>(eventContext).Answers[1].AnswerValue, Is.EqualTo(answerValue2));
//            }
//        }

//        [Test]
//        [TestCase(QuestionType.SingleOption)]
//        [TestCase(QuestionType.MultyOption)]
//        [TestCase(QuestionType.Numeric)]
//        [TestCase(QuestionType.Text)]
//        [TestCase(QuestionType.DateTime)]
//        [TestCase(QuestionType.AutoPropagate)]
//        public void NewUpdateQuestion_When_question_type_is_allowed_Then_raised_QuestionChanged_event_with_same_question_type(
//            QuestionType allowedQuestionType)
//        {
//            using (var eventContext = new EventContext())
//            {
//                // arrange
//                Guid questionId = Guid.NewGuid();
//                Questionnaire questionnaire = CreateQuestionnaireWithOneQuestion(questionId);

//                // act
//                questionnaire.CloneQuestion(
//                    questionId: questionId,
//                    title: "What is your last name?",
//                    alias: "name",
//                    type: allowedQuestionType,
//                    scope: QuestionScope.Interviewer,
//                    condition: string.Empty,
//                    validationExpression: string.Empty,
//                    validationMessage: string.Empty,
//                    isFeatured: false,
//                    isMandatory: false,
//                    isHeaderOfPropagatableGroup: false,
//                    optionsOrder: Order.AZ,
//                    instructions: string.Empty,
//                    triggedGroupIds: new Guid[0],
//                    maxValue: 0,
//                    options: AreOptionsRequiredByQuestionType(allowedQuestionType) ? CreateTwoOptions() : null);

//                // assert
//                Assert.That(GetSingleEvent<QuestionCloned>(eventContext).QuestionType, Is.EqualTo(allowedQuestionType));
//            }
//        }

//        [Test]
//        [TestCase(QuestionType.DropDownList)]
//        [TestCase(QuestionType.GpsCoordinates)]
//        [TestCase(QuestionType.YesNo)]
//        public void NewUpdateQuestion_When_question_type_is_not_allowed_Then_DomainException_with_type_NotAllowedQuestionType_should_be_thrown(
//            QuestionType notAllowedQuestionType)
//        {
//            // arrange
//            Guid questionId = Guid.NewGuid();
//            Questionnaire questionnaire = CreateQuestionnaireWithOneQuestion(questionId);

//            // act
//            TestDelegate act = () => questionnaire.CloneQuestion(
//                questionId: questionId,
//                title: "What is your last name?",
//                alias: "name",
//                type: notAllowedQuestionType,
//                scope: QuestionScope.Interviewer,
//                condition: string.Empty,
//                validationExpression: string.Empty,
//                validationMessage: string.Empty,
//                isFeatured: false,
//                isMandatory: false,
//                isHeaderOfPropagatableGroup: false,
//                optionsOrder: Order.AZ,
//                instructions: string.Empty,
//                triggedGroupIds: new Guid[0],
//                maxValue: 0,
//                options: null);

//            // assert
//            var domainException = Assert.Throws<DomainException>(act);
//            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.NotAllowedQuestionType));
//        }

//        [Test]
//        [TestCase(QuestionType.SingleOption)]
//        [TestCase(QuestionType.MultyOption)]
//        public void NewUpdateQuestion_When_answer_option_value_is_required_Then_DomainException_should_be_thrown(QuestionType questionType)
//        {
//            // arrange
//            Guid targetQuestionPublicKey = Guid.NewGuid();
//            var questionnaire = CreateQuestionnaireWithOneQuestion(targetQuestionPublicKey);

//            // Act
//            TestDelegate act = () =>
//                               questionnaire.CloneQuestion(
//                                   questionId: targetQuestionPublicKey,
//                                   title: "What is your last name?",
//                                   alias: "name",
//                                   type: questionType,
//                                   scope: QuestionScope.Interviewer,
//                                   condition: string.Empty,
//                                   validationExpression: string.Empty,
//                                   validationMessage: string.Empty,
//                                   isFeatured: false,
//                                   isMandatory: false,
//                                   isHeaderOfPropagatableGroup: false,
//                                   optionsOrder: Order.AZ,
//                                   instructions: string.Empty,
//                                   triggedGroupIds: new Guid[0],
//                                   maxValue: 0,
//                                   options: new Option[1] { new Option(id: Guid.NewGuid(), title: "text", value: null) });

//            // Assert
//            var domainException = Assert.Throws<DomainException>(act);
//            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.SelectorValueRequired));
//        }

//        [Test]
//        [TestCase(QuestionType.SingleOption)]
//        [TestCase(QuestionType.MultyOption)]
//        public void NewUpdateQuestion_When_answer_option_value_is_not_null_or_empty_Then_raised_QuestionChanged_event_contains_not_null_and_not_empty_question_answer(
//            QuestionType questionType)
//        {
//            using (var eventContext = new EventContext())
//            {
//                // arrange
//                Guid targetQuestionPublicKey = Guid.NewGuid();
//                string notEmptyAnswerValue1 = "10";
//                string notEmptyAnswerValue2 = "100";
//                var questionnaire = CreateQuestionnaireWithOneQuestion(targetQuestionPublicKey);

//                // act
//                questionnaire.CloneQuestion(
//                    questionId: targetQuestionPublicKey,
//                    title: "What is your last name?",
//                    alias: "name",
//                    type: questionType,
//                    scope: QuestionScope.Interviewer,
//                    condition: string.Empty,
//                    validationExpression: string.Empty,
//                    validationMessage: string.Empty,
//                    isFeatured: false,
//                    isMandatory: false,
//                    isHeaderOfPropagatableGroup: false,
//                    optionsOrder: Order.AZ,
//                    instructions: string.Empty,
//                    triggedGroupIds: new Guid[0],
//                    maxValue: 0,
//                    options: new Option[2]
//                        {
//                            new Option(id: Guid.NewGuid(), title: "text", value: notEmptyAnswerValue1),
//                            new Option(id: Guid.NewGuid(), title: "text1", value: notEmptyAnswerValue2)
//                        });


//                // assert
//                Assert.That(GetSingleEvent<QuestionCloned>(eventContext).Answers[0].AnswerValue, !Is.Empty);
//                Assert.That(GetSingleEvent<QuestionCloned>(eventContext).Answers[1].AnswerValue, !Is.Empty);
//            }
//        }

//        [Test]
//        [TestCase(QuestionType.SingleOption)]
//        [TestCase(QuestionType.MultyOption)]
//        public void NewUpdateQuestion_When_answer_option_values_not_unique_in_options_scope_Then_DomainException_should_be_thrown(QuestionType questionType)
//        {
//            // arrange
//            Guid targetQuestionPublicKey = Guid.NewGuid();
//            var questionnaire = CreateQuestionnaireWithOneQuestion(targetQuestionPublicKey);

//            // Act
//            TestDelegate act = () =>
//                               questionnaire.CloneQuestion(
//                                   questionId: targetQuestionPublicKey,
//                                   title: "What is your last name?",
//                                   alias: "name",
//                                   type: questionType,
//                                   scope: QuestionScope.Interviewer,
//                                   condition: string.Empty,
//                                   validationExpression: string.Empty,
//                                   validationMessage: string.Empty,
//                                   isFeatured: false,
//                                   isMandatory: false,
//                                   isHeaderOfPropagatableGroup: false,
//                                   optionsOrder: Order.AZ,
//                                   instructions: string.Empty,
//                                   triggedGroupIds: new Guid[0],
//                                   maxValue: 0,
//                                   options:
//                                       new Option[2]
//                                           {
//                                               new Option(id: Guid.NewGuid(), value: "1", title: "text 1"),
//                                               new Option(id: Guid.NewGuid(), value: "1", title: "text 2")
//                                           });

//            // Assert
//            var domainException = Assert.Throws<DomainException>(act);
//            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.SelectorValueNotUnique));
//        }

//        [Test]
//        [TestCase(QuestionType.SingleOption)]
//        [TestCase(QuestionType.MultyOption)]
//        public void NewUpdateQuestion_When_answer_option_values_unique_in_options_scope_Then_raised_QuestionChanged_event_contains_only_unique_values_in_answer_values_scope(            QuestionType questionType)
//        {
//            using (var eventContext = new EventContext())
//            {
//                // arrange
//                Guid targetQuestionPublicKey = Guid.NewGuid();
//                var questionnaire = CreateQuestionnaireWithOneQuestion(targetQuestionPublicKey);

//                // act
//                questionnaire.CloneQuestion(
//                    questionId: targetQuestionPublicKey,
//                    title: "What is your last name?",
//                    alias: "name",
//                    type: questionType,
//                    scope: QuestionScope.Interviewer,
//                    condition: string.Empty,
//                    validationExpression: string.Empty,
//                    validationMessage: string.Empty,
//                    isFeatured: false,
//                    isMandatory: false,
//                    isHeaderOfPropagatableGroup: false,
//                    optionsOrder: Order.AZ,
//                    instructions: string.Empty,
//                    triggedGroupIds: new Guid[0],
//                    maxValue: 0,
//                    options:
//                        new Option[2]
//                            {
//                                new Option(id: Guid.NewGuid(), title: "text 1", value: "1"),
//                                new Option(id: Guid.NewGuid(), title: "text 2", value: "2")
//                            });


//                // assert
//                Assert.That(GetSingleEvent<QuestionCloned>(eventContext).Answers.Select(x => x.AnswerValue).Distinct().Count(),
//                            Is.EqualTo(2));
//            }
//        }

//        [Test]
//        public void NewUpdateQuestion_When_question_is_AutoPropagate_and_list_of_triggers_is_null_Then_rised_QuestionChanged_event_should_contains_null_in_triggers_field()
//        {
//            using (var eventContext = new EventContext())
//            {
//                // Arrange
//                var groupId = Guid.NewGuid();
//                var autoPropagateQuestionId = Guid.NewGuid();
//                var autoPropagate = QuestionType.AutoPropagate;
//                Guid[] emptyTriggedGroupIds = null;
//                Questionnaire questionnaire = CreateQuestionnaireWithOneAutoGroupAndQuestionInIt(autoPropagateQuestionId);

//                // Act
//                questionnaire.CloneQuestion(autoPropagateQuestionId, "What is your last name?", autoPropagate, "name", false, false,
//                                                false, QuestionScope.Interviewer, "", "", "", "", new Option[0], Order.AsIs, 0,
//                                                emptyTriggedGroupIds);


//                // Assert
//                Assert.That(GetSingleEvent<QuestionCloned>(eventContext).Triggers, Is.Null);
//            }
//        }

//        [Test]
//        public void NewUpdateQuestion_When_question_is_AutoPropagate_and_list_of_triggers_is_empty_Then_rised_QuestionChanged_event_should_contains_empty_list_in_triggers_field()
//        {
//            using (var eventContext = new EventContext())
//            {
//                // Arrange
//                var groupId = Guid.NewGuid();
//                var autoPropagateQuestionId = Guid.NewGuid();
//                var autoPropagate = QuestionType.AutoPropagate;
//                var emptyTriggedGroupIds = new Guid[0];
//                Questionnaire questionnaire = CreateQuestionnaireWithOneAutoGroupAndQuestionInIt(autoPropagateQuestionId);

//                // Act
//                questionnaire.CloneQuestion(autoPropagateQuestionId, "What is your last name?", autoPropagate, "name", false, false,
//                                                false, QuestionScope.Interviewer, "", "", "", "", new Option[0], Order.AsIs, 0,
//                                                emptyTriggedGroupIds);


//                // Assert
//                Assert.That(GetSingleEvent<QuestionCloned>(eventContext).Triggers, Is.Empty);
//            }
//        }

//        [Test]
//        public void NewUpdateQuestion_When_question_is_AutoPropagate_and_list_of_triggers_contains_absent_group_id_Then_DomainException_should_be_thrown()
//        {
//            // Arrange
//            var autoPropagate = QuestionType.AutoPropagate;
//            var autoPropagateQuestionId = Guid.NewGuid();
//            var groupId = Guid.NewGuid();
//            var absentGroupId = Guid.NewGuid();
//            var triggedGroupIdsWithAbsentGroupId = new[] { absentGroupId };

//            Questionnaire questionnaire = CreateQuestionnaireWithOneGroupAndQuestionInIt(autoPropagateQuestionId, groupId, questionType: QuestionType.AutoPropagate);

//            // Act
//            TestDelegate act = () => questionnaire.CloneQuestion(autoPropagateQuestionId, "What is your last name?", autoPropagate, "name", false, false,
//                                                                     false, QuestionScope.Interviewer, "", "", "", "", new Option[0], Order.AsIs, 0,
//                                                                     triggedGroupIdsWithAbsentGroupId);

//            // Assert
//            var domainException = Assert.Throws<DomainException>(act);
//            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.TriggerLinksToNotExistingGroup));
//        }

//        [Test]
//        public void NewUpdateQuestion_When_question_is_AutoPropagate_and_list_of_triggers_contains_non_propagate_group_id_Then_DomainException_should_be_thrown()
//        {
//            // Arrange
//            var autoPropagate = QuestionType.AutoPropagate;
//            var autoPropagateQuestionId = Guid.NewGuid();
//            var nonPropagateGroupId = Guid.NewGuid();
//            var groupId = Guid.NewGuid();
//            var triggedGroupIdsWithNonPropagateGroupId = new[] { nonPropagateGroupId };

//            Questionnaire questionnaire = CreateQuestionnaireWithTwoRegularGroupsAndQuestionInLast(nonPropagateGroupId, autoPropagateQuestionId);

//            // Act
//            TestDelegate act = () => questionnaire.CloneQuestion(autoPropagateQuestionId, "What is your last name?", autoPropagate, "name", false, false,
//                                                                     false, QuestionScope.Interviewer, "", "", "", "", new Option[0], Order.AsIs, 0,
//                                                                     triggedGroupIdsWithNonPropagateGroupId);


//            // Assert
//            var domainException = Assert.Throws<DomainException>(act);
//            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.TriggerLinksToNotPropagatedGroup));
//        }

//        [Test]
//        public void NewUpdateQuestion_When_question_is_AutoPropagate_and_list_of_triggers_contains_propagate_group_id_Then_rised_QuestionChanged_event_should_contains_that_group_id_in_triggers_field()
//        {
//            using (var eventContext = new EventContext())
//            {
//                // Arrange
//                var autoPropagate = QuestionType.AutoPropagate;
//                var autoPropagateQuestionId = Guid.NewGuid();
//                var autoPropagateGroupId = Guid.NewGuid();
//                var groupId = Guid.NewGuid();
//                var triggedGroupIdsWithAutoPropagateGroupId = new[] { autoPropagateGroupId };

//                Questionnaire questionnaire = CreateQuestionnaireWithAutoGroupAndRegularGroupAndQuestionInIt(autoPropagateGroupId, groupId, autoPropagateQuestionId);

//                // Act
//                questionnaire.CloneQuestion(autoPropagateQuestionId, "What is your last name?", autoPropagate, "name", false, false,
//                                                false, QuestionScope.Interviewer, "", "", "", "", new Option[0], Order.AsIs, 0,
//                                                triggedGroupIdsWithAutoPropagateGroupId);

//                // Assert
//                Assert.That(GetSingleEvent<QuestionCloned>(eventContext).Triggers, Contains.Item(autoPropagateGroupId));
//            }
//        }
    }
}