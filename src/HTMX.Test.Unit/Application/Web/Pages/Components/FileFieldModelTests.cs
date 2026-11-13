namespace HTMX.Test.Unit.Application.Web.Pages.Components;

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Application.Web.Pages.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="FileFieldModel"/> class: the File field page's
/// form, which says what file it received, and what a refused form does with the file the user chose.
/// </summary>
[TestClass]
public sealed class FileFieldModelTests
{
    #region Private Fields

    // The bytes a PNG file starts with, and enough after them to make a small one.
    private static readonly byte[] PngStart = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Unit test to verify that a file HTMX sent is received: the answer is the empty form and a toast that says what
    /// came, with the title ready for the next file.
    /// </summary>
    [TestMethod]
    public async Task OnPostAsync_FileReceivedByHtmx_AnswersTheEmptyFormAndAToast()
    {
        // Arrange (Given)
        FileFieldModel page = CreatePage(HtmxRequest.Partial);
        page.Input = new FileFieldInput { Title = "Quarterly logo", File = CreateFile(Png(2000), "logo.png") };

        // Act (When)
        IActionResult result = await page.OnPostAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            ("_FileFieldReceived", "Received logo.png: a PNG image of 2 KB.", string.Empty, true),
            ((result as PartialViewResult)?.ViewName, page.Toast?.Message, page.Input.Title, page.TitleFocused),
            "HTMX should get the empty form and the toast, with the title taking the keyboard.");
    }

    /// <summary>
    /// Unit test to verify that a file sent without HTMX is received and redirected back to the page, the toast left
    /// in TempData for it, so that refreshing the page does not send the file again.
    /// </summary>
    [TestMethod]
    public async Task OnPostAsync_FileReceivedWithoutHtmx_RedirectsWithTheToast()
    {
        // Arrange (Given)
        FileFieldModel page = CreatePage(HtmxRequest.None);
        page.Input = new FileFieldInput { Title = "Brief", File = CreateFile(Pdf(), "brief.pdf") };

        // Act (When)
        IActionResult result = await page.OnPostAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType<RedirectToPageResult>(
            result,
            "The browser should be sent back to the page.");
        Assert.AreEqual(
            "Received brief.pdf: a PDF document of 1 KB.",
            page.TempData["ToastMessage"],
            "The toast should wait in TempData for the page.");
    }

    /// <summary>
    /// Unit test to verify that a form refused for its title, whose file was fine, answers 422 and keeps the file the
    /// only way it can: with HTMX the file input says hx-preserve, and without it the page asks for the file again.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="kept">Whether the answer should keep the file input on the page.</param>
    /// <param name="askedAgain">Whether the page should ask for the file again.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.Partial), true, false, DisplayName = "With HTMX")]
    [DataRow(nameof(HtmxRequest.None), false, true, DisplayName = "Without HTMX")]
    public async Task OnPostAsync_RefusedForTheTitle_KeepsTheFileWhereItCan(string kind, bool kept, bool askedAgain)
    {
        // Arrange (Given)
        FileFieldModel page = CreatePage(Enum.Parse<HtmxRequest>(kind));
        page.Input = new FileFieldInput { File = CreateFile(Png(500), "logo.png") };
        page.ModelState.AddModelError("Input.Title", "Enter a title.");

        // Act (When)
        await page.OnPostAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (StatusCodes.Status422UnprocessableEntity, kept, askedAgain),
            (page.Response.StatusCode, page.FileKept, page.ChooseFileAgain),
            "The form should be refused, and the file kept or asked for again.");
    }

    /// <summary>
    /// Unit test to verify that a file of a kind the form does not take, whatever its name says, or a file over the
    /// limit, is refused with 422 and its message, and is not kept.
    /// </summary>
    /// <param name="size">The file's size in bytes.</param>
    /// <param name="png">Whether the file starts as a PNG does.</param>
    /// <param name="message">The message expected under the file input.</param>
    [TestMethod]
    [DataRow(500, false, "Choose a PNG or JPEG image, or a PDF document.", DisplayName = "Text named .png")]
    [DataRow(1_500_000, true, "Choose a file of 1 MB or less.", DisplayName = "Over 1 MB")]
    public async Task OnPostAsync_WrongFile_RefusesAndMarksIt(int size, bool png, string message)
    {
        // Arrange (Given)
        FileFieldModel page = CreatePage(HtmxRequest.Partial);
        byte[] content = png ? Png(size) : new byte[size];
        page.Input = new FileFieldInput { Title = "Notes", File = CreateFile(content, "notes.png") };

        // Act (When)
        await page.OnPostAsync(CancellationToken.None).ConfigureAwait(false);
        ModelStateEntry? entry = page.ModelState["Input.File"];

        // Assert (Then)
        Assert.AreEqual(
            (StatusCodes.Status422UnprocessableEntity, message, false),
            (page.Response.StatusCode, entry?.Errors[0].ErrorMessage, page.FileKept),
            "The file should be refused with its message, and not kept.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Makes a PNG file's content: its signature, then zeros.
    /// </summary>
    /// <param name="size">The content's size in bytes.</param>
    /// <returns>The content.</returns>
    private static byte[] Png(int size)
    {
        byte[] content = new byte[size];
        PngStart.CopyTo(content, 0);
        return content;
    }

    /// <summary>
    /// Makes a small PDF file's content: its signature, then a few words.
    /// </summary>
    /// <returns>The content.</returns>
    private static byte[] Pdf()
    {
        return Encoding.ASCII.GetBytes("%PDF-1.7 placeholder");
    }

    /// <summary>
    /// Makes a file as model binding gives it, over the content.
    /// </summary>
    /// <param name="content">The file's bytes.</param>
    /// <param name="name">The file's name, as the browser sent it.</param>
    /// <returns>The file.</returns>
    private static FormFile CreateFile(byte[] content, string name)
    {
        return new FormFile(new MemoryStream(content), 0, content.Length, "Input.File", name);
    }

    /// <summary>
    /// Creates the page as the framework would for one request: the headers HTMX 4 sends for that kind of request,
    /// TempData, and what a partial needs to render.
    /// </summary>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static FileFieldModel CreatePage(HtmxRequest kind)
    {
        PageContext context = PageRequests.Create(kind, "/Components/FileField");
        return new FileFieldModel
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            TempData = new TempDataDictionary(context.HttpContext, new StubTempDataProvider()),
        };
    }

    #endregion Private Methods
}
