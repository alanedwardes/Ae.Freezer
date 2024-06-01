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
        private static readonly Regex CSS_URL_REGEX = new Regex("(url)\\((?<uri>.+?)\\)");
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
                var extractedUri = HttpUtility.HtmlDecode(group.Value).Trim('\'', '"');
                var relativeUri = GetRelativeUri(baseAddress.ToString(), uri, extractedUri);
                if (relativeUri == null)
                {
                    continue;
                }

                if (freezerConfiguration.IsUriValid(relativeUri))
                {
                    uris.Add(relativeUri);
                }
            }

            return uris;
        }

        private string GetRelativeUri(string baseAddress, string currentUri, string extractedUri)
        {
            if (extractedUri.Contains("/..") || extractedUri.Contains("./"))
            {
                return null;
            }

            if (extractedUri.StartsWith("//"))
            {
                return null;
            }

            if (extractedUri.StartsWith("javascript:") || extractedUri.StartsWith("data:"))
            {
                return null;
            }

            var uri = extractedUri.Split("#")[0];
            if (uri.StartsWith("/"))
            {
                return uri[1..];
            }

            if (uri.StartsWith("http://") || uri.StartsWith("https://"))
            {
                if (uri.StartsWith(baseAddress))
                {
                    return uri[baseAddress.Length..];
                }
                else
                {
                    return null;
                }
            }

            return currentUri + uri;
        }
    }
}
