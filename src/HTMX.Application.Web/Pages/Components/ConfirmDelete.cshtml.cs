namespace HTMX.Application.Web.Pages.Components;

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// A file's confirmation page, at /components/confirmation-page/delete/{file}: the question its dialog asks, as a page
/// of its own, which any browser can show. Its form posts to the Confirmation page's Delete, as the dialog's does.
/// </summary>
public sealed class ConfirmDeleteModel : PageModel
{
    private readonly SavedFiles _files;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfirmDeleteModel"/> class.
    /// </summary>
    /// <param name="files">The files.</param>
    public ConfirmDeleteModel(SavedFiles files)
    {
        ArgumentNullException.ThrowIfNull(files);
        this._files = files;
    }

    /// <summary>
    /// Gets the file the page asks about, or <see langword="null"/> when it is already gone.
    /// </summary>
    public SavedFile? Asked { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the whole page.
    /// </summary>
    public bool HeadingFocused { get; private set; }

    /// <summary>
    /// Asks whether to delete the file, or, when it is already gone, says so with 404.
    /// </summary>
    /// <param name="file">The file's number, from the page's address.</param>
    public void OnGet(long file)
    {
        this.HeadingFocused = HtmxHeaders.IsHtmxRequest(this.Request);
        this.Asked = this._files.Find(file);
        if (this.Asked is null)
        {
            this.Response.StatusCode = StatusCodes.Status404NotFound;
        }
    }
}
