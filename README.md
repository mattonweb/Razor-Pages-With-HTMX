# Razor Pages With HTMX

A prototype ASP.NET Core Razor Pages application whose create, read, update, delete and list operations are made
interactive with [HTMX](https://htmx.org/). Built as the worked example for a presentation on enhancing Razor Pages
with HTMX, and kept small enough to read on a screen.

- `src/HTMX Pages Solution.slnx` — the solution: the Razor Pages application in `src/HTMX.Application.Web/`, the
  customer database and everything that reads and writes it in `src/HTMX.Adapter.Customers/`, the unit tests in
  `src/HTMX.Test.Unit/`, and the integration tests, which run the whole application in memory, in
  `src/HTMX.Test.Integration/`.
- `CLAUDE.md` — the working agreement for AI coding sessions in this repository.
- `src/.editorconfig` — the house C# style, a verbatim copy of the one in [Signalbox](https://github.com/mattonweb/Signalbox).

## Running it

```bash
dotnet run --project src/HTMX.Application.Web --launch-profile http
```

Then open <http://localhost:5049>. The tests run with `dotnet test "src/HTMX Pages Solution.slnx"`.

## Pages

- **`/organizations`** — the list: filter, status, sort and key-based paging, each view its own URL, swapped in place
  by HTMX and working without it. Each row's menu archives or restores its organization at once, with a toast whose
  **Undo** does the opposite. The row's form posts to the list's own address with the handler and the organization
  added (`?organization=10216&handler=Archive`), so the answer is the same view of the list: with HTMX the list's
  card, its counts included, and without it a redirect to the same view. In the Active view an archived row leaves
  the list and the keyboard goes to the row now in its place, or to the empty list's heading; Undo puts the row back
  and the keyboard on it. The toast stays while the list pages or filters, so every answer names the view it drew in
  an `HX-Replace-Url` header, and an Undo pressed after **Next** shows the page the row came back on, with the
  address to match.
- **`/organizations/create`** — a new organization: the same fields, rules and answers as the edit form below. It is
  saved at version 1, with a Created version in its history, at the next identifier (11001 on a fresh start), and
  the save goes to its page. A name another organization has, however it is capitalized, is refused with 422.
- **`/organizations/{id}`** — one organization: its name, status and version, who changed it last, and every value it
  holds. It is read in one query that joins the users who created, last changed and archived it. An identifier that
  names no organization gets the same page saying so, with the status 404 Not Found. **Archive** and **Restore** post
  to the page's named handlers (`?handler=Archive`, `?handler=Restore`), run at once, and say so in a toast whose
  **Undo** posts the opposite; each is a version in the organization's history. With HTMX one answer carries both the
  page's content and the toast, in an `<hx-partial>`. Without HTMX the post is redirected back to the page, and the toast
  waits in TempData for it. **Delete permanently**, beside Restore on an archived organization's page, works as a
  user's does: a confirm dialog with its own form, the version it shows, and the list afterwards with a toast. Only an
  archived organization that no contact belongs to can be deleted, so Mockup Media's button opens a dialog that says
  how many contacts keep it, with a link to its Contacts tab. A contact's history that names a deleted organization
  keeps its identifier and shows it as a deleted organization.
- **`/organizations/{id}/contacts`** — the Contacts tab: the people who belong to the organization, in name order,
  archived ones muted among the rest, ten to a page, paged by key as the list is (`?after=10017`). Each row's menu
  archives or restores its contact at once, with a toast whose Undo does the opposite. The row's form posts to the
  list's own address with the handler and the contact added (`?after=10017&contact=10018&handler=ArchiveContact`), so
  the answer is the same page of the list: with HTMX the tab's whole content, header included, with the keyboard back
  on that row's menu button; without it, a redirect to the same page. The answer names that page's address in
  `HX-Replace-Url`, so an Undo pressed after **Next** shows the page the contact came back on, with the address to
  match. An archived contact's menu ends with **Delete permanently**, which asks first in a confirm dialog that posts
  the version the row shows. The list is read again from the same cursor, so the rows after the deleted one move up,
  the keyboard goes to the row now in its place, and the toast has no Undo; a delete that leaves no row on the page
  shows the first page. Previous and Next redraw the whole tab too: the header's Archive and Restore post to the
  page's address, and a delete changes the count on the Contacts tab's label. An archived organization's
  contacts can be seen but not changed until it is restored, and offers no **New contact**. Each name links to the
  contact's own page.
- **`/organizations/{id}/history`** — the History tab: every version, newest first, in a timeline, each with the fields
  it changed. An earlier version whose values differ from now offers **Roll back**, previewed in a dialog with the
  change from now; rolling back saves those values as a new version, under the same rules as an edit, and its toast's
  Undo rolls back again. The page's tabs share one base page model, so Archive and Restore work on any of them and
  answer with the tab they were pressed on.
- **`/contacts/create`** — a new contact: the edit form's fields, organization field and answers, saved at version 1
  with a Created version, at the next identifier (11501 on a fresh start); the save goes to their page. The Contacts
  tab's **New contact** opens it as `/contacts/create?organization=10003`, with that organization chosen and the
  breadcrumbs and Cancel leading back to its tab; an archived organization is not chosen. The organization field's
  handlers are the edit form's, on a base page model both share; on this page their addresses name no contact.
- **`/contacts/{id}`** — one contact: their name, status and version, who changed them last, the organization they
  belong to, as a link to it, and every value they hold, read in one query that joins the organization and the users.
  The breadcrumbs lead back through the organization's Contacts tab. **Archive** and **Restore** work as an
  organization's do, with Undo, and **Edit** goes to the edit form. While the organization is archived the contact can
  be seen but not changed, and the page says why. An identifier that names no contact gets the page saying so, with
  404. **Delete permanently**, beside Restore on an archived contact's page, works as a user's does, with the same
  confirm dialog, and goes to the organization's Contacts tab, which shows the toast. Nothing but their own history
  names a contact, so any archived contact can be deleted while their organization is active, as Darcy Dataset can.
- **`/contacts/{id}/edit`** — the contact's edit form: name, organization, job title, email, phone numbers and notes,
  saved as the next version, with the same answers as an organization's edit form (422, 409 with the saved values beside
  the edited ones, and a 204 with `HX-Location` after a save). The organization starts as a select of every active
  organization, which works without HTMX. As the page loads, HTMX swaps it (`hx-trigger="load"`) for the brand's
  relation field, whose **Change** opens a picker that filters the organizations as you type, ten at a time; a refused
  save that HTMX sent draws the relation field straight away. An organization archived while the form was open is
  refused with 422, under its field. A contact has up to five telephone numbers, a row each in the form: **Add a phone
  number** and each row's **Remove** post the form to their own handlers (`?handler=AddPhoneNumber`), which save nothing
  and draw the field again, alone for HTMX and as the whole page without it, with the keyboard in the new row or in the
  row that took the removed one's place. Every row's field is named `Input.PhoneNumbers`, so the rows bind to one list,
  which the contact keeps in order as a JSON array in one column. Hadley Handout has two numbers.
