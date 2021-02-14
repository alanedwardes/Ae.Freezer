using Ae.Freezer.Entities;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Freezer.Internal
{
    internal static class HttpResponseMessageExtensions
    {
        public static async Task GetWebsiteResource(this HttpClient httpClient, WebsiteResource websiteResource, IFreezerConfiguration freezerConfiguration, CancellationToken token)
        {
            var response = await httpClient.GetAsync(websiteResource.RelativeUri, token);
            websiteResource.ResponseMessage = response;
            websiteResource.ResourceType = freezerConfiguration.TextMimeTypes.Contains(response.Content.Headers.ContentType.MediaType) ? WebsiteResourceType.Text : WebsiteResourceType.Binary;
            websiteResource.TextContent = websiteResource.ResourceType != WebsiteResourceType.Text ? null : await response.Content.ReadAsStringAsync();
        }
    }
}
