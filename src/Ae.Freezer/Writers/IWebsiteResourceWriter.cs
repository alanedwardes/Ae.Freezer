using Ae.Freezer.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Freezer.Writers
{
    /// <summary>
    /// Describes a service which offers methods to write <see cref="WebsiteResource"/> objects to a destination to be served.
    /// </summary>
    public interface IWebsiteResourceWriter
    {
        /// <summary>
        /// Called before the first resource is written.
        /// </summary>
        /// <returns></returns>
        Task PrepareResources();
        /// <summary>
        /// Called in parallel for each resource to be written.
        /// </summary>
        /// <param name="websiteResource"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task WriteResource(WebsiteResource websiteResource, CancellationToken token);
        /// <summary>
        /// Called once all resources have been written.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task FinishResources(IReadOnlyCollection<Uri> resources, CancellationToken token);
    }
}
