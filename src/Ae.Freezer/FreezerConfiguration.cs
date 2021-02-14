using Ae.Freezer.Writers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ae.Freezer
{
    public sealed class FreezerConfiguration : IFreezerConfiguration
    {
        public Uri BaseAddress { get; set; }
        public Uri StartPath { get; set; } = new Uri(string.Empty, UriKind.Relative);
        public Regex ResourceRegex { get; set; } = new Regex("(href|src)=\"(?<uri>.+?)\"");
        public ISet<string> TextMimeTypes { get; set; } = new HashSet<string> { "text/html", "text/xml", "application/rss+xml" };
        public Func<IServiceProvider, IWebsiteResourceWriter> ResourceWriter { get; set; } = x => ActivatorUtilities.CreateInstance<NullWebsiteResourceWriter>(x);
        public ISet<Uri> AdditionalResources { get; set; } = new HashSet<Uri> { new Uri("favicon.ico", UriKind.Relative), new Uri("robots.txt", UriKind.Relative) };
        public Uri NotFoundPage { get; set; } = new Uri("errors/404", UriKind.Relative);
    }
}
