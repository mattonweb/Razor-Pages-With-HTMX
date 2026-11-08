namespace HTMX.Adapter.Contacts.Data.Conversions;

using HTMX.Adapter.Contacts.Contracts;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// Stores a <see cref="WebAddress"/> as text, and wraps it again when it is read.
/// </summary>
internal sealed class WebAddressConverter : ValueConverter<WebAddress, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebAddressConverter"/> class.
    /// </summary>
    public WebAddressConverter()
        : base(static wrapped => wrapped.Value, static stored => new WebAddress(stored))
    {
    }
}
