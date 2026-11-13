namespace HTMX.Test.Integration;

using System.Collections.Generic;
using System.Net;

/// <summary>
/// What the application answered one request with, read in full: its status, its headers and its body.
/// </summary>
internal sealed record Answer
{
    /// <summary>
    /// Gets the answer's status, such as 204 No Content.
    /// </summary>
    required public HttpStatusCode Status { get; init; }

    /// <summary>
    /// Gets the answer's headers and its content's, by name in any case, each with its values joined by commas.
    /// </summary>
    required public IReadOnlyDictionary<string, string> Headers { get; init; }

    /// <summary>
    /// Gets the answer's body, the HTML a page or partial wrote, or empty.
    /// </summary>
    required public string Body { get; init; }
}
