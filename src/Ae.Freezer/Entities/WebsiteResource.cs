using System;
using System.Net.Http;

namespace Ae.Freezer.Entities
{
    public sealed class WebsiteResource : IDisposable
    {
        public WebsiteResource(Uri relativeUri) => RelativeUri = relativeUri;
        public WebsiteResourceType ResourceType { get; set; }
        public Uri RelativeUri { get; }
        public string TextContent { get; set; }
        public HttpResponseMessage ResponseMessage { get; set; }
        public void Dispose() => ResponseMessage.Dispose();
    }
}
