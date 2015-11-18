﻿using System;
using System.Linq;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using NHibernate.Util;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Pdf;

namespace WB.Tests.Unit.BoundedContexts.Designer.PdfQuestionTypeConverterTests
{
    public class when_getting_multyoption_question_with_yesno_flag : PdfQuestionTypeConverterTestsContext
    {
        Establish context = () =>
        {
            question = new MultyOptionsQuestion()
            {
                YesNoView = true
            };

            converter = CreatePdfQuestionTypeConverter();
        };

        Because of = () =>
            pdfQuestionType = converter.GetPdfQuestionTypeFromQuestion(question);

        It should_return_yes_no_pdf_question_type = () =>
        {
            pdfQuestionType.ShouldEqual(PdfQuestionType.YesNo);
        };

        private static PdfQuestionTypeConverter converter;
        private static PdfQuestionType pdfQuestionType;
        private static IQuestion question;
    }
}