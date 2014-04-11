﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Machine.Specifications;
using Moq;
using WB.Core.GenericSubdomains.Utils;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.SharedKernels.SurveyManagement.Implementation.Repositories;
using WB.Core.SharedKernels.SurveyManagement.Views.PreloadedData;
using It = Machine.Specifications.It;

namespace WB.Core.SharedKernels.SurveyManagement.Tests.FilebasedPreloadedDataRepositoryTests
{
    internal class when_preloaded_data_folder_is_missing_GetPreloadedData_is_called : FilebasedPreloadedDataRepositoryTestContext
    {
        private Establish context = () =>
        {
            fileSystemAccessor = CreateIFileSystemAccessorMock();
            fileSystemAccessor.Setup(x => x.OpenOrCreateFile(Moq.It.IsAny<string>(), Moq.It.IsAny<bool>())).Returns(CreateStream());
            filebasedPreloadedDataRepository = CreateFilebasedPreloadedDataRepository(fileSystemAccessor.Object);
        };

        Because of = () => result = filebasedPreloadedDataRepository.GetPreloadedData(Guid.NewGuid());

        It should_result_has_0_elements = () =>
            result.Length.ShouldEqual(0);

        private static Mock<IFileSystemAccessor> fileSystemAccessor;
        private static FilebasedPreloadedDataRepository filebasedPreloadedDataRepository;
        private static PreloadedDataByFile[] result;
    }
}
