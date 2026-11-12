namespace HTMX.Application.Web.Validation;

using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Accepts an absolute http or https address with a host, such as https://placeholder.example, or no value at all. It
/// is the rule <c>WebAddress</c> checks when one is made, checked here first so that the form can say what is wrong
/// instead of failing; <see cref="RequiredAttribute"/> says whether a value must be given.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AbsoluteWebAddressAttribute : ValidationAttribute
{
    /// <summary>
    /// Says whether a value is an absolute http or https address, or empty.
    /// </summary>
    /// <param name="value">The value as bound: a string, or <see langword="null"/> when the field was left
    /// empty.</param>
    /// <returns><see langword="true"/> when the value is acceptable.</returns>
    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        return value is string text
            && Uri.TryCreate(text.Trim(), UriKind.Absolute, out Uri? address)
            && (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp)
            && !string.IsNullOrEmpty(address.Host);
    }
}
