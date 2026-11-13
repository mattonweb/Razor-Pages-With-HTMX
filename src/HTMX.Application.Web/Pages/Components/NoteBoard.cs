namespace HTMX.Application.Web.Pages.Components;

using System;
using System.Collections.Generic;

/// <summary>
/// The Reload on event page's notes: one board for everyone who opens the page, keeping the newest notes only.
/// </summary>
/// <remarks>
/// One instance serves the application, so a note one visitor writes is on the board the next reads, which is why the
/// page's list loads itself again: other people change it too.
/// </remarks>
public sealed class NoteBoard
{
    /// <summary>
    /// How many notes the board keeps; the oldest goes when another is written.
    /// </summary>
    public const int Kept = 10;

    // The first note's number; note numbers are as wide as record identifiers.
    private const long FirstNote = 10001;

    // The notes kept, oldest first. Every read and write locks it.
    private readonly List<Note> _notes = [];

    /// <summary>
    /// Writes a note on the board.
    /// </summary>
    /// <param name="text">The note's text, which must hold something other than white space.</param>
    /// <returns>The note, with its number.</returns>
    public Note Write(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        lock (this._notes)
        {
            long number = this._notes.Count == 0 ? FirstNote : this._notes[this._notes.Count - 1].Number + 1;
            Note note = new Note { Number = number, Text = text.Trim() };
            this._notes.Add(note);
            this.ForgetOldest();
            return note;
        }
    }

    /// <summary>
    /// Reads the notes kept, newest first.
    /// </summary>
    /// <returns>The notes.</returns>
    public Note[] Newest()
    {
        lock (this._notes)
        {
            Note[] notes = [.. this._notes];
            Array.Reverse(notes);
            return notes;
        }
    }

    /// <summary>
    /// Lets go of the oldest note once more than <see cref="Kept"/> are kept. The caller holds the lock.
    /// </summary>
    private void ForgetOldest()
    {
        if (this._notes.Count > Kept)
        {
            this._notes.RemoveAt(0);
        }
    }
}
