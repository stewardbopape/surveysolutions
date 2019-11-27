﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using Moq;
using NHibernate.Collection.Generic;
using NUnit.Framework;
using WB.Core.BoundedContexts.Designer.ValueObjects;
using WB.Core.BoundedContexts.Designer.Verifier;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.Questionnaire.Categories;

namespace WB.Tests.Unit.Designer.QuestionnaireVerificationTests
{
    [TestOf(typeof(QuestionVerifications))]
    internal class QuestionVerificationsTests : QuestionnaireVerifierTestsContext
    {
        [Test]
        public void when_categorical_multi_question_has_more_than_allowed_options_should_return_WB0075()
        {
            // arrange
            Guid filteredComboboxId = Guid.Parse("10000000000000000000000000000000");
            int incrementer = 0;
            var questionnaire = Create.QuestionnaireDocumentWithOneChapter(
                Create.MultyOptionsQuestion(
                    filteredComboboxId,
                    variable: "var",
                    filteredCombobox: true,
                    options:
                    new List<Answer>(
                        new Answer[15001].Select(
                            answer =>
                                new Answer()
                                {
                                    AnswerValue = incrementer.ToString(),
                                    AnswerText = (incrementer++).ToString()
                                }))
                ));

            QuestionnaireVerifier verifier = CreateQuestionnaireVerifier();

            // act
            var verificationMessages = verifier.CheckForErrors(Create.QuestionnaireView(questionnaire));

            // assert
            verificationMessages.Count().Should().Be(1);

            verificationMessages.Single().Code.Should().Be("WB0075");

            verificationMessages.Single().References.Count().Should().Be(1);

            verificationMessages.Single().References.First().Type.Should()
                .Be(QuestionnaireVerificationReferenceType.Question);

            verificationMessages.Single().References.First().Id.Should().Be(filteredComboboxId);
        }

        [Test]
        public void when_verifying_categorical_multi_and_options_count_more_than_200()
        {
            Guid multiOptionId = Guid.Parse("10000000000000000000000000000000");
            int incrementer = 0;
            var questionnaire = Create.QuestionnaireDocumentWithOneChapter(
                Create.MultyOptionsQuestion(
                    multiOptionId,
                    options:
                    new List<Answer>(
                        new Answer[201].Select(
                            answer =>
                                new Answer()
                                {
                                    AnswerValue = incrementer.ToString(),
                                    AnswerText = (incrementer++).ToString()
                                }))
                )
            );

            var verifier = CreateQuestionnaireVerifier();
            var verificationMessages = verifier.CheckForErrors(Create.QuestionnaireView(questionnaire));

            verificationMessages.ShouldContainError("WB0076");

            verificationMessages.Single(e => e.Code == "WB0076").MessageLevel.Should()
                .Be(VerificationMessageLevel.General);

            verificationMessages.Single(e => e.Code == "WB0076").References.Count().Should().Be(1);

            verificationMessages.Single(e => e.Code == "WB0076").References.First().Type.Should()
                .Be(QuestionnaireVerificationReferenceType.Question);

            verificationMessages.Single(e => e.Code == "WB0076").References.First().Id.Should().Be(multiOptionId);
        }

        [Test]
        public void when_verifying_questionnaire_with_categorical_multi_answers_question_that_has_max_allowed_answers_count_more_than_reusable_categories_count()
        {
            // arrange
            Guid multyOptionsQuestionId = Guid.Parse("10000000000000000000000000000000");
            Guid categoriesId = Guid.Parse("11111111111111111111111111111111");

            var questionnaire = CreateQuestionnaireDocument(Create.MultyOptionsQuestion(
                multyOptionsQuestionId,
                categoriesId: categoriesId,
                maxAllowedAnswers: 3,
                variable: "var1"
            ));
            var categoriesService = Mock.Of<ICategoriesService>(x =>
                x.GetCategoriesById(categoriesId) == new List<CategoriesItem>()
                {
                    new CategoriesItem(),
                    new CategoriesItem()
                }.AsQueryable());

            var verifier = CreateQuestionnaireVerifier(categoriesService: categoriesService);
            // act
            var verificationMessages = verifier.CheckForErrors(Create.QuestionnaireView(questionnaire)).ToList();

            // assert
            verificationMessages.Count().Should().Be(1);
            verificationMessages.Single().Code.Should().Be("WB0021");
            verificationMessages.Single().MessageLevel.Should().Be(VerificationMessageLevel.General);
        }

