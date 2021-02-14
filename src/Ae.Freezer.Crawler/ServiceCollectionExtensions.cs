using Ae.Freezer.Crawler.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Ae.Freezer.Crawler
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFreezer(this IServiceCollection services)
        {
            return services.AddSingleton<IFreezer, Internal.Freezer>()
                           .AddSingleton<ILinkFinder, LinkFinder>();
        }
    }
}
