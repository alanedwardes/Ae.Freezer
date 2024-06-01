using Ae.Freezer.Aws.Internal;
using Ae.Freezer.Entities;
using Ae.Freezer.Writers;
using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<AmazonLambdaAtEdgeResourceWriter> _logger;
        private readonly AmazonLambdaAtEdgeResourceWriterConfiguration _configuration;
        private readonly IAmazonLambda _amazonLambda;
        private readonly IAmazonCloudFront _amazonCloudFront;
        private readonly IAmazonIdentityManagementService _identityManagementService;

        /// <summary>
        /// Construct a new <see cref="AmazonLambdaAtEdgeResourceWriter"/> using the specified <see cref="AmazonLambdaAtEdgeResourceWriterConfiguration"/>, <see cref="IAmazonLambda"/> client and <see cref="IAmazonCloudFront"/> client.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        /// <param name="amazonLambda"></param>
        /// <param name="amazonCloudFront"></param>
        /// <param name="identityManagementService"></param>
        public AmazonLambdaAtEdgeResourceWriter(ILogger<AmazonLambdaAtEdgeResourceWriter> logger, AmazonLambdaAtEdgeResourceWriterConfiguration configuration, IAmazonLambda amazonLambda, IAmazonCloudFront amazonCloudFront, IAmazonIdentityManagementService identityManagementService)
        {
            _logger = logger;
            _configuration = configuration;
            _amazonLambda = amazonLambda;
            _amazonCloudFront = amazonCloudFront;
            _identityManagementService = identityManagementService;
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
            var relativeUri = websiteResource.RelativeUri;
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
        public async Task FinishResources(IReadOnlyCollection<string> resources, CancellationToken token)
        {
            _archive.Dispose();
            _archiveStream.Position = 0;

            string functionArn = await CreateOrUpdateFunctionResource();

            // Get the current config of the distribution
            var distributionConfig = await _amazonCloudFront.GetDistributionConfigAsync(new GetDistributionConfigRequest(_configuration.DistributionId), token);

            // Find the existing Lambda@Edge function association
            LambdaFunctionAssociation functionAssociation = GetLambdaFunctionAssociation(distributionConfig.DistributionConfig);

            // Update its ARN to the newly published version
            functionAssociation.LambdaFunctionARN = functionArn;

            // Update the config of the distribution
            await _amazonCloudFront.UpdateDistributionAsync(new UpdateDistributionRequest
            {
                DistributionConfig = distributionConfig.DistributionConfig,
                Id = _configuration.DistributionId,
                IfMatch = distributionConfig.ETag
            });
        }

        private async Task<string> CreateOrUpdateLambdaRole()
        {
            try
            {
                return (await _identityManagementService.CreateRoleAsync(new CreateRoleRequest
                {
                    RoleName = _configuration.LambdaName,
                    AssumeRolePolicyDocument = @"{""Version"":""2012-10-17"",""Statement"":[" +
                        @"{""Effect"":""Allow"",""Principal"":{""Service"":""lambda.amazonaws.com""},""Action"":""sts:AssumeRole""}," +
                        @"{""Effect"": ""Allow"",""Principal"":{""Service"":""edgelambda.amazonaws.com""},""Action"":""sts:AssumeRole""}" +
                        "]}"
                })).Role.Arn;
            }
            catch (Amazon.IdentityManagement.Model.EntityAlreadyExistsException)
            {
                return (await _identityManagementService.GetRoleAsync(new GetRoleRequest { RoleName = _configuration.LambdaName })).Role.Arn;
            }
        }

        private async Task<string> CreateOrUpdateFunctionResource()
        {
            // Update the Lambda@Edge function and deploy a new version
            string functionArn;

            try
            {
                _logger.LogInformation("Attempting to update Lambda function {FunctionName}", _configuration.LambdaName);

                functionArn = (await _amazonLambda.UpdateFunctionCodeAsync(new UpdateFunctionCodeRequest
                {
                    ZipFile = _archiveStream,
                    FunctionName = _configuration.LambdaName,
                    Publish = true
                })).FunctionArn;
            }
            catch (ResourceNotFoundException)
            {
                _logger.LogInformation("Lambda function {FunctionName} does not exist, creating", _configuration.LambdaName);

                functionArn = (await _amazonLambda.CreateFunctionAsync(new Amazon.Lambda.Model.CreateFunctionRequest
                {
                    Code = new FunctionCode { ZipFile = _archiveStream },
                    FunctionName = _configuration.LambdaName,
                    Publish = true,
                    Handler = "index.handler",
                    Runtime = Runtime.Nodejs18X,
                    Architectures = new List<string> { "x86_64" },
                    MemorySize = 128,
                    Timeout = 3,
                    PackageType = PackageType.Zip,
                    Role = await CreateOrUpdateLambdaRole()
                })).FunctionArn + ":1";
            }

            FunctionConfiguration functionConfiguration;
            do
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                functionConfiguration = (await _amazonLambda.GetFunctionAsync(functionArn)).Configuration;
                _logger.LogInformation("Lambda function {FunctionName} has state {State}", functionArn, functionConfiguration.State);
            }
            while (functionConfiguration.State == State.Pending);

            return functionArn;
        }

        private LambdaFunctionAssociation GetLambdaFunctionAssociation(DistributionConfig distributionConfig)
        {
            LambdaFunctionAssociations functionAssociations;
            if (_configuration.CacheBehaviourId == null)
            {
                functionAssociations = distributionConfig.DefaultCacheBehavior.LambdaFunctionAssociations;
            }
            else
            {
                var cacheBehavior = distributionConfig.CacheBehaviors.Items.SingleOrDefault(x => x.CachePolicyId == _configuration.CacheBehaviourId);
                if (cacheBehavior == null)
                {
                    throw new InvalidOperationException($"Unable to find cache behaviour with ID {_configuration.CacheBehaviourId}");
                }

                functionAssociations = cacheBehavior.LambdaFunctionAssociations;
            }

            var functionAssociation = functionAssociations.Items.SingleOrDefault(x => x.EventType == _configuration.LambdaEventType);
            if (functionAssociation == null)
            {
                functionAssociation = new LambdaFunctionAssociation { EventType = EventType.OriginRequest };
                functionAssociations.Items.Add(functionAssociation);
                functionAssociations.Quantity++;
            }

            return functionAssociation;
        }
    }
}
