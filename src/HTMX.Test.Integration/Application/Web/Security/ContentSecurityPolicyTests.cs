namespace HTMX.Test.Integration.Application.Web.Security;

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the Content Security Policy, run through the whole application: the middleware's
/// place in the pipeline puts the policy on every answer, a static file's included.
/// </summary>
[TestClass]
public sealed class ContentSecurityPolicyTests : IDisposable
{
    #region Private Fields

    // The policy in Development, which a test application runs in: the policy, with hot reload's WebSocket allowed.
    private const string DevelopmentPolicy =
        "default-src 'none'; script-src 'self'; style-src 'self'; img-src 'self'; form-action 'self'; "
        + "frame-ancestors 'none'; base-uri 'none'; connect-src 'self' ws://localhost:* wss://localhost:*";

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that a page, a static file and an address the application doesn't have each carry
    /// the policy.
    /// </summary>
    /// <param name="address">The address asked for.</param>
    /// <param name="status">The status it answers with.</param>
    [TestMethod]
    [DataRow("/organizations", HttpStatusCode.OK, DisplayName = "A page")]
    [DataRow("/icons.svg", HttpStatusCode.OK, DisplayName = "A static file")]
    [DataRow("/no-such-page", HttpStatusCode.NotFound, DisplayName = "An address the application doesn't have")]
    public async Task Get_AnyAnswer_CarriesThePolicy(string address, HttpStatusCode status)
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();

        // Act (When)
        Answer answer = await Requests.GetAsync(browser, address, HtmxRequest.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (status, DevelopmentPolicy),
            (answer.Status, answer.Headers["Content-Security-Policy"]),
            "Every answer should carry the policy.");
    }

    /// <summary>
    /// Stops the application and deletes its database.
    /// </summary>
    public void Dispose()
    {
        this._application.Dispose();
    }

    #endregion Public Methods
}
