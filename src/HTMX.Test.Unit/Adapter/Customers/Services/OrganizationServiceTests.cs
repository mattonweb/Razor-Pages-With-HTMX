namespace HTMX.Test.Unit.Adapter.Customers.Services;

using System;
using System.Linq;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Data.Seed;
using HTMX.Adapter.Customers.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="OrganizationService"/> class, reached through
/// <see cref="IOrganizationService"/> over the seeded database.
/// </summary>
[TestClass]
public sealed class OrganizationServiceTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the list's first view, active organizations by name, holds a page of ten in name order
    /// however the names are capitalized, with the counts the status filter shows.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_FirstPageOfActive_ReturnsTenInNameOrder()
    {
        // Arrange (Given)
        OrganizationCollectionRequest request = CreateRequest(RecordStatus.Active, OrganizationSortColumn.Name);

        // Act (When)
        OrganizationCollectionResponse response = await ListAsync(request).ConfigureAwait(false);
        string[] names = [.. response.Items.Select(static item => item.Name.Value)];
        string[] sorted = [.. names.Order(StringComparer.OrdinalIgnoreCase)];

        // Assert (Then)
        Assert.HasCount(
            10,
            response.Items,
            "The first page should hold ten organizations.");
        CollectionAssert.AreEqual(
            sorted,
            names,
            "The page should be in name order, ignoring capitals.");
        Assert.AreEqual(
            949,
            response.Matching.Value,
            "949 active organizations should match.");
        Assert.AreEqual(
            51,
            response.Archived.Value,
            "51 archived organizations should be counted for the status filter.");
        Assert.AreEqual(
            1000,
            response.All.Value,
            "1,000 organizations should be counted in all.");
    }

    /// <summary>
    /// Unit test to verify that the filter finds its text anywhere in a name, however either is capitalized.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_FilterInCapitals_ReturnsOnlyNamesContainingIt()
    {
        // Arrange (Given)
        OrganizationCollectionRequest request = CreateRequest(RecordStatus.All, OrganizationSortColumn.Name, "FREIGHT");

        // Act (When)
        OrganizationCollectionResponse response = await ListAsync(request).ConfigureAwait(false);
        OrganizationSummary[] others =
        [
            .. response.Items.Where(static item =>
                !item.Name.Value.Contains("freight", StringComparison.OrdinalIgnoreCase)),
        ];

        // Assert (Then)
        Assert.IsTrue(
            response.Matching.Value > 1,
            "More than one organization should have Freight in its name.");
        Assert.HasCount(
            response.Matching.Value,
            response.Items,
            "Every match should fit on one page of 100.");
        Assert.HasCount(
            0,
            others,
            "Every organization listed should have freight in its name.");
    }

    /// <summary>
    /// Unit test to verify that a percent sign in the filter is taken literally, not as LIKE's wildcard that would
    /// match every name.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_FilterIsPercentSign_MatchesNothing()
    {
        // Arrange (Given)
        OrganizationCollectionRequest request = CreateRequest(RecordStatus.All, OrganizationSortColumn.Name, "%");

        // Act (When)
        OrganizationCollectionResponse response = await ListAsync(request).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            0,
            response.Matching.Value,
            "No organization's name contains a percent sign.");
    }

    /// <summary>
    /// Unit test to verify that a page past the end of the list, as an old link might ask for, shows the last page.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_PageBeyondTheLast_ReturnsTheLastPage()
    {
        // Arrange (Given)
        OrganizationCollectionRequest request = new OrganizationCollectionRequest
        {
            Status = RecordStatus.Archived,
            Sort = OrganizationSortColumn.Name,
            Direction = SortDirection.Ascending,
            Page = new PageNumber(99),
            PageSize = new PageSize(10),
        };

        // Act (When)
        OrganizationCollectionResponse response = await ListAsync(request).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            6,
            response.Request.Page.Value,
            "51 archived organizations, ten a page, should end on page 6.");
        Assert.HasCount(
            1,
            response.Items,
            "The last page should hold the one organization left over.");
    }

    /// <summary>
    /// Unit test to verify that sorting by contacts, most first, starts with Placeholder Logistics, which has the most.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_SortByContactsDescending_StartsWithPlaceholderLogistics()
    {
        // Arrange (Given)
        OrganizationCollectionRequest request = new OrganizationCollectionRequest
        {
            Status = RecordStatus.All,
            Sort = OrganizationSortColumn.Contacts,
            Direction = SortDirection.Descending,
            Page = new PageNumber(1),
            PageSize = new PageSize(10),
        };

        // Act (When)
        OrganizationCollectionResponse response = await ListAsync(request).ConfigureAwait(false);
        OrganizationSummary first = response.Items[0];

        // Assert (Then)
        Assert.AreEqual(
            OrganizationSeed.PlaceholderLogistics,
            first.Id,
            "Placeholder Logistics should come first.");
        Assert.AreEqual(
            18,
            first.Contacts.Value,
            "Placeholder Logistics should count its 18 contacts.");
    }

    /// <summary>
    /// Unit test to verify that sorting by the last change, newest first, keeps that order down the page.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_SortByUpdatedDescending_ReturnsNewestFirst()
    {
        // Arrange (Given)
        OrganizationCollectionRequest request = new OrganizationCollectionRequest
        {
            Status = RecordStatus.All,
            Sort = OrganizationSortColumn.Updated,
            Direction = SortDirection.Descending,
            Page = new PageNumber(1),
            PageSize = new PageSize(25),
        };

        // Act (When)
        OrganizationCollectionResponse response = await ListAsync(request).ConfigureAwait(false);
        DateTime[] changes = [.. response.Items.Select(static item => item.LastModifiedOn)];
        DateTime[] newestFirst = [.. changes.OrderDescending()];

        // Assert (Then)
        CollectionAssert.AreEqual(
            newestFirst,
            changes,
            "The page should be in order of the last change, newest first.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a request for the first page of 100, ascending.
    /// </summary>
    /// <param name="status">Which organizations to list.</param>
    /// <param name="sort">What to sort by.</param>
    /// <param name="filter">The text the names must contain, if any.</param>
    /// <returns>The request.</returns>
    private static OrganizationCollectionRequest CreateRequest(
        RecordStatus status,
        OrganizationSortColumn sort,
        string? filter = null)
    {
        return new OrganizationCollectionRequest
        {
            Filter = filter is null ? null : new FilterText(filter),
            Status = status,
            Sort = sort,
            Direction = SortDirection.Ascending,
            Page = new PageNumber(1),
            PageSize = new PageSize(status == RecordStatus.Active ? 10 : PageSize.Largest),
        };
    }

    /// <summary>
    /// Reads one page through the service, in a scope of its own as one request would.
    /// </summary>
    /// <param name="request">The page to read.</param>
    /// <returns>The page.</returns>
    private static async Task<OrganizationCollectionResponse> ListAsync(OrganizationCollectionRequest request)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            using (IServiceScope scope = services.CreateScope())
            {
                IOrganizationService organizations = scope.ServiceProvider.GetRequiredService<IOrganizationService>();
                return await organizations.ListAsync(request).ConfigureAwait(false);
            }
        }
    }

    #endregion Private Methods
}
