using Ae.Freezer.Entities;
using Ae.Freezer.Writers;
using BunnyCDN.Net.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Freezer.BunnyCdn
{
    /// <summary>
    /// Describes a <see cref="IWebsiteResourceWriter"/> implementation which writes <see cref="WebsiteResource"/> objects to Bunny CDN using the correct paths.
    /// </summary>
    public sealed class BunnyCdnWebsiteResourceWriter : IWebsiteResourceWriter
    {
        private readonly ILogger<BunnyCdnWebsiteResourceWriter> _logger;
        private readonly IBunnyCdnStorage _bunnyCdnStorage;
        private readonly BunnyCdnWebsiteResourceWriterConfiguration _configuration;

        /// <summary>
        /// Construct a new <see cref="BunnyCdnWebsiteResourceWriter"/> using the specified <see cref="IBunnyCdnStorage"/> client and <see cref="BunnyCdnWebsiteResourceWriterConfiguration"/> object.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        /// <param name="bunnyCdnStorage"></param>
        public BunnyCdnWebsiteResourceWriter(ILogger<BunnyCdnWebsiteResourceWriter> logger, BunnyCdnWebsiteResourceWriterConfiguration configuration, IBunnyCdnStorage bunnyCdnStorage)
        {
            _logger = logger;
            _bunnyCdnStorage = bunnyCdnStorage;
            _configuration = configuration;
        }

        /// <summary>
        /// Construct a new <see cref="BunnyCdnWebsiteResourceWriter"/> using the specified <see cref="BunnyCDNStorage"/> client and <see cref="BunnyCdnWebsiteResourceWriterConfiguration"/> object.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        /// <param name="bunnyCdnStorage"></param>
        public BunnyCdnWebsiteResourceWriter(ILogger<BunnyCdnWebsiteResourceWriter> logger, BunnyCdnWebsiteResourceWriterConfiguration configuration, BunnyCDNStorage bunnyCdnStorage)
            : this(logger, configuration, new BunnyCdnStorageWrapper(bunnyCdnStorage))
        {
        }

        /// <inheritdoc/>
        public async Task WriteResource(WebsiteResource websiteResource, CancellationToken token)
        {
            var key = _configuration.GenerateKey(websiteResource.RelativeUri);
            var path = $"/{_configuration.StorageZoneName}/{key}";

            using var stream = await websiteResource.ReadAsStream();

            // Note: BunnyCDNStorage.UploadAsync does not currently take a CancellationToken.
            await _bunnyCdnStorage.UploadAsync(stream, path);
        }

        /// <inheritdoc/>
        public Task PrepareResources() => Task.CompletedTask;

        /// <inheritdoc/>
        public async Task FinishResources(IReadOnlyCollection<string> resources, CancellationToken token)
        {
            var allWrittenKeys = resources.Select(_configuration.GenerateKey).ToArray();

            if (_configuration.ShouldCleanUnmatchedObjects)
            {
                var objects = await _bunnyCdnStorage.GetStorageObjectsAsync($"/{_configuration.StorageZoneName}/");
                
                var actualKeys = objects.Where(x => !x.IsDirectory).Select(x => x.ObjectName);

                foreach (var unmatchedKey in actualKeys.Except(allWrittenKeys))
                {
                    var unmatchedPath = $"/{_configuration.StorageZoneName}/{unmatchedKey}";
                    _logger.LogInformation("Removing unmatched key {UnmatchedKey} at path {UnmatchedPath}", unmatchedKey, unmatchedPath);
                    await _bunnyCdnStorage.DeleteObjectAsync(unmatchedPath);
                }
            }
        }
    }
}
