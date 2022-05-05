using System;

namespace Ae.Freezer.Internal
{
    /// <summary>
    /// Represents a URI which was found in a document.
    /// </summary>
    public struct FoundUri
    {
        /// <summary>
        /// The name of the attribute the URI was found under.
        /// </summary>
        public string AttributeName { get; set; }
        /// <summary>
        /// The raw attribute value.
        /// </summary>
        public string AttributeValue { get; set; }
        /// <summary>
        /// The parsed URI.
        /// </summary>
        public Uri Uri { get; set; }
        /// <summary>
        /// Implicitly convert from a URI to FoundUri.
        /// </summary>
        public static implicit operator FoundUri(Uri uri) => new FoundUri { Uri = uri };
        /// <inheritdoc />
        public override string ToString() => Uri.ToString();
    }
}