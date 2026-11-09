namespace HTMX.Test.Unit.Application.Web.Pages;

using System;
using System.Linq;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Pages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="TimelineText"/> class: the words a timeline entry
/// leads with, for any kind of record.
/// </summary>
[TestClass]
public sealed class TimelineTextTests
{
    #region Public Methods

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
        string summary = TimelineText.Fields(changes);

        // Assert (Then)
        Assert.AreEqual(
            expected,
            summary,
            "The fields should read as a list in a sentence.");
    }

    /// <summary>
    /// Unit test to verify that a roll-back's entry is summed up by the version it went back to, and an archive's by
    /// its title alone.
    /// </summary>
    /// <param name="change">What the version records.</param>
    /// <param name="expected">The title and the summary.</param>
    [TestMethod]
    [DataRow(ChangeKind.RolledBack, "Rolled back to version 2", DisplayName = "A roll-back")]
    [DataRow(ChangeKind.Archived, "Archived ", DisplayName = "An archive")]
    public void Summary_RollBackOrArchive_ReadsAfterTheTitle(ChangeKind change, string expected)
    {
        // Arrange (Given)
        TimelineEntry entry = new TimelineEntry
        {
            Version = new VersionNumber(3),
            Change = change,
            RolledBackTo = change == ChangeKind.RolledBack ? new VersionNumber(2) : null,
            Changed = new RecordChange
            {
                On = new DateTime(2026, 11, 8, 9, 30, 0, DateTimeKind.Utc),
                By = new UserReference { Id = new UserId(10001), Name = new DisplayName("Demo User") },
            },
            Changes = [],
            RollBack = [],
        };

        // Act (When)
        string words = $"{TimelineText.Title(entry.Change)} {TimelineText.Summary(entry)}";

        // Assert (Then)
        Assert.AreEqual(
            expected,
            words,
            "The entry should lead with what happened and, for a roll-back, the version it went back to.");
    }

    #endregion Public Methods
}
