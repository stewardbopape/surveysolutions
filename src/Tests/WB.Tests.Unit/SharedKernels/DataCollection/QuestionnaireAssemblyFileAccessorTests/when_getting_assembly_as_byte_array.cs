﻿using System;
using Machine.Specifications;
using Moq;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.SharedKernels.DataCollection.Accessors;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.DataCollection.QuestionnaireAssemblyFileAccessorTests
{
    internal class when_getting_assembly_as_byte_array : QuestionnaireAssemblyFileAccessorTestsContext
    {
        Establish context = () =>
        {
            FileSystemAccessorMock.Setup(x => x.ReadAllBytes(Moq.It.IsAny<string>())).Returns(data1);
            FileSystemAccessorMock.Setup(x => x.IsFileExists(Moq.It.IsAny<string>())).Returns(true);
            questionnaireAssemblyFileAccessor = CreateQuestionnaireAssemblyFileAccessor(fileSystemAccessor: FileSystemAccessorMock.Object);
        };

        Because of = () => result = questionnaireAssemblyFileAccessor.GetAssemblyAsByteArray(questionnaireId, version);

        It should_data_of_returned_file_be_equal_to_data1 = () =>
            result.ShouldEqual(data1);

        private static QuestionnaireAssemblyFileAccessor questionnaireAssemblyFileAccessor;
        private static readonly Mock<IFileSystemAccessor> FileSystemAccessorMock = CreateIFileSystemAccessorMock();
        private static Guid questionnaireId = Guid.Parse("33332222111100000000111122223333");
        private static long version = 3;

        private static byte[] data1 = new byte[] { 1 };

        private static byte[] result;
    }
}
