namespace HTMX.Adapter.Customers.Data.Conversions;

using HTMX.Adapter.Customers.Contracts;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// Stores a <see cref="EmailAddress"/> as text, and wraps it again when it is read.
/// </summary>
internal sealed class EmailAddressConverter : ValueConverter<EmailAddress, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmailAddressConverter"/> class.
    /// </summary>
    public EmailAddressConverter()
        : base(static wrapped => wrapped.Value, static stored => new EmailAddress(stored))
    {
    }
}
