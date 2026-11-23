namespace HTMX.Application.Web.Security;

using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

/// <summary>
/// The Content Security Policy every response carries, which tells the browser what a page may load and run: only the
/// scripts that carry the response's nonce, stylesheets and images from the application's own files, HTML made from a
/// string only through HTMX's Trusted Types policy, requests and form posts to the application, and nothing else.
/// Razor encodes what a page writes, so a user's text never becomes markup; if markup got in some other way, the policy
/// stops it running a script or sending what the page holds to another site.
/// </summary>
/// <remarks>
/// HTMX 4 needs nothing relaxed, because no page uses what HTMX compiles from a string: <c>hx-on</c>, a trigger filter
/// such as <c>click[shiftKey]</c>, or a <c>js:</c> value. Those need <c>'unsafe-eval'</c>, which would let a script
/// that got in compile more. No page holds an inline script or a <c>style</c> attribute either, which would need
/// <c>'unsafe-inline'</c>. Scripts are named by a nonce rather than by <c>'self'</c>, which would run any file the
/// application's own address serves as a script; and the policy leaves out <c>'strict-dynamic'</c>, which would trust
/// every script a trusted script creates, as HTMX creates one for each script in an answer it swaps in. The header is
/// set before the rest of the pipeline runs, so it goes with every answer, the production error page's too; the
/// developer exception page clears it, and so keeps its own inline script and styles.
/// </remarks>
public static class ContentSecurityPolicy
{
    // A nonce is 128 random bits, written as 32 hexadecimal digits, new for every response.
    private const int NonceDigits = 32;

    // Nothing loads unless a directive below allows it: no plugins, frames, fonts, media or workers.
    private const string Fallback = "default-src 'none'";

    // In development, also the hot reload script that the SDK adds to each page, which it adds without a nonce: that
    // one file, by its path, on this machine.
    private const string HotReloadScript =
        " http://localhost:*/_framework/aspnetcore-browser-refresh.js"
        + " https://localhost:*/_framework/aspnetcore-browser-refresh.js";

    // Stylesheets only from the application's own files: no <style> element and no style attribute.
    private const string Styles = "style-src 'self'";

    // Images, the icon sprite among them, only from the application's own files.
    private const string Images = "img-src 'self'";

    // Forms post only to the application. This directive and the two below do not fall back to default-src.
    private const string Forms = "form-action 'self'";

    // No other site may show these pages in a frame, which is how clickjacking works.
    private const string Framing = "frame-ancestors 'none'";

    // No <base> element may change the address that relative links and form actions resolve against.
    private const string BaseAddress = "base-uri 'none'";

    // A string becomes HTML or script only through the Trusted Types policy named htmx, which the application's script
    // gives HTMX; a string written into the page any other way is refused. The page may create one more, the policy
    // named default, which the browser asks for a script address written as a string, as HTMX copies a script's src:
    // the application's makes each an empty address, so such a script never runs and the swap goes through.
    private const string TrustedTypes = "require-trusted-types-for 'script'; trusted-types htmx default";

    // HTMX's requests only to the application.
    private const string Connections = "connect-src 'self'";

    // In development, the hot reload script also talks to the SDK or the IDE, over a WebSocket on another port of this
    // machine.
    private const string DevelopmentConnections = "connect-src 'self' ws://localhost:* wss://localhost:*";

    // Where a response's nonce waits for the layout, among the request's items.
    private static readonly object NonceKey = new object();

    /// <summary>
    /// Adds the policy to every response that the rest of the pipeline writes, with a new nonce for each. Development's
    /// policy also lets hot reload run and reach the SDK or the IDE; every other environment gets the policy alone.
    /// </summary>
    /// <param name="application">The application's request pipeline.</param>
    /// <param name="environment">The environment the application runs in.</param>
    /// <returns>The pipeline, to add more to.</returns>
    public static IApplicationBuilder UseContentSecurityPolicy(
        this IApplicationBuilder application,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(environment);
        bool development = environment.IsDevelopment();
        return application.Use((context, next) =>
        {
            string nonce = RandomNumberGenerator.GetHexString(NonceDigits, lowercase: true);
            context.Items[NonceKey] = nonce;
            IHeaderDictionary headers = context.Response.Headers;
            headers.ContentSecurityPolicy = PolicyFor(nonce, development);
            return next(context);
        });
    }

    /// <summary>
    /// Gives the nonce this response's policy names, which the layout writes on each of its scripts.
    /// </summary>
    /// <param name="context">The request.</param>
    /// <returns>The nonce.</returns>
    /// <exception cref="InvalidOperationException">The pipeline set no policy for the request: the application is
    /// misconfigured.</exception>
    public static string Nonce(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Items[NonceKey] as string
            ?? throw new InvalidOperationException("The request has no Content Security Policy, so no nonce.");
    }

    /// <summary>
    /// Writes the policy for one response.
    /// </summary>
    /// <param name="nonce">The response's nonce.</param>
    /// <param name="development">Whether the application runs in development.</param>
    /// <returns>The policy.</returns>
    private static string PolicyFor(string nonce, bool development)
    {
        // Scripts only when they carry the nonce: no inline script without it, no onclick attribute, nothing compiled
        // from a string, and no other file, the application's own included.
        string scripts = "script-src 'nonce-" + nonce + "'" + (development ? HotReloadScript : string.Empty);
        string connections = development ? DevelopmentConnections : Connections;
        return string.Join(
            "; ",
            Fallback,
            scripts,
            Styles,
            Images,
            Forms,
            Framing,
            BaseAddress,
            TrustedTypes,
            connections);
    }
}
