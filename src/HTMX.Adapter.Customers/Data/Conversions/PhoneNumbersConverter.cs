namespace HTMX.Adapter.Customers.Data.Conversions;

using System;
using System.Text.Json;
using HTMX.Adapter.Customers.Contracts;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// Stores <see cref="PhoneNumbers"/> in one text column as a JSON array of the numbers as written, such as
/// <c>["+1 555-0142","+1 555-0199"]</c>, and wraps them again when they are read. A contact's numbers are only ever
/// read and written with the contact, never searched, so they need no table of their own.
/// </summary>
internal sealed class PhoneNumbersConverter : ValueConverter<PhoneNumbers, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PhoneNumbersConverter"/> class.
    /// </summary>
    public PhoneNumbersConverter()
        : base(static numbers => ToText(numbers), static text => FromText(text))
    {
    }

    /// <summary>
    /// Writes the numbers as a JSON array of strings, in order.
    /// </summary>
    /// <param name="numbers">The numbers.</param>
    /// <returns>The JSON.</returns>
    private static string ToText(PhoneNumbers numbers)
    {
        string[] values = Array.ConvertAll(numbers.Values, static number => number.Value);
        return JsonSerializer.Serialize(values);
    }

    /// <summary>
    /// Reads the numbers back from the JSON array.
    /// </summary>
    /// <param name="text">The JSON.</param>
    /// <returns>The numbers.</returns>
    private static PhoneNumbers FromText(string text)
    {
        string[] values = JsonSerializer.Deserialize<string[]>(text) ?? [];
        return new PhoneNumbers(Array.ConvertAll(values, static value => new PhoneNumber(value)));
    }
}