- **`/contacts/{id}/history`** — the contact's History tab, in the same timeline as an organization's. A roll-back saves
  an earlier version's values, the organization among them: rolling Ainsley Archetype back to version 2 moves them back
  to Fictional Freight. A version whose organization has since been archived or deleted can't be rolled back to, and
  its entry says why. Both kinds of record share `RecordPageModel`, which holds the identifier, the toast and the two
  answers an action gives, and the `_Timeline` partial.
- **`/organizations/{id}/edit`** — the edit form: name, industry and website. A save becomes the organization's next
  version, recorded in its history. A form that fails validation comes back with 422 and its messages: the whole page
  without HTMX, and with HTMX only the page's content, swapped in place, with the cursor on the first field to fix. A
  save that succeeds goes to the organization's page: a redirect without HTMX, and with HTMX a 204 with `HX-Location`,
  because `fetch` would follow a redirect itself and HTMX would swap the whole of the next page into the form. An
  archived organization can't be edited, so its page offers no Edit and its edit page says why.
- **`/users`** — the people who use the application, in the side nav under Organizations: the same list as
  `/organizations`, filtered by name, username or email address, with Active, Archived and All, headers that sort by
  name, username, email or last change, key-based paging and Rows per page. Both lists read and write their query
  string through `ListQuery`, so their URLs take the same names. Each name links to the user's own page.
