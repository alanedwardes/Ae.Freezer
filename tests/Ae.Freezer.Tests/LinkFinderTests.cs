using Ae.Freezer.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using Xunit;

namespace Ae.Freezer.Tests
{
    public class LinkFinderTests
    {
        private const string Document1 = "<a href=\"//www.example.com/test1\">test1</a>" +
                                         "<a href=\"https://www.example.com/test2\">test2</a>" +
                                         "<a href=\"/test3\">test3</a>" +
                                         "<a href=\"https://www.example.org/test4\">test4</a>" +
                                         "<a href=\"https://www.example.com/test5#test\">test5</a>";

        [Fact]
        public void FindLinksDocument1()
        {
            ILinkFinder linkFinder = new LinkFinder(new NullLogger<LinkFinder>());

            IEnumerable<FoundUri> links = linkFinder.GetUrisFromLinks(new Uri("https://www.example.com/", UriKind.Absolute), Document1);

            Assert.Collection(links, x =>
            {
                Assert.Equal("//www.example.com/test1", x.AttributeValue);
                Assert.Equal("/test1", x.Uri.ToString());
            }, x =>
            {
                Assert.Equal("https://www.example.com/test2", x.AttributeValue);
                Assert.Equal("/test2", x.Uri.ToString());
            }, x =>
            {
                Assert.Equal("/test3", x.AttributeValue);
                Assert.Equal("/test3", x.Uri.ToString());
            }, x =>
            {
                Assert.Equal("https://www.example.com/test5#test", x.AttributeValue);
                Assert.Equal("/test5", x.Uri.ToString());
            });
        }
    }
}
