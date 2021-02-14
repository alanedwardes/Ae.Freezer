using Ae.Freezer.Entities;
using Ae.Freezer.Writers;
using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Freezer.Aws
{
    public sealed class AmazonS3WebsiteResourceWriter : IWebsiteResourceWriter
    {
        private readonly ILogger<AmazonS3WebsiteResourceWriter> _logger;
        private readonly IAmazonS3 _amazons3;
        private readonly IAmazonCloudFront _amazonCloudFront;
        private readonly AmazonS3WebsiteResourceWriterConfiguration _configuration;

        public AmazonS3WebsiteResourceWriter(ILogger<AmazonS3WebsiteResourceWriter> logger, IAmazonS3 amazons3, IAmazonCloudFront amazonCloudFront, AmazonS3WebsiteResourceWriterConfiguration configuration)
        {
            _logger = logger;
            _amazons3 = amazons3;
            _amazonCloudFront = amazonCloudFront;
            _configuration = configuration;
        }

        public async Task WriteResource(WebsiteResource websiteResource, CancellationToken token)
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = _configuration.BucketName,
                Key = _configuration.GenerateKey(websiteResource.RelativeUri)
            };

            putRequest.Headers.ContentType = websiteResource.ResponseMessage.Content.Headers.ContentType.ToString();

            _configuration.PutRequestModifier(putRequest);

            if (websiteResource.ResourceType == WebsiteResourceType.Text)
            {
                putRequest.InputStream = new MemoryStream(Encoding.UTF8.GetBytes(websiteResource.TextContent));
            }
            else
            {
                putRequest.InputStream = await websiteResource.ResponseMessage.Content.ReadAsStreamAsync();
            }

            await _amazons3.PutObjectAsync(putRequest, token);
        }

        public async Task FlushResources(IReadOnlyCollection<Uri> resources, CancellationToken token)
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

            if (_configuration.ShouldInvalidateCloudFrontCache)
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
