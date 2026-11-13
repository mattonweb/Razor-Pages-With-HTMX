namespace HTMX.Application.Web.Pages.Components;

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The Reload on event page, at /components/reload-on-event: a form that writes a note, and a list of the newest notes
/// that loads itself again whenever a note is written, without the form knowing the list is there.
/// </summary>
/// <remarks>
/// With HTMX the form's answer is the form, empty again, and the header <c>HX-Trigger: notes-changed</c>. HTMX raises
/// that event once the answer is swapped in, and the list, which listens for it, asks for itself again. The list is
/// the page's own address asked for in part, so one handler draws the page and the list. Without HTMX the form posts,
/// and the redirect back draws the page with the list as it is then.
/// </remarks>
public sealed class ReloadOnEventModel : PageModel
{
    /// <summary>
    /// The event a written note raises, which the list listens for.
    /// </summary>
    public const string NotesChanged = "notes-changed";

    // The form's card and the list's card, which the page draws and HTMX gets alone.
    private const string FormPartial = "_NoteForm";
    private const string ListPartial = "_NoteList";

    private readonly NoteBoard _board;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReloadOnEventModel"/> class.
    /// </summary>
    /// <param name="board">The notes, which the form writes and the list reads.</param>
    public ReloadOnEventModel(NoteBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);
        this._board = board;
    }

    /// <summary>
    /// Gets or sets the form's values, as the browser posted them.
    /// </summary>
    [BindProperty]
    public NoteInput Input { get; set; } = new NoteInput();

    /// <summary>
    /// Gets the notes the list shows, newest first.
    /// </summary>
    public Note[] Notes { get; private set; } = [];

    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the whole page.
    /// </summary>
    public bool HeadingFocused { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the note's field takes the keyboard: after a note was written with HTMX, so the
    /// next can be written at once.
    /// </summary>
    public bool TextFocused { get; private set; }

    /// <summary>
    /// Shows the page, or, when HTMX asks for part of it, the list alone: the list asking for itself again after a
    /// note was written. The list's answer moves no keyboard, since nobody asked for it.
    /// </summary>
    /// <returns>The list or the page.</returns>
    public IActionResult OnGet()
    {
        this.Notes = this._board.Newest();
        if (HtmxHeaders.IsPartialRequest(this.Request))
        {
            return this.Partial(ListPartial, this);
        }

        this.HeadingFocused = HtmxHeaders.IsHtmxRequest(this.Request);
        return this.Page();
    }

    /// <summary>
    /// Writes a note. A form that fails answers 422 with the form and raises nothing, since nothing changed. A note
    /// written without HTMX is redirected back to the page; with HTMX the answer is the form, empty again, and the
    /// event that tells the list to load itself again.
    /// </summary>
    /// <returns>The form, or the redirect.</returns>
    public IActionResult OnPost()
    {
        if (!this.ModelState.IsValid)
        {
            this.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            this.Notes = this._board.Newest();
            return HtmxAnswers.PartOrPage(this, FormPartial);
        }

        this._board.Write(this.Input.Text);
        if (!HtmxHeaders.IsPartialRequest(this.Request))
        {
            return this.RedirectToPage();
        }

        HtmxHeaders.RaiseEvent(this.Response, NotesChanged);
        this.ModelState.Clear();
        this.Input = new NoteInput();
        this.TextFocused = true;
        return this.Partial(FormPartial, this);
    }
}
