using Ae.Freezer.Crawler.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Freezer.Crawler.Writers
{
    public sealed class NullWebsiteResourceWriter : IWebsiteResourceWriter
    {
        public Task FlushResources(IReadOnlyCollection<Uri> resources, CancellationToken token) => Task.CompletedTask;

        public Task WriteResource(WebsiteResource websiteResource, CancellationToken token) => Task.CompletedTask;
    }
}
