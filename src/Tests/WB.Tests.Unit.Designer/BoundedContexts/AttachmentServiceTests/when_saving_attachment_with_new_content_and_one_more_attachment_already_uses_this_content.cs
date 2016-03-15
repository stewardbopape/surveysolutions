using System;
using System.Drawing;
using Machine.Specifications;
using Microsoft.Practices.ServiceLocation;
using Moq;
using WB.Core.BoundedContexts.Designer.Implementation.Services.AttachmentService;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.PlainStorage;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.Designer.BoundedContexts.AttachmentServiceTests
{
    internal class when_saving_attachment_with_new_content_and_one_more_attachment_already_uses_this_content : AttachmentServiceTestContext
    {
        Establish context = () =>
        {
            attachmentMetaStorage.Store(Create.AttachmentMeta(attachmentId.FormatGuid(), oldContentHash, questionnaireId: questionnaireId.FormatGuid()), attachmentId.FormatGuid());
            attachmentMetaStorage.Store(Create.AttachmentMeta(otherAttachmentId, fileContentHash), otherAttachmentId);

            Setup.ServiceLocatorForAttachmentService(attachmentContentStorage.Object, attachmentMetaStorage);
            
            attachmentService = Create.AttachmentService();
            attachmentService.ImageFromStream = stream => image;
        };

        Because of = () =>
            attachmentService.SaveAttachmentContent(questionnaireId, attachmentId, AttachmentType.Image, contentType, fileContent, fileName);

        It should_save_attachment_meta = () =>
            attachmentMetaStorage.GetById(attachmentId.FormatGuid()).ShouldNotBeNull();

        It should_not_save_or_update_anything_in_content_storage = () =>
            attachmentContentStorage.Verify(x => x.Store(Moq.It.IsAny<AttachmentContent>(), Moq.It.IsAny<string>()), Times.Never);

        It should_save_attachment_meta_with_specified_properties = () =>
        {
            var attachmentMeta = attachmentMetaStorage.GetById(attachmentId.FormatGuid());
            attachmentMeta.FileName.ShouldEqual(fileName);
            attachmentMeta.QuestionnaireId.ShouldEqual(questionnaireId.FormatGuid());
            attachmentMeta.AttachmentContentHash.ShouldEqual(fileContentHash);
        };

        private static AttachmentService attachmentService;
        private static readonly byte[] fileContent = new byte[] { 96, 97, 98, 99, 100 };
        private static readonly string fileContentHash = GetHash(fileContent);
        private static readonly string oldContentHash = "prev_hash";
        private static readonly Image image = new Bitmap(2, 3);
        private static readonly string fileName = "Attachment.PNG";
        private static readonly Guid questionnaireId = Guid.Parse("11111111111111111111111111111111");
        private static readonly Guid attachmentId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        private static readonly string otherAttachmentId = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";
        private static readonly string contentType = "image/png";
        private static readonly TestPlainStorage<AttachmentMeta> attachmentMetaStorage = new TestPlainStorage<AttachmentMeta>();
        private static readonly Mock<IPlainStorageAccessor<AttachmentContent>> attachmentContentStorage = new Mock<IPlainStorageAccessor<AttachmentContent>>();
    }
}