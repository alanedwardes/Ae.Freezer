using Ae.Freezer.Entities;
using Ae.Freezer.Writers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Freezer.Internal
{
    internal sealed class Freezer : IFreezer
    {
        private readonly ILogger<Freezer> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILinkFinder _linkFinder;

        public Freezer(ILogger<Freezer> logger, IHttpClientFactory httpClientFactory, IServiceProvider serviceProvider, ILinkFinder linkFinder)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _serviceProvider = serviceProvider;
            _linkFinder = linkFinder;
        }

        public async Task Freeze(IFreezerConfiguration freezerConfiguration, CancellationToken token)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = freezerConfiguration.BaseAddress;

            var resourceWriter = freezerConfiguration.ResourceWriter(_serviceProvider);

            var startResource = await httpClient.GetWebsiteResource(freezerConfiguration.StartPath, freezerConfiguration);

            var resources = new Dictionary<Uri, WebsiteResource> { { startResource.RelativeUri, startResource } };

            foreach (var additionalResource in freezerConfiguration.AdditionalResources)
            {
                var resource = await httpClient.GetWebsiteResource(additionalResource, freezerConfiguration);
                if (resource.ResponseMessage.IsSuccessStatusCode)
                {
                    resources.Add(additionalResource, resource);
                    await resourceWriter.WriteResource(resource, token);
                }
            }

            if (freezerConfiguration.NotFoundPage != null)
            {
                var resource = await httpClient.GetWebsiteResource(freezerConfiguration.NotFoundPage, freezerConfiguration);
                resources.Add(freezerConfiguration.NotFoundPage, resource);
                await resourceWriter.WriteResource(resource, token);
            }

            await resourceWriter.WriteResource(startResource, token);

            await FindResourcesRecursive(httpClient, resourceWriter, startResource, freezerConfiguration, resources, token);

            await resourceWriter.FlushResources(resources.Keys, token);

            foreach (var resource in resources)
            {
                resource.Value.Dispose();
            }
        }

        private async Task FindResourcesRecursive(HttpClient httpClient, IWebsiteResourceWriter resourceWriter, WebsiteResource websiteResource, IFreezerConfiguration freezerConfiguration, IDictionary<Uri, WebsiteResource> resources, CancellationToken token)
        {
            foreach (var uri in _linkFinder.GetUrisFromLinks(freezerConfiguration.BaseAddress, websiteResource.TextContent))
            {
                if (resources.ContainsKey(uri))
                {
                    continue;
                }

                var resource = await httpClient.GetWebsiteResource(uri, freezerConfiguration);
                if (!resource.ResponseMessage.IsSuccessStatusCode)
                {
                    _logger.LogError("Resource {RelativeUri} responded with code {StatusCode}", resource.RelativeUri, resource.ResponseMessage.StatusCode);
                    continue;
                }

                resources.Add(uri, resource);

                if (resource.ResourceType == WebsiteResourceType.Text)
                {
                    await FindResourcesRecursive(httpClient, resourceWriter, resource, freezerConfiguration, resources, token);
                }

                await resourceWriter.WriteResource(resource, token);
            }
        }
    }
}
