namespace Ae.Freezer.Aws
{
    public sealed class AmazonLambdaAtEdgeResourceWriterConfiguration
    {
        public string LambdaName { get; set; }
        public string DistributionId { get; set; }
        public string CacheBehaviourId { get; set; }
        public Amazon.CloudFront.EventType LambdaEventType { get; set; } = Amazon.CloudFront.EventType.OriginRequest;
    }
}
