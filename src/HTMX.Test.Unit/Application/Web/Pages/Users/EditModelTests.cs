namespace HTMX.Test.Unit.Application.Web.Pages.Users;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Pages.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the users' <see cref="EditModel"/> class: how the form is filled,
/// what a save sends, and what each outcome answers, with HTMX and without it.
/// </summary>
[TestClass]
public sealed class EditModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the form starts from the user as they are saved, version included.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_KnownIdentifier_FillsTheFormFromTheUser()
    {
        // Arrange (Given)
        EditModel page = CreatePage(new StubUserService(CreateUser(5)), HtmxRequest.None);

        // Act (When)
        IActionResult result = await page.OnGetAsync(10007, CancellationToken.None).ConfigureAwait(false);
        EditModel.InputModel input = page.Input;

        // Assert (Then)
        Assert.IsInstanceOfType<PageResult>(
            result,
            "A known user should get the page.");
        Assert.AreEqual(
            (5, "Pat Example", "pat.example", "pat@example.com"),
            (input.Version, input.Name, input.Username, input.Email),
            "The form should start from the saved values and carry the version they were read at.");
    }

    /// <summary>
    /// Unit test to verify that an identifier that names no user gets the page, which says so, and 404.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_UnknownIdentifier_AnswersNotFound()
    {
        // Arrange (Given)
        EditModel page = CreatePage(new StubUserService(), HtmxRequest.None);

        // Act (When)
        IActionResult result = await page.OnGetAsync(99999, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType<PageResult>(
            result,
            "An unknown user should still get the page, which says they were not found.");
        Assert.AreEqual(
            (99999L, StatusCodes.Status404NotFound),
            (page.Id, page.Response.StatusCode),
            "The page should know which identifier it could not find, and answer 404.");
    }

    /// <summary>
    /// Unit test to verify that a save the browser sent without HTMX is redirected to the user's page, and that the
    /// change sent is the form's, trimmed, from the signed-in user.
    /// </summary>
    [TestMethod]
    public async Task OnPostAsync_SavedWithoutHtmx_RedirectsToTheUser()
    {
        // Arrange (Given)
        StubUserService service = new StubUserService(CreateUser(5), UpdateOutcome.Saved);
        EditModel page = CreatePage(service, HtmxRequest.None);
        page.Input = CreateInput(5, "  Patricia Example  ", " patricia.example ", " patricia@example.com ");

        // Act (When)
        IActionResult result = await page.OnPostAsync(10007, CancellationToken.None).ConfigureAwait(false);
        UserUpdateRequest? sent = service.LastUpdate();

        // Assert (Then)
        RedirectToPageResult redirect = Assert.IsInstanceOfType<RedirectToPageResult>(
            result,
            "A save without HTMX should be redirected.");
        Assert.AreEqual(
            ("./Details", (object?)10007L),
            (redirect.PageName, redirect.RouteValues?["id"]),
            "The save should go to the page of the user it changed.");
        Assert.AreEqual(
            ("Patricia Example", "patricia.example", "patricia@example.com"),
            (sent?.DisplayName.Value, sent?.Username.Value, sent?.Email.Value),
            "Every value should be sent without the spaces around it.");
        Assert.AreEqual(
            (new UserId(10007), new VersionNumber(5), new UserId(10001)),
            (sent?.Id, sent?.Version, sent?.ChangedBy),
            "The change should be for the user the route names, from the form's version, by the signed-in user.");
    }

    /// <summary>
    /// Unit test to verify that a save HTMX sent answers 204 with HX-Location naming the user's page, which HTMX then
    /// loads in place of this one.
    /// </summary>
    [TestMethod]
    public async Task OnPostAsync_SavedByHtmx_AsksHtmxToLoadTheUser()
    {
        // Arrange (Given)
        EditModel page = CreatePage(new StubUserService(CreateUser(5), UpdateOutcome.Saved), HtmxRequest.Partial);
        page.Input = CreateInput(5, "Patricia Example");

        // Act (When)
        IActionResult result = await page.OnPostAsync(10007, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        StatusCodeResult status = Assert.IsInstanceOfType<StatusCodeResult>(
            result,
            "A save HTMX sent should be answered with a status alone.");
        Assert.AreEqual(
            (StatusCodes.Status204NoContent, "/Users/Details/10007"),
            (status.StatusCode, page.Response.Headers["HX-Location"].ToString()),
            "The answer should have no content to swap, and HX-Location should name the user's page.");
    }

    /// <summary>
    /// Unit test to verify that a form that fails validation is not sent, and comes back with 422: only the edit
    /// page's content when HTMX sent it, and the whole page otherwise.
    /// </summary>
    /// <param name="sentByHtmx">Whether HTMX sent the save.</param>
    /// <param name="expected">The kind of result that should answer it.</param>
    [TestMethod]
    [DataRow(true, typeof(PartialViewResult), DisplayName = "Sent by HTMX")]
    [DataRow(false, typeof(PageResult), DisplayName = "Sent without HTMX")]
    public async Task OnPostAsync_InvalidForm_AnswersTheFormWith422(bool sentByHtmx, Type expected)
    {
        // Arrange (Given)
        StubUserService service = new StubUserService(CreateUser(5));
        EditModel page = CreatePage(service, sentByHtmx ? HtmxRequest.Partial : HtmxRequest.None);
        page.Input = CreateInput(5, "Pat Example", username: "pat example");
        page.ModelState.AddModelError("Input.Username", "Use only letters, digits, dots, hyphens and underscores.");

        // Act (When)
        IActionResult result = await page.OnPostAsync(10007, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType(
            result,
            expected,
            "The form should come back as the page or as its content, by what sent it.");
        Assert.AreEqual(
            StatusCodes.Status422UnprocessableEntity,
            page.Response.StatusCode,
            "A form that fails validation should be answered with 422.");
        Assert.IsNull(
            service.LastUpdate(),
            "A form that fails validation should not be sent.");
    }

    /// <summary>
    /// Unit test to verify that a username or an email address another user has is marked on its own field, with 422.
    /// </summary>
    /// <param name="outcome">What the service answers.</param>
    /// <param name="field">The field that should carry the message.</param>
    /// <param name="message">The message it should carry.</param>
    [TestMethod]
    [DataRow(UpdateOutcome.NameTaken, "Input.Username", "Another user already has this username.")]
    [DataRow(UpdateOutcome.EmailTaken, "Input.Email", "Another user already has this email address.")]
    public async Task OnPostAsync_ValueTaken_MarksItsField(UpdateOutcome outcome, string field, string message)
    {
        // Arrange (Given)
        EditModel page = CreatePage(new StubUserService(CreateUser(5), outcome), HtmxRequest.Partial);
        page.Input = CreateInput(5, "Pat Example", "sam.sample", "sam@example.com");

        // Act (When)
        IActionResult result = await page.OnPostAsync(10007, CancellationToken.None).ConfigureAwait(false);
        ModelStateEntry? entry = page.ModelState[field];

        // Assert (Then)
        Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "The form should come back to swap in place.");
        Assert.AreEqual(
            StatusCodes.Status422UnprocessableEntity,
            page.Response.StatusCode,
            "A value that must change should be answered with 422.");
        Assert.AreEqual(
            message,
            entry?.Errors[0].ErrorMessage,
            "The field whose value is taken should say so.");
    }

    /// <summary>
    /// Unit test to verify that a save another save overtook comes back with 409, the message naming who saved, the
    /// saved values shown, and the form now starting from the saved version, so that saving again goes through.
    /// </summary>
    [TestMethod]
    public async Task OnPostAsync_Overtaken_StartsTheFormFromTheSavedVersion()
    {
        // Arrange (Given)
        EditModel page = CreatePage(new StubUserService(CreateUser(7), UpdateOutcome.Overtaken), HtmxRequest.Partial);
        page.Input = CreateInput(5, "Patricia Example");
        ModelStateDictionary posted = page.ModelState;
        posted.SetModelValue("Input.Version", 5, "5");
        posted.MarkFieldValid("Input.Version");

        // Act (When)
        IActionResult result = await page.OnPostAsync(10007, CancellationToken.None).ConfigureAwait(false);
        ModelStateDictionary modelState = page.ModelState;
        ModelStateEntry? summary = modelState[string.Empty];

        // Assert (Then)
        Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "The form should come back to swap in place.");
        Assert.AreEqual(
            (StatusCodes.Status409Conflict, 7, true),
            (page.Response.StatusCode, page.Input.Version, page.Overtaken),
            "The save should be answered 409, with the form starting from the saved version and showing its values.");
        Assert.IsFalse(
            modelState.ContainsKey("Input.Version"),
            "The posted version should be forgotten, so the form writes the saved one.");
        Assert.IsNotNull(
            summary,
            "The form should say why the save was refused.");
        StringAssert.StartsWith(
            summary.Errors[0].ErrorMessage,
            "Demo User saved this user",
            "The message should name who saved.");
    }

    /// <summary>
    /// Unit test to verify that a save to a user archived meanwhile comes back with 409, to show the alert that says
    /// they cannot be edited.
    /// </summary>
    [TestMethod]
    public async Task OnPostAsync_Archived_AnswersConflict()
    {
        // Arrange (Given)
        EditModel page = CreatePage(new StubUserService(CreateUser(6), UpdateOutcome.Archived), HtmxRequest.Partial);
        page.Input = CreateInput(5, "Patricia Example");

        // Act (When)
        IActionResult result = await page.OnPostAsync(10007, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "The edit page's content should come back to swap in place.");
        Assert.AreEqual(
            StatusCodes.Status409Conflict,
            page.Response.StatusCode,
            "A save to an archived user should be answered with 409.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates Pat Example as the service would answer with them, at a version, with invented values.
    /// </summary>
    /// <param name="version">The version they are at.</param>
    /// <returns>The user.</returns>
    private static UserSingleResponse CreateUser(int version)
    {
        RecordChange change = new RecordChange
        {
            On = new DateTime(2026, 11, 8, 9, 30, 0, DateTimeKind.Utc),
            By = new UserReference { Id = new UserId(10001), Name = new DisplayName("Demo User") },
        };
        return new UserSingleResponse
        {
            Id = new UserId(10007),
            DisplayName = new DisplayName("Pat Example"),
            Username = new Username("pat.example"),
            Email = new EmailAddress("pat@example.com"),
            Version = new VersionNumber(version),
            Created = change,
            LastModified = change,
        };
    }

    /// <summary>
    /// Creates the form's values as the browser would post them.
    /// </summary>
    /// <param name="version">The version the form was filled from.</param>
    /// <param name="name">The name typed.</param>
    /// <param name="username">The username typed.</param>
    /// <param name="email">The email address typed.</param>
    /// <returns>The form's values.</returns>
    private static EditModel.InputModel CreateInput(
        int version,
        string name,
        string username = "pat.example",
        string email = "pat@example.com")
    {
        return new EditModel.InputModel
        {
            Version = version,
            Name = name,
            Username = username,
            Email = email,
        };
    }

    /// <summary>
    /// Creates the page as the framework would for one request: Demo User signed in, the headers HTMX 4 sends for that
    /// kind of request, and a stand-in for routing.
    /// </summary>
    /// <param name="service">The user service the page reads and saves through.</param>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static EditModel CreatePage(StubUserService service, HtmxRequest kind)
    {
        PageContext context = PageRequests.Create(kind, "/Users/Edit");
        return new EditModel(service)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            Url = new StubUrlHelper(context),
        };
    }

    #endregion Private Methods
}
