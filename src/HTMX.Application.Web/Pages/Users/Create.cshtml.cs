namespace HTMX.Application.Web.Pages.Users;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The form that creates a user, at /users/create. A save that succeeds goes to the new user's page; a save that is
/// refused comes back with the form and the reason. Without HTMX that is a redirect or the whole page; with HTMX it is
/// a request to load the new user's page, or only the form.
/// </summary>
public sealed class CreateModel : PageModel
{
    private const string FormPartial = "_UserCreateForm";
    private const string DetailsPage = "./Details";

    // The form's names for its fields, as ModelState keys them.
    private const string UsernameKey = nameof(Input) + "." + nameof(UserInput.Username);
    private const string EmailKey = nameof(Input) + "." + nameof(UserInput.Email);

    private readonly IUserService _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateModel"/> class.
    /// </summary>
    /// <param name="users">Creates the user.</param>
    public CreateModel(IUserService users)
    {
        ArgumentNullException.ThrowIfNull(users);
        this._users = users;
    }

    /// <summary>
    /// Gets or sets the form's values, as the browser posted them. Model binding writes them on a post; the form starts
    /// empty.
    /// </summary>
    [BindProperty]
    public UserInput Input { get; set; } = new UserInput();

    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the page, as by a link,
    /// since HTMX moves the keyboard nowhere itself, and not when the browser loaded it on its own or a refused save
    /// drew the form again.
    /// </summary>
    public bool HeadingFocused { get; private set; }

    /// <summary>
    /// Starts an empty form.
    /// </summary>
    public void OnGet()
    {
        this.HeadingFocused = HtmxHeaders.IsHtmxRequest(this.Request);
    }

    /// <summary>
    /// Creates the user. A form that fails validation, or gives a username or an email address another user has,
    /// comes back with 422 and the messages.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The new user's page, or the form again.</returns>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!this.ModelState.IsValid)
        {
            return this.Refuse();
        }

        UserCreateRequest request = this.CreateRequest();
        UserCreateResponse response = await this._users.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return response.Outcome switch
        {
            CreateOutcome.NameTaken =>
                this.RefuseTaken(UsernameKey, "username"),
            CreateOutcome.EmailTaken =>
                this.RefuseTaken(EmailKey, "email address"),
            _ =>
                this.ShowCreated(response),
        };
    }

    /// <summary>
    /// Turns the form into a new user, once it has passed validation: the values trimmed, and the signed-in user as
    /// who created them.
    /// </summary>
    /// <returns>The new user.</returns>
    private UserCreateRequest CreateRequest()
    {
        UserInput input = this.Input;
        return new UserCreateRequest
        {
            DisplayName = new DisplayName(input.Name.Trim()),
            Username = new Username(input.Username.Trim()),
            Email = new EmailAddress(input.Email.Trim()),
            CreatedBy = SignedInUser.Of(this.User),
        };
    }

    /// <summary>
    /// Marks a field whose value another user already has, and answers with the form.
    /// </summary>
    /// <param name="key">The field, as ModelState keys it.</param>
    /// <param name="value">What the field holds, as the message names it, such as "username".</param>
    /// <returns>The form, with the status 422.</returns>
    private IActionResult RefuseTaken(string key, string value)
    {
        // Values match however they are capitalized, so the message does not repeat the one typed.
        this.ModelState.AddModelError(key, $"Another user already has this {value}.");
        return this.Refuse();
    }

    /// <summary>
    /// Answers a refused save with 422 and the form: only the form when HTMX sent it, the whole page otherwise.
    /// </summary>
    /// <returns>The form or the page.</returns>
    private IActionResult Refuse()
    {
        this.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        return HtmxAnswers.PartOrPage(this, FormPartial);
    }

    /// <summary>
    /// Goes to the new user's page: by a redirect without HTMX, and by asking HTMX to load it with HTMX.
    /// </summary>
    /// <param name="response">The answer to a request that created the user.</param>
    /// <returns>The redirect, or 204 No Content with the request to load the page.</returns>
    private IActionResult ShowCreated(UserCreateResponse response)
    {
        UserId created = response.Id ?? throw new InvalidOperationException("A created user always has an identifier.");
        return HtmxAnswers.NextPage(this, DetailsPage, new { id = created.Value });
    }
}
