using System;
using System.Linq;
using Machine.Specifications;
using Main.Core.Documents;
using Moq;
using NUnit.Framework;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.ChangeHistory;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Pdf;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.QuestionnaireList;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.Implementation;
using WB.Core.Infrastructure.PlainStorage;
using WB.Tests.Abc;
using It = Moq.It;

namespace WB.Tests.Unit.Designer.BoundedContexts.Designer.PdfFactoryTests
{
    public class when_load_and_shared_persons_contains_requested_user : PdfFactoryTestsContext
    {
        [OneTimeSetUp]
        public void Context()
        {
            var accountDocument = Create.AccountDocument(userName);
            var questionnaireDocument = Create.QuestionnaireDocument();

            var accountsDocumentReader = Mock.Of<IPlainStorageAccessor<User>>(x => x.GetById(userId.FormatGuid()) == accountDocument);
            var questionnaireRepository = Mock.Of<IPlainKeyValueStorage<QuestionnaireDocument>>(x => x.GetById(questionnaireId.FormatGuid()) == questionnaireDocument);
            var questionnaireChangeHistoryStorage = new InMemoryPlainStorageAccessor<QuestionnaireChangeRecord>();
            questionnaireChangeHistoryStorage.Store(
                new QuestionnaireChangeRecord
                {
                    QuestionnaireId = questionnaireId.FormatGuid(),
                    UserId = userId,
                    UserName = userName
                }, "");

            var questionnaireListItemStorage = new InMemoryPlainStorageAccessor<QuestionnaireListViewItem>();
            var questionnaireListViewItem = Create.QuestionnaireListViewItem();
            questionnaireListViewItem.SharedPersons.Add(Create.SharedPerson(id: userId, email: userEmail));
            questionnaireListItemStorage.Store(questionnaireListViewItem, questionnaireId.FormatGuid());

            factory = CreateFactory(accountsDocumentReader: accountsDocumentReader,
                questionnaireStorage: questionnaireRepository,
                questionnaireChangeHistoryStorage: questionnaireChangeHistoryStorage,
                questionnaireListViewItemStorage: questionnaireListItemStorage);

            BecauseOf();
        }

        private void BecauseOf() => view = factory.Load(questionnaireId.FormatGuid(), userId, userName, null, true);

        [Test]
        public void should_shared_persons_be_empty() => Assert.False(view.SharedPersons.Any());

        private static PdfQuestionnaireModel view;
        private static PdfFactory factory;
        private static Guid questionnaireId = Id.g1;
        private static Guid userId = Id.g2;
        private static string userName = "user";
        private static string userEmail = "user@e.mail";
    }
}