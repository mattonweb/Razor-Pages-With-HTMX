namespace HTMX.Test.Integration.Application.Web.Pages.Organizations;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the form that creates an organization, at /organizations/create, run through the
/// whole application: what a save answers with HTMX and without it, and what a refused save answers.
/// </summary>
[TestClass]
public sealed class CreateTests : IDisposable
{
    #region Private Fields

    // The form's address, which it also posts to.
    private const string CreateAddress = "/organizations/create";

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that a save HTMX sends passes the antiforgery check and answers 204 with HX-Location
    /// naming the new organization's page, at the first identifier after the seed's.
    /// </summary>
    [TestMethod]
    public async Task Post_ByHtmx_Answers204AndLoadsTheNewOrganization()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        string token = await Requests.ReadAntiforgeryTokenAsync(browser, CreateAddress).ConfigureAwait(false);
        IEnumerable<KeyValuePair<string, string>> form = Html.Form(token, Fields("Integration Industries"));

        // Act (When)
        Answer answer = await Requests.PostAsync(browser, CreateAddress, HtmxRequest.Partial, form)
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.NoContent, "/organizations/11001"),
            (answer.Status, answer.Headers["HX-Location"]),
            "HTMX should get nothing to swap and the new organization's page to load.");
    }

    /// <summary>
    /// Integration test to verify that the same save sent by the browser on its own is redirected to the new
    /// organization's page, so that refreshing that page does not post again.
    /// </summary>
    [TestMethod]
    public async Task Post_WithoutHtmx_RedirectsToTheNewOrganization()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        string token = await Requests.ReadAntiforgeryTokenAsync(browser, CreateAddress).ConfigureAwait(false);
        IEnumerable<KeyValuePair<string, string>> form = Html.Form(token, Fields("Integration Industries"));

        // Act (When)
        Answer answer = await Requests.PostAsync(browser, CreateAddress, HtmxRequest.None, form)
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.Redirect, "/organizations/11001"),
            (answer.Status, answer.Headers["Location"]),
            "The browser should be redirected to the new organization's page.");
    }

    /// <summary>
    /// Integration test to verify that a save HTMX sends without a name is refused with 422 and the form alone, with
    /// the message, for HTMX to swap in place of the form.
    /// </summary>
    [TestMethod]
    public async Task Post_NameMissingByHtmx_Answers422WithTheFormAlone()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        string token = await Requests.ReadAntiforgeryTokenAsync(browser, CreateAddress).ConfigureAwait(false);
        IEnumerable<KeyValuePair<string, string>> form = Html.Form(token, Fields(string.Empty));

        // Act (When)
        Answer answer = await Requests.PostAsync(browser, CreateAddress, HtmxRequest.Partial, form)
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.UnprocessableEntity, false),
            (answer.Status, Html.IsWholePage(answer.Body)),
            "A refused save should get 422 and the form, not a page.");
        Assert.Contains(
            "Enter a name.",
            answer.Body,
            "The form should say what to correct.");
    }

    /// <summary>
    /// Stops the application and deletes its database.
    /// </summary>
    public void Dispose()
    {
        this._application.Dispose();
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Builds the form's fields: a name, and an industry, which the form also requires.
    /// </summary>
    /// <param name="name">The organization's name, or empty for none.</param>
    /// <returns>The fields, by name.</returns>
    private static Dictionary<string, string> Fields(string name)
    {
        return new Dictionary<string, string>
        {
            ["Input.Name"] = name,
            ["Input.Industry"] = "Manufacturing",
        };
    }

    #endregion Private Methods
}
