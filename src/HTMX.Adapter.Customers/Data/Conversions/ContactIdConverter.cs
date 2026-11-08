namespace HTMX.Adapter.Customers.Data.Conversions;

using HTMX.Adapter.Customers.Contracts;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// Stores a <see cref="ContactId"/> as an integer, and wraps it again when it is read.
/// </summary>
internal sealed class ContactIdConverter : ValueConverter<ContactId, long>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContactIdConverter"/> class.
    /// </summary>
    public ContactIdConverter()
        : base(static wrapped => wrapped.Value, static stored => new ContactId(stored))
    {
    }
}
