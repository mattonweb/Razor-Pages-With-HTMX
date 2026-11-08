# Razor Pages With HTMX

A prototype ASP.NET Core Razor Pages application whose create, read, update, delete and list operations are made
interactive with [HTMX](https://htmx.org/). Built as the worked example for a presentation on enhancing Razor Pages
with HTMX, and kept small enough to read on a screen.

- `src/HTMX Pages Solution.slnx` — the solution: the Razor Pages application in `src/HTMX.Application.Web/`, the
  database and everything that reads and writes it in `src/HTMX.Adapter.Contacts/`, and the unit tests in
  `src/HTMX.Test.Unit/`.
- `CLAUDE.md` — the working agreement for AI coding sessions in this repository.
- `src/.editorconfig` — the house C# style, a verbatim copy of the one in [Signalbox](https://github.com/mattonweb/Signalbox).

## Running it

```bash
dotnet run --project src/HTMX.Application.Web --launch-profile http
```

Then open <http://localhost:5049>. The tests run with `dotnet test "src/HTMX Pages Solution.slnx"`.

## Sample data

The database starts with 49 invented users, 42 active and 7 archived, the counts the brand's list specimen shows, so
the list runs to five pages of ten. They are written out in `src/HTMX.Adapter.Contacts/Data/Seed/` as histories, one
version per change, and each user's current row is built from their latest version.

- **Identifiers run from 10001.** Every identifier is five digits wide, a comfortable target when it is a link. A user
  created in the application carries on from 10050, and a deleted user's identifier is never given out again.
- **Dates are always recent.** Every date is written as a time before the application starts, such as
  `startedOn.AddDays(-236)`, so the demo looks the same whenever it runs: Pat Example was edited 13 minutes ago, and
  the oldest user was created about eight months ago.
- **Some users have a part to play.** Pat Example (10007) carries the five versions of the brand's history specimen;
  Sam Sample (10009) archived Pat and was archived, so cannot be deleted; Casey Mockup (10002) is archived and can.

## Versions and choices

- **.NET 10** (SDK 10.0.402) and **C# 14**.
- **Persistence: Entity Framework Core 10.0.14 over SQLite, in memory.** The database, named `Contacts` and owned by
  `HTMX.Adapter.Contacts`, is created when the application starts and filled from seed data written in C#; it
  disappears when the application stops. Nothing is written to
  disk, so a clone needs no setup, and every run starts from the same known data. Restarting the application resets
  the demo.
- **Tests:** MSTest 4.5.2.
- **HTMX:** not added yet; its exact version is pinned and recorded here when it is.

## License

MIT, see `LICENSE`.
