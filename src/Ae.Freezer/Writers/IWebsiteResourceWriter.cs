using Ae.Freezer.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Freezer.Writers
{
    public interface IWebsiteResourceWriter
    {
        Task WriteResource(WebsiteResource websiteResource, CancellationToken token);
        Task FlushResources(IReadOnlyCollection<Uri> resources, CancellationToken token);
    }
}
