using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ae.Freezer.Internal
{
    internal sealed class LinkFinder : ILinkFinder
    {
        private static readonly Regex URI_REGEX = new Regex("(href|src)=\"(?<uri>.+?)\"");
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
                if (!Uri.TryCreate(group.Value, UriKind.RelativeOrAbsolute, out Uri uri))
                {
                    _logger.LogWarning("The following URI is invalid: {InvalidUri}", uri);
                    continue;
                }

                var absoluteUri = GetValidAbsoluteUri(baseAddress, uri);
                if (absoluteUri == null)
                {
                    continue;
                }

                uris.Add(baseAddress.MakeRelativeUri(absoluteUri));
            }

            return uris;
        }

        public Uri GetValidAbsoluteUri(Uri baseAddress, Uri uri)
        {
            Uri absoluteUri = GetAbsoluteUri(baseAddress, uri);
            if (absoluteUri == null)
            {
                _logger.LogWarning($"Ignoring uri {uri}");
                return null;
            }

            if (!baseAddress.IsBaseOf(absoluteUri))
            {
                _logger.LogDebug($"Ignoring absolute URI {uri} since it doesn't start with {baseAddress}");
                return null;
            }

            return GetUriWithoutFragment(absoluteUri);
        }

        public Uri GetAbsoluteUri(Uri baseAddress, Uri uri)
        {
            if (uri.IsAbsoluteUri)
            {
                return uri;
            }
            else if (Uri.TryCreate(baseAddress, uri, out Uri absoluteUri))
            {
                return absoluteUri;
            }

            return null;
        }

        private Uri GetUriWithoutFragment(Uri uri) => new UriBuilder(uri) { Fragment = null }.Uri;
    }
}
