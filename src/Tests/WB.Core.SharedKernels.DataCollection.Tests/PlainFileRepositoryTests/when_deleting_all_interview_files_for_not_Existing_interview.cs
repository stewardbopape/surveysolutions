﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Machine.Specifications;
using Moq;
using WB.Core.GenericSubdomains.Utils;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.SharedKernels.DataCollection.Implementation.Repositories;
using It = Machine.Specifications.It;

namespace WB.Core.SharedKernels.DataCollection.Tests.PlainFileRepositoryTests
{
    internal class when_deleting_all_interview_files_for_not_existing_interview : PlainFileRepositoryTestContext
    {
        Establish context = () =>
        {
            plainFileRepository = CreatePlainFileRepository(fileSystemAccessor: FileSystemAccessorMock.Object);
        };

        Because of = () => plainFileRepository.RemoveAllBinaryDataForInterview(interviewId);

        It should_interview_folder_be_never_deleted_from_file_system = () =>
         FileSystemAccessorMock.Verify(x => x.DeleteDirectory(Moq.It.Is<string>(name => name.Contains(interviewId.FormatGuid()))), Times.Never);

        private static PlainInterviewFileStorage plainFileRepository;
        private static readonly Mock<IFileSystemAccessor> FileSystemAccessorMock = CreateIFileSystemAccessorMock();
        private static Guid interviewId = Guid.NewGuid();
    }
}
