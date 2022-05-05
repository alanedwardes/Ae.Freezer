using Ae.Freezer.Aws;
using Amazon;
using Amazon.CloudFront;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Freezer.Console
{
    public sealed class ConcurrentHandler : DelegatingHandler
    {
        private readonly SemaphoreSlim semaphoreSlim;

        public ConcurrentHandler(SemaphoreSlim semaphoreSlim) => this.semaphoreSlim = semaphoreSlim;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
    }

    public static class Program
    {
        public static void Main()
        {
            var services = new ServiceCollection();

            var semaphoreSlim = new SemaphoreSlim(1, 1);

            services.AddLogging(x => x.AddConsole());
            services.AddHttpClient("FreezerClient", x => x.BaseAddress = new Uri("https://wiki.iamestranged.com/"))
                    .AddHttpMessageHandler(x => new ConcurrentHandler(semaphoreSlim));
            services.AddFreezer();

            var crawler = services.BuildServiceProvider().GetRequiredService<IFreezer>();

            crawler.Freeze(new FreezerConfiguration
            {
                HttpClientName = "FreezerClient",
                ResourceWriter = x => new AmazonS3WebsiteResourceWriter(
                    x.GetRequiredService<ILogger<AmazonS3WebsiteResourceWriter>>(),
                    new AmazonS3WebsiteResourceWriterConfiguration
                    {
                        BucketName = "estranged-static-wiki",
                        ShouldCleanUnmatchedObjects = true
                    },
                    new AmazonS3Client(RegionEndpoint.EUWest1),
                    new AmazonCloudFrontClient())
            }, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
