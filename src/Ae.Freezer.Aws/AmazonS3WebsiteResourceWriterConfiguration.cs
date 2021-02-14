using Amazon.S3;
using Amazon.S3.Model;
using System;

namespace Ae.Freezer.Aws
{
    public sealed class AmazonS3WebsiteResourceWriterConfiguration
    {
        public string BucketName { get; set; }
        public bool ShouldCleanUnmatchedObjects { get; set; }
        public string DistributionId { get; set; }
        public bool ShouldInvalidateCloudFrontCache { get; set; }
        public Func<Uri, string> GenerateKey { get; set; } = relativeUri => relativeUri.ToString() == string.Empty ? "index.html" : relativeUri.ToString();
        public Action<PutObjectRequest> PutRequestModifier { get; set; } = x => x.CannedACL = S3CannedACL.PublicRead;
    }
}
