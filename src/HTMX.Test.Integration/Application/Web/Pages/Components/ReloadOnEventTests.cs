namespace HTMX.Test.Integration.Application.Web.Pages.Components;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the Reload on event page, at /components/reload-on-event, run through the whole
/// application: a note written with HTMX raises an event, and the list asked for in part holds the note.
/// </summary>
[TestClass]
public sealed class ReloadOnEventTests : IDisposable
{
    #region Private Fields

    // The page's address, which its form posts to and its list asks for itself again at.
    private const string Address = "/components/reload-on-event";

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that a note written with HTMX answers the form with HX-Trigger naming the event, and
    /// that the list, asked for in part as it asks when it hears the event, then holds the note.
    /// </summary>
    [TestMethod]
    public async Task Post_NoteByHtmx_RaisesTheEventTheListAnswers()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        string token = await Requests.ReadAntiforgeryTokenAsync(browser, Address).ConfigureAwait(false);
        IEnumerable<KeyValuePair<string, string>> form =
            Html.Form(token, [new KeyValuePair<string, string>("Input.Text", "Call the depot about Thursday.")]);

        // Act (When)
        Answer written = await Requests.PostAsync(browser, Address, HtmxRequest.Partial, form).ConfigureAwait(false);
        Answer list = await Requests.GetAsync(browser, Address, HtmxRequest.Partial).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.OK, "notes-changed", HttpStatusCode.OK, false),
            (written.Status, written.Headers["HX-Trigger"], list.Status, Html.IsWholePage(list.Body)),
            "The note should raise the event, and the list answer alone.");
        Assert.Contains(
            "Call the depot about Thursday.",
            list.Body,
            "The list should hold the note.");
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
