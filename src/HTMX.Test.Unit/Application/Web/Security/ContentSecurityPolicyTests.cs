namespace HTMX.Test.Unit.Application.Web.Security;

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
/// response carries, in production and in development, added to a pipeline as the application adds it.
/// </summary>
[TestClass]
public sealed class ContentSecurityPolicyTests
{
    #region Private Fields

    // The whole policy, written out, so that any change to it is made here too, on purpose.
    private const string Policy =
        "default-src 'none'; script-src 'self'; style-src 'self'; img-src 'self'; form-action 'self'; "
        + "frame-ancestors 'none'; base-uri 'none'; connect-src 'self'";

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Unit test to verify that outside development every response carries the policy, which allows nothing inline,
    /// nothing compiled from a string, and nothing from another site.
    /// </summary>
    /// <param name="environmentName">The environment the application runs in.</param>
    [TestMethod]
    [DataRow("Production", DisplayName = "Production")]
    [DataRow("Staging", DisplayName = "Staging")]
    public async Task UseContentSecurityPolicy_NotDevelopment_SendsThePolicy(string environmentName)
    {
        // Arrange (Given)
        DefaultHttpContext http = new DefaultHttpContext();

        // Act (When)
        await RunPipelineAsync(environmentName, http).ConfigureAwait(false);

        // Assert (Then)
        IHeaderDictionary headers = http.Response.Headers;
        Assert.AreEqual(
            Policy,
            headers.ContentSecurityPolicy.ToString(),
            "The response should carry the policy.");
    }

    /// <summary>
    /// Unit test to verify that in development the policy also lets hot reload's script reach the SDK or the IDE over
    /// a WebSocket on this machine, and is otherwise the same.
    /// </summary>
    [TestMethod]
    public async Task UseContentSecurityPolicy_Development_AlsoConnectsToHotReload()
    {
        // Arrange (Given)
        DefaultHttpContext http = new DefaultHttpContext();

        // Act (When)
        await RunPipelineAsync(Environments.Development, http).ConfigureAwait(false);

        // Assert (Then)
        IHeaderDictionary headers = http.Response.Headers;
        Assert.AreEqual(
            Policy + " ws://localhost:* wss://localhost:*",
            headers.ContentSecurityPolicy.ToString(),
            "Development's policy should add only the WebSocket to the connections allowed.");
    }

    /// <summary>
    /// Unit test to verify that the policy only adds a header, and the rest of the pipeline still answers.
    /// </summary>
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
