using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Ae.Freezer.Entities
{
    /// <summary>
    /// Describes a static website resource.
    /// </summary>
    public sealed class WebsiteResource : IDisposable
    {
        /// <summary>
        /// Construct a new <see cref="WebsiteResource"/> using the specified relative <see cref="Uri"/>.
        /// </summary>
        /// <param name="relativeUri"></param>
        public WebsiteResource(Uri relativeUri) => RelativeUri = relativeUri;
        /// <summary>
        /// The type of website resource this instance represents.
        /// </summary>
        public WebsiteResourceType ResourceType { get; set; }
        /// <summary>
        /// The relative URI of this resource.
        /// </summary>
        public Uri RelativeUri { get; }
        /// <summary>
        /// If the <see cref="ResourceType"/> is <see cref="WebsiteResourceType.Text"/>, the content of this website resource.
        /// </summary>
        public string TextContent { get; set; }
        /// <summary>
        /// The response code to use when serving the resource. May differ from the status code contained in <see cref="ResponseMessage"/>.
        /// </summary>
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        /// <summary>
        /// The response message obtained when crawling the resource.
        /// </summary>
        public HttpResponseMessage ResponseMessage { get; set; }
        /// <summary>
        /// Read the content of this <see cref="WebsiteResource"/> as a <see cref="Stream"/>.
        /// </summary>
        /// <returns></returns>
        public async Task<Stream> ReadAsStream()
        {
            if (ResourceType == WebsiteResourceType.Text)
            {
                return new MemoryStream(Encoding.UTF8.GetBytes(TextContent));
            }

            return await ResponseMessage.Content.ReadAsStreamAsync();
        }
        /// <inheritdoc/>
        public void Dispose() => ResponseMessage.Dispose();
    }
}