        [Test]
        public void when_categorical_single_question_with_reusable_categories_has_option_values_that_doesnt_exit_in_parent_question()
        {
            // arrange
            var parentSingleOptionQuestionId = Guid.Parse("9E96D4AB-DF91-4FC9-9585-23FA270B25D7");
            var childCascadedComboboxId = Guid.Parse("C6CC807A-3E81-406C-A110-1044AE3FD89B");
            var childCategoriesId = Guid.Parse("11111111111111111111111111111111");

            var questionnaire = CreateQuestionnaireDocumentWithOneChapter(new SingleQuestion {
                    PublicKey = parentSingleOptionQuestionId,
                    StataExportCaption = "var",
                    QuestionType = QuestionType.SingleOption,
                    Answers = new List<Answer> {
                        new Answer { AnswerText = "one", AnswerValue = "1" },
                        new Answer { AnswerText = "two", AnswerValue = "2" }
                    }
                },
                new SingleQuestion
                {
                    PublicKey = childCascadedComboboxId,
                    QuestionType = QuestionType.SingleOption,
                    StataExportCaption = "var1",
                    CascadeFromQuestionId = parentSingleOptionQuestionId,
                    CategoriesId = childCategoriesId
                }
            );
            var categoriesService = Mock.Of<ICategoriesService>(x =>
                x.GetCategoriesById(childCategoriesId) == new List<CategoriesItem>()
                {
                    new CategoriesItem{ Id = 1, ParentId = 3, Text =  "child 1"},
                    new CategoriesItem{ Id = 2, ParentId = 4, Text =  "child 2"}
                }.AsQueryable());

            var verifier = CreateQuestionnaireVerifier(categoriesService: categoriesService);

            // act
            var verificationErrors = Enumerable.ToList(verifier.CheckForErrors(Create.QuestionnaireView(questionnaire)));

            // assert
            verificationErrors.ShouldContainError("WB0084");
            verificationErrors.GetError("WB0084").References.Should().Contain(@ref => @ref.ItemId == parentSingleOptionQuestionId.FormatGuid());
            verificationErrors.GetError("WB0084").References.Should().Contain(@ref => @ref.ItemId == childCascadedComboboxId.FormatGuid());
            verificationErrors.GetError("WB0084").References.Should().OnlyContain(x => x.Type == QuestionnaireVerificationReferenceType.Question);
        }

