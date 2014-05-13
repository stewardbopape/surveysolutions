﻿using System;
using System.Collections.Generic;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using WB.Core.SharedKernels.SurveyManagement.ValueObjects;

namespace WB.Core.SharedKernels.SurveyManagement.Tests.QuestionDataParserTests
{
    internal class when_pasing_answer_on_multy_option_question : QuestionDataParserTestContext
    {
        private Establish context = () =>
        {
            answer = "2";
            questionDataParser = CreateQuestionDataParser();
        };

        private Because of =
            () =>
                parsingResult =
                    questionDataParser.TryParse(answer, questionVarName,
                        CreateQuestionnaireDocumentWithOneChapter(new MultyOptionsQuestion()
                        {
                            PublicKey = questionId,
                            QuestionType = QuestionType.MultyOption,
                            Answers =
                                new List<Answer>()
                                {
                                    new Answer() { AnswerValue = "1", AnswerText = "1" },
                                    new Answer() { AnswerValue = "2", AnswerText = "2" },
                                    new Answer() { AnswerValue = "3", AnswerText = "3" }
                                },
                            StataExportCaption = questionVarName
                        }), out parcedValue);

        private It should_result_be_equal_to_2 = () =>
            parcedValue.Value.ShouldEqual((decimal)2);

        private It should_result_key_be_equal_to_questionId = () =>
            parcedValue.Key.ShouldEqual(questionId);
    }
}
