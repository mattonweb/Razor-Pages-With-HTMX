namespace HTMX.Application.Web.Pages.Users;

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// One user's edit form, at /users/{id}/edit. A save that succeeds goes to the user's page; a save that is refused
/// comes back with the form and the reason. Without HTMX that is a redirect or the whole page; with HTMX it is a
/// request to load the user's page, or only the edit page's content.
/// </summary>
/// <remarks>
/// A page model's own <c>User</c> is whoever is signed in, so the user the form edits is its <see cref="Account"/>,
/// as on the user's other pages. Anyone may edit themselves; only archiving themselves is refused.
/// </remarks>
public sealed class EditModel : PageModel
{
    private const string EditPartial = "_UserEdit";
    private const string DetailsPage = "./Details";

    // The form's names for its fields, as ModelState keys them.
    private const string UsernameKey = nameof(Input) + "." + nameof(InputModel.Username);
    private const string EmailKey = nameof(Input) + "." + nameof(InputModel.Email);
    private const string VersionKey = nameof(Input) + "." + nameof(InputModel.Version);

    private readonly IUserService _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditModel"/> class.
    /// </summary>
    /// <param name="users">Reads the user and saves the changes.</param>
    public EditModel(IUserService users)
    {
        ArgumentNullException.ThrowIfNull(users);
        this._users = users;
    }

    /// <summary>
    /// Gets or sets the form's values, as the browser posted them. Model binding writes them on a post; the GET handler
    /// fills them from the user.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    /// <summary>
    /// Gets the identifier the route names.
    /// </summary>
    public long Id { get; private set; }

    /// <summary>
    /// Gets the user as they are saved now, or <see langword="null"/> when the identifier names none.
    /// </summary>
    public UserSingleResponse? Account { get; private set; }

