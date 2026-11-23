namespace HTMX.Test.Integration.Application.Web.Security;

using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the Content Security Policy, run through the whole application: the middleware's
/// place in the pipeline puts the policy on every answer, a static file's included, each with a nonce of its own that
/// the layout's scripts carry; and HTMX, under it, gives a script in an answer no text.
/// </summary>
[TestClass]
public sealed class ContentSecurityPolicyTests : IDisposable
{
    #region Private Fields

    // The policy in Development, which a test application runs in, written out around the answer's nonce: the policy,
    // with hot reload's script and its WebSocket allowed.
    private const string BeforeNonce = "default-src 'none'; script-src 'nonce-";
    private const string AfterNonce =
        "' http://localhost:*/_framework/aspnetcore-browser-refresh.js"
        + " https://localhost:*/_framework/aspnetcore-browser-refresh.js; style-src 'self'; img-src 'self'; "
        + "form-action 'self'; frame-ancestors 'none'; base-uri 'none'; require-trusted-types-for 'script'; "
        + "trusted-types htmx default; connect-src 'self' ws://localhost:* wss://localhost:*";

    // The file a planted script names, which the application doesn't have.
    private const string PlantedFile = "/planted-browser-link.js";

    // The nonce in a policy, and in a script tag Razor wrote.
    private static readonly Regex PolicyNonce = new Regex(
        "'nonce-([0-9a-f]{32})'",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex ScriptTag = new Regex(
        "<script\\b[^>]*>",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that a page, a static file and an address the application doesn't have each carry
    /// the policy, with a nonce.
    /// </summary>
    /// <param name="address">The address asked for.</param>
    /// <param name="status">The status it answers with.</param>
    /// <returns>A task that completes when the test has run.</returns>
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
        string policy = answer.Headers["Content-Security-Policy"];
        string nonce = PolicyNonce.Match(policy).Groups[1].Value;

        // Assert (Then)
        Assert.AreEqual(
            (status, BeforeNonce + nonce + AfterNonce),
            (answer.Status, policy),
            "Every answer should carry the policy, with a nonce.");
    }

    /// <summary>
    /// Integration test to verify that each of a page's scripts carries the nonce its policy names, and that the next
    /// page gets another.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task Get_APage_NamesEachOfItsScriptsByTheNonce()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();

        // Act (When)
        Answer page = await Requests.GetAsync(browser, "/components", HtmxRequest.None).ConfigureAwait(false);
        Answer next = await Requests.GetAsync(browser, "/components", HtmxRequest.None).ConfigureAwait(false);
        string nonce = PolicyNonce.Match(page.Headers["Content-Security-Policy"]).Groups[1].Value;
        string nextNonce = PolicyNonce.Match(next.Headers["Content-Security-Policy"]).Groups[1].Value;
        MatchCollection scripts = ScriptTag.Matches(page.Body);
        int named = 0;
        foreach (Match script in scripts)
        {
            named += script.Value.Contains($"nonce=\"{nonce}\"", StringComparison.Ordinal) ? 1 : 0;
        }

        // Assert (Then)
        Assert.AreEqual(
            (3, 3, false),
            (scripts.Count, named, nonce == nextNonce),
            "The page's three scripts should each carry its nonce, and the next page should get another.");
    }

    /// <summary>
    /// Browser test to verify, in Edge, that a script in an answer HTMX swaps in never runs: the Trusted Types policy
    /// the application gives HTMX hands it no text, so the answer swaps in whole and nothing in it runs. The console
    /// reports no error, so the policy refused nothing HTMX does.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    [TestCategory("Browser")]
    [DoNotParallelize]
    public async Task LoadMore_AnAnswerWithAScript_SwapsItInWithoutRunningIt()
    {
        // Arrange (Given)
        Uri application = this._application.StartServerForBrowser();
        BrowserSession edge = await BrowserSession.OpenAsync(application, Scripting.On).ConfigureAwait(false);
        await using (edge.ConfigureAwait(false))
        {
            IPage page = edge.Page;
            await page.GotoAsync("/components/load-more").ConfigureAwait(false);
            await page.RouteAsync("**/components/load-more?*after=*", static route => route.FulfillAsync(Planted()))
                .ConfigureAwait(false);

            // Act (When)
            await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Load more" }).ClickAsync()
                .ConfigureAwait(false);
            await page.Locator("#planted").WaitForAsync().ConfigureAwait(false);
            bool ran = await page.EvaluateAsync<bool>("() => window.plantedScriptRan === true").ConfigureAwait(false);
            string? text = await page.Locator("#planted script").TextContentAsync().ConfigureAwait(false);

            // Assert (Then)
            Assert.AreEqual(
                (false, string.Empty),
                (ran, text),
                "The answer's script should have been swapped in with no text, and not run.");
            Assert.IsEmpty(
                edge.ConsoleErrors,
                "The console should report no error: " + string.Join(" | ", edge.ConsoleErrors));
        }
    }

