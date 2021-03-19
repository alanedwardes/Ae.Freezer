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
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Freezer.Aws
{
    internal sealed class AmazonLambdaAtEdgeResponse
    {
        internal sealed class Header
        {
            [JsonPropertyName("key")]
            public string Key { get; set; }
            [JsonPropertyName("value")]
            public string Value { get; set; }
        }

        [JsonPropertyName("status")]
        public uint Status { get; set; }
        [JsonPropertyName("statusDescription")]
        public string StatusDescription { get; set; }
        [JsonPropertyName("headers")]
        public IDictionary<string, IList<Header>> Headers { get; set; } = new Dictionary<string, IList<Header>>();
        [JsonPropertyName("body")]
        public string Body { get; set; }
        [JsonPropertyName("bodyEncoding")]
        public string BodyEncoding { get; set; }
    }

    public sealed class AmazonLambdaAtEdgeResourceWriter : IWebsiteResourceWriter
    {
        private ZipArchive _archive;
        private readonly MemoryStream _archiveStream = new MemoryStream();
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly AmazonLambdaAtEdgeResourceWriterConfiguration _configuration;
        private readonly IAmazonLambda _amazonLambda;
        private readonly IAmazonCloudFront _amazonCloudFront;

        public AmazonLambdaAtEdgeResourceWriter(AmazonLambdaAtEdgeResourceWriterConfiguration configuration, IAmazonLambda amazonLambda, IAmazonCloudFront amazonCloudFront)
        {
            _configuration = configuration;
            _amazonLambda = amazonLambda;
            _amazonCloudFront = amazonCloudFront;
        }

        public Stream GetLambdaFunctionCode()
        {
            var assembly = GetType().Assembly;
            var assemblyName = assembly.GetName();
            return assembly.GetManifestResourceStream($"{assemblyName.Name}.AmazonLambdaAtEdgeResourceLambda.js");
        }

        public async Task PrepareResources()
        {
            _archive = new ZipArchive(_archiveStream, ZipArchiveMode.Create, true);

            var zipEntryBody = _archive.CreateEntry("index.js");
            using var destination = zipEntryBody.Open();
            using var source = GetLambdaFunctionCode();
            await source.CopyToAsync(destination);
        }

        public async Task WriteResource(WebsiteResource websiteResource, CancellationToken token)
        {
            var relativeUri = websiteResource.RelativeUri.ToString();
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

                var zipEntryBody = _archive.CreateEntry($"content/{zipEntryPath}");

                using var writer = new StreamWriter(zipEntryBody.Open());
                writer.Write(json);
            }
            finally
            {
                _semaphoreSlim.Release();
            }

        }

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
    }
}
