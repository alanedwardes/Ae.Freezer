using Ae.Freezer.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Freezer.Writers
{
    /// <inheritdoc/>
    public sealed class NullWebsiteResourceWriter : IWebsiteResourceWriter
    {
        /// <inheritdoc/>
        public Task FinishResources(IReadOnlyCollection<Uri> resources, CancellationToken token) => Task.CompletedTask;
        /// <inheritdoc/>
        public Task PrepareResources() => Task.CompletedTask;
        /// <inheritdoc/>
        public Task WriteResource(WebsiteResource websiteResource, CancellationToken token) => Task.CompletedTask;
    }
}