    /// <summary>
    /// Browser test to verify, in Edge, that a boosted link whose answer holds a script from a file, as Visual Studio
    /// writes into every page it runs, still loads its page: HTMX copies the script's address with a string, which
    /// Trusted Types refuse unless a default policy turns it into one, and the application's turns it into none, so
    /// the swap goes through and the file is never asked for. The console reports no error.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    [TestCategory("Browser")]
    [DoNotParallelize]
    public async Task BoostedLink_AnAnswerWithAScriptFile_LoadsThePageWithoutTheFile()
    {
        // Arrange (Given)
        Uri application = this._application.StartServerForBrowser();
        BrowserSession edge = await BrowserSession.OpenAsync(application, Scripting.On).ConfigureAwait(false);
        await using (edge.ConfigureAwait(false))
        {
            IPage page = edge.Page;
            await page.GotoAsync("/").ConfigureAwait(false);
            await page.RouteAsync("**/users", PlantScriptFileAsync).ConfigureAwait(false);
            bool asked = false;
            page.Request += (_, request) => asked |= request.Url.EndsWith(PlantedFile, StringComparison.Ordinal);
            ILocator nav = page.GetByRole(AriaRole.Navigation, new PageGetByRoleOptions { Name = "Main" });
            LocatorWaitForOptions attached = new LocatorWaitForOptions { State = WaitForSelectorState.Attached };

            // Act (When)
            await nav.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Users" }).ClickAsync()
                .ConfigureAwait(false);
            await page.Locator("#planted").WaitForAsync(attached).ConfigureAwait(false);
            string? heading = await page.Locator("main h1").TextContentAsync().ConfigureAwait(false);
            string? address = await page.Locator("#planted").GetAttributeAsync("src").ConfigureAwait(false);
            int toasts = await edge.Toast.CountAsync().ConfigureAwait(false);

            // Assert (Then)
            Assert.AreEqual(
                ("Users", string.Empty, false, 0),
                (heading?.Trim(), address, asked, toasts),
                "The page should load with the script swapped in without its file, which is never asked for.");
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
    /// Answers a request with the application's own answer and a script from a file before the end of its body, as
    /// Visual Studio adds its Browser Link and hot reload scripts to every page it runs.
    /// </summary>
    /// <param name="route">The request, held.</param>
    /// <returns>A task that completes when the request has its answer.</returns>
    private static async Task PlantScriptFileAsync(IRoute route)
    {
        IAPIResponse answer = await route.FetchAsync().ConfigureAwait(false);
        string body = await answer.TextAsync().ConfigureAwait(false);
        string planted = $"<script id=\"planted\" src=\"{PlantedFile}\" async></script></body>";
        await route.FulfillAsync(new RouteFulfillOptions
        {
            Response = answer,
            Body = body.Replace("</body>", planted, StringComparison.Ordinal),
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Makes the answer to Load more that a test plants: a row whose cell holds a script, as markup that got into an
    /// answer would.
    /// </summary>
    /// <returns>The answer.</returns>
    private static RouteFulfillOptions Planted()
    {
        return new RouteFulfillOptions
        {
            Status = 200,
            ContentType = "text/html",
            Body = "<tr id=\"planted\"><td class=\"title\">Planted</td>"
                + "<td><script>window.plantedScriptRan = true;</script></td><td>0</td></tr>",
        };
    }

    #endregion Private Methods
}
