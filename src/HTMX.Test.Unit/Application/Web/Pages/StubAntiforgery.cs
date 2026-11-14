namespace HTMX.Test.Unit.Application.Web.Pages;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Stands in for the antiforgery service in tests: it hands out one set of tokens and checks nothing.
/// </summary>
internal sealed class StubAntiforgery : IAntiforgery
{
    private readonly AntiforgeryTokenSet _tokens;

    /// <summary>
    /// Initializes a new instance of the <see cref="StubAntiforgery"/> class.
    /// </summary>
    /// <param name="tokens">The tokens it hands out.</param>
    public StubAntiforgery(AntiforgeryTokenSet tokens)
    {
        this._tokens = tokens;
    }

    /// <inheritdoc/>
    public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
    {
        return this._tokens;
    }

    /// <inheritdoc/>
    public AntiforgeryTokenSet GetTokens(HttpContext httpContext)
    {
        return this._tokens;
    }

    /// <inheritdoc/>
    public Task<bool> IsRequestValidAsync(HttpContext httpContext)
    {
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task ValidateRequestAsync(HttpContext httpContext)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void SetCookieTokenAndHeader(HttpContext httpContext)
    {
        // Nothing to set: no cookie is checked.
    }
}
