namespace HTMX.Application.Web.Validation;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Checks a form's telephone numbers as its rows post them: at most <see cref="Most"/>, and each one a telephone
/// number of 40 characters or fewer. A row left empty is no number at all: it passes, and a save leaves it out.
/// </summary>
/// <remarks>
/// Every row's field has the same name, so the rows bind to one list with no index numbers, and the message belongs
/// to the list. The form marks the rows whose number fails with <see cref="IsWellFormed"/>, the same rule.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class TelephoneNumbersAttribute : ValidationAttribute
{
    /// <summary>
    /// The most numbers a contact can have; the form offers no more rows than this.
    /// </summary>
    public const int Most = 5;

    // The longest number accepted, as written, spaces and punctuation included.
    private const int LongestNumber = 40;

    // What the field says when a save is refused for its numbers.
    private const string TooMany = "Keep it to five telephone numbers or fewer.";
    private const string NotANumber =
        "Enter each telephone number as a number of 40 characters or fewer, such as +1 555-0142.";

    // The framework's own rule for what looks like a telephone number.
    private static readonly PhoneAttribute Telephone = new PhoneAttribute();

    /// <summary>
    /// Says whether one row's value is a telephone number of 40 characters or fewer, or empty.
    /// </summary>
    /// <param name="number">The row's value, as posted.</param>
    /// <returns><see langword="true"/> when the value is acceptable.</returns>
    public static bool IsWellFormed(string? number)
    {
        string? trimmed = number?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return true;
        }

        return trimmed.Length <= LongestNumber && Telephone.IsValid(trimmed);
    }

    /// <inheritdoc/>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        IEnumerable<string?> numbers = value as IEnumerable<string?> ?? [];
        int written = 0;
        bool wellFormed = true;
        foreach (string? number in numbers)
        {
            written += string.IsNullOrWhiteSpace(number) ? 0 : 1;
            wellFormed &= IsWellFormed(number);
        }

        if (written > Most)
        {
            return new ValidationResult(TooMany);
        }

        return wellFormed ? ValidationResult.Success : new ValidationResult(NotANumber);
    }
}
