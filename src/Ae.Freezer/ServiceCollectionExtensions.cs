using Ae.Freezer.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Ae.Freezer
{
    /// <summary>
    /// Describes methods to add the implementation to an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the default <see cref="IFreezer"/> implementation and its required services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddFreezer(this IServiceCollection services)
        {
            return services.AddSingleton<IFreezer, Internal.Freezer>()
                           .AddSingleton<ILinkFinder, LinkFinder>();
        }
    }
}
