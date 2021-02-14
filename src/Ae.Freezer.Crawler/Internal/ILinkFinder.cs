using System;
using System.Collections.Generic;

namespace Ae.Freezer.Crawler.Internal
{
    internal interface ILinkFinder
    {
        Uri GetAbsoluteUriFromString(Uri baseAddress, string uriString);
        ISet<Uri> GetUrisFromLinks(Uri baseAddress, string body);
    }
}