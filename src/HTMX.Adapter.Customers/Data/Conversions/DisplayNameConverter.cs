namespace HTMX.Adapter.Customers.Data.Conversions;

using HTMX.Adapter.Customers.Contracts;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// Stores a <see cref="DisplayName"/> as text, and wraps it again when it is read.
/// </summary>
internal sealed class DisplayNameConverter : ValueConverter<DisplayName, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DisplayNameConverter"/> class.
    /// </summary>
    public DisplayNameConverter()
        : base(static wrapped => wrapped.Value, static stored => new DisplayName(stored))
    {
    }
}
