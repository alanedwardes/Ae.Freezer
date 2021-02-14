using Ae.Freezer.Entities;
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

        public static async Task GetWebsiteResource(this HttpClient httpClient, WebsiteResource websiteResource, IFreezerConfiguration freezerConfiguration)
        {
            var response = await httpClient.GetAsync(websiteResource.RelativeUri);
            var resourceType = response.Content.GetResourceType(freezerConfiguration.TextMimeTypes);
            websiteResource.ResourceType = resourceType;
            websiteResource.ResponseMessage = response;
            websiteResource.TextContent = resourceType != WebsiteResourceType.Text ? null : await response.Content.ReadAsStringAsync();
        }
    }
}
