namespace HTMX.Test.Unit.Application.Web.Pages.Organizations;

using System;
using System.Linq;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Pages;
using HTMX.Application.Web.Pages.Organizations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="OrganizationChanges"/> class: which fields a
/// version or a roll-back changes, and the words that sum them up.
/// </summary>
[TestClass]
public sealed class OrganizationChangesTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that an edit's changes are the fields whose values differ, in the Details tab's order, with
    /// a missing website as no value.
    /// </summary>
    [TestMethod]
    public void Between_EditedNameAndWebsite_GivesThoseFieldsInOrder()
    {
        // Arrange (Given)
        OrganizationVersionResponse before = CreateVersion(1, "Placeholder Freight", "https://placeholder.example");
        OrganizationVersionResponse after = CreateVersion(2, "Placeholder Logistics", website: null);

        // Act (When)
        FieldChange[] changes = OrganizationChanges.Between(before, after);

        // Assert (Then)
        CollectionAssert.AreEqual(
            new[] { "Name", "Website" },
            changes.Select(static change => change.Field).ToArray(),
            "Only the name and the website changed, in that order.");
        Assert.IsNull(
            changes[1].After,
            "A website removed should have no value after.");
    }

    /// <summary>
    /// Unit test to verify that archiving changes only the status, and that a roll-back never does.
    /// </summary>
    [TestMethod]
    public void Between_Archived_GivesOnlyTheStatus()
    {
        // Arrange (Given)
        OrganizationVersionResponse active = CreateVersion(1, "Placeholder Logistics");
        OrganizationVersionResponse archived = CreateVersion(2, "Placeholder Logistics", archived: true);

        // Act (When)
        FieldChange[] archiving = OrganizationChanges.Between(active, archived);
        FieldChange[] rollingBack = OrganizationChanges.RollBack(archived, active);

        // Assert (Then)
        Assert.AreEqual(
            ("Status", "Active", "Archived"),
            (archiving.Single().Field, archiving.Single().Before, archiving.Single().After),
            "Archiving should change only the status.");
        Assert.IsEmpty(
            rollingBack,
            "A roll-back saves values, not the status, so there is nothing to change.");
    }

    /// <summary>
    /// Unit test to verify that a summary names one, two or three fields as a sentence would.
    /// </summary>
    /// <param name="count">How many fields changed.</param>
    /// <param name="expected">The summary.</param>
    [TestMethod]
    [DataRow(1, "Name", DisplayName = "One field")]
    [DataRow(2, "Name and Industry", DisplayName = "Two fields")]
    [DataRow(3, "Name, Industry and Website", DisplayName = "Three fields")]
    public void Fields_SomeFieldsChanged_JoinsThemAsASentence(int count, string expected)
    {
        // Arrange (Given)
        FieldChange[] changes =
        [
            .. new[] { "Name", "Industry", "Website" }
                .Take(count)
                .Select(static field => new FieldChange { Field = field }),
        ];

        // Act (When)
        string summary = OrganizationChanges.Fields(changes);

        // Assert (Then)
        Assert.AreEqual(
            expected,
            summary,
            "The fields should read as a list in a sentence.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a version with invented values, in shipping.
    /// </summary>
    /// <param name="number">The version's number.</param>
    /// <param name="name">The name it left.</param>
    /// <param name="website">The website it left, if any.</param>
    /// <param name="archived">Whether it left the organization archived.</param>
    /// <returns>The version.</returns>
    private static OrganizationVersionResponse CreateVersion(
        int number,
        string name,
        string? website = null,
        bool archived = false)
    {
        DateTime on = new DateTime(2026, 11, 8, 9, 30, 0, DateTimeKind.Utc);
        return new OrganizationVersionResponse
        {
            Version = new VersionNumber(number),
            Change = ChangeKind.Edited,
            Changed = new RecordChange
            {
                On = on,
                By = new UserReference { Id = new UserId(10001), Name = new DisplayName("Demo User") },
            },
            Name = new OrganizationName(name),
            Industry = Industry.Shipping,
            Website = website is null ? null : new WebAddress(website),
            ArchivedOn = archived ? on : null,
        };
    }

    #endregion Private Methods
}
