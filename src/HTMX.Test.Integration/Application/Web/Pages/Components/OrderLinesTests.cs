namespace HTMX.Test.Integration.Application.Web.Pages.Components;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the Order lines page, at /components/order-lines, run through the whole application:
/// lines whose fields post with their positions in their names, which model binding reads in order, and which Remove
/// draws again numbered afresh, each line with its own values.
/// </summary>
[TestClass]
public sealed class OrderLinesTests : IDisposable
{
    #region Private Fields

    // The page's address, which its form also posts to, and its Remove.
    private const string Address = "/components/order-lines";
    private const string RemoveAddress = "/components/order-lines?handler=RemoveLine";

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that Remove, posted for the middle of three lines, answers with two lines numbered 0
    /// and 1, the second drawn with the third line's quantity rather than the one the removed line posted at its
    /// position: HTMX gets the lines alone, the browser on its own the page.
    /// </summary>
    /// <param name="kind">What sends the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="wholePage">Whether the answer should be the whole page.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.Partial), false, DisplayName = "With HTMX")]
    [DataRow(nameof(HtmxRequest.None), true, DisplayName = "Without HTMX")]
    public async Task PostRemoveLine_MiddleOfThree_AnswersTheLinesNumberedAfresh(string kind, bool wholePage)
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        string token = await Requests.ReadAntiforgeryTokenAsync(browser, Address).ConfigureAwait(false);
        List<KeyValuePair<string, string>> fields =
        [
            .. Line(0, "Sample paper", "2"),
            .. Line(1, "Mock mugs", "3"),
            .. Line(2, "Placeholder pens", "5"),
            Field("removeLine", "1"),
        ];

        // Act (When)
        HtmxRequest sender = Enum.Parse<HtmxRequest>(kind);
        Answer answer = await Requests.PostAsync(browser, RemoveAddress, sender, Html.Form(token, fields))
            .ConfigureAwait(false);
        bool thirdLine = answer.Body.Contains("Input.Lines[2]", StringComparison.Ordinal);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.OK, wholePage, false),
            (answer.Status, Html.IsWholePage(answer.Body), thirdLine),
            "The answer should be the lines or the page, with no third line.");
        Assert.Contains(
            "name=\"Input.Lines[1].Quantity\" value=\"5\"",
            answer.Body,
            "The second line should be drawn with the third line's quantity.");
    }

    /// <summary>
    /// Integration test to verify that model binding reads lines in order from position 0 and stops at the first
    /// position missing, so that a line after a gap is lost: the reason Add and Remove number the lines afresh.
    /// </summary>
    [TestMethod]
    public async Task Post_GapInThePositions_ReceivesOnlyTheLinesBeforeIt()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        string token = await Requests.ReadAntiforgeryTokenAsync(browser, Address).ConfigureAwait(false);
        List<KeyValuePair<string, string>> fields = [.. Line(0, "Sample paper", "2"), .. Line(2, "Mock mugs", "3")];

        // Act (When)
        Answer answer = await Requests.PostAsync(browser, Address, HtmxRequest.Partial, Html.Form(token, fields))
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            HttpStatusCode.OK,
            answer.Status,
            "The order should be received.");
        Assert.Contains(
            "Received an order of 1 line and 2 items.",
            answer.Body,
            "The line after the gap should be lost.");
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
    /// Makes one line's fields, named with its position as the form names them.
    /// </summary>
    /// <param name="position">The line's position, from 0.</param>
    /// <param name="product">The line's product.</param>
    /// <param name="quantity">The line's quantity.</param>
    /// <returns>The fields.</returns>
    private static KeyValuePair<string, string>[] Line(int position, string product, string quantity)
    {
        return
        [
            Field($"Input.Lines[{position}].Product", product),
            Field($"Input.Lines[{position}].Quantity", quantity),
        ];
    }

    #endregion Private Methods
}