        [Test]
        public void when_categorical_single_question_has_option_values_that_doesnt_exit_in_parent_question_with_reusable_categories()
        {
            // arrange
            var parentSingleOptionQuestionId = Guid.Parse("9E96D4AB-DF91-4FC9-9585-23FA270B25D7");
            var childCascadedComboboxId = Guid.Parse("C6CC807A-3E81-406C-A110-1044AE3FD89B");
            var parentCategoriesId = Guid.Parse("11111111111111111111111111111111");

            var questionnaire = CreateQuestionnaireDocumentWithOneChapter(new SingleQuestion {
                    PublicKey = parentSingleOptionQuestionId,
                    StataExportCaption = "var",
                    QuestionType = QuestionType.SingleOption,
                    CategoriesId = parentCategoriesId
                },
                new SingleQuestion
                {
                    PublicKey = childCascadedComboboxId,
                    QuestionType = QuestionType.SingleOption,
                    StataExportCaption = "var1",
                    CascadeFromQuestionId = parentSingleOptionQuestionId,
                    Answers = new List<Answer> {
                        new Answer { AnswerText = "child 1", AnswerValue = "1", ParentValue = "3" },
                        new Answer { AnswerText = "child 2", AnswerValue = "2", ParentValue = "4" },
                    }
                }
            );
            var categoriesService = Mock.Of<ICategoriesService>(x =>
                x.GetCategoriesById(parentCategoriesId) == new List<CategoriesItem>()
                {
                    new CategoriesItem{ Id = 1,  Text =  "one"},
                    new CategoriesItem{ Id = 2, Text =  "two"}
                }.AsQueryable());

            var verifier = CreateQuestionnaireVerifier(categoriesService: categoriesService);

            // act
            var verificationErrors = Enumerable.ToList(verifier.CheckForErrors(Create.QuestionnaireView(questionnaire)));

            // assert
            verificationErrors.ShouldContainError("WB0084");
            verificationErrors.GetError("WB0084").References.Should().Contain(@ref => @ref.ItemId == parentSingleOptionQuestionId.FormatGuid());
            verificationErrors.GetError("WB0084").References.Should().Contain(@ref => @ref.ItemId == childCascadedComboboxId.FormatGuid());
            verificationErrors.GetError("WB0084").References.Should().OnlyContain(x => x.Type == QuestionnaireVerificationReferenceType.Question);
        }

        [Test]
        public void when_categorical_single_question_with_reusable_categories_has_option_values_that_doesnt_exit_in_parent_question_with_reusable_categories()
        {
            // arrange
            var parentSingleOptionQuestionId = Guid.Parse("9E96D4AB-DF91-4FC9-9585-23FA270B25D7");
            var childCascadedComboboxId = Guid.Parse("C6CC807A-3E81-406C-A110-1044AE3FD89B");
            var childCategoriesId = Guid.Parse("11111111111111111111111111111111");
            var parentCategoriesId = Guid.Parse("22222222222222222222222222222222");

            var questionnaire = CreateQuestionnaireDocumentWithOneChapter(new SingleQuestion {
                    PublicKey = parentSingleOptionQuestionId,
                    StataExportCaption = "var",
                    QuestionType = QuestionType.SingleOption,
                    CategoriesId = parentCategoriesId
                },
                new SingleQuestion
                {
                    PublicKey = childCascadedComboboxId,
                    QuestionType = QuestionType.SingleOption,
                    StataExportCaption = "var1",
                    CascadeFromQuestionId = parentSingleOptionQuestionId,
                    CategoriesId = childCategoriesId
                }
            );
            var categoriesService = Mock.Of<ICategoriesService>(x =>
                x.GetCategoriesById(parentCategoriesId) == new List<CategoriesItem>()
                {
                    new CategoriesItem{ Id = 1,  Text =  "one"},
                    new CategoriesItem{ Id = 2, Text =  "two"}
                }.AsQueryable() &&
                x.GetCategoriesById(childCategoriesId) == new List<CategoriesItem>()
                {
                    new CategoriesItem{ Id = 1, ParentId = 3, Text =  "child 1"},
                    new CategoriesItem{ Id = 2, ParentId = 4, Text =  "child 2"}
                }.AsQueryable());

            var verifier = CreateQuestionnaireVerifier(categoriesService: categoriesService);

            // act
            var verificationErrors = Enumerable.ToList(verifier.CheckForErrors(Create.QuestionnaireView(questionnaire)));

            // assert
            verificationErrors.ShouldContainError("WB0084");
            verificationErrors.GetError("WB0084").References.Should().Contain(@ref => @ref.ItemId == parentSingleOptionQuestionId.FormatGuid());
            verificationErrors.GetError("WB0084").References.Should().Contain(@ref => @ref.ItemId == childCascadedComboboxId.FormatGuid());
            verificationErrors.GetError("WB0084").References.Should().OnlyContain(x => x.Type == QuestionnaireVerificationReferenceType.Question);
        }
    }
}
