using Amazon.S3.Model;
using System;

namespace Ae.Freezer.Aws
{
    /// <summary>
    /// Describes the configuration required for <see cref="AmazonS3WebsiteResourceWriter"/>.
    /// </summary>
    public sealed class AmazonS3WebsiteResourceWriterConfiguration
    {
        /// <summary>
        /// The Amazon S3 bucket name to use (must be set).
        /// </summary>
        public string BucketName { get; set; }
        /// <summary>
        /// Whether to remove all objects from the bucket which do not match the static website resources written in this session.
        /// </summary>
        public bool ShouldCleanUnmatchedObjects { get; set; }
        /// <summary>
        /// If non-null, the CloudFront distribution to invalidate once the objects are pushed.
        /// </summary>
        public string DistributionId { get; set; }
        /// <summary>
        /// The function to use to generate the Amazon S3 object keys from the relative URIs.
        /// By default, convert empty string (e.g. the root document) to "index.html" and use
        /// the relative URL as the object key verbatim (<see cref="DefaultKeyGenerator"/>).
        /// </summary>
        public Func<Uri, string> GenerateKey { get; set; } = DefaultKeyGenerator;
        /// <summary>
        /// If non-null, the function to use to modify the PUT object request. For example, to customize the <see cref="PutObjectRequest.CannedACL"/> property.
        /// </summary>
        public Action<PutObjectRequest> PutRequestModifier { get; set; }
        /// <summary>
        /// The default S3 key generator function.
        /// </summary>
        public static readonly Func<Uri, string> DefaultKeyGenerator = relativeUri =>
        {
            return relativeUri.ToString() == string.Empty ? "index.html" : relativeUri.ToString();
        };
    }
}
