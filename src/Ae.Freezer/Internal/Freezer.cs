using Ae.Freezer.Entities;
using Ae.Freezer.Writers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

            await resourceWriter.PrepareResources();

            var resources = new ConcurrentDictionary<Uri, WebsiteResource>();

            var tasks = new List<Task>
            {
                GetWebsiteResource(httpClient, resourceWriter, resources, freezerConfiguration.NotFoundPage, freezerConfiguration, HttpStatusCode.NotFound, token)
            };

            foreach (var additionalResource in freezerConfiguration.AdditionalResources)
            {
                tasks.Add(GetWebsiteResource(httpClient, resourceWriter, resources, additionalResource, freezerConfiguration, null, token));
            }

            tasks.Add(FindResourcesRecursive(httpClient, resourceWriter, freezerConfiguration.StartPath, freezerConfiguration, resources, token));

            await Task.WhenAll(tasks);

            foreach (var resource in resources)
            {
                resource.Value.Dispose();
            }

            await resourceWriter.FinishResources(resources.Select(x => x.Key).ToArray(), token);
        }

        private async Task FindResourcesRecursive(HttpClient httpClient, IWebsiteResourceWriter resourceWriter, Uri startUri, IFreezerConfiguration freezerConfiguration, IDictionary<Uri, WebsiteResource> resources, CancellationToken token)
        {
            var startResource = await GetWebsiteResource(httpClient, resourceWriter, resources, startUri, freezerConfiguration, null, token);
            if (startResource == null || startResource.ResourceType != WebsiteResourceType.Text)
            {
                return;
            }

            await Task.WhenAll(_linkFinder.GetUrisFromLinks(freezerConfiguration.BaseAddress, startResource.TextContent).Select(uri => FindResourcesRecursive(httpClient, resourceWriter, uri, freezerConfiguration, resources, token)));
        }

        private async Task<WebsiteResource> GetWebsiteResource(HttpClient httpClient, IWebsiteResourceWriter resourceWriter, IDictionary<Uri, WebsiteResource> resources, Uri uri, IFreezerConfiguration freezerConfiguration, HttpStatusCode? expectedStatusCode, CancellationToken token)
        {
            var resource = new WebsiteResource(uri);
            if (!resources.TryAdd(resource.RelativeUri, resource))
            {
                return null;
            }

            resource.Status = expectedStatusCode ?? HttpStatusCode.OK;

            await httpClient.GetWebsiteResource(resource, freezerConfiguration, token);
            if (resource.ResponseMessage.StatusCode != resource.Status)
            {
                _logger.LogCritical("Resource {RelativeUri} responded with code {StatusCode}", resource.RelativeUri, resource.ResponseMessage.StatusCode);
                return null;
            }

            await resourceWriter.WriteResource(resource, token);
            return resource;
        }
    }
}
