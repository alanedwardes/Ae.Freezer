using Ae.Freezer.Writers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ae.Freezer
{
    /// <inheritdoc/>
    public sealed class FreezerConfiguration : IFreezerConfiguration
    {
        /// <inheritdoc/>
        public string HttpClientName { get; set; } = string.Empty;
        /// <inheritdoc/>
        public string StartPath { get; set; } = string.Empty;
        /// <inheritdoc/>
        public Regex ResourceRegex { get; set; } = new Regex("(href|src)=\"(?<uri>.+?)\"");
        /// <inheritdoc/>
        public ISet<string> TextMimeTypes { get; set; } = new HashSet<string> { "text/html", "text/xml", "application/rss+xml", "text/css" };
        /// <inheritdoc/>
        public Func<IServiceProvider, IWebsiteResourceWriter> ResourceWriter { get; set; } = x => ActivatorUtilities.CreateInstance<NullWebsiteResourceWriter>(x);
        /// <inheritdoc/>
        public ISet<string> AdditionalResources { get; set; } = new HashSet<string> { "favicon.ico", "robots.txt" };
        /// <inheritdoc/>
        public string NotFoundPage { get; set; } = "errors/404";
        /// <inheritdoc/>
        public bool AllowQueryString { get; set; }
        /// <inheritdoc/>
        public Func<string, bool> IsUriValid { get; set; } = x => true;
    }
}
