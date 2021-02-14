using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Freezer.Crawler
{
    public interface IFreezer
    {
        Task Freeze(IFreezerConfiguration freezerConfiguration, CancellationToken token);
    }
}