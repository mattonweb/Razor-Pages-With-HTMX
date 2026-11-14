namespace HTMX.Test.Integration.Application.Web.Pages.Components;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authorization;
using HTMX.Application.Web.Pages.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the Not allowed page, at /components/not-allowed, run through the whole application:
/// Publish runs for a user the Publishing policy lets publish, and, once the Publishing permission page has withdrawn
/// that permission, is refused with 403 and the list as the user may now use it, with HTMX and without it.
/// </summary>
[TestClass]
public sealed class NotAllowedTests : IDisposable
{
    #region Private Fields

    // The page, the address the Mock price list row's Publish posts to, and the permission page's Withdraw.
    private const string Address = "/components/not-allowed";
    private const string PublishPriceList = "/components/not-allowed?article=10003&handler=Publish";
    private const string PermissionPage = "/components/publishing-permission";
    private const string Withdraw = "/components/publishing-permission?handler=Withdraw";

    // What the refusal's toast says.
    private const string RefusedMessage = "You may not publish articles. The list now shows only what you may do.";

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that a user who may publish publishes an article with HTMX, and the answer is the
    /// list alone, the row now offering Unpublish, and a toast.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task PostPublish_MayPublishByHtmx_AnswersTheListAndAToast()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        string token = await Requests.ReadAntiforgeryTokenAsync(browser, Address).ConfigureAwait(false);
        IEnumerable<KeyValuePair<string, string>> form = Html.Form(token, []);

        // Act (When)
        Answer answer = await Requests.PostAsync(browser, PublishPriceList, HtmxRequest.Partial, form)
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.OK, false),
            (answer.Status, Html.IsWholePage(answer.Body)),
            "HTMX should get the list, not a page.");
        Assert.Contains(
            "Mock price list published.",
            answer.Body,
            "The answer should carry the toast that says what was published.");
    }

    /// <summary>
    /// Integration test to verify that Publish, posted from a page drawn before the Publishing permission page
    /// withdrew the permission, is refused with 403 and the list drawn without Publish, with the toast that says why:
    /// the list alone for HTMX, and the whole page without it; and nothing is published.
    /// </summary>
    /// <param name="kind">What sends the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="listAlone">Whether the answer should be the list alone.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.Partial), true, DisplayName = "With HTMX")]
    [DataRow(nameof(HtmxRequest.None), false, DisplayName = "Without HTMX")]
    public async Task PostPublish_PermissionWithdrawn_RefusesWithTheList(string kind, bool listAlone)
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        Answer drawn = await Requests.GetAsync(browser, Address, HtmxRequest.None).ConfigureAwait(false);
        IEnumerable<KeyValuePair<string, string>> form = Html.Form(Html.AntiforgeryToken(drawn.Body), []);
        Answer withdrawn = await Requests.PostAsync(browser, Withdraw, HtmxRequest.None, form).ConfigureAwait(false);

        // Act (When)
        HtmxRequest sender = Enum.Parse<HtmxRequest>(kind);
        Answer answer = await Requests.PostAsync(browser, PublishPriceList, sender, form).ConfigureAwait(false);
        bool offered = drawn.Body.Contains("id=\"publish-10003\"", StringComparison.Ordinal);
        bool stillOffered = answer.Body.Contains("id=\"publish-", StringComparison.Ordinal);
        ArticleDesk desk = this._application.Services.GetRequiredService<ArticleDesk>();

        // Assert (Then)
        Assert.AreEqual(
            (true, HttpStatusCode.Redirect, PermissionPage),
            (offered, withdrawn.Status, withdrawn.Headers["Location"]),
            "The page should have offered Publish before the permission was withdrawn.");
        Assert.AreEqual(
            (HttpStatusCode.Forbidden, !listAlone, false, false),
            (answer.Status, Html.IsWholePage(answer.Body), stillOffered, desk.All()[2].Published),
            "Publish should be refused with the list drawn without it, and nothing published.");
        Assert.Contains(
            RefusedMessage,
            answer.Body,
            "The answer should carry the toast that says why.");
    }

    /// <summary>
    /// Browser test to verify, in Edge, that Publish pressed on a page drawn before the permission was withdrawn is
    /// refused with the list drawn without it, the toast saying why, and the keyboard on the list's heading, since the
    /// button that had it is gone: with HTMX, and with script off, when the browser posts the form itself and the
    /// heading takes the keyboard as the page loads. The console reports nothing but the browser's own line for the
    /// 403.
    /// </summary>
    /// <param name="scripting">How much script the page runs, by its name in <see cref="Scripting"/>.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    [TestCategory("Browser")]
    [DoNotParallelize]
    [DataRow(nameof(Scripting.On), DisplayName = "With HTMX")]
    [DataRow(nameof(Scripting.Off), DisplayName = "With script off")]
    public async Task Publish_AfterThePermissionWasWithdrawn_PutsTheKeyboardOnTheList(string scripting)
    {
        // Arrange (Given)
        Uri application = this._application.StartServerForBrowser();
        BrowserSession edge = await BrowserSession.OpenAsync(application, Enum.Parse<Scripting>(scripting))
            .ConfigureAwait(false);
        await using (edge.ConfigureAwait(false))
        {
            IPage page = edge.Page;
            await page.GotoAsync(Address).ConfigureAwait(false);
            ILocator toast = page.Locator(".toast .message");
            Publishers publishers = this._application.Services.GetRequiredService<Publishers>();

            // Act (When)
            publishers.Withdraw(new UserId(10001));
            await page.Locator("#publish-10003").ClickAsync().ConfigureAwait(false);
            await toast.WaitForAsync().ConfigureAwait(false);
            string? said = await toast.TextContentAsync().ConfigureAwait(false);
            int buttons = await page.Locator("#articles button").CountAsync().ConfigureAwait(false);
            string focused = await page.EvaluateAsync<string>("() => document.activeElement.id").ConfigureAwait(false);

            // Assert (Then)
            ArticleDesk desk = this._application.Services.GetRequiredService<ArticleDesk>();
            Assert.AreEqual(
                (RefusedMessage, 0, "articles-heading", false),
                (said?.Trim(), buttons, focused, desk.All()[2].Published),
                "Publish should be refused with the list drawn without it and the keyboard on its heading.");
            string[] others = Array.FindAll(
                edge.ConsoleErrors,
                static error => !error.Contains("status of 403", StringComparison.Ordinal));
            Assert.IsEmpty(
                others,
                "The console should report nothing but the 403: " + string.Join(" | ", others));
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
}
