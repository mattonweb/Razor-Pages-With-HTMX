namespace HTMX.Test.Unit.Adapter.Customers.Services;

using System;
using System.Collections.Generic;
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
    // More pages than any list in the seed runs to, so a walk that never ends fails instead of hanging.
    private const int MaximumPages = 50;

    #region Public Methods

    /// <summary>
    /// Unit test to verify that the list's first view, active organizations by name, holds a page of ten in name order
    /// however the names are capitalized, with the counts the status filter shows, and a next page but no previous.
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
        OrganizationCursor next = new OrganizationCursor(response.Items[^1].Id, CursorPosition.After);

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
        Assert.IsNull(
            response.Previous,
            "The first page should have no previous page.");
        Assert.AreEqual(
            next,
            response.Next,
            "The next page should start after the last organization shown.");
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
        Assert.IsNull(
            response.Next,
            "An empty list should have no next page.");
    }

    /// <summary>
    /// Unit test to verify that the next page starts just after the last organization of the page before, so that two
    /// pages of ten, one after the other, are exactly the first page of twenty.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_NextCursor_ContinuesWhereThePageEnded()
    {
        // Arrange (Given)
        OrganizationCollectionRequest tens = CreateRequest(RecordStatus.Active, OrganizationSortColumn.Name);
        OrganizationCollectionRequest twenty = CreateRequest(
            RecordStatus.Active,
            OrganizationSortColumn.Name,
            pageSize: 20);

        // Act (When)
        OrganizationCollectionResponse[] pages = await ReadInTurnAsync(tens, CursorPosition.After).ConfigureAwait(false);
        OrganizationCollectionResponse whole = await ListAsync(twenty).ConfigureAwait(false);
        OrganizationId[] followed = [.. pages.SelectMany(static page => page.Items).Select(static item => item.Id)];
        OrganizationId[] expected = [.. whole.Items.Select(static item => item.Id)];

        // Assert (Then)
        CollectionAssert.AreEqual(
            expected,
            followed,
            "The first two pages of ten should be the first page of twenty, in the same order.");
        Assert.AreEqual(
            new OrganizationCursor(pages[1].Items[0].Id, CursorPosition.Before),
            pages[1].Previous,
            "The second page's previous page should end before its first organization.");
    }

    /// <summary>
    /// Unit test to verify that Previous from the second page goes back to exactly the first page, which has no
    /// previous page of its own.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_PreviousCursor_ReturnsThePageBefore()
    {
        // Arrange (Given)
        OrganizationCollectionRequest request = CreateRequest(RecordStatus.Active, OrganizationSortColumn.Name);

        // Act (When)
        OrganizationCollectionResponse[] pages = await ReadInTurnAsync(
            request,
            CursorPosition.After,
            CursorPosition.Before).ConfigureAwait(false);
        OrganizationId[] first = [.. pages[0].Items.Select(static item => item.Id)];
        OrganizationId[] back = [.. pages[2].Items.Select(static item => item.Id)];

        // Assert (Then)
        CollectionAssert.AreEqual(
            first,
            back,
            "Next and then Previous should come back to the first page, in the same order.");
        Assert.IsNull(
            pages[2].Previous,
            "The page Previous came back to should have no previous page.");
    }

    /// <summary>
    /// Unit test to verify that a page read backwards that reaches the start of the list too soon, as after a larger
    /// page size, shows the whole first page instead of a short one.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_PreviousReachesTheStartShort_ReturnsAWholeFirstPage()
    {
        // Arrange (Given)
        OrganizationCollectionRequest tens = CreateRequest(RecordStatus.Active, OrganizationSortColumn.Name);
        OrganizationCollectionResponse[] pages = await ReadInTurnAsync(tens, CursorPosition.After).ConfigureAwait(false);
        OrganizationCollectionRequest larger = At(
            CreateRequest(RecordStatus.Active, OrganizationSortColumn.Name, pageSize: 25),
            pages[1].Previous);

        // Act (When)
        OrganizationCollectionResponse response = await ListAsync(larger).ConfigureAwait(false);

        // Assert (Then)
        Assert.HasCount(
            25,
            response.Items,
            "Only ten organizations come before the cursor, so the whole first page of 25 should show.");
        Assert.IsNull(
            response.Request.Cursor,
            "The page shown should be the first page, with no cursor.");
        Assert.IsNull(
            response.Previous,
            "The first page should have no previous page.");
    }

    /// <summary>
    /// Unit test to verify that a cursor naming an organization that does not exist, as an old link might, starts
    /// again at the first page rather than showing nothing.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_CursorNamesNoOrganization_ReturnsTheFirstPage()
    {
        // Arrange (Given)
        OrganizationCursor unknown = new OrganizationCursor(new OrganizationId(99999), CursorPosition.After);
        OrganizationCollectionRequest request = At(
            CreateRequest(RecordStatus.Active, OrganizationSortColumn.Name),
            unknown);

        // Act (When)
        OrganizationCollectionResponse response = await ListAsync(request).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsNull(
            response.Request.Cursor,
            "The page shown should be the first page, with no cursor.");
        Assert.HasCount(
            10,
            response.Items,
            "The first page should hold ten organizations.");
        Assert.IsNull(
            response.Previous,
            "The first page should have no previous page.");
    }

    /// <summary>
    /// Unit test to verify that following Next from the first page to the last visits every organization exactly once,
    /// in every sort order, including the ones where many organizations tie and the identifier has to settle the order.
    /// </summary>
    /// <param name="sort">What to sort by.</param>
    /// <param name="direction">Which way to sort.</param>
    [TestMethod]
    [DataRow(OrganizationSortColumn.Name, SortDirection.Ascending)]
    [DataRow(OrganizationSortColumn.Name, SortDirection.Descending)]
    [DataRow(OrganizationSortColumn.Industry, SortDirection.Ascending)]
    [DataRow(OrganizationSortColumn.Industry, SortDirection.Descending)]
    [DataRow(OrganizationSortColumn.Contacts, SortDirection.Ascending)]
    [DataRow(OrganizationSortColumn.Contacts, SortDirection.Descending)]
    [DataRow(OrganizationSortColumn.Updated, SortDirection.Ascending)]
    [DataRow(OrganizationSortColumn.Updated, SortDirection.Descending)]
    public async Task ListAsync_FollowingNextToTheEnd_VisitsEveryOrganizationOnce(
        OrganizationSortColumn sort,
        SortDirection direction)
    {
        // Arrange (Given)
        OrganizationCollectionRequest request = CreateRequest(RecordStatus.All, sort, direction: direction);

        // Act (When)
        OrganizationCollectionResponse[] pages = await WalkToTheEndAsync(request).ConfigureAwait(false);
        OrganizationId[] visited = [.. pages.SelectMany(static page => page.Items).Select(static item => item.Id)];
        OrganizationId[] distinct = [.. visited.Distinct()];

        // Assert (Then)
        Assert.HasCount(
            10,
            pages,
            "1,000 organizations, 100 a page, should take ten pages.");
        Assert.HasCount(
            1000,
            visited,
            "Every organization should be shown on the way.");
        Assert.HasCount(
            1000,
            distinct,
            "No organization should be shown twice.");
        Assert.IsNull(
            pages[^1].Next,
            "The last page should have no next page.");
    }

    /// <summary>
    /// Unit test to verify that sorting by contacts, most first, starts with Placeholder Logistics, which has the most.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_SortByContactsDescending_StartsWithPlaceholderLogistics()
    {
        // Arrange (Given)
        OrganizationCollectionRequest request = CreateRequest(
            RecordStatus.All,
            OrganizationSortColumn.Contacts,
            direction: SortDirection.Descending,
            pageSize: 10);

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
        OrganizationCollectionRequest request = CreateRequest(
            RecordStatus.All,
            OrganizationSortColumn.Updated,
            direction: SortDirection.Descending,
            pageSize: 25);

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
    /// Creates a request for the first page: ten a page for active organizations, 100 otherwise, unless a size is
    /// given.
    /// </summary>
    /// <param name="status">Which organizations to list.</param>
    /// <param name="sort">What to sort by.</param>
    /// <param name="filter">The text the names must contain, if any.</param>
    /// <param name="direction">Which way to sort.</param>
    /// <param name="pageSize">How many organizations a page holds, if not the default.</param>
    /// <returns>The request.</returns>
    private static OrganizationCollectionRequest CreateRequest(
        RecordStatus status,
        OrganizationSortColumn sort,
        string? filter = null,
        SortDirection direction = SortDirection.Ascending,
        int? pageSize = null)
    {
        int size = pageSize ?? (status == RecordStatus.Active ? 10 : PageSize.Largest);
        return new OrganizationCollectionRequest
        {
            Filter = filter is null ? null : new FilterText(filter),
            Status = status,
            Sort = sort,
            Direction = direction,
            PageSize = new PageSize(size),
        };
    }

    /// <summary>
    /// Makes the same request with a cursor, as a Previous or Next link would.
    /// </summary>
    /// <param name="request">The request to move.</param>
    /// <param name="cursor">Where the page lies, or <see langword="null"/> for the first page.</param>
    /// <returns>The request with the cursor.</returns>
    private static OrganizationCollectionRequest At(OrganizationCollectionRequest request, OrganizationCursor? cursor)
    {
        return new OrganizationCollectionRequest
        {
            Filter = request.Filter,
            Status = request.Status,
            Sort = request.Sort,
            Direction = request.Direction,
            Cursor = cursor,
            PageSize = request.PageSize,
        };
    }

    /// <summary>
    /// Reads one page through the service, over a database of its own.
    /// </summary>
    /// <param name="request">The page to read.</param>
    /// <returns>The page.</returns>
    private static async Task<OrganizationCollectionResponse> ListAsync(OrganizationCollectionRequest request)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            return await ReadAsync(services, request).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reads a first page and then, in turn, the page each step leads to: Next for After, Previous for Before.
    /// </summary>
    /// <param name="first">The first page to read.</param>
    /// <param name="steps">The links to follow, one after another.</param>
    /// <returns>Every page read, the first page first.</returns>
    private static async Task<OrganizationCollectionResponse[]> ReadInTurnAsync(
        OrganizationCollectionRequest first,
        params CursorPosition[] steps)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            return await FollowAsync(services, first, steps).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reads a first page and then follows Next until there is no next page.
    /// </summary>
    /// <param name="first">The first page to read.</param>
    /// <returns>Every page read, in order.</returns>
    private static async Task<OrganizationCollectionResponse[]> WalkToTheEndAsync(OrganizationCollectionRequest first)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            return await FollowNextAsync(services, first).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reads a first page and then the page each step leads to, over one database.
    /// </summary>
    /// <param name="services">The started services.</param>
    /// <param name="first">The first page to read.</param>
    /// <param name="steps">The links to follow, one after another.</param>
    /// <returns>Every page read, the first page first.</returns>
    private static async Task<OrganizationCollectionResponse[]> FollowAsync(
        ServiceProvider services,
        OrganizationCollectionRequest first,
        CursorPosition[] steps)
    {
        OrganizationCollectionResponse page = await ReadAsync(services, first).ConfigureAwait(false);
        List<OrganizationCollectionResponse> pages = [page];
        foreach (CursorPosition step in steps)
        {
            OrganizationCursor? cursor = step == CursorPosition.After ? page.Next : page.Previous;
            page = await ReadAsync(services, At(first, cursor)).ConfigureAwait(false);
            pages.Add(page);
        }

        return [.. pages];
    }

    /// <summary>
    /// Reads a first page and follows Next until there is no next page, over one database.
    /// </summary>
    /// <param name="services">The started services.</param>
    /// <param name="first">The first page to read.</param>
    /// <returns>Every page read, in order.</returns>
    private static async Task<OrganizationCollectionResponse[]> FollowNextAsync(
        ServiceProvider services,
        OrganizationCollectionRequest first)
    {
        List<OrganizationCollectionResponse> pages = [];
        OrganizationCollectionRequest? request = first;
        while (request is not null && pages.Count < MaximumPages)
        {
            OrganizationCollectionResponse page = await ReadAsync(services, request).ConfigureAwait(false);
            pages.Add(page);
            request = page.Next is OrganizationCursor next ? At(first, next) : null;
        }

        return [.. pages];
    }

    /// <summary>
    /// Reads one page through the service, in a scope of its own as one request would.
    /// </summary>
    /// <param name="services">The started services.</param>
    /// <param name="request">The page to read.</param>
    /// <returns>The page.</returns>
    private static async Task<OrganizationCollectionResponse> ReadAsync(
        ServiceProvider services,
        OrganizationCollectionRequest request)
    {
        using (IServiceScope scope = services.CreateScope())
        {
            IOrganizationService organizations = scope.ServiceProvider.GetRequiredService<IOrganizationService>();
            return await organizations.ListAsync(request).ConfigureAwait(false);
        }
    }

    #endregion Private Methods
}
