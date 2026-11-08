namespace HTMX.Adapter.Customers.Data.Conversions;

using HTMX.Adapter.Customers.Contracts;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// Stores a <see cref="OrganizationName"/> as text, and wraps it again when it is read.
/// </summary>
internal sealed class OrganizationNameConverter : ValueConverter<OrganizationName, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationNameConverter"/> class.
    /// </summary>
    public OrganizationNameConverter()
        : base(static wrapped => wrapped.Value, static stored => new OrganizationName(stored))
    {
    }
}
