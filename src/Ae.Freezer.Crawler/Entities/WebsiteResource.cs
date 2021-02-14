using System;
using System.Net.Http;

namespace Ae.Freezer.Crawler.Entities
{
    public sealed class WebsiteResource : IDisposable
    {
        public WebsiteResourceType ResourceType { get; set; }
        public Uri RelativeUri { get; set; }
        public string TextContent { get; set; }
        public HttpResponseMessage ResponseMessage { get; set; }
        public void Dispose() => ResponseMessage.Dispose();
    }
}
