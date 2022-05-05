using Amazon.S3;
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
        /// Required: The Amazon S3 bucket name to use.
        /// </summary>
        public string BucketName { get; set; }
        /// <summary>
        /// Optional: Whether to remove all objects from the bucket which do not match the static website resources written in this session.
        /// </summary>
        public bool ShouldCleanUnmatchedObjects { get; set; }
        /// <summary>
        /// Optional: The CloudFront distribution to invalidate once the objects are pushed.
        /// </summary>
        public string DistributionId { get; set; }
        /// <summary>
        /// Required: The function to use to generate the Amazon S3 object keys from the relative URIs.
        /// </summary>
        public Func<Uri, string> GenerateKey { get; set; } = relativeUri =>
        {
            var relativeUriString = relativeUri.ToString().TrimStart('/');
            return relativeUriString == string.Empty ? "index.html" : relativeUriString;
        };
        /// <summary>
        /// Required: The function to use to determine the <see cref="S3CannedACL"/> to use when writing the website resources.
        /// </summary>
        public Action<PutObjectRequest> PutRequestModifier { get; set; } = x => x.CannedACL = S3CannedACL.PublicRead;
    }
}
