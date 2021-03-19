using Ae.Freezer.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Freezer.Writers
{
    public interface IWebsiteResourceWriter
    {
        Task PrepareResources();
        Task WriteResource(WebsiteResource websiteResource, CancellationToken token);
        Task FinishResources(IReadOnlyCollection<Uri> resources, CancellationToken token);
    }
}
