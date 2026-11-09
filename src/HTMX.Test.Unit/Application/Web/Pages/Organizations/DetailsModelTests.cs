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
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="DetailsModel"/> class: which organization the page
/// asks for, and what it answers when there is none.
/// </summary>
[TestClass]
public sealed class DetailsModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the page asks for the organization the route names and shows it, with the status 200.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_KnownIdentifier_ShowsTheOrganization()
    {
        // Arrange (Given)
        OrganizationSingleResponse organization = CreateOrganization(10003);
        StubOrganizationService service = new StubOrganizationService(organization);
        DetailsModel page = CreatePage(service, 10003);

        // Act (When)
        IActionResult result = await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);
        HttpResponse response = page.Response;

        // Assert (Then)
        Assert.IsInstanceOfType<PageResult>(
            result,
            "A known organization should get the page.");
        Assert.AreEqual(
            new OrganizationId(10003),
            service.LastLookup(),
            "The page should ask for the organization the route names.");
        Assert.AreSame(
            organization,
            page.Organization,
            "The page should show the organization the service found.");
        Assert.AreEqual(
            StatusCodes.Status200OK,
            response.StatusCode,
            "A known organization should be answered with 200.");
    }

    /// <summary>
    /// Unit test to verify that an identifier that names no organization still gets the page, which says so, and the
    /// status 404, so the browser and any crawler know the address is wrong.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_UnknownIdentifier_AnswersNotFound()
    {
        // Arrange (Given)
        StubOrganizationService service = new StubOrganizationService();
        DetailsModel page = CreatePage(service, 99999);

        // Act (When)
        IActionResult result = await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);
        HttpResponse response = page.Response;

        // Assert (Then)
        Assert.IsInstanceOfType<PageResult>(
            result,
            "An unknown organization should still get the page, which says it was not found.");
        Assert.IsNull(
            page.Organization,
            "The page should have no organization to show.");
        Assert.AreEqual(
            StatusCodes.Status404NotFound,
            response.StatusCode,
            "An unknown organization should be answered with 404.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates an organization as the service would answer with it, with invented values.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <returns>The organization.</returns>
    private static OrganizationSingleResponse CreateOrganization(long id)
    {
        UserReference demo = new UserReference
        {
            Id = new UserId(10001),
            Name = new DisplayName("Demo User"),
        };
        DateTime createdOn = new DateTime(2026, 3, 8, 9, 30, 0, DateTimeKind.Utc);
        return new OrganizationSingleResponse
        {
            Id = new OrganizationId(id),
            Name = new OrganizationName("Placeholder Logistics"),
            Industry = Industry.Shipping,
            Contacts = new RecordCount(18),
            Version = new VersionNumber(1),
            Created = new RecordChange { On = createdOn, By = demo },
            LastModified = new RecordChange { On = createdOn, By = demo },
        };
    }

    /// <summary>
    /// Creates the page as the framework would for one request, with the identifier the route gives.
    /// </summary>
    /// <param name="service">The organization service the page reads through.</param>
    /// <param name="id">The identifier in the route.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static DetailsModel CreatePage(StubOrganizationService service, long id)
    {
        DefaultHttpContext http = new DefaultHttpContext();
        EmptyModelMetadataProvider metadata = new EmptyModelMetadataProvider();
        ActionContext action = new ActionContext(http, new RouteData(), new CompiledPageActionDescriptor());
        PageContext context = new PageContext(action)
        {
            ViewData = new ViewDataDictionary(metadata, new ModelStateDictionary()),
        };
        return new DetailsModel(service)
        {
            PageContext = context,
            MetadataProvider = metadata,
            Input = new DetailsModel.InputModel { Id = id },
        };
    }

    #endregion Private Methods
}
