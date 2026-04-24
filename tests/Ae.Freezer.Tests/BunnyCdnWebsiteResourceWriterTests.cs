using Ae.Freezer.BunnyCdn;
using Ae.Freezer.Entities;
using BunnyCDN.Net.Storage.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ae.Freezer.Tests
{
    public class BunnyCdnWebsiteResourceWriterTests
    {
        private readonly Mock<ILogger<BunnyCdnWebsiteResourceWriter>> _loggerMock;
        private readonly Mock<IBunnyCdnStorage> _storageMock;
        private readonly BunnyCdnWebsiteResourceWriterConfiguration _config;

        public BunnyCdnWebsiteResourceWriterTests()
        {
            _loggerMock = new Mock<ILogger<BunnyCdnWebsiteResourceWriter>>();
            _storageMock = new Mock<IBunnyCdnStorage>();
            _config = new BunnyCdnWebsiteResourceWriterConfiguration
            {
                StorageZoneName = "test-zone",
                ShouldCleanUnmatchedObjects = false
            };
        }

        [Theory]
        [InlineData("", "/test-zone/index.html")]
        [InlineData("about/", "/test-zone/about/index.html")]
        [InlineData("/about/", "/test-zone/about/index.html")]
        [InlineData("css/style.css", "/test-zone/css/style.css")]
        [InlineData("/css/style.css", "/test-zone/css/style.css")]
        public async Task WriteResource_WithDefaultKeyGenerator_CreatesCorrectPath(string relativeUri, string expectedPath)
        {
            // Arrange
            var writer = new BunnyCdnWebsiteResourceWriter(_loggerMock.Object, _config, _storageMock.Object);
            var resource = CreateResource(relativeUri, "test content");

            string actualPath = null;
            _storageMock.Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>()))
                .Callback<Stream, string>((_, path) => actualPath = path)
                .Returns(Task.CompletedTask);

            // Act
            await writer.WriteResource(resource, CancellationToken.None);

            // Assert
            Assert.Equal(expectedPath, actualPath);
            _storageMock.Verify(x => x.UploadAsync(It.IsAny<Stream>(), expectedPath), Times.Once);
        }

        [Fact]
        public async Task FinishResources_WithDefaultKeyGenerator_CleansUnmatchedObjects()
        {
            // Arrange
            _config.ShouldCleanUnmatchedObjects = true;

            var writer = new BunnyCdnWebsiteResourceWriter(_loggerMock.Object, _config, _storageMock.Object);

            // BunnyCDN returns these objects
            _storageMock.Setup(x => x.GetStorageObjectsAsync("/test-zone/"))
                .ReturnsAsync(new List<StorageObject>
                {
                    new StorageObject { ObjectName = "index.html", IsDirectory = false },
                    new StorageObject { ObjectName = "old-file.html", IsDirectory = false },
                    new StorageObject { ObjectName = "about/index.html", IsDirectory = false }
                });

            _storageMock.Setup(x => x.DeleteObjectAsync("/test-zone/old-file.html"))
                .ReturnsAsync(true);

            // Resources being written
            var resources = new List<string> { "", "about/" };

            // Act
            await writer.FinishResources(resources, CancellationToken.None);

            // Assert - old-file.html should be deleted because it wasn't in the written keys
            _storageMock.Verify(x => x.DeleteObjectAsync("/test-zone/old-file.html"), Times.Once);
            _storageMock.Verify(x => x.DeleteObjectAsync("/test-zone/index.html"), Times.Never);
            _storageMock.Verify(x => x.DeleteObjectAsync("/test-zone/about/index.html"), Times.Never);
        }

        [Theory]
        [InlineData("", "index.html")]
        [InlineData("about/", "about/index.html")]
        [InlineData("/about/", "about/index.html")]
        [InlineData("css/style.css", "css/style.css")]
        [InlineData("/css/style.css", "css/style.css")]
        public void DefaultKeyGenerator_TrimsLeadingSlashes(string input, string expected)
        {
            // Act
            var result = BunnyCdnWebsiteResourceWriterConfiguration.DefaultKeyGenerator(input);

            // Assert
            Assert.Equal(expected, result);
            Assert.False(result.StartsWith("/"), "Result should not start with a leading slash");
        }

        private static WebsiteResource CreateResource(string relativeUri, string content)
        {
            return new WebsiteResource(relativeUri)
            {
                ResourceType = WebsiteResourceType.Text,
                TextContent = content,
                Status = HttpStatusCode.OK
            };
        }
    }
}
