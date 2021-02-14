using Ae.Freezer.Aws;
using Amazon.CloudFront;
using Amazon.S3;
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
            services.AddHttpClient();
            services.AddFreezer();

            var crawler = services.BuildServiceProvider().GetRequiredService<IFreezer>();

            crawler.Freeze(new FreezerConfiguration
            {
                BaseAddress = new Uri("https://uncached.alanedwardes.com"),
                ResourceWriter = x => new AmazonS3WebsiteResourceWriter(x.GetRequiredService<ILogger<AmazonS3WebsiteResourceWriter>>(), new AmazonS3Client(), new AmazonCloudFrontClient(), new AmazonS3WebsiteResourceWriterConfiguration
                {
                    BucketName = "ae-blog-static",
                    DistributionId = "E295SAMVLG12SQ",
                    ShouldInvalidateCloudFrontCache = true,
                    ShouldCleanUnmatchedObjects = true
                })
            }, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
