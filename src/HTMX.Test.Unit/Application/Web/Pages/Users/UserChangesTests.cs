namespace HTMX.Test.Unit.Application.Web.Pages.Users;

using System;
using System.Linq;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Pages;
using HTMX.Application.Web.Pages.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="UserChanges"/> class: the values a version holds,
/// in the page's order, and what each entry of the timeline offers while the user is active and once they are
/// archived.
/// </summary>
[TestClass]
public sealed class UserChangesTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a version is described by name, username and email address, in the page's order, and
    /// keeps whether it left the user archived.
    /// </summary>
    [TestMethod]
    public void Describe_ArchivedVersion_GivesItsValuesInOrder()
    {
        // Arrange (Given)
        UserVersionResponse version = CreateVersion(2, "Pat Example", "pat@example.com", archived: true);

        // Act (When)
        HistoryVersion described = UserChanges.Describe(version);

        // Assert (Then)
        CollectionAssert.AreEqual(
            new[] { "Name", "Username", "Email" },
            described.Values.Select(static value => value.Field).ToArray(),
            "The fields should be the page's, in its order.");
        CollectionAssert.AreEqual(
            new[] { "Pat Example", "pat.example", "pat@example.com" },
            described.Values.Select(static value => value.Value).ToArray(),
            "Each field should hold the version's value.");
        Assert.AreEqual(
            (new VersionNumber(2), version.ArchivedOn),
            (described.Version, described.ArchivedOn),
            "The description should keep the version's number, and when it left the user archived.");
    }

    /// <summary>
    /// Unit test to verify that an active user's timeline offers a roll-back to an earlier version whose values differ
    /// from now, and says so of one whose values do not.
    /// </summary>
    [TestMethod]
    public void Entries_ActiveUser_OffersRollBackWhereValuesDiffer()
    {
        // Arrange (Given)
        UserVersionResponse[] versions = CreateHistory();

        // Act (When)
        TimelineEntry[] entries = UserChanges.Entries(CreateUser(archived: false), versions);

        // Assert (Then)
        CollectionAssert.AreEqual(
            new[] { 0, 0, 2 },
            entries.Select(static entry => entry.RollBack.Length).ToArray(),
            "Only version 1 should offer a roll-back, of the name and the email address.");
        Assert.AreEqual(
            "Version 2 holds the same values as now.",
            entries[1].Note,
            "Version 2 should say why it offers none.");
    }

    /// <summary>
    /// Unit test to verify that an archived user's timeline offers no roll-back and says nothing in its place, since
    /// the header already says why.
    /// </summary>
    [TestMethod]
    public void Entries_ArchivedUser_OffersNoRollBack()
    {
        // Arrange (Given)
        UserVersionResponse[] versions = CreateHistory();

        // Act (When)
        TimelineEntry[] entries = UserChanges.Entries(CreateUser(archived: true), versions);

        // Assert (Then)
        Assert.IsTrue(
            entries.All(static entry => entry.RollBack.Length == 0 && entry.Note is null),
            "No entry should offer a roll-back, or say anything in its place.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates Pat Example at version 3, with invented values.
    /// </summary>
    /// <param name="archived">Whether they are archived.</param>
    /// <returns>The user.</returns>
    private static UserSingleResponse CreateUser(bool archived)
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
            Version = new VersionNumber(3),
            Created = change,
            LastModified = change,
            Archived = archived ? change : null,
        };
    }

    /// <summary>
    /// Creates three versions, newest first: two that leave the values as they are now, and the first, with the name
    /// misspelled and another email address.
    /// </summary>
    /// <returns>The versions.</returns>
    private static UserVersionResponse[] CreateHistory()
    {
        return
        [
            CreateVersion(3, "Pat Example", "pat@example.com"),
            CreateVersion(2, "Pat Example", "pat@example.com"),
            CreateVersion(1, "Pat Exampel", "pat@placeholder.example"),
        ];
    }

    /// <summary>
    /// Creates a version with invented values and the username pat.example.
    /// </summary>
    /// <param name="number">The version's number.</param>
    /// <param name="name">The name it left.</param>
    /// <param name="email">The email address it left.</param>
    /// <param name="archived">Whether it left the user archived.</param>
    /// <returns>The version.</returns>
    private static UserVersionResponse CreateVersion(int number, string name, string email, bool archived = false)
    {
        DateTime on = new DateTime(2026, 11, 8, 9, 30, 0, DateTimeKind.Utc);
        return new UserVersionResponse
        {
            Version = new VersionNumber(number),
            Change = ChangeKind.Edited,
            Changed = new RecordChange
            {
                On = on,
                By = new UserReference { Id = new UserId(10001), Name = new DisplayName("Demo User") },
            },
            DisplayName = new DisplayName(name),
            Username = new Username("pat.example"),
            Email = new EmailAddress(email),
            ArchivedOn = archived ? on : null,
        };
    }

    #endregion Private Methods
}
