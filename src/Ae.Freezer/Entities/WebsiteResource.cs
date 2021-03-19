using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Ae.Freezer.Entities
{
    public sealed class WebsiteResource : IDisposable
    {
        public WebsiteResource(Uri relativeUri) => RelativeUri = relativeUri;
        public WebsiteResourceType ResourceType { get; set; }
        public Uri RelativeUri { get; }
        public string TextContent { get; set; }
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public HttpResponseMessage ResponseMessage { get; set; }
        public async Task<Stream> ReadAsStream()
        {
            if (ResourceType == WebsiteResourceType.Text)
            {
                return new MemoryStream(Encoding.UTF8.GetBytes(TextContent));
            }

            return await ResponseMessage.Content.ReadAsStreamAsync();
        }
        public void Dispose() => ResponseMessage.Dispose();
    }
}
