using Ae.Freezer.Writers;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ae.Freezer
{
    public interface IFreezerConfiguration
    {
        Uri BaseAddress { get; }
        Uri StartPath { get; }
        Regex ResourceRegex { get; }
        ISet<string> TextMimeTypes { get; }
        Func<IServiceProvider, IWebsiteResourceWriter> ResourceWriter { get; }
        ISet<Uri> AdditionalResources { get; }
        Uri NotFoundPage { get; set; }
    }
}