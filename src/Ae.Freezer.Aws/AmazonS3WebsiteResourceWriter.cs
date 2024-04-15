using Ae.Freezer.Entities;
using Ae.Freezer.Writers;
using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Freezer.Aws
{
    /// <summary>
    /// Describes a <see cref="IWebsiteResourceWriter"/> implementation which writes <see cref="WebsiteResource"/> objects to Amazon S3 using the correct paths.
    /// </summary>
    public sealed class AmazonS3WebsiteResourceWriter : IWebsiteResourceWriter
    {
        private readonly ILogger<AmazonS3WebsiteResourceWriter> _logger;
        private readonly IAmazonS3 _amazons3;
        private readonly IAmazonCloudFront _amazonCloudFront;
        private readonly AmazonS3WebsiteResourceWriterConfiguration _configuration;

        /// <summary>
        /// Construct a new <see cref="AmazonS3WebsiteResourceWriter"/> using the specified <see cref="IAmazonS3"/> client, <see cref="IAmazonCloudFront"/> client and <see cref="AmazonS3WebsiteResourceWriterConfiguration"/> object.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        /// <param name="amazons3"></param>
        /// <param name="amazonCloudFront"></param>
        public AmazonS3WebsiteResourceWriter(ILogger<AmazonS3WebsiteResourceWriter> logger, AmazonS3WebsiteResourceWriterConfiguration configuration, IAmazonS3 amazons3, IAmazonCloudFront amazonCloudFront)
        {
            _logger = logger;
            _amazons3 = amazons3;
            _amazonCloudFront = amazonCloudFront;
            _configuration = configuration;
        }

        /// <inheritdoc/>
        public async Task WriteResource(WebsiteResource websiteResource, CancellationToken token)
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = _configuration.BucketName,
                Key = _configuration.GenerateKey(websiteResource.RelativeUri),
                InputStream = await websiteResource.ReadAsStream()
            };

            putRequest.Headers.ContentType = websiteResource.ResponseMessage.Content.Headers.ContentType.ToString();

            _configuration.PutRequestModifier?.Invoke(putRequest);

            await _amazons3.PutObjectAsync(putRequest, token);
        }

        /// <inheritdoc/>
        public Task PrepareResources() => Task.CompletedTask;

        /// <inheritdoc/>
        public async Task FinishResources(IReadOnlyCollection<Uri> resources, CancellationToken token)
        {
            var allWrittenKeys = resources.Select(_configuration.GenerateKey).ToArray();

            if (_configuration.ShouldCleanUnmatchedObjects)
            {
                var listRequest = new ListObjectsV2Request { BucketName = _configuration.BucketName };
                var listResponse = await _amazons3.ListObjectsV2Async(listRequest, token);

                var actualKeys = listResponse.S3Objects.Select(x => x.Key);

                foreach (var unmatchedKey in actualKeys.Except(allWrittenKeys))
                {
                    _logger.LogInformation("Removing unmatched key {UnmatchedKey}", unmatchedKey);
                    await _amazons3.DeleteAsync(_configuration.BucketName, unmatchedKey, new Dictionary<string, object>(), token);
                }
            }

            if (_configuration.DistributionId != null)
            {
                var invalidationRequest = new CreateInvalidationRequest
                {
                    DistributionId = _configuration.DistributionId,
                    InvalidationBatch = new InvalidationBatch
                    {
                        CallerReference = Guid.NewGuid().ToString(),
                        Paths = new Paths
                        {
                            Items = allWrittenKeys.Select(x => '/' + x).ToList(),
                            Quantity = allWrittenKeys.Length
                        }
                    }
                };

                await _amazonCloudFront.CreateInvalidationAsync(invalidationRequest, token);
            }
        }
    }
}