- **`/users/create`** — a new user, from **New user** on the list: the edit form's fields, rules and answers, saved at
  version 1 with a Created version, at the next identifier (10050 on a fresh start), and the save goes to their page.
  A username or an email address another user has, archived users included, however it is capitalized, is refused
  with 422 under its own field.
- **`/users/{id}`** — one user: their name, status and version, who changed them last, their username and email
  address, read in one query that joins the Users table to itself for who created, last changed and archived them.
  **Archive** and **Restore** work as an organization's do, with Undo, except that nobody may archive themselves:
  Demo User's own page offers no Archive and says why, and the service refuses one sent anyway; it still offers
  **Edit**, since anyone may change their own values. Wherever a page names who created, changed or archived a
  record, the name links here, muted and marked Archived once that user is; a timeline entry names them as text,
  because its summary line is the button that opens it. An identifier that names no user gets the page saying so,
  with 404.

  **Delete permanently**, beside Restore on an archived user's page, is the one action that cannot be undone, so it
  asks first, in the brand's confirm dialog: a `<dialog>` the button opens with `commandfor`, needing no script, that
  holds its own form. The form posts to the tab's Delete handler with the version it shows, with HTMX or without it.
  A delete goes to the users list, by `HX-Location` or a redirect, and the list shows the toast it left in TempData,
  with no Undo; the user's history goes with them. Only an archived user can be deleted, and only one whom no version
  of any record names as who changed or archived it: Casey Mockup can be, but for Sam Sample the same button opens a
  dialog that says why not, with no way to force it. A delete from an out-of-date dialog is refused in a toast.
- **`/users/{id}/history`** — the user's History tab, in the same timeline. Rolling Pat Example back to version 1
  brings back the misspelled name and the old email address. A roll-back to a username or email address another user
  has taken since, however it is capitalized, is refused, and its toast names the one taken.
- **`/users/{id}/edit`** — the user's edit form: name, username and email address, saved as the next version, with
  the same answers as an organization's edit form (422, 409 with the saved values beside the edited ones, and a 204
  with `HX-Location` after a save). A username takes letters, digits, dots, hyphens and underscores. A username or an
  email address another user has, however it is capitalized, is refused with 422 under its own field. An archived user
  can't be edited, so their page offers no Edit and their edit page says why.
- **`/components`** — the Components section, in the side nav: pages that each show one pattern on its own, apart
  from the records, with HTMX and without it.
- **`/components/file-field`** — a form that sends a title and a file, a PNG or JPEG image or a PDF document of 1 MB
  or less, and says what it received in a toast; it keeps nothing. The form says `enctype="multipart/form-data"`, so
  HTMX sends it as it is. The handler tells the file's kind by its first bytes, not its name. A form refused for its
  title keeps the chosen file: with HTMX the answer's file input says `hx-preserve`, and HTMX keeps the one on the
  page; without HTMX the page asks for the file again. The page caps a request at 10 MB: a larger one is refused before
  it is read, and the browser sees the connection lost.
- **`/components/long-job`** — a pretend job that takes ten seconds, a step a second, counted from when it started.
  **Start the job** goes to the job's own address, `?job=10001`: by a redirect without HTMX, and with HTMX by the job's
  card and `HX-Push-Url`. While the job runs, the card polls that address every second (`hx-trigger="every 1s"`) and
  shows the brand's busy bar; the card for a finished job is drawn without the trigger, which stops the polling, and
  comes with a toast. Without HTMX the card's **Check again** link loads the page. The application keeps the newest
  hundred jobs.
- **`/components/load-more`** — the active organizations by name, ten at first, and **Load more** for the next ten,
  up to a hundred. The address says how many rows show, `?shown=20`. Without HTMX, Load more is a link to that
  address, at the first new row (`#more-11`). With HTMX it asks for the ten after the last row, by key, adds them to
  the table (`hx-swap="beforeend"`), draws the footer again in the same answer through an `<hx-partial>`, names the new
  address in `HX-Replace-Url`, and puts the keyboard on the first new row; `hx-sync="this:drop"` drops a second press
  while the first is on its way.