    /// <summary>
    /// Gets a value indicating whether another save overtook this one, so the form shows the saved values beside
    /// the ones being edited.
    /// </summary>
    public bool Overtaken { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the user was archived after the form was shown, so the alert that says they
    /// can't be edited takes the keyboard in place of the form.
    /// </summary>
    public bool ArchivedMeanwhile { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the page, as by a link,
    /// since HTMX moves the keyboard nowhere itself, and not when the browser loaded it on its own or a refused save
    /// drew the form again.
    /// </summary>
    public bool HeadingFocused { get; private set; }

    /// <summary>
    /// Fills the form from the user the route names.
    /// </summary>
    /// <param name="id">The user's identifier, from the route.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The page, which says so when no user has the identifier.</returns>
    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        UserSingleResponse? account = await this.ReadAsync(id, cancellationToken).ConfigureAwait(false);
        this.HeadingFocused = HtmxHeaders.IsHtmxRequest(this.Request);
        if (account is null)
        {
            this.Response.StatusCode = StatusCodes.Status404NotFound;
            return this.Page();
        }

        this.Input = CreateInput(account);
        return this.Page();
    }

    /// <summary>
    /// Saves the form. A save that succeeds, or that changes nothing, goes to the user's page, which also says so when
    /// the user no longer exists. A refused save answers 422 when a value must change and 409 when the user has moved
    /// on or been archived, with the form and the reason.
    /// </summary>
    /// <param name="id">The user's identifier, from the route.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>Where to go next, or the form again.</returns>
    public async Task<IActionResult> OnPostAsync(long id, CancellationToken cancellationToken)
    {
        if (!this.ModelState.IsValid)
        {
            return await this.RefuseAsync(id, StatusCodes.Status422UnprocessableEntity, cancellationToken)
                .ConfigureAwait(false);
        }

        UserUpdateRequest request = this.CreateRequest(id);
        UpdateOutcome outcome = await this._users.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
        return outcome switch
        {
            UpdateOutcome.NameTaken =>
                await this.RefuseTakenAsync(id, UsernameKey, "username", cancellationToken).ConfigureAwait(false),
            UpdateOutcome.EmailTaken =>
                await this.RefuseTakenAsync(id, EmailKey, "email address", cancellationToken).ConfigureAwait(false),
            UpdateOutcome.Overtaken =>
                await this.RefuseOvertakenAsync(id, cancellationToken).ConfigureAwait(false),
            UpdateOutcome.Archived =>
                await this.RefuseArchivedAsync(id, cancellationToken).ConfigureAwait(false),
            _ =>
                this.ShowUser(id),
        };
    }

    /// <summary>
    /// Fills the form from the user as they are saved.
    /// </summary>
    /// <param name="account">The user.</param>
    /// <returns>The form's values.</returns>
    private static InputModel CreateInput(UserSingleResponse account)
    {
        return new InputModel
        {
            Version = account.Version.Value,
            Name = account.DisplayName.Value,
            Username = account.Username.Value,
            Email = account.Email.Value,
        };
    }

    /// <summary>
    /// Turns the form into a change, once it has passed validation: the values trimmed, and the signed-in user as who
    /// made it.
    /// </summary>
    /// <param name="id">The user's identifier.</param>
    /// <returns>The change.</returns>
    private UserUpdateRequest CreateRequest(long id)
    {
        InputModel input = this.Input;
        return new UserUpdateRequest
        {
            Id = new UserId(id),
            Version = new VersionNumber(input.Version),
            DisplayName = new DisplayName(input.Name.Trim()),
            Username = new Username(input.Username.Trim()),
            Email = new EmailAddress(input.Email.Trim()),
            ChangedBy = SignedInUser.Of(this.User),
        };
    }

    /// <summary>
    /// Says that the user was archived after the form was shown, and answers with the alert that says they can't be
    /// edited, which takes the keyboard, since the form and its Save button are gone.
    /// </summary>
    /// <param name="id">The user's identifier.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The edit page's content, with the status 409.</returns>
    private Task<IActionResult> RefuseArchivedAsync(long id, CancellationToken cancellationToken)
    {
        this.ArchivedMeanwhile = true;
        return this.RefuseAsync(id, StatusCodes.Status409Conflict, cancellationToken);
    }

    /// <summary>
    /// Reads the user as they are saved now, for the page's header and, after a conflict, the saved values.
    /// </summary>
    /// <param name="id">The user's identifier.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The user, or <see langword="null"/> when there is none.</returns>
    private async Task<UserSingleResponse?> ReadAsync(long id, CancellationToken cancellationToken)
    {
        this.Id = id;
        this.Account = await this._users.FindAsync(new UserId(id), cancellationToken).ConfigureAwait(false);
        return this.Account;
    }

    /// <summary>
    /// Marks a field whose value another user already has, and answers with the form.
    /// </summary>
    /// <param name="id">The user's identifier.</param>
    /// <param name="key">The field, as ModelState keys it.</param>
    /// <param name="value">What the field holds, as the message names it, such as "username".</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The form, with the status 422.</returns>
    private Task<IActionResult> RefuseTakenAsync(long id, string key, string value, CancellationToken cancellationToken)
    {
        // Values match however they are capitalized, so the message does not repeat the one typed.
        this.ModelState.AddModelError(key, $"Another user already has this {value}.");
        return this.RefuseAsync(id, StatusCodes.Status422UnprocessableEntity, cancellationToken);
    }

    /// <summary>
    /// Says that another save reached the user first, and answers with the form, which keeps the values being edited,
    /// shows the saved ones beside them, and now starts from the saved version, so that saving again replaces it on
    /// purpose.
    /// </summary>
    /// <param name="id">The user's identifier.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The form, with the status 409.</returns>
    private async Task<IActionResult> RefuseOvertakenAsync(long id, CancellationToken cancellationToken)
    {
        UserSingleResponse? saved = await this.ReadAsync(id, cancellationToken).ConfigureAwait(false);
        if (saved is null)
        {
            return this.ShowUser(id);
        }

        // The save that overtook this one may be the same person's, from another tab, so the message names who made it.
        UserReference editor = saved.LastModified.By;
        string message = $"{editor.Name.Value} saved this user while you were editing them. Your changes are still "
            + "here, and the saved values are shown under the fields that differ. Save again only if yours should "
            + "replace theirs.";
        this.ModelState.AddModelError(string.Empty, message);

        // A tag helper writes the value that was posted ahead of the model's, so the posted version is forgotten first.
        this.ModelState.Remove(VersionKey);
        this.Input.Version = saved.Version.Value;
        this.Overtaken = true;
        return this.Answer(StatusCodes.Status409Conflict);
    }

    /// <summary>
    /// Answers a refused save with the form and its messages, after reading the user as they are saved now.
    /// </summary>
    /// <param name="id">The user's identifier.</param>
    /// <param name="statusCode">The status that says why the save was refused.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The form, or the user's page when they no longer exist.</returns>
    private async Task<IActionResult> RefuseAsync(long id, int statusCode, CancellationToken cancellationToken)
    {
        UserSingleResponse? saved = await this.ReadAsync(id, cancellationToken).ConfigureAwait(false);
        return saved is null ? this.ShowUser(id) : this.Answer(statusCode);
    }

    /// <summary>
    /// Answers a refused save with the edit page's content when HTMX sent it, to swap into the page it already shows,
    /// and with the whole page otherwise.
    /// </summary>
    /// <param name="statusCode">The status that says why the save was refused.</param>
    /// <returns>The content or the page.</returns>
    private IActionResult Answer(int statusCode)
    {
        this.Response.StatusCode = statusCode;
        return HtmxAnswers.PartOrPage(this, EditPartial);
    }

    /// <summary>
    /// Goes to the user's page: by a redirect without HTMX, and by asking HTMX to load it with HTMX.
    /// </summary>
    /// <param name="id">The user's identifier.</param>
    /// <returns>The redirect, or 204 No Content with the request to load the page.</returns>
    private IActionResult ShowUser(long id)
    {
        return HtmxAnswers.NextPage(this, DetailsPage, new { id });
    }

    /// <summary>
    /// The form's values, as the browser posted them: a user's values, and the version they were filled from.
    /// </summary>
    public sealed class InputModel : UserInput
    {
        /// <summary>
        /// Gets or sets the version the form was filled from, which the form carries in a hidden field.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int Version { get; set; }
    }
}
