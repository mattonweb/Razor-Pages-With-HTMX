namespace HTMX.Test.Integration.Application.Web.Pages.Components;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HTMX.Application.Web.Pages.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the Confirmation page, at /components/confirmation-page, run through the whole
/// application: each Delete is a link to a confirmation page any browser can show, beside a template holding the
/// dialog's button, which a browser with invoker commands gets in its place; both ask the same question and post to the
/// same Delete.
/// </summary>
[TestClass]
public sealed class ConfirmationPageTests : IDisposable
{
    #region Private Fields

    // The list, the Sample contract's confirmation page, and the Delete both its dialog and that page post to.
    private const string Address = "/components/confirmation-page";
    private const string ContractPage = "/components/confirmation-page/delete/10002";
    private const string DeleteContract = "/components/confirmation-page?file=10002&handler=Delete";

    // The same Delete, as Razor writes it in a form's action.
    private const string DeleteContractInHtml = "/components/confirmation-page?file=10002&amp;handler=Delete";

    // What the list's toast says once the Sample contract is deleted.
    private const string Deleted = "Sample contract.pdf deleted for good.";

    // Makes the page see no invoker commands, as a browser from before December 2025 would: it runs before the page's
    // own scripts, and takes away the property the application's script looks for.
    private const string WithoutInvokerCommands = "delete HTMLButtonElement.prototype.commandForElement;";

