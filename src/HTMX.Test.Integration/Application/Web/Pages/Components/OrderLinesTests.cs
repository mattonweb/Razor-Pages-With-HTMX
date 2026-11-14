namespace HTMX.Test.Integration.Application.Web.Pages.Components;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Playwright;
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
    /// Browser test to verify, in Edge, that removing the middle of three lines leaves two, the second holding the
    /// third's values and the keyboard, with HTMX, without HTMX's script, and with script off, when the browser posts
    /// the form to Remove's handler itself. The console reports no error.
    /// </summary>
    /// <param name="scripting">How much script the page runs, by its name in <see cref="Scripting"/>.</param>
    [TestMethod]
    [TestCategory("Browser")]
    [DoNotParallelize]
    [DataRow(nameof(Scripting.On), DisplayName = "With HTMX")]
    [DataRow(nameof(Scripting.WithoutHtmx), DisplayName = "Without HTMX's script")]
    [DataRow(nameof(Scripting.Off), DisplayName = "With script off")]
    public async Task RemoveLine_InTheMiddle_LeavesTheKeyboardOnTheLineInItsPlace(string scripting)
    {
        // Arrange (Given)
        Uri application = this._application.StartServerForBrowser();
        BrowserSession edge = await BrowserSession.OpenAsync(application, Enum.Parse<Scripting>(scripting))
            .ConfigureAwait(false);
        await using (edge.ConfigureAwait(false))
        {
            IPage page = edge.Page;
            await page.GotoAsync(Address).ConfigureAwait(false);
            await FillLineAsync(page, 0, "Sample paper", "2").ConfigureAwait(false);
            await AddLineAsync(page, 1).ConfigureAwait(false);
            await FillLineAsync(page, 1, "Mock mugs", "3").ConfigureAwait(false);
            await AddLineAsync(page, 2).ConfigureAwait(false);
            await FillLineAsync(page, 2, "Placeholder pens", "5").ConfigureAwait(false);

            // Act (When)
            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Remove line 2" })
                .ClickAsync()
                .ConfigureAwait(false);
            await page.Locator("#Input_Lines_2__Product").WaitForAsync(Gone()).ConfigureAwait(false);
            await page.WaitForLoadStateAsync().ConfigureAwait(false);
            string product = await page.Locator("#Input_Lines_1__Product").InputValueAsync().ConfigureAwait(false);
            string quantity = await page.Locator("#Input_Lines_1__Quantity").InputValueAsync().ConfigureAwait(false);
            string? focused = await page.Locator(":focus").GetAttributeAsync("aria-label").ConfigureAwait(false);

            // Assert (Then)
            Assert.AreEqual(
                ("Placeholder pens", "5", "Product, line 2"),
                (product, quantity, focused),
                "The second line should hold the third's values, and the keyboard.");
            Assert.IsEmpty(
                edge.ConsoleErrors,
                "The console should report no error: " + string.Join(" | ", edge.ConsoleErrors));
        }
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
    /// Says to wait until an element has left the page.
    /// </summary>
    /// <returns>The wait's options.</returns>
    private static LocatorWaitForOptions Gone()
    {
        return new LocatorWaitForOptions { State = WaitForSelectorState.Detached };
    }

    /// <summary>
    /// Chooses a line's product and writes its quantity, in the browser.
    /// </summary>
    /// <param name="page">The page.</param>
    /// <param name="position">The line's position, from 0.</param>
    /// <param name="product">The product to choose.</param>
    /// <param name="quantity">The quantity to write.</param>
    /// <returns>A task that completes when both are set.</returns>
    private static async Task FillLineAsync(IPage page, int position, string product, string quantity)
    {
        await page.Locator($"#Input_Lines_{position}__Product").SelectOptionAsync(product).ConfigureAwait(false);
        await page.Locator($"#Input_Lines_{position}__Quantity").FillAsync(quantity).ConfigureAwait(false);
    }

    /// <summary>
    /// Presses Add a line, in the browser, and waits for the new line.
    /// </summary>
    /// <param name="page">The page.</param>
    /// <param name="position">The new line's position, from 0.</param>
    /// <returns>A task that completes when the new line is on the page.</returns>
    private static async Task AddLineAsync(IPage page, int position)
    {
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Add a line" })
            .ClickAsync()
            .ConfigureAwait(false);
        await page.Locator($"#Input_Lines_{position}__Product").WaitForAsync().ConfigureAwait(false);
    }

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
