---
name: brand-htmlx-pages
description: The HTMLX Pages brand for the Razor Pages and HTMX application in this repository. Use whenever writing, styling or reviewing its markup, CSS, partial views or page layouts. Covers the design tokens (color, type, space, shape) taken from the DevExtreme UI Template Gallery and MudBlazor, light and dark mode, the class naming rule, buttons, icons, the application shell, the list page with bulk selection, the record details page and side panel, forms and dialogs, alerts, toasts and confirm dialogs, and version history with roll-back. Draft — every group is ready for review; the HTMX wiring is not settled yet.
owner: Matt
version: 0.1.0
visibility: public
---

# HTMLX Pages

HTMLX Pages is the look and the markup contract of the CRUDL++ demo in this repository: create, read, update,
delete, list, archive (soft delete), history and roll-back, built with ASP.NET Core Razor Pages and HTMX. The
layout patterns come from the [DevExtreme UI Template Gallery](https://github.com/DevExpress/devextreme-ui-template-gallery)
(MIT), and the component names and color roles from [MudBlazor](https://github.com/MudBlazor/MudBlazor) (MIT).
No code, CSS or artwork is copied from either: the gallery runs on DevExtreme, which has its own commercial license,
and this repository is public. Everything in this skill was written for this repository.

## Status: draft 0.1.0

| Group | Components | State |
|---|---|---|
| Foundations | tokens, type, space, shape, icons, buttons, form-control defaults | Settled for review |
| Shell | app shell, side nav with icon rail, user avatar, page header, breadcrumbs, card with header, menu, dialog | Settled for review |
| List and archive | toolbar, filter box, segmented status filter, grid, chip, pager, empty state, busy bar | Settled for review |
| List and archive, added | checkbox column with select-all and a bulk-action bar; read-only list with no row actions | Ready for review |
| Read | details page title with status chip, tabs, fields, related-record link, side panel | Settled for review |
| Create and update | fields with server validation (text, textarea, number, email, URL, phone, switch, date, single select, choices, relation picker), edit form, create dialog, form action bar (Duplicate, Archive, Cancel, Save) | Ready for review |
| Feedback | alert banner, toast with Undo, confirm dialogs (delete permanently, one or many; a record others depend on; discard unsaved changes) | Ready for review |
| History and roll-back | version badge, version timeline, change diff, roll-back preview | Ready for review |
| HTMX wiring | which requests each component makes and what the handlers return | Proposed below, not settled |

"Settled for review" groups were built and checked with Matt; "Ready for review" groups were built while he was
away and wait for his review. Until a group is settled, do not invent its markup in the application; settle it
here first, with Matt. The bulk selection, read-only list, related-record link, field set, form action bar and the
confirm dialogs were added on 2026-11-07 after comparing the plan with the PocketBase dashboard: they are the base
elements a records screen needs. PocketBase's other features (filter expressions, column chooser, drafts, files,
rich text, live updates) are not part of this brand.

## Files

Everything under `assets/` goes into the application; nothing outside it does.

- `assets/css/tokens.css`: every color, size and font as a custom property. The only place a literal color appears.
- `assets/css/base.css`: element defaults, form controls, the icon class and buttons.
- `assets/css/shell.css`: application shell, user avatar, page header, breadcrumbs, card, dialog and popover menu.
- `assets/css/list.css`: everything on a list page, including selection and the bulk-action bar.
- `assets/css/read.css`: tabs, fields, related-record links and the side panel.
- `assets/css/forms.css`: fields, validation, switch, choices, the relation field and the form action bar.
- `assets/css/feedback.css`: alert banners, toasts and confirm dialogs.
- `assets/css/history.css`: version badges, the timeline, the diff and the roll-back preview.
- `assets/icons.svg`: the icon sprite, including the logo.
- `references/specimens/*.html`: the reference pages, one per group. Each component appears there in its real
  markup and every state, built from the files in `assets/`. Serve the skill folder over HTTP to view them (the
  sprite does not load into a page opened from a file):
  `python .claude/skills/brand-htmlx-pages/scripts/serve_specimens.py`, then open
  `http://localhost:5180/references/specimens/foundations.html`.
- `scripts/check_contrast.py`: the WCAG contrast check for every token pair, light and dark.
- `scripts/serve_specimens.py`: serves the skill folder with browser caching off, so an edited stylesheet or a new
  icon shows on a plain reload.

## Rules

**Color.** Application CSS and markup use tokens only, never a literal color. Blue (`--accent`) marks anything
the user can click. Violet (`--brand`) is the brand: the logo, the user avatar and version badges, never a button.
Statuses use the five tones (`success`, `danger`, `warning`, `info`, `neutral`), each a text color, a `-subtle`
fill and a `-line`. A toast reverses the page's colors (`--inverse`, `--on-inverse`, `--accent-on-inverse`).
Every text pair passes WCAG AA (4.5:1) in both modes and form-control borders pass 3:1.
`python .claude/skills/brand-htmlx-pages/scripts/check_contrast.py` checks them; a new token gets its pairs added
to that script and passes it before anything uses it.

**Light and dark.** Each color token is written once as `light-dark(light, dark)`. The page follows the
operating system; `<html data-theme="light">` or `"dark"` pins a mode.

**Class names.** One lowercase block name per component (`.btn`, `.grid`, `.chip`), a plain word for each
variant scoped to its block (`.btn.primary`, `.chip.success`), and ARIA attributes for state: `aria-current`,
`aria-sort`, `aria-busy`, `aria-selected`, `aria-invalid`, `disabled`. No utility classes beyond `.muted`, `.mono`
and `.visually-hidden`.

**Text width.** Paragraph text fills its container: no `max-width`, and no `ch` measure, on a paragraph or any
other block of text. A control may stop short (the filter box stops at 360px); text does not.

**Buttons.** One primary button per view. `danger` is only for an action that cannot be undone (delete
permanently, discard unsaved changes); archive, restore and roll back can be undone, so they are never danger.
An icon-only button carries an `aria-label`.

**Icons.** From the sprite only: `<svg class="icon" aria-hidden="true"><use href="/icons.svg#name"></use></svg>`.
A missing icon is drawn into the sprite on the same 24px grid as plain strokes, never fetched from an icon set.

**Script.** The brand needs almost none. Menus and the side panel are popovers, dialogs open and close through
`commandfor` and `command`, the bulk bar shows and counts in CSS, and Clear is a reset button. Only two pieces
need script, and the application keeps them: the nav toggle (rail or full) and the select-all box in a grid
header. Everything else that moves is HTMX.

**Shell.** `.app` holds `.app-header`, `.app-nav` and `main.app-main#main`, with a skip link first in `<body>`.
The current page's nav link has `aria-current="page"`. The nav is full from 900px and an icon rail below that;
`data-nav="rail"` or `"full"` on `.app` overrides, and each nav link has a `title` for the rail's tooltip.
The signed-in user is an avatar of initials at the right of the header, with `role="img"` and an `aria-label`
naming the user. It is the only avatar in the application: grids, panels and history entries show names, not
avatars. A card is a size container, so a component inside it adapts to the card's width with `@container`.
A dialog is a native modal `<dialog class="dialog">` (`.sm` for a confirm) with `.dialog-header`,
`.dialog-body` and `.dialog-footer`; Escape closes it and the page behind is inert.

**List.** A list page is a page header (breadcrumbs, `h1`, the one primary action) over a card holding toolbar,
busy bar, grid and pager. The toolbar puts the filter box on the left and the Active / Archived / All status filter
at the right end. The filter box narrows the list as you type; its placeholder reads "Filter …", never "Search …",
and its icon is `filter`, not `search`. Sort headers, status filters and page links are real links and the filter
box is a GET form, so the page works before HTMX is added. The sorted header carries `aria-sort`. The first column
is the title cell, `<td class="title">`: a link to the record, then a `<span class="sub">` second line. An archived
row in the All view has `class="archived"` and a neutral `Archived` chip. Each row's actions are a native popover
menu with an id built from the record id: View, Edit, History, then Archive for an active record; View, Restore,
then Delete permanently (danger) for an archived one. The pager has three columns: the range shown ("Showing 1–10
of 49") on the left, Previous and Next centered, and a Rows per page select (10, 25, 50, 100) on the right; on a
card narrower than 560px the buttons move to a second line. A Previous or Next with nowhere to go is a
`<span class="btn" aria-disabled="true">`, not a link. An empty list says why it is empty and offers at most one
action.

**Bulk selection.** A grid that allows it starts with a `.select` column: a checkbox per row with
`form="bulk-form"` and `name="ids"`, tied to one empty `<form id="bulk-form" method="post">` in the card, and a
select-all box in the header. A checked row is highlighted. The `.bulk-bar` follows the grid and appears with the
first checked row, showing the count; Clear is `<button type="reset" form="bulk-form">`. Its actions follow the
status filter: Archive in Active; Restore and Delete permanently in Archived; Archive and Restore in All. Each
action is a submit button with its own `formaction`. A read-only list (records the user may see but not change,
or a report) is the same grid with no selection column, no row menu and no New button.

**Read.** A record's details page has a page header whose title is the record's name with its status chip
beside it (`<div class="title">`), a muted line under it saying who updated it and when, and its actions on the
right: Archive (secondary) and Edit (primary). Under the header, `nav.tabs` holds Details and History as links,
each with its own URL. The fields sit in cards, each with a `card-header` naming the group, as a
`<dl class="fields">` with every label over its value. Email and web addresses are links; a value never set
reads "Not set" (`.unset`); identifiers and numbers are `.mono`. Another record is always a related-record link,
`<a class="related">` led by its type's icon (`user` for a contact, `building` for a company); a link to an
archived record is muted and followed by an `Archived` chip, in a field or a grid cell alike. The Record card
shows the current version badge. The side panel, `<aside class="panel" popover>`, shows the same fields for one row
of a list, with Open full record and Edit in its footer; the row it shows carries `aria-selected="true"`. Without
HTMX the row's name opens the details page.

**Forms.** The edit form uses the same cards and the same `.fields` grid as the details page, so every value keeps
its place between reading and editing. Each field is a `.field`: label, control, optional `.hint`, then the
validation message. Controls take no class; write them with the tag helpers (`<label asp-for>`, `<input asp-for>`,
`<select asp-for>`, `<textarea asp-for>`, `<span asp-validation-for>`, `<div asp-validation-summary="ModelOnly">`)
and the brand styles what they emit: `.input-validation-error`, `.field-validation-error`,
`.validation-summary-errors`. Add `aria-invalid="true"` and `aria-describedby` (pointing at the message's id)
to an invalid control, and `aria-required="true"` to a required one; the asterisk follows from `aria-required` or
`data-val-required`. A yes-or-no value is a switch (`<label class="switch">` around `input[role="switch"]`) in a
fieldset whose legend names it. Several values from a short fixed list are `.choices`: a fieldset of checkbox pills.
Another record is a `.relation` field: the chosen record as a related-record link, and a Change button that opens a
picker dialog built from the list components (filter box, grid with a radio per row) listing active records only.
A form ends with `.form-actions`, which sticks to the bottom of the window: Duplicate and Archive at the left,
Cancel and Save at the right. A new record starts in a create dialog with only the fields it needs to exist;
without HTMX the New link opens a create page with the same fields. An error that belongs to no single field,
such as someone else saving the record first, goes in the validation summary at the top of the form.

**Feedback.** An action that can be undone (archive, restore, roll back) runs at once and says so in a toast with
Undo. Only an action that cannot be undone (delete permanently, discard unsaved changes) asks first, in a confirm
dialog: `.dialog.sm`, a question naming the record as its title, a `.dialog-icon` in the dialog's tone, the
message, then the safe choice first with `autofocus` and the destructive `.btn.danger` last. When an action is not
allowed at all (a record others depend on), the dialog says why and what to do instead, and offers no way to force
it. Toasts appear in one `<div class="toasts" role="status" aria-live="polite">` in the layout and leave by
themselves after 8 seconds, waiting while the pointer or keyboard is on them. A state that lasts is an `.alert` banner
between the page header and the content: archived (neutral, with Restore), an old version (info, with Back to the
current version), someone else's newer change (warning), a failed save (danger, `role="alert"`).

**History.** Every save makes a version, and so do archive, restore and roll back. The History tab is a
`.timeline` of versions, newest first; each entry is a `<details>` whose `<summary>` holds a chevron, the
`.version` badge, what happened (Created, Edited and the field names, Archived, Restored, Rolled back to version N),
and who and when. Opening it shows a `.diff` of only the changed fields (Field, Before, After, as `<del>` and
`<ins>`), then View this version and Roll back to this version; the current version has neither. Rolling back
saves the chosen version's values as a new version, so it never rewrites history and can itself be rolled back.
Roll back opens a preview dialog with the diff from now to that version and a primary (not danger) button; a value
that cannot come back, such as a related record since deleted, keeps its current value and the preview says so
in a warning alert.

**Sample data.** Invented and obviously so: names like Pat Example and Sam Sample, addresses at `example.com`,
`example.org` or `example.net`, companies like Placeholder Logistics.

## Open questions

- How the application takes `assets/`: a copy in `wwwroot` with this folder as the source, or a build step
  that links them. Decided in the session that creates the application.
- The HTMX wiring, for Matt to settle. A proposal, not yet tried in the application:
  - Every link and form stays real, so each page works without HTMX; HTMX only narrows what is swapped.
  - The list card is one region (`id="contacts-list"`): sort, status and page links carry `hx-get`,
    `hx-target="#contacts-list"`, `hx-select="#contacts-list"` and `hx-push-url="true"`, so the page handler stays
    one `OnGet` that renders the whole page; the filter box adds `hx-trigger="input changed delay:300ms, search"`;
    the rows-per-page select adds `hx-trigger="change"`. The alternative is `hx-boost` on the region with the
    handler returning only the list partial for HTMX requests; it needs fewer attributes but a second code path.
  - The side panel and the create and picker dialogs load their contents with `hx-get` into the panel or dialog
    body and open it after the swap (`showPopover()`, `showModal()`).
  - A toast arrives as an out-of-band swap (`hx-swap-oob="beforeend:.toasts"`) in the response to the action; its
    Undo is an `hx-post` to the undo handler.
  - The edit form asks before discarding unsaved changes; that needs a few lines of script, or an HTMX
    `hx-confirm` replaced by the brand's confirm dialog.
