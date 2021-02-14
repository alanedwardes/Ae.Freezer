using Ae.Freezer.Entities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ae.Freezer.Internal
{
    internal static class HttpResponseMessageExtensions
    {
        public static WebsiteResourceType GetResourceType(this HttpContent content, ISet<string> textMimeTypes)
        {
            return textMimeTypes.Contains(content.Headers.ContentType.MediaType) ? WebsiteResourceType.Text : WebsiteResourceType.Binary;
        }

        public static async Task<WebsiteResource> ToWebsiteResource(this HttpResponseMessage response, IFreezerConfiguration freezerConfiguration)
        {
            var resourceType = response.Content.GetResourceType(freezerConfiguration.TextMimeTypes);
            return new WebsiteResource
            {
                ResourceType = resourceType,
                RelativeUri = freezerConfiguration.BaseAddress.MakeRelativeUri(response.RequestMessage.RequestUri),
                ResponseMessage = response,
                TextContent = resourceType != WebsiteResourceType.Text ? null : await response.Content.ReadAsStringAsync()
            };
        }

        public static async Task<WebsiteResource> GetWebsiteResource(this HttpClient httpClient, Uri relativeUri, IFreezerConfiguration freezerConfiguration)
        {
            var response = await httpClient.GetAsync(relativeUri);

            return await response.ToWebsiteResource(freezerConfiguration);
        }
    }
}
