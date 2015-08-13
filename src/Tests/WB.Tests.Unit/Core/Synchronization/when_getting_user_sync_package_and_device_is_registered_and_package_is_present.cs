using System;

using Machine.Specifications;

using Moq;

using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernel.Structures.Synchronization.SurveyManagement;
using WB.Core.Synchronization.Documents;
using WB.Core.Synchronization.Implementation.SyncManager;
using WB.Core.Synchronization.SyncStorage;

using It = Machine.Specifications.It;

namespace WB.Tests.Unit.Core.Synchronization
{
    internal class when_getting_user_sync_package_and_device_is_registered_and_package_is_present : SyncManagerTestContext
    {
        Establish context = () =>
        {
            tabletDocument = CreateTabletDocument(deviceId, androidId);
            devices = Mock.Of<IReadSideRepositoryReader<TabletDocument>>(x => x.GetById(deviceId.FormatGuid()) == tabletDocument);

            userPackageStorage = Mock.Of<IQueryableReadSideRepositoryReader<UserSyncPackageMeta>>
                (x => x.GetById(syncedPackageId) == new UserSyncPackageMeta
                {
                    PackageId = syncedPackageId,
                    Content = content
                });

            syncManager = CreateSyncManager(devices: devices, usersReader: userPackageStorage);
        };

        Because of = () =>
            package = syncManager.ReceiveUserSyncPackage(deviceId, syncedPackageId, userId);

        It should_return_not_empty_package = () =>
            package.ShouldNotBeNull();

        It should_return_package_with_PackageId_specified = () =>
            package.PackageId.ShouldEqual(syncedPackageId);

        It should_return_package_with_Content_specified = () =>
            package.Content.ShouldEqual(content);

        private static SyncManager syncManager;
        private static UserSyncPackageDto package;

        private const string androidId = "Android";
        private static Guid deviceId = androidId.ToGuid();
        private static TabletDocument tabletDocument;
        private static IReadSideRepositoryReader<TabletDocument> devices;

        private const string syncedPackageId = "some_sync_package_id";
        private const string content = "some_sync_package_content";
        private static IQueryableReadSideRepositoryReader<UserSyncPackageMeta> userPackageStorage;

        private static Guid userId = Guid.Parse("11111111111111111111111111111111");
    }
}