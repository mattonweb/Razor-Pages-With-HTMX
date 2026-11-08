namespace HTMX.Adapter.Customers.Data.Conversions;

using HTMX.Adapter.Customers.Contracts;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// Stores a <see cref="UserId"/> as an integer, and wraps it again when it is read.
/// </summary>
internal sealed class UserIdConverter : ValueConverter<UserId, long>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserIdConverter"/> class.
    /// </summary>
    public UserIdConverter()
        : base(static wrapped => wrapped.Value, static stored => new UserId(stored))
    {
    }
}
