namespace HTMX.Adapter.Contacts.Data.Conversions;

using HTMX.Adapter.Contacts.Contracts;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// Stores a <see cref="OrganizationId"/> as an integer, and wraps it again when it is read.
/// </summary>
internal sealed class OrganizationIdConverter : ValueConverter<OrganizationId, long>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationIdConverter"/> class.
    /// </summary>
    public OrganizationIdConverter()
        : base(static wrapped => wrapped.Value, static stored => new OrganizationId(stored))
    {
    }
}
