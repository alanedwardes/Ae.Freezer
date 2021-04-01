using System.Threading;
using System.Threading.Tasks;

namespace Ae.Freezer
{
    /// <summary>
    /// Provides methods which given a configuration object, freeze a website into static resources.
    /// </summary>
    public interface IFreezer
    {
        /// <summary>
        /// Freezes the website using the specified <see cref="IFreezerConfiguration"/>.
        /// </summary>
        Task Freeze(IFreezerConfiguration freezerConfiguration, CancellationToken token);
    }
}