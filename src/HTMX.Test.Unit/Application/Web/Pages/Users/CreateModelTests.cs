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
/// Contains unit tests for verifying the behavior of the users' <see cref="CreateModel"/> class: what a save sends,
/// and what each outcome answers, with HTMX and without it.
/// </summary>
[TestClass]
public sealed class CreateModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a new user the browser sent without HTMX is redirected to their own page, and that
    /// what is sent is the form's values, trimmed, from the signed-in user.
    /// </summary>
    [TestMethod]
    public async Task OnPostAsync_CreatedWithoutHtmx_RedirectsToTheNewUser()
    {
        // Arrange (Given)
        StubUserService service = new StubUserService();
        CreateModel page = CreatePage(service, HtmxRequest.None);
        page.Input = CreateInput("  Newcomer User  ", " newcomer.user ", " newcomer@example.com ");

        // Act (When)
        IActionResult result = await page.OnPostAsync(CancellationToken.None).ConfigureAwait(false);
        UserCreateRequest? sent = service.LastCreation();

        // Assert (Then)
        RedirectToPageResult redirect = Assert.IsInstanceOfType<RedirectToPageResult>(
            result,
            "A new user sent without HTMX should be redirected.");
        Assert.AreEqual(
            ("./Details", (object?)10050L),
            (redirect.PageName, redirect.RouteValues?["id"]),
            "The save should go to the page of the user it created.");
        Assert.AreEqual(
            ("Newcomer User", "newcomer.user", "newcomer@example.com"),
            (sent?.DisplayName.Value, sent?.Username.Value, sent?.Email.Value),
            "Every value should be sent without the spaces around it.");
        Assert.AreEqual(
            new UserId(10001),
            sent?.CreatedBy,
            "The new user should be the signed-in user's.");
    }

    /// <summary>
    /// Unit test to verify that a new user HTMX sent answers 204 with HX-Location naming their page, which HTMX then
    /// loads in place of the form.
    /// </summary>
    [TestMethod]
    public async Task OnPostAsync_CreatedByHtmx_AsksHtmxToLoadTheNewUser()
    {
        // Arrange (Given)
        CreateModel page = CreatePage(new StubUserService(), HtmxRequest.Partial);
        page.Input = CreateInput("Newcomer User", "newcomer.user", "newcomer@example.com");

        // Act (When)
        IActionResult result = await page.OnPostAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        StatusCodeResult status = Assert.IsInstanceOfType<StatusCodeResult>(
            result,
            "A new user HTMX sent should be answered with a status alone.");
        Assert.AreEqual(
            (StatusCodes.Status204NoContent, "/Users/Details/10050"),
            (status.StatusCode, page.Response.Headers["HX-Location"].ToString()),
            "The answer should have no content to swap, and HX-Location should name the new user's page.");
    }

    /// <summary>
    /// Unit test to verify that a form that fails validation is not sent, and comes back with 422: only the form when
    /// HTMX sent it, and the whole page otherwise.
    /// </summary>
    /// <param name="sentByHtmx">Whether HTMX sent the save.</param>
    /// <param name="expected">The kind of result that should answer it.</param>
    [TestMethod]
    [DataRow(true, typeof(PartialViewResult), DisplayName = "Sent by HTMX")]
    [DataRow(false, typeof(PageResult), DisplayName = "Sent without HTMX")]
    public async Task OnPostAsync_InvalidForm_AnswersTheFormWith422(bool sentByHtmx, Type expected)
    {
        // Arrange (Given)
        StubUserService service = new StubUserService();
        CreateModel page = CreatePage(service, sentByHtmx ? HtmxRequest.Partial : HtmxRequest.None);
        page.Input = CreateInput("Newcomer User", string.Empty, "newcomer@example.com");
        page.ModelState.AddModelError("Input.Username", "Enter a username.");

        // Act (When)
        IActionResult result = await page.OnPostAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType(
            result,
            expected,
            "The form should come back as the page or as the form alone, by what sent it.");
        Assert.AreEqual(
            StatusCodes.Status422UnprocessableEntity,
            page.Response.StatusCode,
            "A form that fails validation should be answered with 422.");
        Assert.IsNull(
            service.LastCreation(),
            "A form that fails validation should not be sent.");
    }

    /// <summary>
    /// Unit test to verify that a username or an email address another user has is marked on its own field, with 422,
    /// and the form alone comes back to swap in place.
    /// </summary>
    /// <param name="outcome">What the service answers.</param>
    /// <param name="field">The field that should carry the message.</param>
    /// <param name="message">The message it should carry.</param>
    [TestMethod]
    [DataRow(CreateOutcome.NameTaken, "Input.Username", "Another user already has this username.")]
    [DataRow(CreateOutcome.EmailTaken, "Input.Email", "Another user already has this email address.")]
    public async Task OnPostAsync_ValueTaken_MarksItsField(CreateOutcome outcome, string field, string message)
    {
        // Arrange (Given)
        CreateModel page = CreatePage(new StubUserService(creation: outcome), HtmxRequest.Partial);
        page.Input = CreateInput("Newcomer User", "pat.example", "pat@example.com");

        // Act (When)
        IActionResult result = await page.OnPostAsync(CancellationToken.None).ConfigureAwait(false);
        ModelStateEntry? entry = page.ModelState[field];

        // Assert (Then)
        PartialViewResult partial = Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "The form should come back to swap in place.");
        Assert.AreEqual(
            ("_UserCreateForm", StatusCodes.Status422UnprocessableEntity),
            (partial.ViewName, page.Response.StatusCode),
            "The form alone should come back, with 422.");
        Assert.AreEqual(
            message,
            entry?.Errors[0].ErrorMessage,
            "The field whose value is taken should say so.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the form's values as the browser would post them.
    /// </summary>
    /// <param name="name">The name typed.</param>
    /// <param name="username">The username typed.</param>
    /// <param name="email">The email address typed.</param>
    /// <returns>The form's values.</returns>
    private static UserInput CreateInput(string name, string username, string email)
    {
        return new UserInput
        {
            Name = name,
            Username = username,
            Email = email,
        };
    }

    /// <summary>
    /// Creates the page as the framework would for one request: Demo User signed in, the headers HTMX 4 sends for that
    /// kind of request, and a stand-in for routing.
    /// </summary>
    /// <param name="service">The user service the page creates through.</param>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static CreateModel CreatePage(StubUserService service, HtmxRequest kind)
    {
        PageContext context = PageRequests.Create(kind, "/Users/Create");
        return new CreateModel(service)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            Url = new StubUrlHelper(context),
        };
    }

    #endregion Private Methods
}
