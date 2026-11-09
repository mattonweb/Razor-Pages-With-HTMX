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
/// Contains unit tests for verifying the behavior of the <see cref="CreateModel"/> class: what a save sends, and what
/// each outcome answers, with HTMX and without it.
/// </summary>
[TestClass]
public sealed class CreateModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a new organization the browser sent without HTMX is redirected to its own page, and
    /// that what is sent is the form's values, trimmed, from the signed-in user.
    /// </summary>
    [TestMethod]
    public async Task OnPostAsync_CreatedWithoutHtmx_RedirectsToTheNewOrganization()
    {
        // Arrange (Given)
        StubOrganizationService service = new StubOrganizationService();
        CreateModel page = CreatePage(service, HtmxRequest.None);
        page.Input = CreateInput("  Newcomer Networks  ", website: "  https://newcomer.example  ");

        // Act (When)
        IActionResult result = await page.OnPostAsync(CancellationToken.None).ConfigureAwait(false);
        OrganizationCreateRequest? sent = service.LastCreation();

        // Assert (Then)
        RedirectToPageResult redirect = Assert.IsInstanceOfType<RedirectToPageResult>(
            result,
            "A new organization sent without HTMX should be redirected.");
        Assert.AreEqual(
            "./Details",
            redirect.PageName,
            "The save should go to the organization's page.");
        Assert.AreEqual(
            11001L,
            redirect.RouteValues?["id"],
            "The save should go to the page of the organization it created.");
        Assert.IsNotNull(
            sent,
            "The page should send the new organization.");
        Assert.AreEqual(
            "Newcomer Networks",
            sent.Name.Value,
            "The name should be sent without the spaces around it.");
        Assert.AreEqual(
            new WebAddress("https://newcomer.example"),
            sent.Website,
            "The website should be sent without the spaces around it.");
        Assert.AreEqual(
            new UserId(10001),
            sent.CreatedBy,
            "The new organization should be the signed-in user's.");
    }

    /// <summary>
    /// Unit test to verify that a new organization HTMX sent answers 204 with HX-Location naming its page, which HTMX
    /// then loads in place of the form.
    /// </summary>
    [TestMethod]
    public async Task OnPostAsync_CreatedByHtmx_AsksHtmxToLoadTheNewOrganization()
    {
        // Arrange (Given)
        CreateModel page = CreatePage(new StubOrganizationService(), HtmxRequest.Partial);
        page.Input = CreateInput("Newcomer Networks");

        // Act (When)
        IActionResult result = await page.OnPostAsync(CancellationToken.None).ConfigureAwait(false);
        IHeaderDictionary headers = page.Response.Headers;

        // Assert (Then)
        StatusCodeResult status = Assert.IsInstanceOfType<StatusCodeResult>(
            result,
            "A new organization HTMX sent should be answered with a status alone.");
        Assert.AreEqual(
            StatusCodes.Status204NoContent,
            status.StatusCode,
            "The answer should have no content to swap.");
        Assert.AreEqual(
            "/Organizations/Details/11001",
            headers["HX-Location"].ToString(),
            "HX-Location should name the new organization's page.");
    }

    /// <summary>
    /// Unit test to verify that a form that fails validation is not sent, and comes back with 422: only the form when
    /// HTMX sent it, to swap in place, and the whole page otherwise.
    /// </summary>
    /// <param name="sentByHtmx">Whether HTMX sent the save.</param>
    /// <param name="expected">The kind of result that should answer it.</param>
    [TestMethod]
    [DataRow(true, typeof(PartialViewResult), DisplayName = "Sent by HTMX")]
    [DataRow(false, typeof(PageResult), DisplayName = "Sent without HTMX")]
    public async Task OnPostAsync_InvalidForm_AnswersTheFormWith422(bool sentByHtmx, Type expected)
    {
        // Arrange (Given)
        StubOrganizationService service = new StubOrganizationService();
        CreateModel page = CreatePage(service, sentByHtmx ? HtmxRequest.Partial : HtmxRequest.None);
        page.Input = CreateInput(string.Empty);
        page.ModelState.AddModelError("Input.Name", "Enter a name.");

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
    /// Unit test to verify that a name another organization has is marked on the Name field, with 422, and the form
    /// alone comes back to swap in place.
    /// </summary>
    [TestMethod]
    public async Task OnPostAsync_NameTaken_MarksTheNameField()
    {
        // Arrange (Given)
        StubOrganizationService service = new StubOrganizationService(creation: CreateOutcome.NameTaken);
        CreateModel page = CreatePage(service, HtmxRequest.Partial);
        page.Input = CreateInput("placeholder logistics");

        // Act (When)
        IActionResult result = await page.OnPostAsync(CancellationToken.None).ConfigureAwait(false);
        ModelValidationState nameState = page.ModelState.GetFieldValidationState("Input.Name");

        // Assert (Then)
        PartialViewResult partial = Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "The form should come back to swap in place.");
        Assert.AreEqual(
            "_OrganizationCreateForm",
            partial.ViewName,
            "The form alone should come back.");
        Assert.AreEqual(
            ModelValidationState.Invalid,
            nameState,
            "The Name field should carry the message.");
        Assert.AreEqual(
            StatusCodes.Status422UnprocessableEntity,
            page.Response.StatusCode,
            "A name that must change should be answered with 422.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the form's values as the browser would post them, in shipping.
    /// </summary>
    /// <param name="name">The name typed.</param>
    /// <param name="website">The website typed, if any.</param>
    /// <returns>The form's values.</returns>
    private static OrganizationInput CreateInput(string name, string? website = null)
    {
        return new OrganizationInput
        {
            Name = name,
            Industry = Industry.Shipping,
            Website = website,
        };
    }

    /// <summary>
    /// Creates the page as the framework would for one request: Demo User signed in, the headers HTMX 4 sends for that
    /// kind of request, and a stand-in for routing.
    /// </summary>
    /// <param name="service">The organization service the page creates through.</param>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static CreateModel CreatePage(StubOrganizationService service, HtmxRequest kind)
    {
        PageContext context = PageRequests.Create(kind, "/Organizations/Create");
        return new CreateModel(service)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            Url = new StubUrlHelper(context),
        };
    }

    #endregion Private Methods
}
