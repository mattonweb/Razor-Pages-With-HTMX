namespace HTMX.Test.Unit.Application.Web.Pages.Components;

using System;
using System.Linq;
using HTMX.Application.Web.Pages.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="NoteBoard"/> class: the Reload on event page's
/// shared notes, which keeps the newest ten.
/// </summary>
[TestClass]
public sealed class NoteBoardTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that notes are numbered from 10001 in the order they are written, trimmed, and read newest
    /// first.
    /// </summary>
    [TestMethod]
    public void Write_TwoNotes_ReadsThemNewestFirst()
    {
        // Arrange (Given)
        NoteBoard board = new NoteBoard();

        // Act (When)
        board.Write("  First note.  ");
        board.Write("Second note.");
        Note[] notes = board.Newest();

        // Assert (Then)
        CollectionAssert.AreEqual(
            new[] { "10002 Second note.", "10001 First note." },
            notes.Select(static note => $"{note.Number} {note.Text}").ToArray(),
            "Notes should be numbered in order, trimmed, and read newest first.");
    }

    /// <summary>
    /// Unit test to verify that the board keeps the newest ten notes, letting the oldest go.
    /// </summary>
    [TestMethod]
    public void Write_ElevenNotes_KeepsTheNewestTen()
    {
        // Arrange (Given)
        NoteBoard board = new NoteBoard();

        // Act (When)
        for (int note = 1; note <= 11; note++)
        {
            board.Write($"Note {note}.");
        }

        Note[] notes = board.Newest();

        // Assert (Then)
        Assert.AreEqual(
            (10, 10011L, 10002L),
            (notes.Length, notes[0].Number, notes[notes.Length - 1].Number),
            "The board should keep notes 10002 to 10011.");
    }

    /// <summary>
    /// Unit test to verify that a note of nothing but white space is refused.
    /// </summary>
    [TestMethod]
    public void Write_Blank_Throws()
    {
        // Arrange (Given)
        NoteBoard board = new NoteBoard();

        // Act (When)
        ArgumentException thrown = Assert.ThrowsExactly<ArgumentException>(
            () => board.Write("   "),
            "A blank note should be refused.");

        // Assert (Then)
        Assert.AreEqual(
            "text",
            thrown.ParamName,
            "The exception should name the text.");
    }

    #endregion Public Methods
}
