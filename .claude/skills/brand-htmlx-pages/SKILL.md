---
name: brand-htmlx-pages
description: The HTMLX Pages component library for the Razor Pages and HTMX application in this repository. Use whenever writing, styling or reviewing its markup, CSS, partial views or page layouts. Covers the design tokens (color, type, space, shape) taken from the DevExtreme UI Template Gallery and MudBlazor, light and dark mode, the class naming rule, buttons and icons, and a catalog of components by family, each with its markup, variants, states, accessibility and when to use it — layout (app shell, page header, card, hero, toolbar, tabs), navigation (side nav, breadcrumbs, menu, segmented control, pager), data display (grid, chip, empty state, fields, related-record link, side panel, version badge, timeline, diff), inputs (field and validation, switch, choices, relation field, filter box, form action bar) and feedback (dialog, confirm dialog, busy bar, bulk bar, alert, toast). Draft; the status table says which components are settled.
owner: Matt
version: 0.5.1
visibility: public
---

# HTMLX Pages

HTMLX Pages is the look of this repository's Razor Pages application and the catalog of components its pages are
built from: what each component is for, the markup it expects, its variants and states, and when to use it. It says
nothing about what the application does. A page is a composition of these components, and which ones a page uses is
the application's decision, recorded in the application.

The layout patterns come from the [DevExtreme UI Template Gallery](https://github.com/DevExpress/devextreme-ui-template-gallery)
(MIT), and the component names and color roles from [MudBlazor](https://github.com/MudBlazor/MudBlazor) (MIT). No
code, CSS or artwork is copied from either: the gallery runs on DevExtreme, which has its own commercial license, and
this repository is public. Everything in this skill was written for this repository.

## Status: draft 0.5.1

| Family | Components | State |
|---|---|---|
| Foundations | tokens, color roles, type, space, shape, icons, buttons, form-control defaults | Settled for review |
| Foundations, added | without script and without HTMX: script-only controls leave, Apply buttons show | Ready for review |
| Layout | app shell, avatar, page header, card, toolbar, tabs | Settled for review |
| Layout, added | hero | Ready for review |
| Navigation | side nav and rail, breadcrumbs, menu, segmented control, pager | Settled for review |
| Navigation, added | side nav foot, external icon | Ready for review |
| Data display | grid (title cell, sort header, archived row), chip, empty state, fields, related-record link, side panel | Settled for review |
| Data display, added | selection column, read-only grid, version badge, timeline, diff | Ready for review |
| Inputs | filter box | Settled for review |
| Inputs, added | field and validation (checked as you type too), switch, choices, relation field and picker, form action bar | Ready for review |
| Feedback | dialog, busy bar | Settled for review |
| Feedback, added | confirm dialog, preview dialog, bulk bar, alert, toast | Ready for review |

"Settled for review" components were built and checked with Matt; "Ready for review" ones were built while he was
away and wait for his review. A page uses only components that are here. One the catalog lacks is added here first,
with Matt, and never invented in the application's markup.

## Files

Everything under `assets/` goes into the application; nothing outside it does.

- `assets/css/tokens.css`: every color, size and font as a custom property. The only place a literal color appears.
- `assets/css/base.css`: element defaults, form-control defaults, the utilities, the skip link, icons and buttons.
- `assets/css/layout.css`: app shell, avatar, page header, card, toolbar and tabs.
- `assets/css/navigation.css`: side nav and rail, breadcrumbs, menu, segmented control and pager.
- `assets/css/data-display.css`: grid, chip, empty state, fields, related-record link, side panel, version badge,
  timeline and diff.
- `assets/css/inputs.css`: filter box, field and validation, switch, choices, relation field and form action bar.
- `assets/css/feedback.css`: dialog, busy bar, bulk bar, alert, toast, confirm dialog and preview dialog.

  A page links the stylesheets in the order above, tokens first and feedback last.
- `assets/icons.svg`: the icon sprite, including the logo.
- `references/specimens/*.html`: one page per family (`foundations`, `layout`, `navigation`, `data-display`,
  `inputs`, `feedback`), each component on its own in every state, built from the files in `assets/`. Serve the skill
  folder over HTTP to view them (the sprite does not load into a page opened from a file):
  `python .claude/skills/brand-htmlx-pages/scripts/serve_specimens.py`, then open
  `http://localhost:5180/references/specimens/foundations.html`.
- `scripts/check_contrast.py`: the WCAG contrast check for every token pair, light and dark.
- `scripts/serve_specimens.py`: serves the skill folder with browser caching off, so an edited stylesheet or a new
  icon shows on a plain reload.

**Open question:** how the application takes `assets/`, as a copy in `wwwroot` with this folder as the source, or
through a build step that links them.

## Foundations

**Color.** Markup and CSS use tokens only, never a literal color. Blue (`--accent`) marks anything the user can
click. Violet (`--brand`) is the brand: the logo, the avatar and version badges, never a button. Statuses use the five
tones (`success`, `danger`, `warning`, `info`, `neutral`), each a text color, a `-subtle` fill and a `-line`. The
inverse pair (`--inverse`, `--on-inverse`, `--accent-on-inverse`) reverses the page's colors. Every text pair passes
WCAG AA (4.5:1) in both modes and form-control borders pass 3:1;
`python .claude/skills/brand-htmlx-pages/scripts/check_contrast.py` checks them, and a new token gets its pairs added
to that script and passes it before anything uses it.

**Light and dark.** Each color token is written once as `light-dark(light, dark)`. The page follows the operating
system; `<html data-theme="light">` or `"dark"` pins a mode.

**Type.** One family (`--font`) and a monospace one (`--font-mono`) for identifiers, numbers and version badges, in
`.mono`. Six sizes, `--text-xs` to `--text-2xl`, and two weights, `--weight-regular` and `--weight-strong`.

**Space and shape.** A 4px grid, `--space-1` to `--space-8`. Controls round to `--radius-sm`; cards, menus and dialogs
to `--radius-md`; chips and the avatar are `--radius-full`. A surface at rest has `--shadow-card`; anything that
floats over the page has `--shadow-popup`.

**Icons.** From the sprite only: `<svg class="icon" aria-hidden="true"><use href="/icons.svg#name"></use></svg>`,
20px, or 16px with `.icon.sm`. Line icons on a 24px grid that take the text color. An icon beside a label is
`aria-hidden`; an icon alone sits in a button with an `aria-label`. A missing icon is drawn into the sprite on the
same grid as plain strokes, never fetched from an icon set.

**Buttons.** `.btn` is the secondary button; `.btn.primary`, `.btn.ghost` and `.btn.danger` are the variants, and
`.icon-only` makes a square icon button, which must carry an `aria-label`. One primary button per view. `danger` is
only for an action that cannot be undone; an action that can be undone is never danger. A disabled button is
`disabled`, or `aria-disabled="true"` on a link; a button whose request is in flight keeps its width and shows a
spinner, from HTMX's `.htmx-request` or from `aria-busy="true"`. `.btn-group` lays out a row of buttons.

**Form controls.** Every text-like input, select and textarea is styled with no class at all, so what the tag
helpers emit needs nothing added. Checkboxes and radios take the accent color. Disabled and read-only controls look
different from editable ones.

## Conventions

**Class names.** One lowercase block name per component (`.btn`, `.grid`, `.chip`), a plain word for each variant
scoped to its block (`.btn.primary`, `.chip.success`), and ARIA attributes for state: `aria-current`, `aria-sort`,
`aria-busy`, `aria-selected`, `aria-invalid`, `disabled`. No utility classes beyond `.muted`, `.mono` and
`.visually-hidden`.

**Text width.** Paragraph text fills its container: no `max-width`, and no `ch` measure, on a paragraph or any other
block of text. A control may stop short (the filter box stops at 360px); text does not.

**Without HTMX.** Every component works as plain HTML first. Navigation is real links, filters are GET forms, actions
are buttons in forms that post; HTMX only narrows what a response replaces. A form that HTMX applies as you go, the
filter box and Rows per page, holds an Apply button, a plain `.btn` of `type="submit"`, which its block's CSS hides
while script runs and HTMX is there. It shows when the browser runs no script, and when the application's script
finds no HTMX and sets `data-htmx="absent"` on the root element.

**Script.** The components need almost none. Menus and the side panel are popovers, dialogs open and close through
`commandfor` and `command`, the bulk bar shows and counts in CSS, a toast leaves by itself, and Clear is a reset
button. Only three pieces need script, and the application keeps them: the side nav's toggle between full and rail,
a toast's Dismiss, and a grid header's select-all box. Each says so, with `data-action` or `data-select-all`, and
base.css takes it off the page when the browser runs no script (`@media (scripting: none)`), so that nothing on the
page does nothing when pressed.

**Sample text.** Text in specimens and examples is invented and obviously so: names like Pat Example and Sam Sample,
addresses at `example.com`, `example.org` or `example.net`, websites under `.example`.

## Components

Each entry gives what the component is for, its markup, its variants and states, what it needs for accessibility,
and when to use it. The specimen page for its family shows it in every state.

### Layout

**App shell** (`layout.css`). The frame of every page. `.app` holds `.app-header`, `.app-nav` and
`main.app-main#main`, with a `.skip-link` to `#main` first in `<body>`. The header holds the menu button
(`data-action="toggle-nav"`, gone when the browser runs no script, which leaves the nav at its default) and the
`.brand` at the left and, in `.end`, whatever belongs to the whole application, ending with the avatar. The nav is
full from 900px and an icon rail below that; `data-nav="rail"` or `"full"` on `.app` overrides the default.

**Avatar** (`layout.css`). The signed-in user's initials, `<span class="avatar" role="img" aria-label="Signed in as
…">`, at the right end of the header. It is the only avatar: grids, panels and histories show names, not avatars.

**Page header** (`layout.css`). The top of `main`: `.breadcrumbs`, then `.title-row` with the `h1` at the left and
the page's actions at the right, the one primary action last, then an optional `p.muted` line. A page about one
record wraps its title in `<div class="title">` with the record's status chip, and its version badge if it has one,
beside the `h1`, and its muted line says who changed the record last and when.

**Card** (`layout.css`). The surface every list, form and panel sits on. `.card-header` names what the card holds and
may carry one action; `.card-body` pads the content. A card is a size container, so a component inside it adapts to
the card's width with `@container` rather than to the window's. `.cards` stacks cards with the standard gap.

**Hero** (`layout.css`). The opening of a landing page, such as the home page, and nowhere else: never on a list or a
record's page. In a card, `<div class="hero">` holds an illustration, `img.hero-art`, beside `.hero-text`: an `h2`, a
paragraph that says what the application is for, and a `.btn-group` of the page's main actions, the primary one
first. The illustration is an image file with colors of its own, the one place a color that is not a token appears;
it must read on `--surface` in light and dark mode, and it sets no color the page uses. It says `alt=""` when the
heading already says what it shows, and real alt text when it carries meaning. On a card narrower than 560px it moves
above the text. One per page.

**Toolbar** (`layout.css`). The row at the top of a list's card: a filter box at the left and a segmented control at
the right end, wrapping onto two lines on a narrow card. It holds nothing else.

**Tabs** (`layout.css`). Links to the sections of one page: `<nav class="tabs" aria-label="…">` of links, each
section with its own URL. The current tab carries `aria-current="page"`; a `.count` may follow a label. Use them to
split one page into sections; moving between pages is the side nav's job.

### Navigation

**Side nav** (`navigation.css`). The application's pages: `.app-nav`, each link an icon and a `<span>` label with a
`title` for the rail's tooltip, and `.nav-label` over a group of links. The current page's link carries
`aria-current="page"`. In the rail the labels leave the screen but stay in the accessibility tree. Links that leave
the application, such as to its source repository, sit in `<div class="end">` at the foot of the nav, pinned to the
bottom under a rule. Such a link opens a new tab (`target="_blank" rel="noopener"`), leads with the `external` icon,
and says it opens a new tab in a visually hidden span after its label and in its `title`.

**Breadcrumbs** (`navigation.css`). Where a page sits: `<nav class="breadcrumbs" aria-label="Breadcrumb">` around
an `<ol>` of links that ends in the current page, as text with `aria-current="page"`.

**Menu** (`navigation.css`). A short list of actions on one thing: `<div class="menu" id="…" popover>`, opened by the
button that names it in `popovertarget`, and placed under that button with its right edge on the button's, as a menu
at the right end of a row or a header needs. Each item is a link or a button with an icon; an `<hr>` separates
groups, and an action that cannot be undone comes last with `class="danger"`. The button that opens it, usually
`.btn.ghost.icon-only` with the `more` icon, says whose actions they are in its `aria-label`. A menu's id is unique
on the page, so a row's menu takes its id from the row's record.

**Segmented control** (`navigation.css`). A few mutually exclusive views of the same content, such as a status
filter: `<nav class="segmented" aria-label="…">` of links, the current one with `aria-current="page"`, each with an
optional `.count`. In a toolbar it sits at the right end.

**Pager** (`navigation.css`). `<footer class="pager">` at the bottom of a list's card, in three columns: `.summary`
with the range shown ("Showing 1–10 of 128"), `.pages` with Previous and Next, and a `.page-size` GET form with a
Rows per page select (10, 25, 50, 100) and an Apply button that shows only without HTMX (Conventions). A Previous or
Next with nowhere to go is a
`<span class="btn" aria-disabled="true">`, not a link. On a card narrower than 560px the buttons move to a second
line.

### Data display

**Grid** (`data-display.css`). A table of records: `table.grid` in a `.grid-wrap`, which scrolls sideways when the
card is too narrow, with a `caption` (visually hidden if need be) that says what the table holds and how it is sorted.
- *Title cell:* the first column is `<td class="title">`: a link to the record, then a muted second line in
  `<span class="sub">`.
- *Sort header:* a sortable header is a link, `<th><a href="?sort=…">Name <svg class="icon sm">…</svg></a></th>`; the
  sorted one carries `aria-sort="ascending"` or `"descending"` and shows its arrow, the others show theirs on hover or
  focus.
- *Numbers:* `.num` right-aligns a column and its figures.
- *Row actions:* the last cell, `<td class="actions">`, holds the row's menu button.
- *Archived row:* a row whose record is archived, among rows that are not, has `class="archived"`, muted text and a
  neutral `Archived` chip.
- *Selected row:* the row the side panel shows carries `aria-selected="true"`.
- *Selection column (ready for review):* a `.select` cell first in each row with a checkbox, or a radio in a picker,
  whose `<label>` is the row's title, so a click on the name selects the row; a checked row is highlighted. The header
  cell holds a select-all checkbox with an `aria-label`. The bulk bar acts on the selection.
- *Read-only (ready for review):* for records the user may see but not change, and for reports: no selection column
  and no row actions. The title still links to the record.

**Chip** (`data-display.css`). A status label with a dot, never a button: `<span class="chip success">Active</span>`.
Tones `success`, `danger`, `warning`, `info` and `brand`; neutral is the default.

**Empty state** (`data-display.css`). What a list shows when it has nothing to show: `.empty` with one icon, one
heading, one sentence saying why, and at most one action, such as clearing a filter that matched nothing.

**Fields** (`data-display.css`). Label-over-value pairs: `<dl class="fields"><div><dt>…</dt><dd>…</dd></div>…</dl>`,
filling as many 260px columns as fit (two or three in a card, one in the side panel); `.wide` spans the row. Email
and web addresses are links; another record is a related-record link; a value never set reads "Not set" in
`<span class="unset">`, never a blank or a dash; identifiers and numbers are `.mono`.

**Related-record link** (`data-display.css`). Another record, always as `<a class="related">` led by its type's icon
(`user` for a person, `users` for a group, `building` for an organization). A link to an archived record stays a
link, adds `.archived` to be muted, and is followed by a neutral `Archived` chip, in a field or a grid cell alike.

**Side panel** (`data-display.css`). A quick look at one record without leaving the page:
`<aside class="panel" id="…" popover aria-labelledby="…">` with `.panel-header` (the record's name and status chip in
`.title`, and a close button), `.panel-body` with the record's fields, and `.panel-footer` with Open full record and
the one primary action. It slides in from the right and is not modal: the page beside it stays usable, and Escape,
the close button or a click outside closes it. The link that opens it is still a link to the record's page, so it
works without script.

**Version badge** (ready for review, `data-display.css`). A version number in the brand violet:
`<span class="version">v4</span>`; `.version.current`, the version a record is at now, is filled.

**Timeline** (ready for review, `data-display.css`). A record's versions, newest first, in
`<ol class="timeline" reversed>`. Each `li` holds a `<details>` whose `<summary>` has a chevron icon
(`.icon.sm.chevron`), the version badge, what happened, and who and when in `.when`; opening it shows `.entry-body`
with the diff and, in `.entry-actions`, the actions on that version. The current version is `li.current`, has no
actions, and may start open.

**Diff** (ready for review, `data-display.css`). Changed values before and after: `table.diff` in a `.diff-wrap`, with
the columns Field, Before and After, the old value in `<del>` and the new in `<ins>`, and only the fields that
changed. A missing value reads "Not set". The column headers carry the meaning; the colors only reinforce it.

### Inputs

**Field** (ready for review, `inputs.css`). One value in a form: `.field` holding the label, the control, an optional
`p.hint`, then the validation message. Write them with the tag helpers (`<label asp-for>`, `<input asp-for>`,
`<select asp-for>`, `<textarea asp-for>`, `<span asp-validation-for>`) and lay them out in a `.fields` grid, so a
value keeps its place between reading and editing.
- *Required:* the label gets an asterisk from `aria-required="true"` or from the `data-val-required` the tag helpers
  add for `[Required]`.
- *Invalid:* the control carries `.input-validation-error` and `aria-invalid="true"` and points at its
  `.field-validation-error` message with `aria-describedby`. An error that belongs to no single field goes in the
  validation summary (`<div asp-validation-summary="ModelOnly">`, styled as `.validation-summary-errors`) at the top
  of the form.
- *Hint:* `p.hint` under the control, also named in the control's `aria-describedby`.
- *Checked as you type:* the message is a polite live region (`aria-live="polite"`), so a screen reader reads a
  message that appears or changes while the user is still in the field. Nothing else changes: the control and the
  message look as in any other state.
- *Disabled and read-only:* the control's own `disabled` or `readonly`.
- A group of controls, such as a switch, choices or a relation field, is a `fieldset.field` whose `legend` names it.

**Switch** (ready for review, `inputs.css`). A yes-or-no value: `<label class="switch">` around
`<input type="checkbox" role="switch">` and its label text, in a `fieldset.field`.

**Choices** (ready for review, `inputs.css`). Several values from a short fixed list:
`<fieldset class="field choices">` with its `legend` and a `.options` row of checkbox labels, drawn as pills. For one
value from a list, use a select.

**Relation field** (ready for review, `inputs.css`). Another record as a value: in a `fieldset.field`, a `.relation`
holding a hidden input with the chosen record's id, the chosen record as a related-record link, and a Change button
that opens a picker dialog. With nothing chosen it holds `<span class="unset">` and a Choose button. The picker is a
dialog built from the list components: a filter box and a grid whose selection column holds radios. It lists only
the records that can be chosen.

**Filter box** (`inputs.css`). A search input that narrows a list as you type: a GET
`<form class="filter-box" role="search">` with the `filter` icon and an `<input type="search">` with an
`aria-label`, then an Apply button that shows only without HTMX (Conventions). Its placeholder reads "Filter …",
never "Search …", and its icon is `filter`, not `search`. It stops at 360px wide, Apply included.

**Form action bar** (ready for review, `inputs.css`). The last thing in a form: `.form-actions`, which sticks to the
bottom of the window while a long form scrolls. Actions on the whole record sit at the left as ghost buttons; Cancel
and the one primary action sit at the right, in `.end`.

### Feedback

An action that can be undone runs at once and says so in a toast with Undo. Only an action that cannot be undone asks
first, in a confirm dialog. A state that lasts is an alert on the page.

**Dialog** (`feedback.css`). A native modal `<dialog class="dialog" aria-labelledby="…">` with `.dialog-header` (the
`h2` title and a close button), `.dialog-body` and `.dialog-footer`, directly or inside one `<form>`. Buttons open and
close it with `commandfor` and `command="show-modal"` or `"close"`, so it needs no script; Escape closes it and the
page behind is inert. The footer ends with the one primary action. Use it for a short task that must finish before
the page goes on, such as a short form or a picker.

**Confirm dialog** (ready for review, `feedback.css`). Only for an action that cannot be undone: `.dialog.sm` with
`aria-describedby` on its message. The title asks the question and names the record; `.dialog-body.confirm` leads with
a `.dialog-icon` in the dialog's tone (danger by default, `.warning` or `.info`), then the `.message`; the footer puts
the safe choice first with `autofocus` and the destructive `.btn.danger` last. When the action is not allowed at all,
the dialog says why and what to do instead, with a warning icon, an OK button and no way to force it.

**Preview dialog** (ready for review, `feedback.css`). Shows the result of a change before it is made: a `.dialog`
whose `.dialog-body.rollback` says what will happen, warns in an `.alert.warning` about anything that cannot be
carried out, and shows the diff from now to the result. What it previews can be undone, so its button is primary, not
danger.

**Busy bar** (`feedback.css`). A 2px line under a card's toolbar, `<div class="busy-bar">`, that runs while a request
from the card is in flight, while the grid's rows dim; the toolbar stays usable. HTMX's `.htmx-request` shows it, and
so does `aria-busy="true"` on the card without HTMX. A button that sent a request shows its own spinner instead.

**Bulk bar** (ready for review, `feedback.css`). What can be done to the rows checked in a grid's selection column.
Each row's checkbox names one empty `<form id="bulk-form" method="post">` in the card with `form="bulk-form"`;
`<div class="bulk-bar" role="region" aria-label="…">` follows the grid, appears with the first checked row and counts
them, in CSS alone. Clear is `<button type="reset" form="bulk-form">`, and each action is a submit button with its own
`formaction`; one that cannot be undone is `.danger` and comes last.

**Alert** (ready for review, `feedback.css`). A banner between the page header and the content, about a state that
lasts: `.alert` with an icon, `.content`, then at most one or two buttons in `.actions`. Tones `info`, `success`,
`warning` and `danger`; neutral is the default. The text keeps the page's color; the tone colors the icon, fill and
line. A failure that needs attention now is `role="alert"`; the rest are `role="status"`.

**Toast** (ready for review, `feedback.css`). A short message about an action that just ran:
`<div class="toast"><span class="message">…</span>…</div>`, with Undo as a ghost button when the action can be
undone and an icon-only Dismiss button, `data-action="dismiss-toast"`, which the application's script makes work
and which is gone when the browser runs no script. Toasts appear in one
`<div class="toasts" role="status" aria-live="polite">` that sits empty in the layout until a response adds one. A
toast leaves by itself after 8 seconds, waiting while the pointer or keyboard is on it, and reverses the page's
colors.
