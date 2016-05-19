﻿using Main.Core.Documents;
using Moq;
using WB.Core.BoundedContexts.Designer.Views.Account;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.ChangeHistory;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Pdf;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.QuestionnaireList;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.SharedPersons;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;

namespace WB.Tests.Unit.Designer.BoundedContexts.Designer.PdfFactoryTests
{
    public class PdfFactoryTestsContext
    {
        public static PdfFactory CreateFactory(
            IReadSideKeyValueStorage<QuestionnaireDocument> questionnaireStorage = null,
            IQueryableReadSideRepositoryReader<QuestionnaireChangeRecord> questionnaireChangeHistoryStorage = null,
            IReadSideRepositoryReader<AccountDocument> accountsDocumentReader = null,
            IQueryableReadSideRepositoryReader<QuestionnaireListViewItem> questionnaireListViewItemStorage = null,
            IReadSideKeyValueStorage<QuestionnaireSharedPersons> sharedPersonsStorage = null,
            PdfSettings pdfSettings = null)
        {
            return new PdfFactory(
                questionnaireStorage: questionnaireStorage ?? Mock.Of<IReadSideKeyValueStorage<QuestionnaireDocument>>(),
                questionnaireChangeHistoryStorage: questionnaireChangeHistoryStorage ?? Mock.Of<IQueryableReadSideRepositoryReader<QuestionnaireChangeRecord>>(),
                accountsDocumentReader: accountsDocumentReader ?? Mock.Of<IReadSideRepositoryReader<AccountDocument>>(),
                questionnaireListViewItemStorage: questionnaireListViewItemStorage ?? Mock.Of<IQueryableReadSideRepositoryReader<QuestionnaireListViewItem>>(),
                sharedPersonsStorage: sharedPersonsStorage ?? Mock.Of<IReadSideKeyValueStorage<QuestionnaireSharedPersons>>(),
                pdfSettings: pdfSettings ?? new PdfSettings(0, 0, 0, 0, 0, 0, 0));
        }
    }
}