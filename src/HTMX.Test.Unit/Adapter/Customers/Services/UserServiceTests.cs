namespace HTMX.Test.Unit.Adapter.Customers.Services;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the user service behind <see cref="IUserService"/>, over a seeded
/// in-memory database: the pages of the users list, filtered, narrowed by status, sorted by each column either way,
/// and paged by key.
/// </summary>
[TestClass]
public sealed class UserServiceTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the list's first view, active users by name, holds a page of ten in name order, with
    /// the counts the status filter shows, and a next page but no previous.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_FirstPageOfActive_ReturnsTenInNameOrder()
    {
        // Arrange (Given)
        UserCollectionRequest request = CreateRequest(RecordStatus.Active, UserSortColumn.Name);

        // Act (When)
        UserCollectionResponse response = await UserServiceCalls.ListAsync(request).ConfigureAwait(false);
        string[] names = [.. response.Items.Select(static item => item.DisplayName.Value)];
        string[] sorted = [.. names.Order(StringComparer.OrdinalIgnoreCase)];
        UserCursor next = new UserCursor(response.Items[^1].Id, CursorPosition.After);

        // Assert (Then)
        CollectionAssert.AreEqual(
            sorted,
            names,
            "The page should be in name order, ignoring capitals.");
        Assert.AreEqual(
            (10, 42, 7, 49),
            (response.Items.Length, response.Matching.Value, response.Archived.Value, response.All.Value),
            "The page should hold ten of the 42 active users, with 7 archived and 49 in all.");
        Assert.AreEqual(
            ((UserCursor?)null, (UserCursor?)next),
            (response.Previous, response.Next),
            "The first page should have no previous page, and its next should start after its last user.");
    }

    /// <summary>
    /// Unit test to verify that the filter finds its text in a name, a username or an email address, however either is
    /// capitalized.
    /// </summary>
    /// <param name="filter">The text to find.</param>
    /// <param name="expected">The username of a user it should find.</param>
    [TestMethod]
    [DataRow("y d", "avery.draft", DisplayName = "In a name: Avery Draft")]
    [DataRow("Y.D", "avery.draft", DisplayName = "In a username: avery.draft")]
    [DataRow("EXAMPLE.NET", "jordan.demo", DisplayName = "In an email address: jordan@example.net")]
    public async Task ListAsync_Filter_FindsItInTheNameUsernameOrEmail(string filter, string expected)
    {
        // Arrange (Given)
        UserCollectionRequest request = CreateRequest(RecordStatus.All, UserSortColumn.Name, filter: filter);

        // Act (When)
        UserCollectionResponse response = await UserServiceCalls.ListAsync(request).ConfigureAwait(false);
        string[] usernames = [.. response.Items.Select(static item => item.Username.Value)];

        // Assert (Then)
        CollectionAssert.Contains(
            usernames,
            expected,
            $"The filter {filter} should find {expected}.");
        Assert.IsTrue(
            response.Items.All(item => Holds(item, filter)),
            "Every user found should hold the filter in their name, username or email address.");
    }

    /// <summary>
    /// Unit test to verify that Previous from the second page leads back to the first page, whole.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_PreviousFromTheSecondPage_ReturnsTheFirstPage()
    {
        // Arrange (Given)
        UserCollectionRequest request = CreateRequest(RecordStatus.Active, UserSortColumn.Name);

        // Act (When)
        UserCollectionResponse[] pages = await UserServiceCalls
            .ReadInTurnAsync(request, CursorPosition.After, CursorPosition.Before)
            .ConfigureAwait(false);

        // Assert (Then)
        CollectionAssert.AreEqual(
            pages[0].Items.Select(static item => item.Id).ToArray(),
            pages[2].Items.Select(static item => item.Id).ToArray(),
            "Next and then Previous should show the first page again.");
        Assert.IsNull(
            pages[2].Previous,
            "Back on the first page, there should be no previous page.");
    }

    /// <summary>
    /// Unit test to verify that a cursor naming no user, as an old link might, shows the first page.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_CursorNamesNoUser_ReturnsTheFirstPage()
    {
        // Arrange (Given)
        UserCollectionRequest first = CreateRequest(RecordStatus.Active, UserSortColumn.Name);
        UserCursor nobody = new UserCursor(new UserId(99999), CursorPosition.After);

        // Act (When)
        UserCollectionResponse response = await UserServiceCalls
            .ListAsync(UserServiceCalls.At(first, nobody))
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            ((UserCursor?)null, (UserCursor?)null, 10),
            (response.Request.Cursor, response.Previous, response.Items.Length),
            "The answer should be the first page, which forgets the cursor.");
    }

    /// <summary>
    /// Unit test to verify that following Next from the first page to the last shows every user once, in the order the
    /// list is sorted, by each column and either way: each sort's seek leaves no one out and shows no one twice.
    /// </summary>
    /// <param name="sort">What to sort by.</param>
    /// <param name="direction">Which way to sort.</param>
    [TestMethod]
    [DataRow(UserSortColumn.Name, SortDirection.Ascending)]
    [DataRow(UserSortColumn.Name, SortDirection.Descending)]
    [DataRow(UserSortColumn.Username, SortDirection.Ascending)]
    [DataRow(UserSortColumn.Username, SortDirection.Descending)]
    [DataRow(UserSortColumn.Email, SortDirection.Ascending)]
    [DataRow(UserSortColumn.Email, SortDirection.Descending)]
    [DataRow(UserSortColumn.Updated, SortDirection.Ascending)]
    [DataRow(UserSortColumn.Updated, SortDirection.Descending)]
    public async Task ListAsync_FollowingNextToTheEnd_VisitsEveryUserOnceInOrder(
        UserSortColumn sort,
        SortDirection direction)
    {
        // Arrange (Given)
        UserCollectionRequest request = CreateRequest(RecordStatus.All, sort, direction: direction);

        // Act (When)
        UserCollectionResponse[] pages = await UserServiceCalls.WalkToTheEndAsync(request).ConfigureAwait(false);
        UserSummary[] visited = [.. pages.SelectMany(static page => page.Items)];
        UserId[] expected = [.. Sorted(visited, sort, direction).Select(static item => item.Id)];

        // Assert (Then)
        Assert.AreEqual(
            (5, 9, (UserCursor?)null),
            (pages.Length, pages[^1].Items.Length, pages[^1].Next),
            "49 users, ten to a page, should take five pages, the last of nine, with no next page.");
        CollectionAssert.AreEqual(
            expected,
            visited.Select(static item => item.Id).ToArray(),
            "Every user should be shown once, in the list's order, ties in identifier order.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the request for the first page of the users list.
    /// </summary>
    /// <param name="status">Which users to show.</param>
    /// <param name="sort">What to sort by.</param>
    /// <param name="filter">The text to find, if any.</param>
    /// <param name="direction">Which way to sort.</param>
    /// <returns>The request, ten to a page.</returns>
    private static UserCollectionRequest CreateRequest(
        RecordStatus status,
        UserSortColumn sort,
        string? filter = null,
        SortDirection direction = SortDirection.Ascending)
    {
        return new UserCollectionRequest
        {
            Filter = filter is null ? null : new FilterText(filter),
            Status = status,
            Sort = sort,
            Direction = direction,
            PageSize = new PageSize(10),
        };
    }

    /// <summary>
    /// Says whether a user holds the filter's text in their name, username or email address, ignoring capitals.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="filter">The text.</param>
    /// <returns><see langword="true"/> when one of the three holds it.</returns>
    private static bool Holds(UserSummary user, string filter)
    {
        string[] values = [user.DisplayName.Value, user.Username.Value, user.Email.Value];
        return values.Any(value => value.Contains(filter, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Sorts users as the list should: by the column's text ignoring capitals, or by when they were last changed, then
    /// by identifier, all one way.
    /// </summary>
    /// <param name="users">The users.</param>
    /// <param name="sort">What to sort by.</param>
    /// <param name="direction">Which way to sort.</param>
    /// <returns>The users in the order expected.</returns>
    private static UserSummary[] Sorted(UserSummary[] users, UserSortColumn sort, SortDirection direction)
    {
        // A round-trip date sorts as text in time order, so every column can be compared as text.
        Func<UserSummary, string> key = sort switch
        {
            UserSortColumn.Username =>
                static user => user.Username.Value,
            UserSortColumn.Email =>
                static user => user.Email.Value,
            UserSortColumn.Updated =>
                static user => user.LastModifiedOn.ToString("O", CultureInfo.InvariantCulture),
            _ =>
                static user => user.DisplayName.Value,
        };
        IOrderedEnumerable<UserSummary> ordered = direction == SortDirection.Descending
            ? users.OrderByDescending(key, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(static user => user.Id.Value)
            : users.OrderBy(key, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static user => user.Id.Value);
        return [.. ordered];
    }

    #endregion Private Methods
}
