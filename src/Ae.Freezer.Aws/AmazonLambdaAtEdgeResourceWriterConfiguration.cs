namespace Ae.Freezer.Aws
{
    /// <summary>
    /// Describes the configuration to update the Lambda@Edge function and CloudFront distribution.
    /// </summary>
    public sealed class AmazonLambdaAtEdgeResourceWriterConfiguration
    {
        /// <summary>
        /// The name of an existing Lambda function to replace and publish a new version.
        /// </summary>
        public string LambdaName { get; set; }
        /// <summary>
        /// The ID of the CloudFront distribution to update with the new Lambda function version.
        /// </summary>
        public string DistributionId { get; set; }
        /// <summary>
        /// If non-null, the cache behaviour to update, if not set will use the default behaviour.
        /// </summary>
        public string CacheBehaviourId { get; set; }
        /// <summary>
        /// The CloudFront event type to use when finding the existing Lambda@Edge function association. Defaults to <see cref="Amazon.CloudFront.EventType.OriginRequest"/>.
        /// </summary>
        public Amazon.CloudFront.EventType LambdaEventType { get; set; } = Amazon.CloudFront.EventType.OriginRequest;
    }
}
