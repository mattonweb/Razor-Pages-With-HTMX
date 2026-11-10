namespace HTMX.Test.Unit.Application.Web.Pages.Organizations;

using System;
using System.Linq;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Pages;
using HTMX.Application.Web.Pages.Organizations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="OrganizationChanges"/> class: the values a version
/// holds, in the Details tab's order, and what each entry of the timeline offers while the organization is active and
/// once it is archived.
/// </summary>
[TestClass]
public sealed class OrganizationChangesTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a version is described by name, industry and website, in the Details tab's order, with
    /// a missing website as no value, and keeps whether it left the organization archived.
    /// </summary>
    [TestMethod]
    public void Describe_ArchivedVersionWithoutWebsite_GivesItsValuesInOrder()
    {
        // Arrange (Given)
        OrganizationVersionResponse version = CreateVersion(2, "Placeholder Logistics", website: null, archived: true);

        // Act (When)
        HistoryVersion described = OrganizationChanges.Describe(version);

        // Assert (Then)
        CollectionAssert.AreEqual(
            new[] { "Name", "Industry", "Website" },
            described.Values.Select(static value => value.Field).ToArray(),
            "The fields should be the Details tab's, in its order.");
        CollectionAssert.AreEqual(
            new[] { "Placeholder Logistics", "Shipping", null },
            described.Values.Select(static value => value.Value).ToArray(),
            "Each field should hold the version's value as the page shows it, and a missing website none.");
        Assert.AreEqual(
            (new VersionNumber(2), version.ArchivedOn),
            (described.Version, described.ArchivedOn),
            "The description should keep the version's number, and when it left the organization archived.");
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
