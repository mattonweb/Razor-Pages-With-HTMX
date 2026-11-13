namespace HTMX.Test.Integration.Application.Web.Pages.Contacts;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the form that edits a contact, at /contacts/{id}/edit, run through the whole
/// application: its telephone number rows, which Add posts the form to draw again, and which a save keeps in order.
/// Hadley Handout, contact 10007, has two numbers in the seed.
/// </summary>
[TestClass]
public sealed class EditTests : IDisposable
{
    #region Private Fields

    // Hadley Handout's edit page, which the form also posts to, and its Add.
    private const string EditAddress = "/contacts/10007/edit";
    private const string AddAddress = "/contacts/10007/edit?handler=AddPhoneNumber";

    // The name every row's field has.
    private const string RowName = "name=\"Input.PhoneNumbers\"";

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that Add, posted with the form's two rows, answers HTMX with the telephone number
    /// field alone, holding three rows, the new one taking the keyboard; and the browser on its own with the page.
    /// </summary>
    /// <param name="kind">What sends the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="wholePage">Whether the answer should be the whole page.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.Partial), false, DisplayName = "With HTMX")]
    [DataRow(nameof(HtmxRequest.None), true, DisplayName = "Without HTMX")]
    public async Task PostAddPhoneNumber_TwoRows_AnswersThreeRows(string kind, bool wholePage)
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        string token = await Requests.ReadAntiforgeryTokenAsync(browser, EditAddress).ConfigureAwait(false);
        IEnumerable<KeyValuePair<string, string>> form = Html.Form(token, Rows("+1 555-0188", "+1 555-0189"));

        // Act (When)
        Answer answer = await Requests.PostAsync(browser, AddAddress, Enum.Parse<HtmxRequest>(kind), form)
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.OK, wholePage, 3),
            (answer.Status, Html.IsWholePage(answer.Body), answer.Body.Split(RowName).Length - 1),
            "The answer should be the field or the page, with a third row.");
        Assert.Contains(
            "autofocus",
            answer.Body,
            "The new row should take the keyboard.");
    }

    /// <summary>
    /// Integration test to verify that a save HTMX sends with the numbers in a new order answers 204 and the contact's
    /// page, and that the page then lists them in that order, read back from the database.
    /// </summary>
    [TestMethod]
    public async Task Post_NumbersReorderedByHtmx_SavesThemInOrder()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        string token = await Requests.ReadAntiforgeryTokenAsync(browser, EditAddress).ConfigureAwait(false);
        List<KeyValuePair<string, string>> fields =
        [
            Field("Input.Version", "1"),
            Field("Input.Name", "Hadley Handout"),
            Field("Input.OrganizationId", "10001"),
            .. Rows("+1 555-0189", "+1 555-0188"),
        ];

        // Act (When)
        Answer saved = await Requests.PostAsync(browser, EditAddress, HtmxRequest.Partial, Html.Form(token, fields))
            .ConfigureAwait(false);
        Answer page = await Requests.GetAsync(browser, "/contacts/10007", HtmxRequest.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.NoContent, "/contacts/10007"),
            (saved.Status, saved.Headers["HX-Location"]),
            "The save should go to the contact's page.");

        // Razor writes + as &#x2B;, so the page is searched for the numbers' digits.
        int first = page.Body.IndexOf("555-0189", StringComparison.Ordinal);
        int second = page.Body.IndexOf("555-0188", StringComparison.Ordinal);
        Assert.IsTrue(
            first >= 0 && first < second,
            "The contact's page should list the numbers in their new order.");
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
    /// Makes the telephone number rows' fields, which share a name, in order.
    /// </summary>
    /// <param name="numbers">The rows' numbers.</param>
    /// <returns>The fields.</returns>
    private static KeyValuePair<string, string>[] Rows(params string[] numbers)
    {
        return Array.ConvertAll(numbers, static number => Field("Input.PhoneNumbers", number));
    }

    #endregion Private Methods
}
