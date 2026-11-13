namespace HTMX.Test.Integration;

/// <summary>
/// A file as a form sends it: the field it is sent under, its name and its bytes.
/// </summary>
internal sealed record FilePart
{
    /// <summary>
    /// Gets the form's name for the file's field, such as <c>Input.File</c>.
    /// </summary>
    required public string Field { get; init; }

    /// <summary>
    /// Gets the file's name, as the browser sends it.
    /// </summary>
    required public string FileName { get; init; }

    /// <summary>
    /// Gets the file's bytes.
    /// </summary>
    required public byte[] Content { get; init; }
}
