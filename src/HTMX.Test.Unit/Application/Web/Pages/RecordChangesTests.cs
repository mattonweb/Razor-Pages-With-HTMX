namespace HTMX.Test.Unit.Application.Web.Pages;

using System;
using System.Linq;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Pages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="RecordChanges"/> class, for any kind of record:
/// which fields a version or a roll-back changes, and what each entry of the timeline offers.
/// </summary>
[TestClass]
public sealed class RecordChangesTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that an edit's changes are the fields whose values differ, in the order the version lists
    /// them, with a value removed as no value after.
    /// </summary>
    [TestMethod]
    public void Between_EditedNameAndWebsite_GivesThoseFieldsInOrder()
    {
        // Arrange (Given)
        HistoryVersion before = CreateVersion(1, "Placeholder Freight", "https://placeholder.example");
        HistoryVersion after = CreateVersion(2, "Placeholder Logistics", website: null);

        // Act (When)
        FieldChange[] changes = RecordChanges.Between(before, after);

        // Assert (Then)
        CollectionAssert.AreEqual(
            new[] { "Name", "Website" },
            changes.Select(static change => change.Field).ToArray(),
            "Only the name and the website changed, in that order.");
        Assert.AreEqual(
            ("https://placeholder.example", null),
            (changes[1].Before, changes[1].After),
            "A website removed should have its value before and none after.");
    }

    /// <summary>
    /// Unit test to verify that archiving changes only the status, and that a roll-back never does.
    /// </summary>
    [TestMethod]
    public void Between_Archived_GivesOnlyTheStatus()
    {
        // Arrange (Given)
        HistoryVersion active = CreateVersion(1, "Placeholder Logistics");
        HistoryVersion archived = CreateVersion(2, "Placeholder Logistics", archived: true);

        // Act (When)
        FieldChange[] archiving = RecordChanges.Between(active, archived);
        FieldChange[] rollingBack = RecordChanges.RollBack(archived, active);

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
    /// Unit test to verify that a record that can be rolled back is offered a roll-back to an earlier version whose
    /// values differ from now, told of one whose values do not, and shown what each version changed.
    /// </summary>
    [TestMethod]
    public void Entries_Active_OffersRollBackWhereValuesDiffer()
    {
        // Arrange (Given)
        HistoryVersion[] versions = CreateHistory(unavailable: null);

        // Act (When)
        TimelineEntry[] entries = RecordChanges.Entries(versions, active: true);

        // Assert (Then)
        CollectionAssert.AreEqual(
            new[] { 0, 0, 1 },
            entries.Select(static entry => entry.RollBack.Length).ToArray(),
            "Only version 1, named Placeholder Freight, should offer a roll-back, of the name alone.");
        Assert.AreEqual(
            ("Version 2 holds the same values as now.", "Name", 0),
            (entries[1].Note, entries[1].Changes.Single().Field, entries[2].Changes.Length),
            "Version 2 should say why it offers none and show that it changed the name; version 1 changed nothing.");
    }

    /// <summary>
    /// Unit test to verify that a record that can't be rolled back now is offered no roll-back and told nothing in
    /// its place, not even why one version is out of reach, since the page's header already says why.
    /// </summary>
    [TestMethod]
    public void Entries_NotActive_OffersNoRollBack()
    {
        // Arrange (Given)
        HistoryVersion[] versions = CreateHistory(unavailable: "Version 1 is out of reach.");

        // Act (When)
        TimelineEntry[] entries = RecordChanges.Entries(versions, active: false);

        // Assert (Then)
        Assert.IsTrue(
            entries.All(static entry => entry.RollBack.Length == 0 && entry.Note is null),
            "No entry should offer a roll-back, or say anything in its place.");
    }

    /// <summary>
    /// Unit test to verify that a version the record can't go back to, whatever its values, offers no roll-back and
    /// says why in its place.
    /// </summary>
    [TestMethod]
    public void Entries_RollBackUnavailable_SaysWhyInItsPlace()
    {
        // Arrange (Given)
        HistoryVersion[] versions = CreateHistory(unavailable: "Version 1 is out of reach.");

        // Act (When)
        TimelineEntry[] entries = RecordChanges.Entries(versions, active: true);

        // Assert (Then)
        Assert.AreEqual(
            (0, "Version 1 is out of reach."),
            (entries[2].RollBack.Length, entries[2].Note),
            "Version 1 should offer no roll-back and say why.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates three versions, newest first: two that leave the values as Placeholder Logistics, and the first, as
    /// Placeholder Freight.
    /// </summary>
    /// <param name="unavailable">Why the record can't go back to the first version, or <see langword="null"/> when
    /// nothing but its values decides.</param>
    /// <returns>The versions.</returns>
    private static HistoryVersion[] CreateHistory(string? unavailable)
    {
        return
        [
            CreateVersion(3, "Placeholder Logistics"),
            CreateVersion(2, "Placeholder Logistics"),
            CreateVersion(1, "Placeholder Freight", unavailable: unavailable),
        ];
    }

    /// <summary>
    /// Creates a version of an invented record with two fields, Name and Website, that Demo User made.
    /// </summary>
    /// <param name="number">The version's number.</param>
    /// <param name="name">The name it left.</param>
    /// <param name="website">The website it left, if any.</param>
    /// <param name="archived">Whether it left the record archived.</param>
    /// <param name="unavailable">Why the record can't go back to it, if anything but its values decides.</param>
    /// <returns>The version.</returns>
    private static HistoryVersion CreateVersion(
        int number,
        string name,
        string? website = null,
        bool archived = false,
        string? unavailable = null)
    {
        DateTime on = new DateTime(2026, 11, 8, 9, 30, 0, DateTimeKind.Utc);
        return new HistoryVersion
        {
            Version = new VersionNumber(number),
            Change = ChangeKind.Edited,
            Changed = new RecordChange
            {
                On = on,
                By = new UserReference { Id = new UserId(10001), Name = new DisplayName("Demo User") },
            },
            ArchivedOn = archived ? on : null,
            Values =
            [
                new FieldValue { Field = "Name", Value = name },
                new FieldValue { Field = "Website", Value = website },
            ],
            RollBackUnavailable = unavailable,
        };
    }

    #endregion Private Methods
}
