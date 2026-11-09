namespace HTMX.Test.Unit.Application.Web.Pages.Organizations;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Pages.Organizations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="EditModel"/> class: how the form is filled, what a
/// save sends, and what each outcome answers, with HTMX and without it.
/// </summary>
[TestClass]
public sealed class EditModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the form starts from the organization as it is saved, version included.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_KnownIdentifier_FillsTheFormFromTheOrganization()
    {
        // Arrange (Given)
        StubOrganizationService service = new StubOrganizationService(CreateOrganization(2));
        EditModel page = CreatePage(service, HtmxRequest.None);

        // Act (When)
        IActionResult result = await page.OnGetAsync(10003, CancellationToken.None).ConfigureAwait(false);
        EditModel.InputModel input = page.Input;

        // Assert (Then)
        Assert.IsInstanceOfType<PageResult>(
            result,
            "A known organization should get the page.");
        Assert.AreEqual(
            2,
            input.Version,
            "The form should carry the version it was filled from.");
        Assert.AreEqual(
            "Placeholder Logistics",
            input.Name,
            "The form should start from the saved name.");
        Assert.AreEqual(
            Industry.Shipping,
            input.Industry,
            "The form should start from the saved industry.");
        Assert.AreEqual(
            "https://placeholder.example",
            input.Website,
            "The form should start from the saved website.");
    }

    /// <summary>
    /// Unit test to verify that an identifier that names no organization gets the page, which says so, and 404.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_UnknownIdentifier_AnswersNotFound()
    {
        // Arrange (Given)
        EditModel page = CreatePage(new StubOrganizationService(), HtmxRequest.None);

        // Act (When)
        IActionResult result = await page.OnGetAsync(99999, CancellationToken.None).ConfigureAwait(false);
        HttpResponse response = page.Response;

        // Assert (Then)
        Assert.IsInstanceOfType<PageResult>(
            result,
            "An unknown organization should still get the page, which says it was not found.");
        Assert.AreEqual(
            99999L,
            page.Id,
            "The page should know which identifier it could not find.");
        Assert.AreEqual(
            StatusCodes.Status404NotFound,
            response.StatusCode,
            "An unknown organization should be answered with 404.");
    }

    /// <summary>
    /// Unit test to verify that a save the browser sent without HTMX is redirected to the organization's page, so that
    /// refreshing that page does not post the form again, and that the change sent is the form's, trimmed, from the
    /// signed-in user.
    /// </summary>
    [TestMethod]
    public async Task OnPostAsync_SavedWithoutHtmx_RedirectsToTheOrganization()
    {
        // Arrange (Given)
        StubOrganizationService service = new StubOrganizationService(CreateOrganization(2), UpdateOutcome.Saved);
        EditModel page = CreatePage(service, HtmxRequest.None);
        page.Input = CreateInput(2, "  Placeholder Logistics Group  ", website: "   ");

        // Act (When)
        IActionResult result = await page.OnPostAsync(10003, CancellationToken.None).ConfigureAwait(false);
        OrganizationUpdateRequest? sent = service.LastUpdate();

        // Assert (Then)
        RedirectToPageResult redirect = Assert.IsInstanceOfType<RedirectToPageResult>(
            result,
            "A save without HTMX should be redirected.");
        Assert.AreEqual(
            "./Details",
            redirect.PageName,
            "The save should go to the organization's page.");
        Assert.AreEqual(
            10003L,
            redirect.RouteValues?["id"],
            "The save should go to the page of the organization it changed.");
        Assert.IsNotNull(
            sent,
            "The page should send the change.");
        Assert.AreEqual(
            "Placeholder Logistics Group",
            sent.Name.Value,
            "The name should be sent without the spaces around it.");
        Assert.IsNull(
            sent.Website,
            "A website of spaces should be sent as none.");
        Assert.AreEqual(
            new VersionNumber(2),
            sent.Version,
            "The change should start from the version the form carried.");
        Assert.AreEqual(
            new UserId(10001),
            sent.ChangedBy,
            "The change should be the signed-in user's.");
    }

    /// <summary>
    /// Unit test to verify that a save HTMX sent answers 204 with HX-Location naming the organization's page, which
    /// HTMX then loads in place of this one, because <c>fetch</c> would follow a redirect itself.
    /// </summary>
    [TestMethod]
    public async Task OnPostAsync_SavedByHtmx_AsksHtmxToLoadTheOrganization()
    {
        // Arrange (Given)
        StubOrganizationService service = new StubOrganizationService(CreateOrganization(2), UpdateOutcome.Saved);
        EditModel page = CreatePage(service, HtmxRequest.Partial);
        page.Input = CreateInput(2, "Placeholder Logistics Group");

        // Act (When)
        IActionResult result = await page.OnPostAsync(10003, CancellationToken.None).ConfigureAwait(false);
        IHeaderDictionary headers = page.Response.Headers;

        // Assert (Then)
        StatusCodeResult status = Assert.IsInstanceOfType<StatusCodeResult>(
            result,
            "A save HTMX sent should be answered with a status alone.");
        Assert.AreEqual(
            StatusCodes.Status204NoContent,
            status.StatusCode,
            "The answer should have no content to swap.");
        Assert.AreEqual(
            "/Organizations/Details/10003",
            headers["HX-Location"].ToString(),
            "HX-Location should name the organization's page.");
    }

    /// <summary>
    /// Unit test to verify that a form that fails validation is not sent, and comes back with 422: only the edit
    /// page's content when HTMX sent it, to swap in place, and the whole page otherwise.
    /// </summary>
    /// <param name="sentByHtmx">Whether HTMX sent the save.</param>
    /// <param name="expected">The kind of result that should answer it.</param>
    [TestMethod]
    [DataRow(true, typeof(PartialViewResult), DisplayName = "Sent by HTMX")]
    [DataRow(false, typeof(PageResult), DisplayName = "Sent without HTMX")]
    public async Task OnPostAsync_InvalidForm_AnswersTheFormWith422(bool sentByHtmx, Type expected)
    {
        // Arrange (Given)
        StubOrganizationService service = new StubOrganizationService(CreateOrganization(2));
        EditModel page = CreatePage(service, sentByHtmx ? HtmxRequest.Partial : HtmxRequest.None);
        page.Input = CreateInput(2, string.Empty);
        page.ModelState.AddModelError("Input.Name", "Enter a name.");

        // Act (When)
        IActionResult result = await page.OnPostAsync(10003, CancellationToken.None).ConfigureAwait(false);
        HttpResponse response = page.Response;

        // Assert (Then)
        Assert.IsInstanceOfType(
            result,
            expected,
            "The form should come back as the page or as its content, by what sent it.");
        Assert.AreEqual(
            StatusCodes.Status422UnprocessableEntity,
            response.StatusCode,
            "A form that fails validation should be answered with 422.");
        Assert.IsNull(
            service.LastUpdate(),
            "A form that fails validation should not be sent.");
    }

    /// <summary>
    /// Unit test to verify that a name another organization has is marked on the Name field, with 422.
    /// </summary>
    [TestMethod]
    public async Task OnPostAsync_NameTaken_MarksTheNameField()
    {
        // Arrange (Given)
        StubOrganizationService service = new StubOrganizationService(CreateOrganization(2), UpdateOutcome.NameTaken);
        EditModel page = CreatePage(service, HtmxRequest.Partial);
        page.Input = CreateInput(2, "Sample & Sons");

        // Act (When)
        IActionResult result = await page.OnPostAsync(10003, CancellationToken.None).ConfigureAwait(false);
        ModelValidationState nameState = page.ModelState.GetFieldValidationState("Input.Name");

        // Assert (Then)
        Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "The form should come back to swap in place.");
        Assert.AreEqual(
            ModelValidationState.Invalid,
            nameState,
            "The Name field should carry the message.");
        Assert.AreEqual(
            StatusCodes.Status422UnprocessableEntity,
            page.Response.StatusCode,
            "A name that must change should be answered with 422.");
    }

    /// <summary>
    /// Unit test to verify that a save another save overtook comes back with 409, the message naming who saved, the
    /// saved values shown, and the form now starting from the saved version, so that saving again goes through.
    /// </summary>
    [TestMethod]
    public async Task OnPostAsync_Overtaken_StartsTheFormFromTheSavedVersion()
    {
        // Arrange (Given)
        StubOrganizationService service = new StubOrganizationService(CreateOrganization(4), UpdateOutcome.Overtaken);
        EditModel page = CreatePage(service, HtmxRequest.Partial);
        page.Input = CreateInput(2, "Placeholder Logistics Group");
        ModelStateDictionary posted = page.ModelState;
        posted.SetModelValue("Input.Version", 2, "2");
        posted.MarkFieldValid("Input.Version");

        // Act (When)
        IActionResult result = await page.OnPostAsync(10003, CancellationToken.None).ConfigureAwait(false);
        ModelStateDictionary modelState = page.ModelState;
        ModelStateEntry? summary = modelState[string.Empty];

        // Assert (Then)
        Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "The form should come back to swap in place.");
        Assert.AreEqual(
            StatusCodes.Status409Conflict,
            page.Response.StatusCode,
            "A save another save overtook should be answered with 409.");
        Assert.AreEqual(
            4,
            page.Input.Version,
            "The form should now start from the saved version.");
        Assert.IsFalse(
            modelState.ContainsKey("Input.Version"),
            "The posted version should be forgotten, so the form writes the saved one.");
        Assert.IsTrue(
            page.Overtaken,
            "The form should show the saved values.");
        Assert.IsNotNull(
            summary,
            "The form should say why the save was refused.");
        StringAssert.StartsWith(
            summary.Errors[0].ErrorMessage,
            "Demo User saved this organization",
            "The message should name who saved.");
    }

    /// <summary>
    /// Unit test to verify that a save to an organization archived meanwhile comes back with 409, to show the alert
    /// that says it cannot be edited.
    /// </summary>
    [TestMethod]
    public async Task OnPostAsync_Archived_AnswersConflict()
    {
        // Arrange (Given)
        StubOrganizationService service = new StubOrganizationService(CreateOrganization(3), UpdateOutcome.Archived);
        EditModel page = CreatePage(service, HtmxRequest.Partial);
        page.Input = CreateInput(2, "Placeholder Logistics Group");

        // Act (When)
        IActionResult result = await page.OnPostAsync(10003, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "The edit page's content should come back to swap in place.");
        Assert.AreEqual(
            StatusCodes.Status409Conflict,
            page.Response.StatusCode,
            "A save to an archived organization should be answered with 409.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates Placeholder Logistics as the service would answer with it, at a version, with invented values.
    /// </summary>
    /// <param name="version">The version it is at.</param>
    /// <returns>The organization.</returns>
    private static OrganizationSingleResponse CreateOrganization(int version)
    {
        UserReference demo = new UserReference
        {
            Id = new UserId(10001),
            Name = new DisplayName("Demo User"),
        };
        DateTime changedOn = new DateTime(2026, 11, 8, 9, 30, 0, DateTimeKind.Utc);
        return new OrganizationSingleResponse
        {
            Id = new OrganizationId(10003),
            Name = new OrganizationName("Placeholder Logistics"),
            Industry = Industry.Shipping,
            Website = new WebAddress("https://placeholder.example"),
            Contacts = new RecordCount(18),
            Version = new VersionNumber(version),
            Created = new RecordChange { On = changedOn, By = demo },
            LastModified = new RecordChange { On = changedOn, By = demo },
        };
    }

    /// <summary>
    /// Creates the form's values as the browser would post them.
    /// </summary>
    /// <param name="version">The version the form was filled from.</param>
    /// <param name="name">The name typed.</param>
    /// <param name="website">The website typed, if any.</param>
    /// <returns>The form's values.</returns>
    private static EditModel.InputModel CreateInput(int version, string name, string? website = null)
    {
        return new EditModel.InputModel
        {
            Version = version,
            Name = name,
            Industry = Industry.Shipping,
            Website = website,
        };
    }

    /// <summary>
    /// Creates the page as the framework would for one request: Demo User signed in, the headers HTMX 4 sends for that
    /// kind of request, and a stand-in for routing.
    /// </summary>
    /// <param name="service">The organization service the page reads and saves through.</param>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static EditModel CreatePage(StubOrganizationService service, HtmxRequest kind)
    {
        PageContext context = PageRequests.Create(kind, "/Organizations/Edit");
        return new EditModel(service)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            Url = new StubUrlHelper(context),
        };
    }

    #endregion Private Methods
}
