﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Machine.Specifications;
using Main.Core.Documents;
using Moq;
using NUnit.Framework;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport.Verifier;
using WB.Core.BoundedContexts.Headquarters.ValueObjects.PreloadedData;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.PreloadedDataVerifierTests
{
    internal class when_verifying_preloaded_data_and_data_has_1_unmapped_file : PreloadedDataVerifierTestContext
    {
        [Test]
        public void Should_return_1_error()
        {
            var  questionnaire = CreateQuestionnaireDocumentWithOneChapter();
            var questionnaireId = Guid.Parse("11111111111111111111111111111111");
            var importDataVerifier = CreatePreloadedDataVerifier(questionnaire);
            

            importDataVerifier.VerifyPanelFiles(questionnaireId,
                    1,
                    new[]
                    {
                        CreatePreloadedDataByFile(fileName: questionnaire.Title + ".csv"),
                    },
                    status);

            Assert.AreEqual(status.VerificationState.Errors.Count(), 1);
            Assert.AreEqual(status.VerificationState.Errors.First().Code,"PL0004");
            Assert.AreEqual(status.VerificationState.Errors.First().References.First().Type, PreloadedDataVerificationReferenceType.File);

     }
}
}
