namespace Ae.Freezer.Entities
{
    /// <summary>
    /// Describes the type of website resource.
    /// </summary>
    public enum WebsiteResourceType
    {
        /// <summary>
        /// This resource is buffered text (as a string)
        /// </summary>
        Text,
        /// <summary>
        /// This resource is unbuffered binary (as a stream)
        /// </summary>
        Binary
    }
}
