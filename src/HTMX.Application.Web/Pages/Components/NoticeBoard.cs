namespace HTMX.Application.Web.Pages.Components;

using System;

/// <summary>
/// The Seen notices page's board: invented notices, each new until it has been seen, one board for everyone who opens
/// the page.
/// </summary>
public sealed class NoticeBoard
{
    // The notices, in the order the page lists them, all new. Every read and write locks the array.
    private readonly Notice[] _notices =
    [
        new Notice { Number = 10001, Text = "The sample printer on the second floor is out of placeholder toner." },
        new Notice { Number = 10002, Text = "A mock fire drill is planned for Thursday at ten." },
        new Notice { Number = 10003, Text = "The dummy coffee machine has been descaled." },
        new Notice { Number = 10004, Text = "The placeholder parking spaces are repainted on Monday." },
        new Notice { Number = 10005, Text = "The test lift is back in service." },
        new Notice { Number = 10006, Text = "Sample paper deliveries move to Wednesdays." },
        new Notice { Number = 10007, Text = "The pretend canteen serves soup all week." },
        new Notice { Number = 10008, Text = "Lost and found holds one imaginary umbrella." },
        new Notice { Number = 10009, Text = "The demo meeting room has a new screen." },
        new Notice { Number = 10010, Text = "The invented quarterly figures are on the shared board." },
        new Notice { Number = 10011, Text = "The example bike shed has four new racks." },
        new Notice { Number = 10012, Text = "Mock mugs are back in the kitchen cupboard." },
        new Notice { Number = 10013, Text = "The fictional book club meets on the last Friday." },
        new Notice { Number = 10014, Text = "The stand-in plants on the third floor need watering." },
        new Notice { Number = 10015, Text = "The trial standing desks can be booked from Tuesday." },
        new Notice { Number = 10016, Text = "The made-up recycling rota is on the kitchen door." },
    ];

    /// <summary>
    /// Reads every notice, in order.
    /// </summary>
    /// <returns>The notices.</returns>
    public Notice[] All()
    {
        lock (this._notices)
        {
            return [.. this._notices];
        }
    }

    /// <summary>
    /// Marks one notice seen.
    /// </summary>
    /// <param name="number">The notice's number.</param>
    /// <returns><see langword="true"/> when the board has a notice by that number, now seen.</returns>
    public bool MarkSeen(long number)
    {
        lock (this._notices)
        {
            int position = Array.FindIndex(this._notices, notice => notice.Number == number);
            if (position < 0)
            {
                return false;
            }

            this._notices[position] = Marked(this._notices[position], seen: true);
            return true;
        }
    }

    /// <summary>
    /// Marks every notice seen, or every notice new again.
    /// </summary>
    /// <param name="seen">Whether the notices are seen.</param>
    public void MarkAll(bool seen)
    {
        lock (this._notices)
        {
            for (int position = 0; position < this._notices.Length; position++)
            {
                this._notices[position] = Marked(this._notices[position], seen);
            }
        }
    }

    /// <summary>
    /// Marks a notice seen or new.
    /// </summary>
    /// <param name="notice">The notice.</param>
    /// <param name="seen">Whether it is seen.</param>
    /// <returns>The notice, so marked.</returns>
    private static Notice Marked(Notice notice, bool seen)
    {
        return notice with { Seen = seen };
    }
}
