using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ae.Freezer.Crawler.Internal
{
    internal sealed class LinkFinder : ILinkFinder
    {
        private static Regex URI_REGEX = new Regex("(href|src)=\"(?<uri>.+?)\"");
        private readonly ILogger<LinkFinder> _logger;

        public LinkFinder(ILogger<LinkFinder> logger)
        {
            _logger = logger;
        }

        public ISet<Uri> GetUrisFromLinks(Uri baseAddress, string body)
        {
            var uris = new HashSet<Uri>();

            foreach (Group group in URI_REGEX.Matches(body).Select(x => x.Groups["uri"]))
            {
                var absoluteUri = GetAbsoluteUriFromString(baseAddress, group.Value);
                if (absoluteUri != null)
                {
                    uris.Add(baseAddress.MakeRelativeUri(absoluteUri));
                }
            }

            return uris;
        }

        public Uri GetAbsoluteUriFromString(Uri baseAddress, string uriString)
        {
            if (uriString.StartsWith("//"))
            {
                uriString = uriString.Replace("//", baseAddress.Scheme + "://");
            }

            if (Uri.TryCreate(uriString, UriKind.Absolute, out Uri absoluteUri))
            {
                if (baseAddress.IsBaseOf(absoluteUri))
                {
                    return GetUriWithoutFragment(absoluteUri);
                }
                else
                {
                    _logger.LogDebug($"Ignoring absolute URI {absoluteUri} since it doesn't start with {baseAddress}");
                    return null;
                }
            }
            else if (Uri.TryCreate(uriString, UriKind.Relative, out Uri relativeUri))
            {
                return GetUriWithoutFragment(new Uri(baseAddress, relativeUri));
            }

            _logger.LogWarning($"Unable to parse URI {uriString}");
            return null;
        }

        private Uri GetUriWithoutFragment(Uri uri) => new UriBuilder(uri) { Fragment = null }.Uri;
    }
}
