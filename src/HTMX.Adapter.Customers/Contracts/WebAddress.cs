namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// The address of a website: an absolute http or https address, such as https://placeholder.example.
/// </summary>
public readonly record struct WebAddress
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebAddress"/> class.
    /// </summary>
    /// <param name="value">The address, such as https://placeholder.example; it must be an absolute http or https
    /// address with a host.</param>
    public WebAddress(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        // A page turns the address into a link and reads its host, so anything else is refused here, where it enters.
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? address)
            || !IsWebScheme(address.Scheme)
            || string.IsNullOrEmpty(address.Host))
        {
            throw new ArgumentException("A web address must be an absolute http or https address.", nameof(value));
        }

        this.Value = value;
    }

    /// <summary>
    /// Gets the address.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Says whether a scheme is one a browser opens as a web page.
    /// </summary>
    /// <param name="scheme">The address's scheme, which <see cref="Uri"/> gives in lower case.</param>
    /// <returns><see langword="true"/> for http and https.</returns>
    private static bool IsWebScheme(string scheme)
    {
        return scheme == Uri.UriSchemeHttps || scheme == Uri.UriSchemeHttp;
    }
}
