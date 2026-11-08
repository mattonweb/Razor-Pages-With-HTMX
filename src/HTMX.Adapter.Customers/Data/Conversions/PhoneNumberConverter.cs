namespace HTMX.Adapter.Customers.Data.Conversions;

using HTMX.Adapter.Customers.Contracts;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// Stores a <see cref="PhoneNumber"/> as text, and wraps it again when it is read.
/// </summary>
internal sealed class PhoneNumberConverter : ValueConverter<PhoneNumber, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PhoneNumberConverter"/> class.
    /// </summary>
    public PhoneNumberConverter()
        : base(static wrapped => wrapped.Value, static stored => new PhoneNumber(stored))
    {
    }
}
