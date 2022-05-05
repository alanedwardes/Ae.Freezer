using Ae.Freezer.Aws.Internal;
using Ae.Freezer.Entities;
using Ae.Freezer.Writers;
using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Freezer.Aws
{
    /// <summary>
    /// Describes a <see cref="IWebsiteResourceWriter"/> implementation which writes a Node.JS Lambda@Edge function and publishes it to a CloudFront distribution containing all <see cref="WebsiteResource"/> instances.
    /// </summary>
    public sealed class AmazonLambdaAtEdgeResourceWriter : IWebsiteResourceWriter
    {
        private ZipArchive _archive;
        private readonly MemoryStream _archiveStream = new MemoryStream();
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly AmazonLambdaAtEdgeResourceWriterConfiguration _configuration;
        private readonly IAmazonLambda _amazonLambda;
        private readonly IAmazonCloudFront _amazonCloudFront;

        /// <summary>
        /// Construct a new <see cref="AmazonLambdaAtEdgeResourceWriter"/> using the specified <see cref="AmazonLambdaAtEdgeResourceWriterConfiguration"/>, <see cref="IAmazonLambda"/> client and <see cref="IAmazonCloudFront"/> client.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="amazonLambda"></param>
        /// <param name="amazonCloudFront"></param>
        public AmazonLambdaAtEdgeResourceWriter(AmazonLambdaAtEdgeResourceWriterConfiguration configuration, IAmazonLambda amazonLambda, IAmazonCloudFront amazonCloudFront)
        {
            _configuration = configuration;
            _amazonLambda = amazonLambda;
            _amazonCloudFront = amazonCloudFront;
        }

        private static Stream GetLambdaFunctionCode()
        {
            var assembly = typeof(AmazonLambdaAtEdgeResourceWriter).Assembly;
            var assemblyName = assembly.GetName();
            return assembly.GetManifestResourceStream($"{assemblyName.Name}.AmazonLambdaAtEdgeResourceLambda.js");
        }

        private static Stream CreateZipEntry(ZipArchive archive, string entryName)
        {
            var entry = archive.CreateEntry(entryName);
            entry.ExternalAttributes |= Convert.ToInt32("755", 8) << 16;
            return entry.Open();
        }

        /// <inheritdoc/>
        public async Task PrepareResources()
        {
            _archive = new ZipArchive(_archiveStream, ZipArchiveMode.Create, true);

            using var destination = CreateZipEntry(_archive, "index.js");
            using var source = GetLambdaFunctionCode();
            await source.CopyToAsync(destination);
        }

        /// <inheritdoc/>
        public async Task WriteResource(WebsiteResource websiteResource, CancellationToken token)
        {
            var relativeUri = websiteResource.FoundUri.Uri.OriginalString;
            var zipEntryPath = relativeUri.EndsWith("/") || string.IsNullOrWhiteSpace(relativeUri) ? relativeUri + "index" : relativeUri;

            await _semaphoreSlim.WaitAsync(token);
            try
            {
                using var destination = new MemoryStream();
                using var source = await websiteResource.ReadAsStream();
                await source.CopyToAsync(destination);
                source.Position = 0;

                var response = new AmazonLambdaAtEdgeResponse
                {
                    Body = Convert.ToBase64String(destination.ToArray()),
                    BodyEncoding = "base64",
                    Headers = new Dictionary<string, IList<AmazonLambdaAtEdgeResponse.Header>>
                    {
                        {
                            "Content-Type", new List<AmazonLambdaAtEdgeResponse.Header>
                            {
                                new AmazonLambdaAtEdgeResponse.Header
                                {
                                    Key = "Content-Type",
                                    Value = websiteResource.ResponseMessage.Content.Headers.ContentType.ToString()
                                }
                            }
                        }
                    },
                    Status = (uint)websiteResource.Status
                };

                var json = JsonSerializer.Serialize(response);

                using var zipEntryStream = CreateZipEntry(_archive, $"content/{zipEntryPath}");
                using var writer = new StreamWriter(zipEntryStream);
                writer.Write(json);
            }
            finally
            {
                _semaphoreSlim.Release();
            }

        }

        /// <inheritdoc/>
        public async Task FinishResources(IReadOnlyCollection<Uri> resources, CancellationToken token)
        {
            _archive.Dispose();
            _archiveStream.Position = 0;

            // Update the Lambda@Edge function and deploy a new version
            var update = await _amazonLambda.UpdateFunctionCodeAsync(new UpdateFunctionCodeRequest
            {
                ZipFile = _archiveStream,
                FunctionName = _configuration.LambdaName,
                Publish = true
            });

            // Get the current config of the distribution
            var distributionConfig = await _amazonCloudFront.GetDistributionConfigAsync(new GetDistributionConfigRequest(_configuration.DistributionId), token);

            // Find the existing Lambda@Edge function association
            LambdaFunctionAssociation functionAssociation = GetLambdaFunctionAssociation(distributionConfig.DistributionConfig);

            // Update its ARN to the newly published version
            functionAssociation.LambdaFunctionARN = update.FunctionArn;

            // Update the config of the distribution
            await _amazonCloudFront.UpdateDistributionAsync(new UpdateDistributionRequest
            {
                DistributionConfig = distributionConfig.DistributionConfig,
                Id = _configuration.DistributionId,
                IfMatch = distributionConfig.ETag
            });
        }

        private LambdaFunctionAssociation GetLambdaFunctionAssociation(DistributionConfig distributionConfig)
        {
            IList<LambdaFunctionAssociation> functionAssociations;
            if (_configuration.CacheBehaviourId == null)
            {
                functionAssociations = distributionConfig.DefaultCacheBehavior.LambdaFunctionAssociations.Items;
            }
            else
            {
                var cacheBehavior = distributionConfig.CacheBehaviors.Items.SingleOrDefault(x => x.CachePolicyId == _configuration.CacheBehaviourId);
                if (cacheBehavior == null)
                {
                    throw new InvalidOperationException($"Unable to find cache behaviour with ID {_configuration.CacheBehaviourId}");
                }

                functionAssociations = cacheBehavior.LambdaFunctionAssociations.Items;
            }

            var functionAssociation = functionAssociations.SingleOrDefault(x => x.EventType == _configuration.LambdaEventType);
            if (functionAssociation == null)
            {
                throw new InvalidOperationException($"Unable to find existing Lambda function associated with event type {_configuration.LambdaEventType}");
            }

            return functionAssociation;
        }

        /// <inheritdoc/>
        public void ProcessResource(WebsiteResource websiteResource)
        {
        }
    }
}
