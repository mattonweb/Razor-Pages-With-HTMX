namespace HTMX.Application.Web.Authentication;

using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Signs every request in as Demo User, the seed's user 10001. The demo has no sign-in page, but its pages still read
/// who is signed in from the request's <c>User</c>, as they would behind a real one; an application with real sign-in
/// registers its own scheme instead, and no page changes.
/// </summary>
public sealed class DemoAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// The name the scheme is registered under.
    /// </summary>
    public const string SchemeName = "Demo";

    // Demo User, as the seed writes them: README.md names the seed's users.
    private const string DemoUserId = "10001";
    private const string DemoUserName = "Demo User";

    /// <summary>
    /// Initializes a new instance of the <see cref="DemoAuthenticationHandler"/> class.
    /// </summary>
    /// <param name="options">The scheme's options.</param>
    /// <param name="logger">Makes the handler's logger.</param>
    /// <param name="encoder">Encodes URLs, for handlers that redirect; this one never does.</param>
    public DemoAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <summary>
    /// Signs the request in as Demo User.
    /// </summary>
    /// <returns>Success, always, with Demo User's identifier and name as claims.</returns>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, DemoUserId),
            new Claim(ClaimTypes.Name, DemoUserName),
        ];
        ClaimsIdentity identity = new ClaimsIdentity(claims, SchemeName);
        ClaimsPrincipal user = new ClaimsPrincipal(identity);
        AuthenticationTicket ticket = new AuthenticationTicket(user, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