    // The Sample contract's dialog, by its id, whatever classes style it.
    private static readonly Regex ContractDialog = new Regex(
        "<dialog\\b[^>]*\\bid=\"delete-file-10002\"",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that the list draws each Delete as a link to the file's confirmation page, with the
    /// dialog's button in a template beside it, and the dialog itself.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task Get_TheList_DrawsALinkAndTheDialogsButton()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();

        // Act (When)
        Answer page = await Requests.GetAsync(browser, Address, HtmxRequest.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.Contains(
            $"id=\"delete-10002\" href=\"{ContractPage}\" data-dialog-link",
            page.Body,
            "The Delete should be a link to the file's confirmation page.");
        Assert.Contains(
            "commandfor=\"delete-file-10002\" command=\"show-modal\"",
            page.Body,
            "A template should hold the button that opens the file's dialog.");
        Assert.IsTrue(ContractDialog.IsMatch(page.Body), "The page should hold the file's dialog.");
    }

    /// <summary>
    /// Integration test to verify that a file's confirmation page asks the dialog's question, in a form that posts to
    /// the same Delete, and that a file already gone gets the page saying so, with 404.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task Get_AConfirmationPage_PostsToTheSameDelete()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        this._application.Services.GetRequiredService<SavedFiles>().Delete(10003);

        // Act (When)
        Answer page = await Requests.GetAsync(browser, ContractPage, HtmxRequest.None).ConfigureAwait(false);
        Answer gone = await Requests.GetAsync(browser, "/components/confirmation-page/delete/10003", HtmxRequest.None)
            .ConfigureAwait(false);
        bool posts = page.Body.Contains($"action=\"{DeleteContractInHtml}\"", StringComparison.Ordinal);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.OK, true, HttpStatusCode.NotFound),
            (page.Status, posts, gone.Status),
            "The page should post to the dialog's Delete, and a file already gone answer 404.");
        Assert.Contains(
            "Delete Sample contract.pdf for good?",
            page.Body,
            "The page should ask the dialog's question.");
    }

    /// <summary>
    /// Integration test to verify that the confirmation page's form, posted without HTMX, deletes the file and is
    /// redirected back to the list, which shows the toast.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task Post_FromTheConfirmationPage_DeletesAndGoesBackToTheList()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        string token = await Requests.ReadAntiforgeryTokenAsync(browser, ContractPage).ConfigureAwait(false);
        IEnumerable<KeyValuePair<string, string>> form = Html.Form(token, []);

        // Act (When)
        Answer deleted = await Requests.PostAsync(browser, DeleteContract, HtmxRequest.None, form)
            .ConfigureAwait(false);
        Answer list = await Requests.GetAsync(browser, Address, HtmxRequest.None).ConfigureAwait(false);
        bool listed = list.Body.Contains("id=\"file-10002\"", StringComparison.Ordinal);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.Redirect, Address, false),
            (deleted.Status, deleted.Headers["Location"], listed),
            "The file should be deleted and the browser sent back to the list.");
        Assert.Contains(
            Deleted,
            list.Body,
            "The list should show the toast.");
    }

    /// <summary>
    /// Browser test to verify, in Edge, which has invoker commands, that each Delete becomes the dialog's button: it
    /// opens the file's dialog, whose Delete permanently removes the row with HTMX, says so in a toast and puts the
    /// keyboard on the Delete of the row now in its place. The console reports no error.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    [TestCategory("Browser")]
    [DoNotParallelize]
    public async Task Delete_WithInvokerCommands_AsksInTheDialog()
    {
        // Arrange (Given)
        Uri application = this._application.StartServerForBrowser();
        BrowserSession edge = await BrowserSession.OpenAsync(application, Scripting.On).ConfigureAwait(false);
        await using (edge.ConfigureAwait(false))
        {
            IPage page = edge.Page;
            await page.GotoAsync(Address).ConfigureAwait(false);
            ILocator dialog = page.Locator("#delete-file-10002");

            // Act (When)
            string opener = await page.Locator("#delete-10002").EvaluateAsync<string>("element => element.tagName")
                .ConfigureAwait(false);
            await page.Locator("#delete-10002").ClickAsync().ConfigureAwait(false);
            await dialog.WaitForAsync().ConfigureAwait(false);
            await dialog.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Delete permanently" })
                .ClickAsync()
                .ConfigureAwait(false);
            await edge.Toast.WaitForAsync().ConfigureAwait(false);
            string? said = await edge.Toast.TextContentAsync().ConfigureAwait(false);
            string focused = await page.EvaluateAsync<string>("() => document.activeElement.id").ConfigureAwait(false);

            // Assert (Then)
            Assert.AreEqual(
                ("BUTTON", Deleted, "delete-10003", Address),
                (opener, said?.Trim(), focused, new Uri(page.Url).AbsolutePath),
                "The dialog should have asked, and the keyboard be on the row in the deleted one's place.");
            Assert.IsEmpty(
                edge.ConsoleErrors,
                "The console should report no error: " + string.Join(" | ", edge.ConsoleErrors));
        }
    }

    /// <summary>
    /// Browser test to verify, in Edge, that a browser without invoker commands, as Edge is made to look here, or one
    /// with script off, keeps each Delete a link: it goes to the file's confirmation page, whose Delete permanently
    /// removes the file and goes back to the list with its toast. The console reports no error.
    /// </summary>
    /// <param name="scripting">How much script the page runs, by its name in <see cref="Scripting"/>.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    [TestCategory("Browser")]
    [DoNotParallelize]
    [DataRow(nameof(Scripting.On), DisplayName = "With HTMX, without invoker commands")]
    [DataRow(nameof(Scripting.Off), DisplayName = "With script off")]
    public async Task Delete_WithoutTheDialog_AsksOnAPage(string scripting)
    {
        // Arrange (Given)
        Uri application = this._application.StartServerForBrowser();
        BrowserSession edge = await BrowserSession.OpenAsync(application, Enum.Parse<Scripting>(scripting))
            .ConfigureAwait(false);
        await using (edge.ConfigureAwait(false))
        {
            IPage page = edge.Page;
            await page.AddInitScriptAsync(WithoutInvokerCommands).ConfigureAwait(false);
            await page.GotoAsync(Address).ConfigureAwait(false);

            // Act (When)
            string opener = await page.Locator("#delete-10002").EvaluateAsync<string>("element => element.tagName")
                .ConfigureAwait(false);
            await page.Locator("#delete-10002").ClickAsync().ConfigureAwait(false);
            await page.WaitForURLAsync("**" + ContractPage).ConfigureAwait(false);
            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Delete permanently" })
                .ClickAsync()
                .ConfigureAwait(false);
            await page.WaitForURLAsync("**" + Address).ConfigureAwait(false);
            string? said = await edge.Toast.TextContentAsync().ConfigureAwait(false);
            int rows = await page.Locator("#file-10002").CountAsync().ConfigureAwait(false);

            // Assert (Then)
            Assert.AreEqual(
                ("A", Deleted, 0),
                (opener, said?.Trim(), rows),
                "The link should have led to the confirmation page, and its Delete removed the file.");
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
}
