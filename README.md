# Razor Pages With HTMX

A prototype ASP.NET Core Razor Pages application whose create, read, update, delete and list operations are made
interactive with [HTMX](https://htmx.org/). Built as the worked example for a presentation on enhancing Razor Pages
with HTMX, and kept small enough to read on a screen.

- `src/HTMX Pages Solution.slnx` — the solution: the Razor Pages application in `src/HTMX.Application.Web/`, the
  customer database and everything that reads and writes it in `src/HTMX.Adapter.Customers/`, and the unit tests in
  `src/HTMX.Test.Unit/`.
- `CLAUDE.md` — the working agreement for AI coding sessions in this repository.
- `src/.editorconfig` — the house C# style, a verbatim copy of the one in [Signalbox](https://github.com/mattonweb/Signalbox).

## Running it

```bash
dotnet run --project src/HTMX.Application.Web --launch-profile http
```

Then open <http://localhost:5049>. The tests run with `dotnet test "src/HTMX Pages Solution.slnx"`.

## Pages

- **`/organizations`** — the list: filter, status, sort and key-based paging, each view its own URL, swapped in place
  by HTMX and working without it.
- **`/organizations/{id}`** — one organization: its name, status and version, who changed it last, and every value it
  holds. It is read in one query that joins the users who created, last changed and archived it. An identifier that
  names no organization gets the same page saying so, with the status 404 Not Found.
- **`/organizations/{id}/edit`** — the edit form: name, industry and website. A save becomes the organization's next
  version, recorded in its history. A form that fails validation comes back with 422 and its messages: the whole page
  without HTMX, and with HTMX only the page's content, swapped in place, with the cursor on the first field to fix. A
  save that succeeds goes to the organization's page: a redirect without HTMX, and with HTMX a 204 with `HX-Location`,
  because `fetch` would follow a redirect itself and HTMX would swap the whole of the next page into the form. An
  archived organization can't be edited, so its page offers no Edit and its edit page says why.

## Sample data

The database starts with invented data, built in `src/HTMX.Adapter.Customers/Data/Seed/` as histories, one version
per change, with each current row built from its latest version:

- **49 users**, 42 active and 7 archived, so the list runs to five pages of ten with a last page of nine.
- **1,000 organizations**, 946 active and 54 archived, so paging is tested properly at every page size, the Archived
  filter included. Fifteen are written out because each has a part to play; the other 985 are generated from two word
  lists (Example Freight, Stand-In Software), the same ones on every run. Every 19th generated organization is
  archived: 19 shares no factor with the 40 first words or the other cycles in the generator, so the archived ones
  take every name, size and industry instead of repeating one pattern.
- **1,500 contacts**, 1,440 active and 60 archived, every one belonging to exactly one organization. Eight are written
  out because each has a part to play; the other 1,492 are generated, none to three for each generated organization
  and 18 in all at Placeholder Logistics, so its own list of contacts pages. Each joined their organization while it
  was active.

The seed is written without logging its SQL, so the console shows only the SQL each request runs.

- **Identifiers run from 10001** for every kind of record. Every identifier is five digits wide, a comfortable target
  when it is a link. New records carry on from the seed's highest (users from 10050, organizations from 11001,
  contacts from 11501), and a deleted record's identifier is never given out again.
- **Dates are always recent.** Every date is written as a time before the application starts, such as
  `startedOn.AddDays(-236)`, so the demo looks the same whenever it runs: Pat Example was edited 13 minutes ago, and
  the oldest user was created about eight months ago.
- **Some records have a part to play.** Pat Example (10007) has the longest history, five versions; Sam Sample
  (10009) archived Pat and was archived, so cannot be deleted; Casey Mockup (10002) is archived and can. Among the
  organizations, Placeholder Logistics (10003) has a history of two versions, Mockup Media (10009) is archived with
  two contacts, so cannot be deleted, and Draft Distribution (10013) is archived with none, so can. Among the contacts,
  Ainsley Archetype (10001) moved from Fictional Freight to Placeholder Logistics, so Fictional Freight can be deleted
  although Ainsley's history names it, and Darcy Dataset (10004) is archived and can be deleted.

## Versions and choices

- **.NET 10** (SDK 10.0.402) and **C# 14**.
- **Persistence: Entity Framework Core 10.0.14 over SQLite, in memory.** The database, named `Customers` and owned by
  `HTMX.Adapter.Customers`, is created when the application starts and filled from seed data written in C#; it
  disappears when the application stops. Nothing is written to
  disk, so a clone needs no setup, and every run starts from the same known data. Restarting the application resets
  the demo.
- **Tests:** MSTest 4.5.2.
- **Look:** the HTMLX Pages brand. Its stylesheets and icon sprite were copied from
  `.claude/skills/brand-htmlx-pages/assets/` into `wwwroot`, and the pages use its components' markup.
- **HTMX 4.0.1**, pinned: `wwwroot/js/htmx.min.js`, downloaded from <https://unpkg.com/htmx.org@4.0.1/dist/htmx.min.js>.
  Every page still works without it, as plain Razor Pages of links and forms that GET. With it, a list's links and
  forms swap only the list's card: HTMX 4 sends `HX-Request-Type: partial` when it will swap part of the page, and the
  page handler then returns the card's partial view instead of the whole page. Two HTMX 4 rules shape the markup: an
  attribute reaches an element's descendants only when written with `:inherited` (`hx-boost:inherited="true"`), and the
  scripts load in `<head>`, because the back button swaps in a whole new body.
- **Paging is key-based** (also called keyset or seek paging), the pattern that stays fast however deep a list goes.
  Next and Previous name the organization the page starts after or ends before, `?after=10459` or `?before=10001`,
  never a page number. The database seeks to that organization through an index on the sorted column and the
  identifier, where an offset would read and throw away every row before the page. A page is read one row longer than
  it shows, which says whether there is a next page without counting. A cursor doesn't know where in the list its page
  starts, so the pager shows the rows on the page and the total ("Showing 10 of 946"). A cursor whose organization is
  gone starts again at the first page. Sorting by Contacts pages the same way but cannot use an index, because the
  count is worked out for each organization. With 1,000 rows in memory the difference cannot be measured; the demo
  pages this way because it is the pattern to copy.
- **Every URL is lower case** (`/organizations`), and the list's query string uses short names: `filter`, `status`,
  `sort`, `dir`, `size`, `after` and `before`.
- **A save never overwrites a newer one** (optimistic concurrency). The edit form carries the version it was filled
  from, and the save names it: `UPDATE "Organizations" … WHERE "Id" = @id AND "Version" = @version`, then the new
  version's row, in one transaction. If another save got there first, from someone else or from the same person in
  another tab, nothing is written and the form comes back with 409, the values being edited, and the saved value under
  each field that differs; saving again then replaces the newer version on purpose.
- **Who is signed in.** There is no sign-in page: `DemoAuthenticationHandler` signs every request in as Demo User
  (10001), and a page reads who made a change from its `User`, as it would behind a real sign-in.

## License

MIT, see `LICENSE`.
