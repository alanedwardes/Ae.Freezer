using Ae.Freezer.Aws;
using Ae.Freezer.Writers;
using Amazon;
using Amazon.CloudFront;
using Amazon.IdentityManagement;
using Amazon.Lambda;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace Ae.Freezer.Console
{
    public static class Program
    {
        public static void Main()
        {
            var services = new ServiceCollection();

            services.AddLogging(x => x.AddConsole());
            services.AddHttpClient("FreezerClient", x => x.BaseAddress = new Uri("https://uncached.alanedwardes.com"));
            services.AddFreezer();

            var crawler = services.BuildServiceProvider().GetRequiredService<IFreezer>();

            crawler.Freeze(new FreezerConfiguration
            {
                HttpClientName = "FreezerClient",
                //ResourceWriter = x => new AmazonS3WebsiteResourceWriter(x.GetRequiredService<ILogger<AmazonS3WebsiteResourceWriter>>(), new AmazonS3Client(), new AmazonCloudFrontClient(), new AmazonS3WebsiteResourceWriterConfiguration
                //{
                //    BucketName = "ae-blog-static",
                //    DistributionId = "E295SAMVLG12SQ",
                //    ShouldInvalidateCloudFrontCache = true,
                //    ShouldCleanUnmatchedObjects = true
                //})
                ResourceWriter = x => new AmazonLambdaAtEdgeResourceWriter(x.GetRequiredService<ILogger<AmazonLambdaAtEdgeResourceWriter>>(), new AmazonLambdaAtEdgeResourceWriterConfiguration
                {
                    LambdaName = "TestEdgeResponder2",
                    DistributionId = "E2PUGLR3DO3SIG"
                }, new AmazonLambdaClient(RegionEndpoint.USEast1), new AmazonCloudFrontClient(), new AmazonIdentityManagementServiceClient())
                //ResourceWriter = x => new NullWebsiteResourceWriter()
            }, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