- **`/components/reload-on-event`** — a form that writes a note and a list of the newest ten, on one board shared by
  everyone who opens the page. With HTMX the form's answer is the form, empty again, and `HX-Trigger: notes-changed`;
  the list, which listens with `hx-trigger="notes-changed from:document"`, asks for itself again at the page's own
  address and moves no keyboard. The form knows nothing of the list. A refused note raises nothing. Without HTMX the
  form posts and the redirect back draws the list as it is then.
- **`/components/order-lines`** — an order of up to five lines, each a product and a quantity of 1 to 99, which says
  what it received in a toast; it keeps nothing. A line's fields post with its position in their names,
  `Input.Lines[1].Quantity`. **Add a line** and each line's **Remove** post the form to handlers of their own, which
  clear ModelState and draw the lines again, numbered afresh: the lines alone with HTMX, the page without it. The new
  line, or the line in the removed one's place, takes the keyboard. A refused order answers 422 with each wrong field
  marked and its message under the lines, with its line's number.
- **`/components/stock-table`** — four invented items and how many of each are on hand, on one shelf shared by
  everyone, each row saving its own count from 0 to 999. A form can't hold a table row's cells, so each row's form
  sits in its last cell around **Save**, and the count's field names it with the `form` attribute; the browser and
  HTMX both post the field with that form, antiforgery token included. With HTMX the answer is the row alone and a
  toast; without it, a redirect back with the toast. A refused count answers 422 with only its row marked.
- **`/components/seen-notices`** — sixteen invented notices on one board shared by everyone, each marking itself
  seen as it scrolls into view (`hx-trigger="revealed"`), which answers 204; its New chip goes the next time the
  list is drawn. That request has no form around it, so the list sends the antiforgery token in the
  `RequestVerificationToken` header through `hx-headers:inherited`. Without HTMX nothing marks itself, and **Mark all
  as seen** and **Mark all as new** are forms that work either way.
- **`/components/sign-in-expiry`** — a page only a signed-in visitor may see, with a card whose **Check again** asks
  the server for the time with `hx-get`. The visitor signs in at `/components/visitor-sign-in` with one button, for
  thirty seconds that do not slide, through a cookie scheme of its own beside the Demo sign-in. Once the sign-in has
  expired, the browser on its own is redirected to the sign-in page, and HTMX gets 204 and `HX-Redirect` instead, so
  that it loads the sign-in page with a reload rather than drawing it inside the card; either way the sign-in comes
  back to the page, and only to a page of this site.

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
  once it is archived, although Ainsley's history names it, and Darcy Dataset (10004) is archived and can be deleted.

## Versions and choices

- **.NET 10** (SDK 10.0.402) and **C# 14**.
- **Persistence: Entity Framework Core 10.0.14 over SQLite, in memory.** The database, named `Customers` and owned by
  `HTMX.Adapter.Customers`, is created when the application starts and filled from seed data written in C#; it
  disappears when the application stops. Nothing is written to
  disk, so a clone needs no setup, and every run starts from the same known data. Restarting the application resets
  the demo.
- **Tests:** MSTest 4.5.2. The integration tests start the whole application in memory with `WebApplicationFactory`
  (Microsoft.AspNetCore.Mvc.Testing 10.0.13, the version of the ASP.NET Core it runs on) and send requests as the
  browser and HTMX would: each kind of request's answer, the antiforgery check, the Content Security Policy, and what
  an archive and a create answer. Each test's application has a database of its own, named through the
  `Customers:DatabaseName` setting that `Program` reads; nothing sets it when the application runs, so its database
  is `Customers`.
