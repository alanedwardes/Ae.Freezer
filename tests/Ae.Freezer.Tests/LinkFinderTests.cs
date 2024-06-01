using Ae.Freezer.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using Xunit;

namespace Ae.Freezer.Tests
{
    public class LinkFinderTests
    {
        private const string Document1 = "<a href=\"//www.example.com/test1\">test1</a>" +
                                         "<a href=\"https://www.example.com/test2\">test2</a>" +
                                         "<a href=\"/test3\">test3</a>" +
                                         "<a href=\"https://www.example.org/test4\">test4</a>" +
                                         "<a href=\"https://www.example.com/test5#test\">test5</a>" +
                                         "<a href=\"https://www.example.com/test5?test=wibble\">test6</a>";

        [Fact]
        public void FindLinksIgnoreQueryStringAndFragment()
        {
            ILinkFinder linkFinder = new LinkFinder(new NullLogger<LinkFinder>());

            var links = linkFinder.GetUrisFromLinks(new Uri("https://www.example.com/", UriKind.Absolute), "index.html", Document1, new FreezerConfiguration());

            Assert.True(links.SetEquals(new[] { "test1", "test2", "test3", "test5" }));
        }

        [Fact]
        public void FindLinksAllowQueryString()
        {
            ILinkFinder linkFinder = new LinkFinder(new NullLogger<LinkFinder>());

            var links = linkFinder.GetUrisFromLinks(new Uri("https://www.example.com/", UriKind.Absolute), "index.html", Document1, new FreezerConfiguration { AllowQueryString = true });

            Assert.True(links.SetEquals(new[] { "test1", "test2", "test3", "test5", "test5?test=wibble" }));
        }
    }
}
