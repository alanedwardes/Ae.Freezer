using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ae.Freezer.Aws.Internal
{
    internal sealed class AmazonLambdaAtEdgeResponse
    {
        internal sealed class Header
        {
            [JsonPropertyName("key")]
            public string Key { get; set; }
            [JsonPropertyName("value")]
            public string Value { get; set; }
        }

        [JsonPropertyName("status")]
        public uint Status { get; set; }
        [JsonPropertyName("statusDescription")]
        public string StatusDescription { get; set; }
        [JsonPropertyName("headers")]
        public IDictionary<string, IList<Header>> Headers { get; set; } = new Dictionary<string, IList<Header>>();
        [JsonPropertyName("body")]
        public string Body { get; set; }
        [JsonPropertyName("bodyEncoding")]
        public string BodyEncoding { get; set; }
    }
}
