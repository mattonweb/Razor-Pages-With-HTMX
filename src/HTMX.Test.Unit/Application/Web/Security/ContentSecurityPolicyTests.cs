namespace HTMX.Test.Unit.Application.Web.Security;

using System;
using System.Threading.Tasks;
using HTMX.Application.Web.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="ContentSecurityPolicy"/> class: the policy every
/// response carries, with a nonce of its own, in production and in development, added to a pipeline as the
/// application adds it.
/// </summary>
[TestClass]
public sealed class ContentSecurityPolicyTests
{
    #region Private Fields

    // The whole policy, written out around the response's nonce, so that any change to it is made here too, on purpose.
    private const string ScriptsBeforeNonce = "default-src 'none'; script-src 'nonce-";
    private const string AfterScripts =
        "; style-src 'self'; img-src 'self'; form-action 'self'; frame-ancestors 'none'; base-uri 'none'; "
        + "require-trusted-types-for 'script'; trusted-types htmx; connect-src 'self'";

    // What Development adds: the hot reload script, by its path, and its WebSocket.
    private const string HotReloadScript =
        " http://localhost:*/_framework/aspnetcore-browser-refresh.js"
        + " https://localhost:*/_framework/aspnetcore-browser-refresh.js";

    private const string HotReloadConnections = " ws://localhost:* wss://localhost:*";

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Unit test to verify that outside development every response carries the policy, which runs only the scripts that
    /// carry the response's nonce, makes HTML from a string only through HTMX's Trusted Types policy, and allows
    /// nothing inline, nothing compiled from a string, and nothing from another site.
    /// </summary>
    /// <param name="environmentName">The environment the application runs in.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    [DataRow("Production", DisplayName = "Production")]
    [DataRow("Staging", DisplayName = "Staging")]
    public async Task UseContentSecurityPolicy_NotDevelopment_SendsThePolicy(string environmentName)
    {
        // Arrange (Given)
        DefaultHttpContext http = new DefaultHttpContext();

        // Act (When)
        await RunPipelineAsync(environmentName, http).ConfigureAwait(false);
        string nonce = ContentSecurityPolicy.Nonce(http);

        // Assert (Then)
        IHeaderDictionary headers = http.Response.Headers;
        Assert.AreEqual(
            ScriptsBeforeNonce + nonce + "'" + AfterScripts,
            headers.ContentSecurityPolicy.ToString(),
            "The response should carry the policy, naming its nonce.");
    }

    /// <summary>
    /// Unit test to verify that in development the policy also runs hot reload's script, by its path, and lets it reach
    /// the SDK or the IDE over a WebSocket on this machine, and is otherwise the same.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task UseContentSecurityPolicy_Development_AlsoAllowsHotReload()
    {
        // Arrange (Given)
        DefaultHttpContext http = new DefaultHttpContext();

        // Act (When)
        await RunPipelineAsync(Environments.Development, http).ConfigureAwait(false);
        string nonce = ContentSecurityPolicy.Nonce(http);

        // Assert (Then)
        IHeaderDictionary headers = http.Response.Headers;
        Assert.AreEqual(
            ScriptsBeforeNonce + nonce + "'" + HotReloadScript + AfterScripts + HotReloadConnections,
            headers.ContentSecurityPolicy.ToString(),
            "Development's policy should add only hot reload's script and its WebSocket.");
    }

    /// <summary>
    /// Unit test to verify that each response gets a nonce of its own, 128 random bits written as 32 hexadecimal
    /// digits, so that a nonce read from one page names no script on the next.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task UseContentSecurityPolicy_TwoResponses_GetNoncesOfTheirOwn()
    {
        // Arrange (Given)
        DefaultHttpContext first = new DefaultHttpContext();
        DefaultHttpContext second = new DefaultHttpContext();

        // Act (When)
        await RunPipelineAsync(Environments.Production, first).ConfigureAwait(false);
        await RunPipelineAsync(Environments.Production, second).ConfigureAwait(false);
        string one = ContentSecurityPolicy.Nonce(first);
        string other = ContentSecurityPolicy.Nonce(second);

        // Assert (Then)
        Assert.AreEqual(
            (32, 32, true, true, false),
            (one.Length, other.Length, IsHexadecimal(one), IsHexadecimal(other), one == other),
            "Each response should get its own nonce of 32 hexadecimal digits.");
    }

    /// <summary>
    /// Unit test to verify that asking for the nonce of a request the policy was not set for fails at once, rather than
    /// writing a script that no policy names.
    /// </summary>
    [TestMethod]
    public void Nonce_NoPolicy_Throws()
    {
        // Arrange (Given)
        DefaultHttpContext http = new DefaultHttpContext();

        // Act (When)
        InvalidOperationException thrown = Assert.ThrowsExactly<InvalidOperationException>(
            () => ContentSecurityPolicy.Nonce(http),
            "A request with no policy should have no nonce.");

        // Assert (Then)
        Assert.IsNotNull(
            thrown.Message,
            "The exception should say what is missing.");
    }

    /// <summary>
    /// Unit test to verify that the policy only adds a header, and the rest of the pipeline still answers.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task UseContentSecurityPolicy_AnyRequest_RunsTheRestOfThePipeline()
    {
        // Arrange (Given)
        DefaultHttpContext http = new DefaultHttpContext();

        // Act (When)
        await RunPipelineAsync(Environments.Production, http).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            StatusCodes.Status204NoContent,
            http.Response.StatusCode,
            "The endpoint after the policy should have answered.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Says whether a text is written only in lower-case hexadecimal digits.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns><see langword="true"/> when it is.</returns>
    private static bool IsHexadecimal(string text)
    {
        return text.Length > 0 && Array.TrueForAll(text.ToCharArray(), char.IsAsciiHexDigitLower);
    }

    /// <summary>
    /// Builds a pipeline as the application does, with the policy before an endpoint that answers 204, and runs one
    /// request through it.
    /// </summary>
    /// <param name="environmentName">The environment the application runs in.</param>
    /// <param name="http">The request, whose response the test reads.</param>
    /// <returns>A task that completes when the request has been answered.</returns>
    private static async Task RunPipelineAsync(string environmentName, HttpContext http)
    {
        HostingEnvironment environment = new HostingEnvironment { EnvironmentName = environmentName };
        using (ServiceProvider provider = new ServiceCollection().BuildServiceProvider())
        {
            ApplicationBuilder application = new ApplicationBuilder(provider);
            application.UseContentSecurityPolicy(environment);
            application.Run(static context =>
            {
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            });
            RequestDelegate pipeline = application.Build();
            await pipeline(http).ConfigureAwait(false);
        }
    }

    #endregion Private Methods
}
