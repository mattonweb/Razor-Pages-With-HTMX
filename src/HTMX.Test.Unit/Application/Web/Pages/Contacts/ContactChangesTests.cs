namespace HTMX.Test.Unit.Application.Web.Pages.Contacts;

using System;
using System.Linq;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Pages;
using HTMX.Application.Web.Pages.Contacts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="ContactChanges"/> class: the values a version
/// holds, the organization among them, and what each entry of a contact's timeline offers, which depends on the
/// organization a version names.
/// </summary>
[TestClass]
public sealed class ContactChangesTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a version is described by the Details tab's fields, in its order, and that an
    /// organization since deleted is named by its identifier.
    /// </summary>
    [TestMethod]
    public void Describe_VersionInADeletedOrganization_NamesItByItsIdentifier()
    {
        // Arrange (Given)
        ContactVersionResponse version = CreateHistory(fictionalFreight: null)[1];

        // Act (When)
        HistoryVersion described = ContactChanges.Describe(version);

        // Assert (Then)
        CollectionAssert.AreEqual(
            new[] { "Name", "Organization", "Job title", "Email", "Phone numbers", "Notes" },
            described.Values.Select(static value => value.Field).ToArray(),
            "The fields should be the Details tab's, in its order.");
        Assert.AreEqual(
            "Deleted organization 10002",
            described.Values[1].Value,
            "A deleted organization should be named by its identifier.");
    }

    /// <summary>
    /// Unit test to verify that an earlier version in another, active organization offers a roll-back that moves the
    /// contact back there, named by the organization's name.
    /// </summary>
    [TestMethod]
    public void Entries_VersionInAnotherActiveOrganization_OffersToMoveThemBack()
    {
        // Arrange (Given)
        ContactVersionResponse[] versions = CreateHistory(CreateFictionalFreight(archived: false));

        // Act (When)
        TimelineEntry[] entries = ContactChanges.Entries(CreateContact(organizationArchived: false), versions);
        FieldChange move = entries[1].RollBack.Single(static change => change.Field == "Organization");

        // Assert (Then)
        Assert.AreEqual(
            ("Placeholder Logistics", "Fictional Freight"),
            (move.Before, move.After),
            "Rolling back to version 1 should move Ainsley Archetype back to Fictional Freight.");
    }

    /// <summary>
    /// Unit test to verify that an earlier version whose organization is archived or deleted offers no roll-back and
    /// says why in its place.
    /// </summary>
    /// <param name="deleted">Whether the organization is deleted rather than archived.</param>
    /// <param name="expected">What the entry says.</param>
    [TestMethod]
    [DataRow(
        false,
        "Fictional Freight is archived, so Ainsley Archetype can't go back to version 1.",
        DisplayName = "Organization archived")]
    [DataRow(
        true,
        "The organization of version 1 no longer exists, so Ainsley Archetype can't go back to it.",
        DisplayName = "Organization deleted")]
    public void Entries_VersionInAnOrganizationThatTakesNoOneIn_SaysWhy(bool deleted, string expected)
    {
        // Arrange (Given)
        OrganizationReference? fictionalFreight = deleted ? null : CreateFictionalFreight(archived: true);
        ContactVersionResponse[] versions = CreateHistory(fictionalFreight);

        // Act (When)
        TimelineEntry[] entries = ContactChanges.Entries(CreateContact(organizationArchived: false), versions);

        // Assert (Then)
        Assert.AreEqual(
            (0, expected),
            (entries[1].RollBack.Length, entries[1].Note),
            "Version 1 should offer no roll-back and say why.");
    }

    /// <summary>
    /// Unit test to verify that a contact whose organization is archived is offered no roll-back at all, since the
    /// header already says why.
    /// </summary>
    [TestMethod]
    public void Entries_OrganizationArchived_OffersNoRollBack()
    {
        // Arrange (Given)
        ContactVersionResponse[] versions = CreateHistory(CreateFictionalFreight(archived: false));

        // Act (When)
        TimelineEntry[] entries = ContactChanges.Entries(CreateContact(organizationArchived: true), versions);

        // Assert (Then)
        Assert.IsTrue(
            entries.All(static entry => entry.RollBack.Length == 0 && entry.Note is null),
            "No entry should offer a roll-back, or say anything in its place.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates Ainsley Archetype at Placeholder Logistics, at version 2, with invented values.
    /// </summary>
    /// <param name="organizationArchived">Whether Placeholder Logistics is archived.</param>
    /// <returns>The contact.</returns>
    private static ContactSingleResponse CreateContact(bool organizationArchived)
    {
        RecordChange change = CreateChange();
        return new ContactSingleResponse
        {
            Id = new ContactId(10001),
            Name = new DisplayName("Ainsley Archetype"),
            Organization = new OrganizationReference
            {
                Id = new OrganizationId(10003),
                Name = new OrganizationName("Placeholder Logistics"),
                ArchivedOn = organizationArchived ? change.On : null,
            },
            Version = new VersionNumber(2),
            Created = change,
            LastModified = change,
        };
    }

    /// <summary>
    /// Creates Fictional Freight as a version names it.
    /// </summary>
    /// <param name="archived">Whether it is archived.</param>
    /// <returns>The organization.</returns>
    private static OrganizationReference CreateFictionalFreight(bool archived)
    {
        return new OrganizationReference
        {
            Id = new OrganizationId(10002),
            Name = new OrganizationName("Fictional Freight"),
            ArchivedOn = archived ? CreateChange().On : null,
        };
    }

    /// <summary>
    /// Creates two versions, newest first: a move to Placeholder Logistics, and the first, at Fictional Freight.
    /// </summary>
    /// <param name="fictionalFreight">Fictional Freight as the first version names it, or <see langword="null"/>
    /// once it is deleted.</param>
    /// <returns>The versions.</returns>
    private static ContactVersionResponse[] CreateHistory(OrganizationReference? fictionalFreight)
    {
        return
        [
            new ContactVersionResponse
            {
                Version = new VersionNumber(2),
                Change = ChangeKind.Edited,
                Changed = CreateChange(),
                Name = new DisplayName("Ainsley Archetype"),
                OrganizationId = new OrganizationId(10003),
                Organization = new OrganizationReference
                {
                    Id = new OrganizationId(10003),
                    Name = new OrganizationName("Placeholder Logistics"),
                },
            },
            new ContactVersionResponse
            {
                Version = new VersionNumber(1),
                Change = ChangeKind.Created,
                Changed = CreateChange(),
                Name = new DisplayName("Ainsley Archetype"),
                OrganizationId = new OrganizationId(10002),
                Organization = fictionalFreight,
            },
        ];
    }

    /// <summary>
    /// Creates a change Demo User made.
    /// </summary>
    /// <returns>The change.</returns>
    private static RecordChange CreateChange()
    {
        return new RecordChange
        {
            On = new DateTime(2026, 11, 8, 9, 30, 0, DateTimeKind.Utc),
            By = new UserReference { Id = new UserId(10001), Name = new DisplayName("Demo User") },
        };
    }

    #endregion Private Methods
}
