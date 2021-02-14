using System.Threading;
using System.Threading.Tasks;

namespace Ae.Freezer
{
    public interface IFreezer
    {
        Task Freeze(IFreezerConfiguration freezerConfiguration, CancellationToken token);
    }
}