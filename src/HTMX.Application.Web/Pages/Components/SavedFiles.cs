namespace HTMX.Application.Web.Pages.Components;

using System.Collections.Generic;

/// <summary>
/// The Confirmation page's files: a few invented ones, which a delete removes for good, until Put them all back brings
/// them back. One list for everyone who opens the page.
/// </summary>
public sealed class SavedFiles
{
    // The files the list starts with, and comes back to.
    private static readonly SavedFile[] Seed =
    [
        new SavedFile { Number = 10001, Name = "Placeholder invoice.pdf" },
        new SavedFile { Number = 10002, Name = "Sample contract.pdf" },
        new SavedFile { Number = 10003, Name = "Mock photo.png" },
    ];

    // The files, in the order the list shows them. Every read and write locks it.
    private readonly List<SavedFile> _files = [.. Seed];

    /// <summary>
    /// Reads every file, in the order the list shows them.
    /// </summary>
    /// <returns>The files.</returns>
    public SavedFile[] All()
    {
        lock (this._files)
        {
            return [.. this._files];
        }
    }

    /// <summary>
    /// Finds a file by its number.
    /// </summary>
    /// <param name="number">The file's number.</param>
    /// <returns>The file, or <see langword="null"/> when the list has none by that number.</returns>
    public SavedFile? Find(long number)
    {
        lock (this._files)
        {
            return this._files.Find(file => file.Number == number);
        }
    }

    /// <summary>
    /// Removes a file for good.
    /// </summary>
    /// <param name="number">The file's number.</param>
    /// <returns>The file removed, or <see langword="null"/> when the list had none by that number.</returns>
    public SavedFile? Delete(long number)
    {
        lock (this._files)
        {
            int position = this._files.FindIndex(file => file.Number == number);
            if (position < 0)
            {
                return null;
            }

            SavedFile deleted = this._files[position];
            this._files.RemoveAt(position);
            return deleted;
        }
    }

    /// <summary>
    /// Brings every file back, in its first order, so that the page can be tried again.
    /// </summary>
    public void PutBack()
    {
        lock (this._files)
        {
            this._files.Clear();
            this._files.AddRange(Seed);
        }
    }
}
