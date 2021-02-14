using Ae.Freezer.Entities;
using Ae.Freezer.Writers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

            var startResource = new WebsiteResource { RelativeUri = freezerConfiguration.StartPath };
            await httpClient.GetWebsiteResource(startResource, freezerConfiguration);

            var resources = new ConcurrentDictionary<Uri, WebsiteResource>();
            resources.TryAdd(startResource.RelativeUri, startResource);

            foreach (var additionalResource in freezerConfiguration.AdditionalResources)
            {
                var resource = new WebsiteResource { RelativeUri = additionalResource };
                await httpClient.GetWebsiteResource(resource, freezerConfiguration);
                if (resource.ResponseMessage.IsSuccessStatusCode)
                {
                    resources.TryAdd(additionalResource, resource);
                    await resourceWriter.WriteResource(resource, token);
                }
            }

            if (freezerConfiguration.NotFoundPage != null)
            {
                var resource = new WebsiteResource { RelativeUri = freezerConfiguration.NotFoundPage };
                await httpClient.GetWebsiteResource(resource, freezerConfiguration);
                resources.TryAdd(freezerConfiguration.NotFoundPage, resource);
                await resourceWriter.WriteResource(resource, token);
            }

            await resourceWriter.WriteResource(startResource, token);

            var tasks = new List<Task>();
            await FindResourcesRecursive(httpClient, resourceWriter, startResource.TextContent, freezerConfiguration, resources, tasks, token);
            await Task.WhenAll(tasks);

            await resourceWriter.FlushResources(resources.Select(x => x.Key).ToArray(), token);

            foreach (var resource in resources)
            {
                resource.Value.Dispose();
            }
        }

        private async Task FindResourcesRecursive(HttpClient httpClient, IWebsiteResourceWriter resourceWriter, string textContent, IFreezerConfiguration freezerConfiguration, IDictionary<Uri, WebsiteResource> resources, IList<Task> tasks, CancellationToken token)
        {
            foreach (var uri in _linkFinder.GetUrisFromLinks(freezerConfiguration.BaseAddress, textContent))
            {
                var resource = new WebsiteResource { RelativeUri = uri };
                if (!resources.TryAdd(uri, resource))
                {
                    continue;
                }

                await httpClient.GetWebsiteResource(resource, freezerConfiguration);
                if (!resource.ResponseMessage.IsSuccessStatusCode)
                {
                    _logger.LogError("Resource {RelativeUri} responded with code {StatusCode}", resource.RelativeUri, resource.ResponseMessage.StatusCode);
                    continue;
                }

                if (resource.ResourceType == WebsiteResourceType.Text)
                {
                    tasks.Add(FindResourcesRecursive(httpClient, resourceWriter, resource.TextContent, freezerConfiguration, resources, tasks, token));
                }

                await resourceWriter.WriteResource(resource, token);
            }
        }
    }
}