- **Look:** the HTMLX Pages brand. Its stylesheets and icon sprite were copied from
  `.claude/skills/brand-htmlx-pages/assets/` into `wwwroot`, and the pages use its components' markup. The home page's
  robot, `wwwroot/images/robot.svg`, is "Artificial intelligence technology concept" from
  [FreeSVG](https://freesvg.org/artificial-intelligence-technology-concept), CC0 (public domain), with the blue square
  behind it removed.
- **HTMX 4.0.1**, pinned: `wwwroot/js/htmx.min.js`, downloaded from <https://unpkg.com/htmx.org@4.0.1/dist/htmx.min.js>.
  Its SHA-256 is `a0abb04f2f45a1f89d1d4844023245537e5e6014c87b144553e0607b4cc32130`, the integrity unpkg publishes for
  that file, so the copy here is the one released; `Get-FileHash` checks it again. Every page still works without
  HTMX, as plain Razor Pages of links and forms that GET. With it, a list's links and
  forms swap only the list's card: HTMX 4 sends `HX-Request-Type: partial` when it will swap part of the page, and the
  page handler then returns the card's partial view instead of the whole page. Previous, Next and Rows per page are the
  exception: they take the page's `<main>` from the whole page (`hx-select="#main"`), and a page that HTMX asked for
  marks its `<h1>` `autofocus`, so after a page change the keyboard starts on the heading, as it would after a page
  loaded without HTMX, instead of on nothing at the bottom of the grid, and the user can tell the change happened. A
  record's page does the same when HTMX loads it, as after a save or for another page of its Contacts tab. Two HTMX 4
  rules shape the markup: an attribute reaches an element's descendants only when written with `:inherited`
  (`hx-boost:inherited="true"`), and the scripts load in `<head>`, because the back button swaps in a whole new body.
- **Every link is boosted.** The layout's `<body>` says `hx-boost:inherited="true"`, so every link to another page of
  the application, and every form without an `hx-post` of its own, loads the page in place: HTMX fetches it, swaps its
  body in, takes its title and pushes its address. Every page's heading takes the keyboard when HTMX loaded the page,
  Back and Forward included, the home page and the not-found pages too. HTMX 4.0.1 sends Back's request without
  `HX-Request`, so `HtmxHeaders.IsHtmxRequest` also reads `HX-History-Restore-Request`. A link to another site, one
  that opens a new tab, `mailto:` and the skip link work as they always did.
- **Paging is key-based** (also called keyset or seek paging), the pattern that stays fast however deep a list goes.
  Next and Previous name the organization the page starts after or ends before, `?after=10459` or `?before=10001`,
  never a page number. The database seeks to that organization through an index on the sorted column and the
  identifier, where an offset would read and throw away every row before the page. A page is read one row longer than
  it shows, which says whether there is a next page without counting. A cursor doesn't know where in the list its page
  starts, so the pager shows the rows on the page and the total ("Showing 10 of 946"). A cursor whose organization is
  gone starts again at the first page. Sorting by Contacts pages the same way but cannot use an index, because the
  count is worked out for each organization. With 1,000 rows in memory the difference cannot be measured; the demo
  pages this way because it is the pattern to copy. An organization's contacts page the same way, by name, through
  one index on the organization, the name and the identifier.
- **Every URL is lower case** (`/organizations`), and the list's query string uses short names: `filter`, `status`,
  `sort`, `dir`, `size`, `after` and `before`.
- **A save never overwrites a newer one** (optimistic concurrency). The edit form carries the version it was filled
  from, and the save names it: `UPDATE "Organizations" … WHERE "Id" = @id AND "Version" = @version`, then the new
  version's row, in one transaction. If another save got there first, from someone else or from the same person in
  another tab, nothing is written and the form comes back with 409, the values being edited, and the saved value under
  each field that differs; saving again then replaces the newer version on purpose.
- **A Content Security Policy on every answer**, set in `Security/ContentSecurityPolicy.cs`: scripts, stylesheets and
  images only from the application's own files, requests and form posts only to the application, no frames, and
  nothing inline or compiled from a string. HTMX needs nothing relaxed for it, because no page uses `hx-on`, a trigger
  filter or a `js:` value. In Development the policy also lets hot reload's WebSocket reach `dotnet watch` or Visual
  Studio on another port of `localhost`.
- **Who is signed in.** There is no sign-in page: `DemoAuthenticationHandler` signs every request in as Demo User
  (10001), and a page reads who made a change from its `User`, as it would behind a real sign-in.

## License

MIT, see `LICENSE`.
