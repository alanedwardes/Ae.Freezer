using Ae.Freezer.Writers;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Ae.Freezer
{
    /// <summary>
    /// Describes configuration to use with a <see cref="IFreezer"/> instance.
    /// </summary>
    public interface IFreezerConfiguration
    {
        /// <summary>
        /// The name of the <see cref="HttpClient"/> to retreive from the <see cref="IHttpClientFactory"/>.
        /// </summary>
        string HttpClientName { get; }
        /// <summary>
        /// The path to start at, defaults to "/"
        /// </summary>
        Uri StartPath { get; }
        /// <summary>
        /// The regex to use to figure out links from the HTML content.
        /// </summary>
        Regex ResourceRegex { get; }
        /// <summary>
        /// The mime types of content to be treated as text.
        /// </summary>
        ISet<string> TextMimeTypes { get; }
        /// <summary>
        /// The instance of <see cref="IWebsiteResourceWriter"/> to use to "freeze" the found resources.
        /// </summary>
        Func<IServiceProvider, IWebsiteResourceWriter> ResourceWriter { get; }
        /// <summary>
        /// A set of additional relative URIs of resources to add into the crawl.
        /// </summary>
        ISet<Uri> AdditionalResources { get; }
        /// <summary>
        /// The URI of the "not found" page to use. Defaults to "errors/404".
        /// </summary>
        Uri NotFoundPage { get; }
        /// <summary>
        /// If true, don't strip query strings from found URIs. Disallowed by default since
        /// storage providers like Amazon S3 don't work well with the ? and &amp; characters.
        /// </summary>
        public bool AllowQueryString { get; }
    }
}