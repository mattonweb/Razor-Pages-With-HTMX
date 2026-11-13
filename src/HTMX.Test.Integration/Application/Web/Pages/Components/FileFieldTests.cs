namespace HTMX.Test.Integration.Application.Web.Pages.Components;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the File field page, at /components/file-field, run through the whole application:
/// a form that sends a file as multipart form data, past the antiforgery check and model binding.
/// </summary>
[TestClass]
public sealed class FileFieldTests : IDisposable
{
    #region Private Fields

    // The page's address, which its form also posts to.
    private const string Address = "/components/file-field";

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that a file HTMX sends, multipart with the page's token, is received: the answer is
    /// the form again and, for the toast region, a toast that says what came.
    /// </summary>
    [TestMethod]
    public async Task Post_FileByHtmx_AnswersTheFormAndAToast()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        string token = await Requests.ReadAntiforgeryTokenAsync(browser, Address).ConfigureAwait(false);
        IEnumerable<KeyValuePair<string, string>> fields = Html.Form(token, [Field("Input.Title", "Quarterly logo")]);

        // Act (When)
        Answer answer = await Requests.PostFileAsync(browser, Address, HtmxRequest.Partial, fields, Png())
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.OK, false),
            (answer.Status, Html.IsWholePage(answer.Body)),
            "HTMX should get the form, not a page.");
        Assert.Contains(
            "Received logo.png: a PNG image of 1 KB.",
            answer.Body,
            "The answer should carry the toast that says what was received.");
    }

    /// <summary>
    /// Integration test to verify that a file sent with no title is refused with 422, and the answer's file input says
    /// hx-preserve, so that HTMX keeps the file the user chose.
    /// </summary>
    [TestMethod]
    public async Task Post_TitleMissingByHtmx_KeepsTheFileInput()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        string token = await Requests.ReadAntiforgeryTokenAsync(browser, Address).ConfigureAwait(false);
        IEnumerable<KeyValuePair<string, string>> fields = Html.Form(token, [Field("Input.Title", string.Empty)]);

        // Act (When)
        Answer answer = await Requests.PostFileAsync(browser, Address, HtmxRequest.Partial, fields, Png())
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            HttpStatusCode.UnprocessableEntity,
            answer.Status,
            "A form with no title should be refused with 422.");
        Assert.Contains(
            "hx-preserve",
            answer.Body,
            "The file input should ask HTMX to keep the one on the page.");
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

    /// <summary>
    /// Makes a small PNG file, as the file input sends it: its signature, then zeros.
    /// </summary>
    /// <returns>The file.</returns>
    private static FilePart Png()
    {
        byte[] content = new byte[600];
        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        signature.CopyTo(content, 0);
        return new FilePart { Field = "Input.File", FileName = "logo.png", Content = content };
    }

    #endregion Private Methods
}
