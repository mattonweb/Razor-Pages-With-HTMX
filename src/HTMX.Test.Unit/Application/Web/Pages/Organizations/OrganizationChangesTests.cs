namespace HTMX.Test.Unit.Application.Web.Pages.Organizations;

using System;
using System.Linq;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Pages;
using HTMX.Application.Web.Pages.Organizations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="OrganizationChanges"/> class: which fields a
/// version or a roll-back changes, and what each entry of the timeline offers.
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
    /// Unit test to verify that an active organization's timeline offers a roll-back to an earlier version whose
    /// values differ from now, and says so of one whose values do not.
    /// </summary>
    [TestMethod]
    public void Entries_ActiveOrganization_OffersRollBackWhereValuesDiffer()
    {
        // Arrange (Given)
        OrganizationVersionResponse[] versions = CreateHistory();

        // Act (When)
        TimelineEntry[] entries = OrganizationChanges.Entries(CreateOrganization(archived: false), versions);

        // Assert (Then)
        CollectionAssert.AreEqual(
            new[] { 0, 0, 1 },
            entries.Select(static entry => entry.RollBack.Length).ToArray(),
            "Only version 1, named Placeholder Freight, should offer a roll-back, of the name alone.");
        Assert.AreEqual(
            "Version 2 holds the same values as now.",
            entries[1].Note,
            "Version 2 should say why it offers none.");
    }

    /// <summary>
    /// Unit test to verify that an archived organization's timeline offers no roll-back and says nothing in its place,
    /// since the header already says why.
    /// </summary>
    [TestMethod]
    public void Entries_ArchivedOrganization_OffersNoRollBack()
    {
        // Arrange (Given)
        OrganizationVersionResponse[] versions = CreateHistory();

        // Act (When)
        TimelineEntry[] entries = OrganizationChanges.Entries(CreateOrganization(archived: true), versions);

        // Assert (Then)
        Assert.IsTrue(
            entries.All(static entry => entry.RollBack.Length == 0 && entry.Note is null),
            "No entry should offer a roll-back, or say anything in its place.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates Placeholder Logistics at version 3, with invented values.
    /// </summary>
    /// <param name="archived">Whether it is archived.</param>
    /// <returns>The organization.</returns>
    private static OrganizationSingleResponse CreateOrganization(bool archived)
    {
        RecordChange change = new RecordChange
        {
            On = new DateTime(2026, 11, 8, 9, 30, 0, DateTimeKind.Utc),
            By = new UserReference { Id = new UserId(10001), Name = new DisplayName("Demo User") },
        };
        return new OrganizationSingleResponse
        {
            Id = new OrganizationId(10003),
            Name = new OrganizationName("Placeholder Logistics"),
            Industry = Industry.Shipping,
            Contacts = new RecordCount(18),
            Version = new VersionNumber(3),
            Created = change,
            LastModified = change,
            Archived = archived ? change : null,
        };
    }

    /// <summary>
    /// Creates three versions, newest first: two that leave the values as Placeholder Logistics, and the first, as
    /// Placeholder Freight.
    /// </summary>
    /// <returns>The versions.</returns>
    private static OrganizationVersionResponse[] CreateHistory()
    {
        return
        [
            CreateVersion(3, "Placeholder Logistics"),
            CreateVersion(2, "Placeholder Logistics"),
            CreateVersion(1, "Placeholder Freight"),
        ];
    }

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
