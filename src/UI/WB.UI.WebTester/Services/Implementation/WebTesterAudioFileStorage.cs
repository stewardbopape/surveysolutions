﻿using System;
using System.Collections.Generic;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.Views.BinaryData;

namespace WB.UI.WebTester.Services.Implementation
{
    public class WebTesterAudioFileStorage : IAudioFileStorage
    {
        private readonly ICacheStorage<MultimediaFile, string> mediaStorage;

        public WebTesterAudioFileStorage(ICacheStorage<MultimediaFile, string> mediaStorage)
        {
            this.mediaStorage = mediaStorage;
        }

        public byte[] GetInterviewBinaryData(Guid interviewId, string fileName)
        {
            return this.mediaStorage.Get(fileName, interviewId)?.Data;
        }

        public List<InterviewBinaryDataDescriptor> GetBinaryFilesForInterview(Guid interviewId)
        {
            throw new NotImplementedException();
        }

        public void StoreInterviewBinaryData(Guid interviewId, string fileName, byte[] data, string contentType)
        {
            mediaStorage.Store(new MultimediaFile
            {
                Filename = fileName,
                Data = data,
                MimeType = contentType
            }, fileName, interviewId);
        }

        public void RemoveInterviewBinaryData(Guid interviewId, string fileName)
        {
            mediaStorage.Remove(fileName, interviewId);
        }
    }
}
