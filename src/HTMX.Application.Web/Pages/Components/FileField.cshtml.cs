namespace HTMX.Application.Web.Pages.Components;

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The File field page, at /components/file-field: a form that sends a title and a file, which says what it received
/// and keeps nothing. It shows a form with a file working with HTMX and without it, and what a refused save does with
/// the file the user chose.
/// </summary>
/// <remarks>
/// A browser cannot put a file into a file input that a page draws, so a form that comes back after a refused save has
/// lost the file. With HTMX the answer's file input says <c>hx-preserve</c> when the file was fine, and HTMX keeps the
/// input already on the page, file and all, in its place. Without HTMX the whole page comes back, and it asks for the
/// file to be chosen again. The request's size is capped below the server's default, though well above the file's
/// limit: a request over the cap is refused before it is read, and the browser sees no answer at all.
/// </remarks>
[RequestSizeLimit(RequestLimit)]
public sealed class FileFieldModel : PageModel
{
    // The largest file the form takes: one megabyte.
    private const long LargestFile = 1024 * 1024;

    // The most a request to this page may carry, well above the largest file: a body over it is refused before the
    // handler runs, and the browser reports the connection lost rather than any message, so the cap is set where only
    // a file nobody meant to send reaches it, and a file a little too large still gets the form's own message.
    private const long RequestLimit = 10 * LargestFile;

    // The form's partial, and the answer to HTMX for a file received: the form, empty again, and the toast.
    private const string FormPartial = "_FileFieldForm";
    private const string ReceivedPartial = "_FileFieldReceived";

    // The form's name for the file, as ModelState keys it.
    private const string FileKey = nameof(Input) + "." + nameof(FileFieldInput.File);

    /// <summary>
    /// Gets or sets the form's values, as the browser posted them.
    /// </summary>
    [BindProperty]
    public FileFieldInput Input { get; set; } = new FileFieldInput();

    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the page, as by a link.
    /// </summary>
    public bool HeadingFocused { get; private set; }

    /// <summary>
    /// Gets the toast that says what the form received, or <see langword="null"/>.
    /// </summary>
    public Toast? Toast { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the answer keeps the file the user chose: after a save HTMX sent that was
    /// refused for something else, the file input says <c>hx-preserve</c>, and HTMX keeps the one on the page.
    /// </summary>
    public bool FileKept { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the page asks for the file to be chosen again: after a save sent without HTMX
    /// that was refused for something else, since the page that comes back cannot hold the file.
    /// </summary>
    public bool ChooseFileAgain { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the title takes the keyboard: after HTMX sent a file that was received, so the
    /// next can be sent at once.
    /// </summary>
    public bool TitleFocused { get; private set; }

    /// <summary>
    /// Shows the empty form, and the toast a file received without HTMX left before its redirect.
    /// </summary>
    public void OnGet()
    {
        this.HeadingFocused = HtmxHeaders.IsHtmxRequest(this.Request);
        this.Toast = TempDataToasts.Take(this.TempData, "#file-form");
    }

    /// <summary>
    /// Receives the form: says what the file is, and keeps nothing. A form that fails is answered with 422 and the
    /// form, keeping the file where it can. A form that passes is answered with the empty form and a toast with HTMX,
    /// and with a redirect back to the page, which shows the toast, without it.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The form, or the redirect.</returns>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        string? kind = await this.CheckFileAsync(cancellationToken).ConfigureAwait(false);
        bool partial = HtmxHeaders.IsPartialRequest(this.Request);
        if (!this.ModelState.IsValid || kind is null)
        {
            return this.Refuse(partial, fileFine: kind is not null);
        }

        IFormFile file = this.Input.File ?? throw new InvalidOperationException("A valid form always has a file.");
        string size = Math.Ceiling(file.Length / 1024.0).ToString("N0", CultureInfo.CurrentCulture);
        Toast toast = new Toast { Message = $"Received {Path.GetFileName(file.FileName)}: {kind} of {size} KB." };
        if (!partial)
        {
            TempDataToasts.Leave(this.TempData, toast);
            return this.RedirectToPage();
        }

        this.ModelState.Clear();
        this.Input = new FileFieldInput();
        this.Toast = toast;
        this.TitleFocused = true;
        return this.Partial(ReceivedPartial, this);
    }

    /// <summary>
    /// Checks the file the form sent, once the framework has checked that there is one: no larger than the form takes,
    /// and of a kind it takes, by its first bytes. A file that fails is marked in ModelState.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The file's kind, such as <c>a PNG image</c>, or <see langword="null"/> when there is no file or it
    /// failed.</returns>
    private async Task<string?> CheckFileAsync(CancellationToken cancellationToken)
    {
        if (this.Input.File is not IFormFile file)
        {
            return null;
        }

        if (file.Length > LargestFile)
        {
            this.ModelState.AddModelError(FileKey, "Choose a file of 1 MB or less.");
            return null;
        }

        string? kind = await FileSignature.KindOfAsync(file, cancellationToken).ConfigureAwait(false);
        if (kind is null)
        {
            this.ModelState.AddModelError(FileKey, "Choose a PNG or JPEG image, or a PDF document.");
        }

        return kind;
    }

    /// <summary>
    /// Answers a refused form with 422: the form alone for HTMX, which keeps the file when it was fine, and the whole
    /// page otherwise, which asks for it again.
    /// </summary>
    /// <param name="partial">Whether HTMX sent the form, to swap the answer into the page.</param>
    /// <param name="fileFine">Whether a file was sent and passed.</param>
    /// <returns>The form or the page.</returns>
    private IActionResult Refuse(bool partial, bool fileFine)
    {
        this.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        this.FileKept = partial && fileFine;
        this.ChooseFileAgain = !partial && fileFine;
        return HtmxAnswers.PartOrPage(this, FormPartial);
    }
}
