using System;
using System.Collections.Generic;

namespace Ae.Freezer.Internal
{
    internal interface ILinkFinder
    {
        IEnumerable<FoundUri> GetUrisFromLinks(Uri baseAddress, string body);
    }
}