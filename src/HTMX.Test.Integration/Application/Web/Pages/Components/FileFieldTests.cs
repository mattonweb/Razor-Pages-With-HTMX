namespace HTMX.Test.Integration.Application.Web.Pages.Components;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Playwright;
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

    // Keeps each value the progress bar shows, and the amount its label gives with it, in the page, from a change to
    // the bar's value attribute, which the script sets through the bar's value just before it writes the amount.
    private const string RecordProgress =
        "() => { window.progressSeen = []; window.amountSeen = []; "
        + "const bar = document.querySelector('#file-progress'); "
        + "const amount = document.querySelector('[data-upload-amount]'); new MutationObserver(() => { "
        + "window.progressSeen.push(bar.value); window.amountSeen.push(amount.textContent); "
        + "}).observe(bar, { attributes: true }); }";

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
    /// Integration test to verify that the page draws the upload's progress bar hidden, so that without script or
    /// without HTMX it never shows, and only the application's script shows it once it sends the form.
    /// </summary>
    [TestMethod]
    public async Task Get_ThePage_DrawsTheProgressBarHidden()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();

        // Act (When)
        Answer page = await Requests.GetAsync(browser, Address, HtmxRequest.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.Contains(
            "<div data-upload-progress hidden>",
            page.Body,
            "The progress bar's container should be drawn hidden.");
        Assert.Contains(
            "<label for=\"file-progress\">Sending the file: <span data-upload-amount>0%</span></label>",
            page.Body,
            "The container should hold the bar's label, which names it and says the amount sent.");
        Assert.Contains(
            "<progress id=\"file-progress\" max=\"1\" value=\"0\"></progress>",
            page.Body,
            "The container should hold the progress bar, at nothing sent.");
    }

    /// <summary>
    /// Browser test to verify, in Edge, that a file sent over a slow connection shows how much of it has gone: the
    /// application's script sends the form instead of HTMX's fetch, the progress bar moves through values between none
    /// and all of it while its label gives the amount in figures, and HTMX still swaps the answer in, with its toast.
    /// The console reports no error.
    /// </summary>
    [TestMethod]
    [TestCategory("Browser")]
    [DoNotParallelize]
    public async Task Send_OverASlowConnection_ShowsHowMuchHasGone()
    {
        // Arrange (Given)
        Uri application = this._application.StartServerForBrowser();
        BrowserSession edge = await BrowserSession.OpenAsync(application, Scripting.On).ConfigureAwait(false);
        await using (edge.ConfigureAwait(false))
        {
            IPage page = edge.Page;
            await page.GotoAsync(Address).ConfigureAwait(false);
            await SlowUploadsAsync(page).ConfigureAwait(false);
            await page.Locator("#Input_Title").FillAsync("Slow logo").ConfigureAwait(false);
            await page.Locator("#Input_File").SetInputFilesAsync(LargePng()).ConfigureAwait(false);
            await page.EvaluateAsync(RecordProgress).ConfigureAwait(false);
            ILocator toast = edge.Toast;

            // Act (When)
            await page.Locator("#file-form button[type=submit]").ClickAsync().ConfigureAwait(false);
            await toast.WaitForAsync().ConfigureAwait(false);
            double[] seen = await page.EvaluateAsync<double[]>("() => window.progressSeen").ConfigureAwait(false);
            string[] amounts = await page.EvaluateAsync<string[]>("() => window.amountSeen").ConfigureAwait(false);
            string? said = await toast.TextContentAsync().ConfigureAwait(false);
            bool hidden = await page.Locator("[data-upload-progress]").IsHiddenAsync().ConfigureAwait(false);

            // Assert (Then)
            Assert.IsTrue(
                Array.Exists(seen, static value => value > 0 && value < 1)
                && Array.Exists(amounts, static amount => amount != "0%" && amount != "100%"),
                "The bar and its label should have shown part of the file sent: " + string.Join(", ", amounts));
            Assert.AreEqual(
                ("Received slow.png: a PNG image of 600 KB.", 1.0, "100%", true),
                (said?.Trim(), seen.LastOrDefault(), amounts.LastOrDefault(), hidden),
                "The whole file should have gone, the label and the toast say so, and the new form's bar be hidden.");
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

    /// <summary>
    /// Makes a PNG file of 600 KB, as a browser test chooses it in the file input: its signature, then zeros.
    /// </summary>
    /// <returns>The file.</returns>
    private static FilePayload LargePng()
    {
        byte[] content = new byte[600 * 1024];
        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        signature.CopyTo(content, 0);
        return new FilePayload { Name = "slow.png", MimeType = "image/png", Buffer = content };
    }

    /// <summary>
    /// Slows what the page sends to 300 KB a second, through the browser's own network emulation, so that an upload
    /// takes long enough to report its progress more than once.
    /// </summary>
    /// <param name="page">The page.</param>
    /// <returns>A task that completes when the browser has been told.</returns>
    private static async Task SlowUploadsAsync(IPage page)
    {
        ICDPSession devTools = await page.Context.NewCDPSessionAsync(page).ConfigureAwait(false);
        await devTools.SendAsync("Network.enable").ConfigureAwait(false);
        Dictionary<string, object> conditions = new Dictionary<string, object>
        {
            ["offline"] = false,
            ["latency"] = 0,
            ["downloadThroughput"] = -1,
            ["uploadThroughput"] = 300 * 1024,
        };
        await devTools.SendAsync("Network.emulateNetworkConditions", conditions).ConfigureAwait(false);
    }

    #endregion Private Methods
}
