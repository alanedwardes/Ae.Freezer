using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Ae.Freezer.Internal
{
    internal sealed class LinkFinder : ILinkFinder
    {
        private static readonly Regex CSS_URL_REGEX = new Regex("(url)\\((?<uri>.+?)\\)");
        private readonly ILogger<LinkFinder> _logger;

        public LinkFinder(ILogger<LinkFinder> logger)
        {
            _logger = logger;
        }

        public ISet<string> GetUrisFromLinks(Uri baseAddress, string uri, string body, IFreezerConfiguration freezerConfiguration)
        {
            var uris = new HashSet<string>();

            var doc = new HtmlDocument();
            doc.LoadHtml(body);

            IEnumerable<string> GetAttributeFromNodes(string selector, string attribute)
            {
                foreach (var node in doc.DocumentNode.SelectNodes(selector) ?? Enumerable.Empty<HtmlNode>())
                {
                    var value = node.GetAttributeValue(attribute, string.Empty);
                    yield return HttpUtility.HtmlDecode(value);
                }
            }

            var linkHrefs = GetAttributeFromNodes(".//link[@href!='']", "href");
            var aHrefs = GetAttributeFromNodes(".//a[@href!='']", "href");
            var imgSrcs = GetAttributeFromNodes(".//img[@src!='']", "src");
            var imgSets = GetAttributeFromNodes(".//img[@srcset!='']", "srcset").Select(x => x.Split(",")).SelectMany(x => x).Select(x => x.Trim().Split()[0]);
            var matches = CSS_URL_REGEX.Matches(body).Select(x => x.Groups["uri"]).Select(x => x.Value.Trim('\'', '"'));

            foreach (var possibleUrl in new[] { linkHrefs, aHrefs, imgSrcs, imgSets, matches }.SelectMany(x => x))
            {
                var relativeUri = GetRelativeUri(baseAddress, uri, possibleUrl, freezerConfiguration);
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

        private string GetRelativeUri(Uri baseAddress, string currentUri, string extractedUri, IFreezerConfiguration freezerConfiguration)
        {
            if (extractedUri.Contains("/..") || extractedUri.Contains("./"))
            {
                _logger.LogCritical("Path traversials are not yet supported for {Uri}", extractedUri);
                return null;
            }

            if (extractedUri.StartsWith("javascript:") || extractedUri.StartsWith("data:") || extractedUri.StartsWith("mailto:"))
            {
                _logger.LogWarning("Unsupported URL scheme for {Uri}", extractedUri);
                return null;
            }

            var uri = extractedUri.Split("#")[0];
            if (!freezerConfiguration.AllowQueryString)
            {
                uri = uri.Split("?")[0];
            }

            if (uri.StartsWith("//"))
            {
                uri = baseAddress.Scheme + ':' + uri;
            }

            if (uri.StartsWith("/"))
            {
                return uri[1..];
            }

            if (uri.StartsWith("http://") || uri.StartsWith("https://"))
            {
                var baseAddressString = baseAddress.ToString();
                if (uri.StartsWith(baseAddressString))
                {
                    return uri[baseAddressString.Length..];
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
