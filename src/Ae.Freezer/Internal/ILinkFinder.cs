using System;
using System.Collections.Generic;

namespace Ae.Freezer.Internal
{
    internal interface ILinkFinder
    {
        ISet<Uri> GetUrisFromLinks(Uri baseAddress, string body, IFreezerConfiguration freezerConfiguration);
    }
}