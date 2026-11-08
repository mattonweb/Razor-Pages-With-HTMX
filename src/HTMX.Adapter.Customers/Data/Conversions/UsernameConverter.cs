namespace HTMX.Adapter.Customers.Data.Conversions;

using HTMX.Adapter.Customers.Contracts;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// Stores a <see cref="Username"/> as text, and wraps it again when it is read.
/// </summary>
internal sealed class UsernameConverter : ValueConverter<Username, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UsernameConverter"/> class.
    /// </summary>
    public UsernameConverter()
        : base(static wrapped => wrapped.Value, static stored => new Username(stored))
    {
    }
}
