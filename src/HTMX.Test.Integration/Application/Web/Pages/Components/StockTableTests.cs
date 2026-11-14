namespace HTMX.Test.Integration.Application.Web.Pages.Components;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the Stock table page, at /components/stock-table, run through the whole
/// application: a row's form, which holds only the antiforgery token and Save, posts with the row's field, which names
/// the form from another cell, as a browser and HTMX send them.
/// </summary>
[TestClass]
public sealed class StockTableTests : IDisposable
{
    #region Private Fields

    // The page's address, and the address the Mock mugs row's form posts to.
    private const string Address = "/components/stock-table";
    private const string MugsSave = "/components/stock-table?item=10003&handler=Save";

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that a row's form, posted with its field and its token as HTMX sends them, saves the
    /// count and answers with the row alone and a toast.
    /// </summary>
    [TestMethod]
    public async Task PostSave_CountByHtmx_AnswersTheRowAndAToast()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        string token = await Requests.ReadAntiforgeryTokenAsync(browser, Address).ConfigureAwait(false);
        IEnumerable<KeyValuePair<string, string>> form = Html.Form(token, [Field("Input.OnHand", "15")]);

        // Act (When)
        Answer answer = await Requests.PostAsync(browser, MugsSave, HtmxRequest.Partial, form).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.OK, false),
            (answer.Status, Html.IsWholePage(answer.Body)),
            "HTMX should get the row, not a page.");
        Assert.Contains(
            "Mock mugs: 15 on hand.",
            answer.Body,
            "The answer should carry the toast that says what was saved.");
    }

    /// <summary>
    /// Integration test to verify that a count out of range, posted without HTMX, answers 422 with the whole page, in
    /// which only the row it was posted for is marked.
    /// </summary>
    [TestMethod]
    public async Task PostSave_CountOutOfRangeWithoutHtmx_MarksOnlyItsRow()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        string token = await Requests.ReadAntiforgeryTokenAsync(browser, Address).ConfigureAwait(false);
        IEnumerable<KeyValuePair<string, string>> form = Html.Form(token, [Field("Input.OnHand", "1500")]);

        // Act (When)
        Answer answer = await Requests.PostAsync(browser, MugsSave, HtmxRequest.None, form).ConfigureAwait(false);
        int marked = answer.Body.Split("aria-invalid=\"true\"").Length - 1;

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.UnprocessableEntity, true, 1),
            (answer.Status, Html.IsWholePage(answer.Body), marked),
            "The page should come back refused, with one field marked.");
        Assert.Contains(
            "aria-describedby=\"on-hand-10003-error\"",
            answer.Body,
            "The Mock mugs row's field should be the one marked, naming its message.");
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
    /// Makes one of a form's fields.
    /// </summary>
    /// <param name="name">The field's name.</param>
    /// <param name="value">Its value.</param>
    /// <returns>The field.</returns>
    private static KeyValuePair<string, string> Field(string name, string value)
    {
        return new KeyValuePair<string, string>(name, value);
    }

    #endregion Private Methods
}
