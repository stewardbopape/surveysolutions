﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Moq;
using NUnit.Framework;
using WB.Core.BoundedContexts.Headquarters.DataExport.Factories;
using WB.Core.BoundedContexts.Headquarters.DataExport.Services.Exporters;
using WB.Core.BoundedContexts.Headquarters.Views.DataExport;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.BoundedContexts.Headquarters.Views.InterviewHistory;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Implementation.ServiceVariables;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.Infrastructure.Transactions;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Tests.Abc;
using WB.Tests.Abc.Storage;

namespace WB.Tests.Unit.DataExportTests.InterviewsExporterTests
{
    [TestFixture]
    [TestOf(typeof(InterviewsExporter))]
    class When_exporting_data_for_one_interview
    {
        [Test]
        public void It_should_export_service_column_with_interview_key()
        {
            //arrange
            SetUp();
            Guid interviewId = Id.g1;
            var interviewKey = "11-11-11-11";

            var questionnaire = Create.Entity.QuestionnaireDocumentWithOneChapter(
                Create.Entity.NumericIntegerQuestion(variable: "q1")
            );

            interviewSummaries = new TestInMemoryWriter<InterviewSummary>(interviewId.FormatGuid(), Create.Entity.InterviewSummary(interviewId, key: interviewKey));

            exporter = new InterviewsExporter(
                fileSystemAccessor.Object,
                logger.Object,
                interviewDataExportSettings,
                csvWriter.Object,
                rowReader.Object,
                interviewSummaries,
                transactionManagerProvider.Object);

            var questionnaireExportStructure = Create.Entity.QuestionnaireExportStructure(questionnaire);

            rowReader.Setup(x => x.ReadExportDataForInterview(interviewId))
                .Returns(new List<InterviewDataExportRecord>
                {
                    Create.Entity.InterviewDataExportRecord(
                        interviewId, 
                        levelName: "Questionnaire",
                        systemVariableValues: new []{ "0.234567", interviewKey},
                        answers: new [] { "8" })
                });
            //act
            exporter.Export(questionnaireExportStructure, new List<Guid>{ interviewId }, "path", new Progress<int>(), CancellationToken.None);

            //assert
            Assert.That(dataInCsvFile[0].File, Is.EqualTo("Questionnaire.tab"));
            Assert.That(dataInCsvFile[0].Data[0][3], Is.EqualTo(ServiceColumns.Key));
            Assert.That(dataInCsvFile[1].Data[0][3], Is.EqualTo(interviewKey));
        }

        [Test]
        public void It_should_export_service_column_with_has_error_and_status()
        {
            //arrange
            SetUp();
            Guid interviewId = Id.g1;

            var questionnaire = Create.Entity.QuestionnaireDocumentWithOneChapter(
                Create.Entity.NumericIntegerQuestion(variable: "q1")
            );

            interviewSummaries = new TestInMemoryWriter<InterviewSummary>(interviewId.FormatGuid(), 
                Create.Entity.InterviewSummary(interviewId, hasErrors: true, status: InterviewStatus.Completed));

            exporter = new InterviewsExporter(
                fileSystemAccessor.Object,
                logger.Object,
                interviewDataExportSettings,
                csvWriter.Object,
                rowReader.Object,
                interviewSummaries,
                transactionManagerProvider.Object);

            var questionnaireExportStructure = Create.Entity.QuestionnaireExportStructure(questionnaire);

            rowReader.Setup(x => x.ReadExportDataForInterview(interviewId))
                .Returns(new List<InterviewDataExportRecord>
                {
                    Create.Entity.InterviewDataExportRecord(
                        interviewId,
                        levelName: "Questionnaire",
                        systemVariableValues: new []{ "0.234567"},
                        answers: new [] { "8" })
                });
            //act
            exporter.Export(questionnaireExportStructure, new List<Guid> { interviewId }, "path", new Progress<int>(), CancellationToken.None);

            //assert
            Assert.That(dataInCsvFile.Count, Is.EqualTo(2));
            Assert.That(dataInCsvFile[0].File, Is.EqualTo("Questionnaire.tab"));
            Assert.That(dataInCsvFile[0].Data[0][4], Is.EqualTo(ServiceColumns.HasAnyError));
            Assert.That(dataInCsvFile[1].Data[0][4], Is.EqualTo(true.ToString()));
            Assert.That(dataInCsvFile[1].Data[0][5], Is.EqualTo(InterviewStatus.Completed.ToString()));
        }
        private void SetUp()
        {
            fileSystemAccessor = new Mock<IFileSystemAccessor>();
            logger = new Mock<ILogger>();
            csvWriter = new Mock<ICsvWriter>();
            rowReader = new Mock<InterviewExportredDataRowReader>();
            interviewSummaries = new TestInMemoryWriter<InterviewSummary>();
            transactionManagerProvider = new Mock<ITransactionManagerProvider>();
            transactionManagerProvider.Setup(x => x.GetTransactionManager()).Returns(Mock.Of<ITransactionManager>());

            csvWriter
                .Setup(x => x.WriteData(It.IsAny<string>(), It.IsAny<IEnumerable<string[]>>(), It.IsAny<string>()))
                .Callback<string, IEnumerable<string[]>, string>((string s, IEnumerable<string[]> p, string t) =>
                {
                    dataInCsvFile.Add(new CsvData
                    {
                        File = s,
                        Data = p.ToList()
                    });
                });

            fileSystemAccessor
                .Setup(x => x.CombinePath(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string b, string p) => p);
        }

        private List<CsvData> dataInCsvFile = new List<CsvData>();
        private InterviewsExporter exporter;
        private Mock<IFileSystemAccessor> fileSystemAccessor;
        private Mock<ILogger> logger;
        private InterviewDataExportSettings interviewDataExportSettings = new InterviewDataExportSettings("folder", false, 1, 1, 1, 1);
        private Mock<ICsvWriter> csvWriter;
        private Mock<InterviewExportredDataRowReader> rowReader;
        private TestInMemoryWriter<InterviewSummary> interviewSummaries;
        private Mock<ITransactionManagerProvider> transactionManagerProvider;

        class CsvData
        {
            public string File { get; set; }
            public List<string[]> Data { get; set; }
        }
    }
}
