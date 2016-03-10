﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WB.Core.SharedKernels.DataCollection.Views.BinaryData;

namespace WB.Core.SharedKernels.DataCollection.Repositories
{
    public interface IQuestionnaireAttachmentStorage
    {
        Task StoreAttachmentContentAsync(string attachmentId, byte[] attachmentData);
//        Task StoreAsync(AttachmentMetadata attachmentMetadata, byte[] attachmentData);
//        Task<AttachmentMetadata> GetAttachmentAsync(string attachmentId);
        Task<byte[]> GetAttachmentContentAsync(string attachmentId);
        Task<bool> IsExistAttachmentContent(string attachmentId);
    }
}