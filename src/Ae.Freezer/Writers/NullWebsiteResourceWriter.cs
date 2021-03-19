using Ae.Freezer.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Freezer.Writers
{
    public sealed class NullWebsiteResourceWriter : IWebsiteResourceWriter
    {
        public Task FinishResources(IReadOnlyCollection<Uri> resources, CancellationToken token) => Task.CompletedTask;

        public Task PrepareResources() => Task.CompletedTask;

        public Task WriteResource(WebsiteResource websiteResource, CancellationToken token) => Task.CompletedTask;
    }
}
