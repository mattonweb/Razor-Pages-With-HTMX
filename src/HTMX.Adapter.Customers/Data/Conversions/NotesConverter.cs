namespace HTMX.Adapter.Customers.Data.Conversions;

using HTMX.Adapter.Customers.Contracts;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// Stores a <see cref="Notes"/> as text, and wraps it again when it is read.
/// </summary>
internal sealed class NotesConverter : ValueConverter<Notes, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotesConverter"/> class.
    /// </summary>
    public NotesConverter()
        : base(static wrapped => wrapped.Value, static stored => new Notes(stored))
    {
    }
}
