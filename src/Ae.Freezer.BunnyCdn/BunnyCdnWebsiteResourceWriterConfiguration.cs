using System;

namespace Ae.Freezer.BunnyCdn
{
    /// <summary>
    /// Describes the configuration required for <see cref="BunnyCdnWebsiteResourceWriter"/>.
    /// </summary>
    public sealed class BunnyCdnWebsiteResourceWriterConfiguration
    {
        /// <summary>
        /// The Bunny CDN Storage Zone name to use (must be set).
        /// </summary>
        public string StorageZoneName { get; set; }
        /// <summary>
        /// Whether to remove all objects from the storage zone which do not match the static website resources written in this session.
        /// </summary>
        public bool ShouldCleanUnmatchedObjects { get; set; }
        /// <summary>
        /// The function to use to generate the Bunny CDN object keys from the relative URIs.
        /// By default, convert empty string (e.g. the root document) to "index.html" and use
        /// the relative URL as the object key verbatim (<see cref="DefaultKeyGenerator"/>).
        /// </summary>
        public Func<string, string> GenerateKey { get; set; } = DefaultKeyGenerator;
        /// <summary>
        /// The default Bunny CDN key generator function.
        /// </summary>
        public static readonly Func<string, string> DefaultKeyGenerator = relativeUri =>
        {
            if (relativeUri == string.Empty) return "index.html";
            if (relativeUri.EndsWith("/")) return relativeUri + "index.html";
            return relativeUri;
        };
    }
}
