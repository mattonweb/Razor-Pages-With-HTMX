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

## Sample data

The database starts with invented data, built in `src/HTMX.Adapter.Customers/Data/Seed/` as histories, one version
per change, with each current row built from its latest version:

- **49 users**, 42 active and 7 archived, so the list runs to five pages of ten with a last page of nine.
- **1,000 organizations**, 949 active and 51 archived, so paging is tested properly at every page size, the Archived
  filter included. Fifteen are written out because each has a part to play; the other 985 are generated from two word
  lists (Example Freight, Stand-In Software), the same ones on every run.
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

## License

MIT, see `LICENSE`.
