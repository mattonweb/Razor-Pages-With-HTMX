namespace HTMX.Adapter.Contacts.Data.Conversions;

using HTMX.Adapter.Contacts.Contracts;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// Stores a <see cref="VersionNumber"/> as an integer, and wraps it again when it is read.
/// </summary>
internal sealed class VersionNumberConverter : ValueConverter<VersionNumber, int>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VersionNumberConverter"/> class.
    /// </summary>
    public VersionNumberConverter()
        : base(static wrapped => wrapped.Value, static stored => new VersionNumber(stored))
    {
    }
}
