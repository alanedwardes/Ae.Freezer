using System;
using System.Collections.Generic;

namespace Ae.Freezer.Internal
{
    internal interface ILinkFinder
    {
        ISet<string> GetUrisFromLinks(Uri baseAddress, string uri, string body, IFreezerConfiguration freezerConfiguration);
    }
}