﻿using System;
using System.Globalization;
using Machine.Specifications;
using Main.Core.Documents;
using WB.Core.BoundedContexts.Headquarters.DataExport.Denormalizers;
using WB.Core.SharedKernels.SurveyManagement.Views.DataExport;
using WB.Core.SharedKernels.SurveyManagement.Views.Interview;
using System.Threading;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.Factories.ExportViewFactoryTests
{
    internal class when_creating_interview_export_view_by_interview_with_1_answerd_double_question_in_russian_culture : ExportViewFactoryTestsContext
    {
        Establish context = () =>
        {
            dateTimeQuestionId = Guid.Parse("10000000000000000000000000000000");

            interviewData =
                Create.InterviewData(Create.InterviewQuestion(questionId: dateTimeQuestionId,
                    answer: value));

            questionnaireDocument =
                Create.QuestionnaireDocument(children: Create.NumericRealQuestion(id: dateTimeQuestionId, variable: "real"));

            exportViewFactory = CreateExportViewFactory();
        };

        Because of = () =>
        {
            var originalCulture = new
            {
                Culture = Thread.CurrentThread.CurrentCulture,
                UICulture = Thread.CurrentThread.CurrentUICulture
            };

            try
            {
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
                result = exportViewFactory.CreateInterviewDataExportView(
                            exportViewFactory.CreateQuestionnaireExportStructure(questionnaireDocument, 1),
                            interviewData);
            }
            finally 
            {
                Thread.CurrentThread.CurrentCulture = originalCulture.Culture;
                Thread.CurrentThread.CurrentUICulture = originalCulture.UICulture;
            }
            
        };


        It should_create_record__with_one_datetime_question_which_contains_composite_answer = () =>
          result.Levels[0].Records[0].GetQuestions()[0].Answers.ShouldEqual(new[] { value.ToString(CultureInfo.InvariantCulture)  });

        private static ExportViewFactory exportViewFactory;
        private static InterviewDataExportView result;
        private static Guid dateTimeQuestionId;
        private static QuestionnaireDocument questionnaireDocument;
        private static InterviewData interviewData;
        private static double value = 5.55;

        private static CultureInfo culture = CultureInfo.GetCultureInfo("ru-ru");
    }
}