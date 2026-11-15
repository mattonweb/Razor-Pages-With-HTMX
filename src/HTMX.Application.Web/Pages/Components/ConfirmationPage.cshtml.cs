namespace HTMX.Application.Web.Pages.Components;

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The Confirmation page, at /components/confirmation-page: a list of files, each with a Delete that asks first, in a
/// dialog where the browser has invoker commands and on a page of its own everywhere else.
/// </summary>
/// <remarks>
/// Each row's Delete is a link to the file's confirmation page (<see cref="ConfirmDeleteModel"/>), which every browser
/// can follow, with or without script. Beside it a template holds the button that opens the file's dialog with
/// <c>commandfor</c>; the application's script puts the button in the link's place when the browser has invoker
/// commands. The dialog's form and the confirmation page's form both post to <see cref="OnPostDelete"/>.
/// </remarks>
public sealed class ConfirmationPageModel : PageModel
{
    // What a change answers HTMX with: the list's card, which the page draws too, and a toast.
    private const string AnswerPartial = "_SavedFilesAnswer";

    // The element a toast's Undo would replace; no toast here offers one.
    private const string ListTarget = "#saved-files";

    private readonly SavedFiles _files;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfirmationPageModel"/> class.
    /// </summary>
    /// <param name="files">The files, which the list shows and Delete removes.</param>
    public ConfirmationPageModel(SavedFiles files)
    {
        ArgumentNullException.ThrowIfNull(files);
        this._files = files;
    }

    /// <summary>
    /// Gets the files the list shows, in order.
    /// </summary>
    public SavedFile[] Files { get; private set; } = [];

    /// <summary>
    /// Gets the number of the file whose Delete takes the keyboard: the row now in the place of the one deleted, or the
    /// last row when the list is shorter; <see langword="null"/> when none does.
    /// </summary>
    public long? FocusedFile { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the list's heading takes the keyboard: when a delete left no row.
    /// </summary>
    public bool ListHeadingFocused { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the whole page.
    /// </summary>
    public bool HeadingFocused { get; private set; }

    /// <summary>
    /// Gets the toast that says what changed, or <see langword="null"/>.
    /// </summary>
    public Toast? Toast { get; private set; }

    /// <summary>
    /// Shows the list, and the toast a change without HTMX, or from the confirmation page, left before its redirect.
    /// </summary>
    public void OnGet()
    {
        this.HeadingFocused = HtmxHeaders.IsHtmxRequest(this.Request);
        this.Toast = TempDataToasts.Take(this.TempData, ListTarget);
        this.Files = this._files.All();
    }

    /// <summary>
    /// Deletes a file for good, as its dialog or its confirmation page asked. A file already gone says so.
    /// </summary>
    /// <param name="file">The file's number, which the form names in its address.</param>
    /// <returns>The list and a toast for HTMX's request from the dialog, or the redirect back to the list.</returns>
    public IActionResult OnPostDelete(long file)
    {
        int place = Array.FindIndex(this._files.All(), saved => saved.Number == file);
        SavedFile? deleted = this._files.Delete(file);
        string message = deleted is null ? "That file was already deleted." : $"{deleted.Name} deleted for good.";
        return this.Changed(message, place);
    }

    /// <summary>
    /// Brings every file back, so that the page can be tried again.
    /// </summary>
    /// <returns>The list and a toast for HTMX, or the redirect back to the list.</returns>
    public IActionResult OnPostPutBack()
    {
        this._files.PutBack();
        return this.Changed("Every file is back.", place: -1);
    }

    /// <summary>
    /// Answers a change: the list as it is now and a toast for a request HTMX sent to swap the list, the keyboard on
    /// the row in the place of the one deleted; and otherwise a redirect back to the list with the toast left in
    /// TempData, as for the browser on its own and for the confirmation page's form, which HTMX loads as a page.
    /// </summary>
    /// <param name="message">What changed.</param>
    /// <param name="place">Where the deleted row was, from 0, or -1 when no row was deleted.</param>
    /// <returns>The list and the toast, or the redirect.</returns>
    private IActionResult Changed(string message, int place)
    {
        Toast toast = new Toast { Message = message };
        if (!HtmxHeaders.IsPartialRequest(this.Request))
        {
            TempDataToasts.Leave(this.TempData, toast);
            return this.RedirectToPage();
        }

        this.Files = this._files.All();
        this.Toast = toast;
        this.FocusAfterDelete(place);
        return this.Partial(AnswerPartial, this);
    }

    /// <summary>
    /// Chooses where the keyboard goes once a row is deleted: the Delete of the row now in its place, or of the last
    /// row when the list is shorter, or the list's heading when no row is left. Nothing moves it when no row was
    /// deleted: the button pressed keeps its id, and HTMX gives the keyboard back to it.
    /// </summary>
    /// <param name="place">Where the deleted row was, from 0, or -1 when no row was deleted.</param>
    private void FocusAfterDelete(int place)
    {
        if (place < 0)
        {
            return;
        }

        if (this.Files.Length == 0)
        {
            this.ListHeadingFocused = true;
            return;
        }

        this.FocusedFile = this.Files[Math.Min(place, this.Files.Length - 1)].Number;
    }
}
