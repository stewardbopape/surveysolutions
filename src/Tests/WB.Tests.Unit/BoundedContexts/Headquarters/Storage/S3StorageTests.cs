﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Moq;
using NUnit.Framework;
using WB.Core.BoundedContexts.Headquarters.Storage.AmazonS3;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Tests.Abc;

namespace WB.Tests.Unit.BoundedContexts.Headquarters.Storage
{
   [TestFixture]
    public class S3FileStorageTests
    {
        private S3FileStorage storage;
        private AmazonS3Settings settings;
        private Mock<IAmazonS3> client;
        private Mock<ITransferUtility> transferUtility;

        [SetUp]
        public void SetUp()
        {
            this.settings = new AmazonS3Settings
            {
                BucketName = "test",
                Endpoint = "http://localhost",
                Folder = "base",
                Prefix = "fiji"
            };

            this.client = new Mock<IAmazonS3>();
            this.transferUtility = new Mock<ITransferUtility>();

            this.storage = Create.Storage.S3FileStorage(settings, client.Object, transferUtility.Object, Mock.Of<ILoggerProvider>(l => l.GetForType(It.IsAny<Type>()) == Mock.Of<ILogger>()));
        }

        [Test]
        public async Task should_provide_proper_prefix_to_key_when_get_binary()
        {
            client.Setup(c => c.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetObjectResponse()
                {
                    ResponseStream = new MemoryStream()
                });

            await this.storage.GetBinaryAsync("somePath");

            var expectedKey = this.settings.BasePath + "/somePath";

            client.Verify(c => c.GetObjectAsync(
                It.Is<GetObjectRequest>(r =>
                    r.BucketName == settings.BucketName
                    && r.Key == expectedKey), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task should_not_throw_if_key_no_found_when_get_binary()
        {
            client.Setup(c => c.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AmazonS3Exception("Error", ErrorType.Sender, "NoSuchKey", "", HttpStatusCode.NotFound));

            var binary = await this.storage.GetBinaryAsync("somePath");
            Assert.That(binary, Is.Null);
        }

        [Test]
        public async Task should_provide_proper_prefix_to_key_when_get_list()
        {
            client.Setup(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListObjectsV2Response
                {
                    S3Objects = new List<S3Object>()
                });

            await this.storage.ListAsync("somePath");

            var expectedKey = this.settings.BasePath + "/somePath";

            client.Verify(c => c.ListObjectsV2Async(
                It.Is<ListObjectsV2Request>(r =>
                    r.BucketName == settings.BucketName
                    && r.Prefix == expectedKey), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task should_return_objects_with_normalized_keys()
        {
            client.Setup(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListObjectsV2Response
                {
                    S3Objects = new List<S3Object>
                    {
                        new S3Object{Key = this.settings.BasePath + "/one"},
                        new S3Object{Key = this.settings.BasePath + "/two"}
                    }
                });

            var result = await this.storage.ListAsync("");

            Assert.That(result, Has.One.Property(nameof(FileObject.Path)).EqualTo("one"));
            Assert.That(result, Has.One.Property(nameof(FileObject.Path)).EqualTo("two"));
        }

        [Test]
        public void should_provide_proper_prefix_to_key_when_get_direct_link()
        {
            client.Setup(c => c.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>()))
                .Returns("link");

            this.storage.GetDirectLink("somePath", TimeSpan.Zero);

            var expectedKey = this.settings.BasePath + "/somePath";

            client.Verify(c => c.GetPreSignedURL(
                It.Is<GetPreSignedUrlRequest>(r =>
                    r.BucketName == settings.BucketName
                    && r.Key == expectedKey)), Times.Once);
        }

        [TestCase("https://localhost", Protocol.HTTPS)]
        [TestCase("http://localhost", Protocol.HTTP)]
        [TestCase("", Protocol.HTTPS)]
        [TestCase(null, Protocol.HTTPS)]
        public void should_request_proper_protocol_when_get_direct_link_for(string s3ServiceEndpoint, Protocol expectedProtocol)
        {
            settings.Endpoint = s3ServiceEndpoint;

            client.Setup(c => c.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>())).Returns("link");

            this.storage.GetDirectLink("somePath", TimeSpan.Zero);

             client.Verify(c => c.GetPreSignedURL(It.Is<GetPreSignedUrlRequest>(r => r.Protocol == expectedProtocol)), Times.Once);
        }

        [Test]
        public void should_use_proper_keys_for_upload()
        {
            transferUtility.Setup(tu => tu.Upload(It.IsAny<TransferUtilityUploadRequest>()));

            this.storage.Store("somekey", new byte[]{1,2,3,4,5}, String.Empty, null);

            transferUtility.Verify(tu => tu.Upload(It.Is<TransferUtilityUploadRequest>(
                tr => tr.BucketName == settings.BucketName && tr.Key == this.settings.BasePath + "/somekey")), Times.Once);
        }

        [Test]
        public async Task should_use_proper_key_for_deletion()
        {
            client.Setup(c => c.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeleteObjectResponse());

            await this.storage.RemoveAsync("somePath");

            client.Verify(c => c.DeleteObjectAsync(settings.BucketName, settings.BasePath + "/somePath", It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
