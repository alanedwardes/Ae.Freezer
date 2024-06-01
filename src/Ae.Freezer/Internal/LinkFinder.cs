using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Ae.Freezer.Internal
{
    internal sealed class LinkFinder : ILinkFinder
    {
        private static readonly Regex HTML_ATTRIBUTE_REGEX = new Regex("(href|src)=\"(?<uri>.+?)\"");
        private static readonly Regex CSS_URL_REGEX = new Regex("(url)\\([\"']?(?<uri>.+?)[\"']?\\)");
        private readonly ILogger<LinkFinder> _logger;

        public LinkFinder(ILogger<LinkFinder> logger)
        {
            _logger = logger;
        }

        public ISet<string> GetUrisFromLinks(Uri baseAddress, string uri, string body, IFreezerConfiguration freezerConfiguration)
        {
            var uris = new HashSet<string>();

            var matches = new[] { HTML_ATTRIBUTE_REGEX, CSS_URL_REGEX }.SelectMany(x => x.Matches(body)).Select(x => x.Groups["uri"]);

            foreach (Group group in matches)
            {
                var extractedUri = HttpUtility.HtmlDecode(group.Value);
                if (!Uri.TryCreate(extractedUri, UriKind.RelativeOrAbsolute, out Uri createdUri))
                {
                    _logger.LogWarning("The following URI is invalid: {InvalidUri}", createdUri);
                    continue;
                }

                var absoluteUri = GetValidAbsoluteUri(baseAddress, createdUri, freezerConfiguration);
                if (absoluteUri == null)
                {
                    continue;
                }

                var relativeUri = GetRelativeUri(baseAddress, absoluteUri);
                if (freezerConfiguration.IsUriValid(relativeUri))
                {
                    uris.Add(relativeUri);
                }
            }

            return uris;
        }

        private string GetRelativeUri(Uri baseAddress, Uri uri)
        {
            if (!uri.IsAbsoluteUri || !baseAddress.IsBaseOf(uri))
            {
                throw new InvalidOperationException();
            }

            return uri.PathAndQuery.StartsWith('/') ? uri.PathAndQuery[1..] : uri.PathAndQuery;
        }

        private Uri GetValidAbsoluteUri(Uri baseAddress, Uri uri, IFreezerConfiguration freezerConfiguration)
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

            return GetUriWithoutFragment(absoluteUri, freezerConfiguration);
        }

        private Uri GetAbsoluteUri(Uri baseAddress, Uri uri)
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

        private Uri GetUriWithoutFragment(Uri originalUri, IFreezerConfiguration freezerConfiguration)
        {
            var builder = new UriBuilder(originalUri) { Fragment = null };
            if (!freezerConfiguration.AllowQueryString)
            {
                builder.Query = null;
            }

            var processedUri = builder.Uri;
            if (!originalUri.Equals(processedUri))
            {
                _logger.LogWarning("Rewriting Uri {OriginalUri} to {RewrittenUri}", originalUri, processedUri);
            }

            return processedUri;
        }
    }
}
