using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace Ae.Freezer.Internal
{
    internal sealed class LinkFinder : ILinkFinder
    {
        private static readonly Regex URI_REGEX = new Regex("(?<attribute>(href|src))=\"(?<uri>.+?)\"");
        private readonly ILogger<LinkFinder> _logger;

        public LinkFinder(ILogger<LinkFinder> logger)
        {
            _logger = logger;
        }

        public IEnumerable<FoundUri> GetUrisFromLinks(Uri baseAddress, string body)
        {
            foreach (Match foundUri in URI_REGEX.Matches(body))
            {
                var attribute = foundUri.Groups["attribute"].Value;
                var rawUri = foundUri.Groups["uri"].Value;
                if (!Uri.TryCreate(WebUtility.HtmlDecode(rawUri), UriKind.RelativeOrAbsolute, out Uri uri))
                {
                    _logger.LogWarning("The following URI is invalid: {InvalidUri}", uri);
                    continue;
                }

                var absoluteUri = GetValidAbsoluteUri(baseAddress, uri);
                if (absoluteUri == null)
                {
                    continue;
                }

                var relativeUri = "/" + absoluteUri.AbsoluteUri[baseAddress.AbsoluteUri.Length..];

                yield return new FoundUri
                {
                    AttributeName = attribute,
                    AttributeValue = rawUri,
                    Uri = new Uri(relativeUri, UriKind.Relative)
                };
            }
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
