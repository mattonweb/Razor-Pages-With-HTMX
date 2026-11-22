---
name: brand-htmlx-pages
description: The HTMLX Pages brand for web applications that manage records — how its pages look and how its components behave, whatever framework or styling approach an application uses. Use whenever building, styling or reviewing the user interface of an application that adopts the brand. Covers the design tokens (color with light and dark values, type, space, shape, elevation, sizes, motion) taken from the DevExtreme UI Template Gallery and MudBlazor, the icon sprite and the favicon, the contrast rule, forced colors, the conventions every page follows, and a specification of every component, with its parts, measurements, variants, states, accessibility, when to use it and reference images in light and dark — layout (app shell, avatar, page header, card, hero, toolbar, tabs), navigation (side nav, breadcrumbs, menu, segmented control, pager), data display (grid, chip, empty state, fields, related-record link, side panel, version badge, timeline, diff), inputs (field and validation, switch, choices, rows, relation field, filter box, form action bar) and feedback (dialog, confirm dialog, preview dialog, busy bar, progress bar, bulk bar, alert, toast). It holds no stylesheet and names no class; each application builds the components its own way.
version: 2.0.0
visibility: public
---

# HTMLX Pages

HTMLX Pages is a brand for web applications that manage records: how a page looks and how each of its components
behaves. It says nothing about what an application does, and nothing about how one is built. There is no stylesheet
here and no class name. Each component is specified by its parts, its measurements in the brand's tokens, its
variants and states, what it must tell assistive technology, and when to use it, with reference images in light and
dark. An application builds each one in whatever way it styles its pages, and names it as it likes.

The brand assumes the web platform (HTML elements, ARIA roles and states, CSS custom properties) and nothing above
it.

The layout patterns come from the [DevExtreme UI Template Gallery](https://github.com/DevExpress/devextreme-ui-template-gallery)
(MIT), and the component names and color roles from [MudBlazor](https://github.com/MudBlazor/MudBlazor) (MIT). No
code, CSS or artwork is copied from either: the gallery runs on DevExtreme, which has its own commercial license.
Everything here was written for this brand.

## Adopting the brand

- Copy `assets/tokens.css`, `assets/icons.svg` and the three favicon files into the application, unchanged. The
  tokens file sets every token as a CSS custom property on the root element. An application uses the properties
  directly, or maps them into its styling framework's theme under that framework's names; the brand's names stay as
  they are. Nothing links a copy to this folder: a later change here reaches an application only when it copies the
  file again.
- Build each component to its spec. A measurement that has a token is that token; one that has none is the value the
  spec gives.
- Compare the result with the component's reference images, by eye. They were drawn at 1x in a 1280px window with
  Segoe UI Variable; another browser or operating system draws text a little differently.
- An application uses only the components specified here. One the catalog lacks is specified here first, with its
  reference images, and never invented in an application's markup.

## Files

- `assets/tokens.css`: every color, size and font as a custom property. The only place a literal color appears,
  apart from the logo, in the sprite and the favicon files, and a hero's illustration.
- `assets/icons.svg`: the icon sprite, including the logo.
- `assets/favicon.svg`, `assets/favicon.ico` and `assets/apple-touch-icon.png`: the favicon (Foundations).
- `references/images/<family>/<name>-light.png` and `-dark.png`: the reference images, one for each component and for
  each state that only shows when opened (`menu-open`, `side-panel-open`, `dialog-open` and the confirm dialogs,
  `relation-picker-open`), plus the app shell full and as a rail. The families are `foundations`, `layout`,
  `navigation`, `data-display`, `inputs` and `feedback`.
- `scripts/check_contrast.py`: the WCAG contrast check for every token pair, light and dark:
  `python .claude/skills/brand-htmlx-pages/scripts/check_contrast.py`.

## Foundations

Images: [color](references/images/foundations/color-light.png),
[type](references/images/foundations/type-light.png),
[space and shape](references/images/foundations/space-and-shape-light.png),
[icons](references/images/foundations/icons-light.png), each with a `-dark` twin.

### Color

Pages use tokens only, never a literal color. The roles:

- **Accent**, for anything the user can act on. `--accent` colors links, focus and selection; `--accent-fill`, with
  `-hover` and `-active`, is the face of a primary button and of a checked control, with `--on-accent` text on it;
  `--accent-subtle` fills a selected nav item, row or option.
- **Brand**, violet: the logo, the avatar, version badges and the current version in a timeline, never a button.
  `--brand` is the fill, `--on-brand` the text on it, `--brand-text` violet text, and `--brand-subtle` a pale fill.
- **Neutrals.** `--text`, `--text-muted` for secondary text, `--text-disabled`, `--icon` for icons beside text,
  `--bg` the page behind everything, `--surface` for cards, the header, the nav and anything floating,
  `--surface-hover`, `--border` for dividers and frames, `--border-input` for form-control borders.
- **Inverse**, a toast's colors, reversing the page's: `--inverse`, `--on-inverse` and `--accent-on-inverse`.
- **Tones** for statuses: `success`, `danger`, `warning`, `info` and `neutral`, each a text color (`--success`), a
  subtle fill (`--success-subtle`) and a line (`--success-line`). Danger also has `--danger-fill` and
  `--danger-fill-hover`, the face of a danger button.
- **Scrim**: `--scrim`, behind a modal dialog.

Every text pair passes WCAG AA (4.5:1) in both modes, and icons, form-control borders and the part of a progress bar
that is done, on its track, pass 3:1. The contrast script checks them; a new color token gets its pairs added to the
script and passes it before anything uses it.

### Light and dark

Each color token is written once, as `light-dark(light, dark)`. The page follows the operating system;
`data-theme="light"` or `"dark"` on the root element pins a mode.

### Forced colors

When the operating system forces its own colors, as a Windows contrast theme does, the browser draws the page in the
theme's colors: text and lines in its text and link colors, every background in its window color, and no shadows.
The page lets it, and keeps what a fill or a shadow showed with the theme's own system colors, never one of its own:
- **Focus** keeps its ring, which is an outline (Focus), in `Highlight`. A form control's border no longer changes
  color, so it shows the full ring.
- **Current**: the current link in the side nav, the current segment of a segmented control and the current
  version's badge are `Highlight` behind `HighlightText`. The current tab's bar is `Highlight`, and the other tabs
  have no bar, rather than one in the text color.
- **Busy and progress**: the busy bar's segment and the part of a progress bar that is done are `Highlight`, and a
  progress bar's track has a 1px border. A busy button hides its label in `ButtonFace` and turns a `GrayText` ring
  with a `Highlight` top.
- **Disabled**: a disabled button's text and border are `GrayText`.
- **Edges**: what is drawn by its fill alone (the avatar, a version badge, a tab's count, a toast) has a 1px border in
  the theme's text color, and a chip's dot takes that color too.
- **Icons** take the color of the text around them.

The browser draws a plate in the window color behind text whose colors it forces, so a part filled with `Highlight`
turns forcing off for itself (`forced-color-adjust: none`), names each of its colors as a system color, and drops
any shadow, which forcing would have taken away.

### Type

- **Families.** `--font` for everything, and `--font-mono` for identifiers, code, numbers shown as values and
  version badges, at 0.93 of the surrounding size (a version badge sets its own). A column of numbers keeps the body
  font, in figures of equal width.
- **Sizes.** `--text-xs` 12px (chips, badges, counts, the avatar, nav group labels); `--text-sm` 13px (breadcrumbs,
  table headers, labels, hints, validation messages, second lines, the pager, the diff); `--text-md` 14px, the body;
  `--text-lg` 16px (the product name, card and dialog titles, h3); `--text-xl` 18px (h2); `--text-2xl` 22px (h1, the
  page title). An h4 is `--text-md`.
- **Leading.** `--leading` (1.43, 20px on 14px) for text; 1.25 for headings.
- **Weights.** `--weight-regular` and `--weight-strong` (600), nothing else. Strong is for headings, labels, buttons,
  table headers, title links, chips, tabs, the segments of a segmented control, and the current link in the side
  nav.
- **Headings and paragraphs** carry no margins of their own: the container that holds them sets the space between.

### Space, shape and depth

- **Space.** A 4px grid: `--space-1` 4px, `-2` 8px, `-3` 12px, `-4` 16px, `-5` 20px, `-6` 24px, `-8` 32px.
- **Radius.** Controls round to `--radius-sm` (4px); cards, menus, dialogs, alerts, toasts and bars to `--radius-md`
  (8px); chips, pills, counts and the avatar are `--radius-full`.
- **Depth.** A card at rest has `--shadow-card`, and so does the current segment of a segmented control; the header
  and the side nav have no shadow. Anything that floats over the page (a menu, the side panel, a dialog, a toast,
  the bulk bar, the form action bar) has `--shadow-popup`. Lines are 1px of `--border`.

### Sizes and breakpoints

- `--header-height` 56px; `--nav-width` 250px and `--nav-rail-width` 48px; `--control-height` 32px, every button,
  input and select; `--row-height` 44px, a grid row.
- **The window breakpoint is 900px.** From 900px the side nav is full; below it, a rail, and the main area's padding
  shrinks.
- **The card breakpoint is 560px.** A component inside a card adapts to the card's width, not the window's: on a card
  narrower than 560px the pager and the hero take their narrow layouts.

### Motion

A button's spinner turns once every 0.7s, steadily; the busy bar crosses in 1.1s, easing in and out; the side panel
slides in and out over 0.2s, easing out; a switch and a timeline's chevron move in 0.15s; a toast rises 8px as it
fades in over 0.2s, easing out, and fades out over 0.3s, easing in. When the user asks for reduced motion,
transitions stop (the side panel, a switch, a chevron, a grid's fading rows), and busy indicators slow down (the
spinner to 2s, the busy bar to 3s) rather than vanish, so the state stays visible.

### Focus

Everything that takes focus shows the focus ring when it has keyboard focus: a 2px `--accent` outline, 2px outside
it, rounded to `--radius-sm`. It is an outline, never a shadow, so that no shadow of a component's own replaces it and
forced colors keep it (Forced colors). A form control (a text input, select, textarea, checkbox or radio) shows its
focus instead with an `--accent` border and a 1px `--accent` outline against it; a switch and a choice pill show the
full ring (Inputs). Focus is never hidden without that replacement.

### Icons

- From the sprite only: line icons on a 24px grid, drawn with no fill and a 1.75 stroke in the text color, round caps
  and joins.
- **Sizes.** 20px; 16px small, for sort arrows, chevrons and related-record links; 18px inside a control (a button,
  a menu item, the filter box); 40px with a 1.5 stroke in an empty state.
- **Accessibility.** An icon beside a label is hidden from assistive technology. An icon alone sits in a button with
  an accessible name.
- **The sprite's names:** `logo`, `menu`, `home`, `users`, `user`, `building`, `palette`, `list`, `external`,
  `chevron-left`, `chevron-right`, `chevron-down`, `arrow-up`, `arrow-down`, `sort`, `search`, `filter`, `plus`,
  `more`, `eye`, `edit`, `archive`, `restore`, `trash`, `copy`, `history`, `x`, `check`, `info`, `check-circle`,
  `alert`, `x-circle`, `inbox`, `sun`, `moon`. A missing icon is drawn into the sprite on the same grid as plain
  strokes, never fetched from an icon set.

### Favicon

- **The logo, alone.** `favicon.svg` is the sprite's `logo` as a file of its own: the violet square, its corners
  rounded 6px on the 24px grid, and the white `</>`, in fixed colors, so it reads the same on a light tab and a dark
  one. `favicon.ico` holds it at 16, 32 and 48px, and `apple-touch-icon.png` at 180px, the square with no rounded
  corners and no transparency, since the device rounds it. Each is the SVG drawn at that size; the 16px one keeps the
  logo's own stroke, since a thicker one fills the `</>` in.
- **Every page names all three** in its head: the `.ico` first, then the SVG with `type="image/svg+xml"`, then the
  touch icon. A browser that draws an SVG icon takes the SVG; one that does not takes the `.ico`. The `.ico` is also
  served as `/favicon.ico` at the site's root, so a request for it by name never fails.

### Buttons

Images: [light](references/images/foundations/button-light.png), [dark](references/images/foundations/button-dark.png).

- **Measure.** `--control-height` high, 12px of padding at each side, `--radius-sm`, a 1px border, strong text that
  never wraps, and an 18px icon 8px before the label.
- **Secondary**, the default: a `--surface` face, a `--border-input` border and `--text`; `--surface-hover` under the
  pointer.
- **Primary**: an `--accent-fill` face and border with `--on-accent` text; `--accent-fill-hover` under the pointer,
  `--accent-fill-active` while pressed. One primary button per view.
- **Ghost**: no face and no border, `--icon` text; `--surface-hover` under the pointer. For actions on a row, in a
  toolbar or header, Add and Remove in a form, and Undo in a toast.
- **Danger**: a `--danger-fill` face and border with `--on-accent` text; `--danger-fill-hover` under the pointer.
  Only for an action that cannot be undone; an action that can be undone is never danger.
- **Icon-only**: square, `--control-height` on each side with no padding, and always an accessible name. Usually
  ghost.
- **Disabled**: a `--neutral-subtle` face and border with `--text-disabled` text, and a not-allowed cursor; it does
  not change under the pointer, whatever its variant. A link
  drawn as a button that has nowhere to go is not a link at all: it says `aria-disabled="true"`.
- **Busy**, while the request it sent is running: the button keeps its width, its label goes, and a 16px spinner
  turns in its middle (a 2px ring of `--border` with an `--accent` arc; on a primary button, `--on-accent` at 40%
  with an `--on-accent` arc). It ignores further clicks, and says `aria-busy="true"`.
- **A row of buttons** sits 8px apart and wraps.

### Form controls

- **Text inputs and selects**: `--control-height` high, 12px of padding at each side (8px in a select), a 1px
  `--border-input` border, `--radius-sm`, a `--surface` face and `--text`, in the body font. A textarea is at least
  96px high, with 8px of padding above and below, and resizes up and down only. A placeholder is `--text-muted`.
- **Checkboxes and radios** are 16px and take `--accent-fill` when checked.
- **Disabled**: a `--neutral-subtle` face, a `--neutral-line` border, `--text-disabled` text and a not-allowed
  cursor. **Read-only**: a
  `--bg` face, so it never looks editable.
- **Invalid**: a `--danger` border, and a 1px `--danger` ring when focused.
- Every control looks the same wherever it appears; a form never restyles one.

### Page

The page is `--bg`, with `--text` in `--font` at `--text-md` and `--leading`. Links are `--accent` with no
underline, underlined under the pointer; a link drawn as something else (a button, a nav link, a menu item, a tab, a
segment, the product name, a sort header) is never underlined. A skip link to the main content comes first in the
page, out of sight until it has focus; then it shows 8px from the top left corner, on `--surface` at `--radius-sm`,
with 8px by 12px of padding.

## Conventions

**State is shown and announced alike.** Every state a component shows is also exposed to assistive technology:
`aria-current="page"` on the current page's link or segment, `aria-sort` on a sorted column, `aria-selected` on the
selected row, `aria-invalid` on an invalid control, `aria-busy` on what is waiting for a request, `aria-expanded` on
what opens something, and `disabled` or `aria-disabled` on what cannot be used.

**Text width.** Paragraph text fills its container: no maximum width and no measure on a paragraph or any other block
of text. A control may stop short (the filter box stops at 360px); text does not.

**No sideways scroll.** Nothing in the main area makes the window scroll sideways, at any window width. What is too
wide for its space scrolls inside its own frame, as the grid and the diff do, or wraps, as the toolbar and a row of
actions do. Text kept for assistive technology alone, such as the hidden header of a grid's actions column, sits
inside that frame too, so that it never stands past the window's edge.

**Without script.** Every component is designed to work as plain HTML: navigation is links, filters and Rows per
page are forms that send with GET, and actions are buttons in forms that post. An application whose pages work
without script shows it in two ways:
- A control that only script makes work (the side nav's menu button, a toast's Dismiss, a grid's select-all box) is
  not on the page when the browser runs no script, so that nothing on the page does nothing when pressed.
- A form that applies itself as the user types or chooses (the filter box, Rows per page) carries an Apply button, a
  secondary submit button, shown only when the page's script cannot apply the form: when the browser runs no script,
  or when the script that applies it failed to load.

**Words.** A filter's placeholder reads "Filter …", never "Search …". A value never set reads "Not set", never a blank
or a dash. An archived record says "Archived" in a neutral chip. A link that opens a new tab says "(opens in a new
tab)", out of sight after its label and in its tooltip.

**Sample text.** Text in examples, images and demonstrations is invented and obviously so: names like Pat Example and
Sam Sample, addresses at `example.com`, `example.org` or `example.net`, websites under `.example`.

## Components

Each entry gives what the component is for, its parts and their measurements, its variants and states, what it needs
for accessibility, and when to use it. Its images show it in every state.

### Layout

#### App shell

Images: full [light](references/images/layout/app-shell-full-light.png),
[dark](references/images/layout/app-shell-full-dark.png); rail
[light](references/images/layout/app-shell-rail-light.png), [dark](references/images/layout/app-shell-rail-dark.png).

The frame of every page, at least the window's height: a header across the top, the side nav down the left, and the
main content beside it.
- **Header**: `--header-height` high, sticking to the top, `--surface` with a bottom `--border`, 8px of padding at
  each side and 8px between items. At the left, the menu button (ghost, icon-only, `menu`), then the product: the
  28px `logo` and the product's name in `--text-lg` strong `--text`, 8px apart, never underlined. At the right end,
  whatever belongs to the whole application, ending with the avatar.
- **Side nav**: `--nav-width` wide, or `--nav-rail-width` as a rail; it sticks below the header, fills the rest of
  the window's height and scrolls on its own. Full from 900px and a rail below that; the menu button switches
  between the two and says whether the nav is expanded, and an application may pin either. The menu button needs
  script, so without it the nav stays at its default.
- **Main**: 24px of padding at the top and 32px at the sides and bottom; 16px all round below 900px.

#### Avatar

Images: [light](references/images/layout/avatar-light.png), [dark](references/images/layout/avatar-dark.png).

The signed-in user's initials at the right end of the header: a 32px circle of `--brand-subtle` with the initials in
`--brand-text`, `--text-xs` strong capitals. It is an image to assistive technology, named "Signed in as …". It is the
only avatar: grids, panels and histories show names.

#### Page header

Images: [light](references/images/layout/page-header-light.png), [dark](references/images/layout/page-header-dark.png).

The top of the main area: the breadcrumbs, then the title row, then an optional `--text-muted` line, 8px apart, with
24px below the whole header. The title row holds the h1 at the left and the page's actions at the right, centered on
one another, 12px apart and wrapping; the actions sit 8px apart, the one primary action last. On a page about one
record the title is the record's name with its status chip, and its version badge if it has one, 12px apart; its
muted line says who changed the record last and when.

#### Card

Images: [light](references/images/layout/card-light.png), [dark](references/images/layout/card-dark.png).

The surface every list, form and panel sits on: `--surface`, a 1px `--border`, `--radius-md` and `--shadow-card`.
- **Header** (optional): the card's title, an h2 at `--text-lg`, and at most one action at the right, 12px apart;
  12px by 16px of padding and a bottom `--border`.
- **Body**: 16px of padding.
- Cards in a column stand 16px apart. A component inside a card adapts to the card's width, at the card breakpoint
  (560px), not to the window's.

#### Hero

Images: [light](references/images/layout/hero-light.png), [dark](references/images/layout/hero-dark.png).

The opening of a landing page, such as the home page, and nowhere else: never on a list or a record's page; one per
page. In a card, an illustration 180px square beside the text and centered on it, 24px apart, with 24px of padding.
The text is an h2, a `--text-muted` paragraph that says what the application is for, and a row of the page's main
actions, the primary one first, 12px apart with 8px more above the actions. On a card narrower than 560px the
illustration shrinks to 120px and moves above the text, everything aligned left, with 16px of gap and padding. The
illustration is an image with colors of its own, the one place a color that is not a token appears; it must read on
`--surface` in light and dark mode, and it sets no color the page uses. Its alternative text is empty when the
heading already says what it shows, and real when it carries meaning.

#### Toolbar

Images: [light](references/images/layout/toolbar-light.png), [dark](references/images/layout/toolbar-dark.png).

The row at the top of a list's card: a filter box at the left and a segmented control at the right end, 12px apart,
with 12px by 16px of padding and a bottom `--border`. It wraps onto two lines on a narrow card, and holds nothing
else.

#### Tabs

Images: [light](references/images/layout/tabs-light.png), [dark](references/images/layout/tabs-dark.png).

Links to the sections of one page, each section with its own address, in a navigation landmark with a label.
- The tabs sit 4px apart in a row that wraps, over a bottom `--border`, with 16px below.
- **A tab**: 8px by 12px of padding, `--text-muted` strong, over a 2px bar that covers the row's border, transparent
  until the tab is current. `--text` under the pointer.
- **Current**, `aria-current="page"`: `--accent` text and an `--accent` bar.
- **Count** (optional), after the label, 8px away: a pill at least 20px wide, 6px of padding at each side,
  `--neutral-subtle` with `--text-muted` `--text-xs` figures of equal width.
- Use tabs to split one page into sections; moving between pages is the side nav's job.

### Navigation

#### Side nav

Images: [light](references/images/navigation/side-nav-light.png),
[dark](references/images/navigation/side-nav-dark.png); see the app shell for the rail.

The application's pages, in a navigation landmark labelled "Main": `--surface` with a right `--border`, 8px of padding
above and below, links 2px apart.
- **A link**: 40px high, 14px of padding at each side (which centers a 20px icon in the 48px rail), its icon in
  `--icon` and its label in `--text` 12px after it, never wrapping; `--surface-hover` under the pointer. Each link has
  a tooltip with its label, for the rail.
- **Current**, `aria-current="page"`: `--accent-subtle`, `--accent` text and icon, strong, and a 3px `--accent` bar
  along its inner left edge.
- **Group label** (optional), over a group of links: `--text-xs` strong capitals in `--text-muted`, spaced 0.04em,
  with 16px of padding above, 4px below and 14px at the sides.
- **Rail**: the labels and group labels leave the screen but stay in the accessibility tree.
- **Foot**: links that leave the application, such as to its source repository, pinned to the bottom of the nav under
  a top `--border`, 8px below it. Such a link opens a new tab, leads with the `external` icon, and says it opens a new
  tab (Conventions).

#### Breadcrumbs

Images: [light](references/images/navigation/breadcrumbs-light.png),
[dark](references/images/navigation/breadcrumbs-dark.png).

Where a page sits: an ordered list of links in a navigation landmark labelled "Breadcrumb", in `--text-sm`, 8px apart
and wrapping, with a "/" in `--text-disabled` 8px before each item after the first. The links are `--accent`; the
last item is the current page, as text in `--text` with `aria-current="page"`.

#### Menu

Images: closed [light](references/images/navigation/menu-light.png), [dark](references/images/navigation/menu-dark.png);
open [light](references/images/navigation/menu-open-light.png), [dark](references/images/navigation/menu-open-dark.png).

A short list of actions on one thing, opened by a button.
- **Placement**: 4px below its button, with its right edge on the button's, as a menu at the right end of a row or a
  header needs; above the button when there is no room below. If it cannot be placed by its button, it shows in the
  middle of the window.
- **The menu**: at least 180px wide, 4px of padding, `--surface`, a 1px `--border`, `--radius-md` and
  `--shadow-popup`.
- **An item**, a link or a button: `--control-height` high, the menu's full width, 8px of padding at each side,
  `--radius-sm`, an 18px icon in `--icon` and the label in `--text`, 8px apart, aligned left; `--surface-hover` under
  the pointer.
- **Groups** are separated by a 1px `--border` line with 4px above and below. An action that cannot be undone comes
  last, its label and icon in `--danger`.
- **The button** that opens it is usually ghost and icon-only with the `more` icon; its accessible name says whose
  actions they are ("Actions for Pat Example"), and it says whether the menu is open. On a list, each row has its own.
- **Closing**: Escape, or a click outside the menu.

#### Segmented control

Images: [light](references/images/navigation/segmented-control-light.png),
[dark](references/images/navigation/segmented-control-dark.png).

A few mutually exclusive views of the same content, such as a status filter: links in a navigation landmark with a
label.
- **The track**: `--neutral-subtle`, `--radius-sm`, 2px of padding and 2px between segments.
- **A segment**: 28px high, 12px of padding at each side, rounded 3px, `--text-muted` strong; `--text` under the
  pointer. An optional count follows 8px after the label, in `--text-xs` regular figures of equal width.
- **Current**, `aria-current="page"`: a `--surface` face, `--text` and `--shadow-card`.
- In a toolbar it sits at the right end.

#### Pager

Images: [light](references/images/navigation/pager-light.png), [dark](references/images/navigation/pager-dark.png);
for a list that grows, [light](references/images/navigation/pager-growing-light.png),
[dark](references/images/navigation/pager-growing-dark.png).

The foot of a list's card: a top `--border`, 12px by 16px of padding, `--text-sm`, in three columns 12px apart.
- **Left**, the range shown, in `--text-muted`: "Showing 1–10 of 128".
- **Middle**, Previous and Next, 8px apart. A Previous or Next with nowhere to go is a disabled button that is not a
  link (Buttons).
- **Right**, Rows per page: its `--text-muted` label and a select of 10, 25, 50 and 100 in figures of equal width,
  8px apart, as a form that applies as it changes, with an Apply button for when it cannot (Conventions).
- On a card narrower than 560px the range and Rows per page share the first line and Previous and Next take the
  second, centered.
- **A list that grows**, by Load more rather than by pages, has two parts: on the left how many rows show, of how
  many ("Showing 20 of 946"), and in the middle Load more, a secondary button, with nothing on the right. On a card
  narrower than 560px Load more stays on the first line, at the right. A list that stops short of every record says
  so in Load more's place once it gets there, in `--text-muted`, with a link to where the rest can be seen; on a
  narrow card that takes the second line, centered.
- **At the head.** A list that grows as its last row comes into view has its pager at the head of its card instead,
  between the card's header and the list, with its `--border` below it rather than above, and the card ends with the
  list. Anything under such a list would move away with each load, out of reach until the list stopped growing.

### Data display

#### Grid

Images: [light](references/images/data-display/grid-light.png), [dark](references/images/data-display/grid-dark.png).

A table of records, filling its card's width and scrolling sideways when the card is too narrow, with a caption
(visually hidden if need be) that says what the table holds and how it is sorted.
- **Header cells**: 40px high, 16px of padding at each side, a bottom `--border`, `--text-sm` strong `--text-muted`,
  aligned left, never wrapping.
- **Body cells**: at least `--row-height`, 8px by 16px of padding, a bottom `--border` (none under the last row),
  centered vertically. A row is `--surface-hover` under the pointer, unless it is selected or checked.
- **Title cell**, the first column after any selection column: a link to the record, strong, on its own line, then a
  second line in `--text-sm` `--text-muted`.
- **Sort header**: a sortable column's header is a link to the list sorted by it, followed 4px later by a 16px arrow.
  The sorted column says `aria-sort="ascending"` or `"descending"`, is `--text`, and shows its arrow; the others show
  theirs only under the pointer or with keyboard focus; a hidden arrow keeps its space, so nothing moves when it
  shows. The link keeps the header's color, `--text` under the pointer, never underlined.
- **Numbers**: a column of numbers aligns right, in figures of equal width.
- **Row actions**: the last cell, no wider than its content, aligned right with no padding above or below, holds the
  row's menu button.
- **Archived row**: a row whose record is archived, among rows that are not, is `--text-muted` in every cell but the
  actions, its title link too, with a neutral "Archived" chip; its other links keep their color.
- **Selected row**: the row the side panel shows says `aria-selected="true"` and is `--accent-subtle`.
- **Selection column**: first in each row, no wider than its content, with no padding on its right; a checkbox, or a
  radio in a picker, whose label is the row's title (strong, the whole title cell), so a click on the name selects
  the row. A checked row is `--accent-subtle`. The header cell holds a select-all checkbox with an accessible name,
  which needs script (Conventions). The bulk bar acts on the selection.
- **Read-only**: for records the user may see but not change, and for reports: no selection column and no row
  actions. The title still links to the record.
- **Busy**: while the list's request runs, its rows fade to half strength over 0.2s; they return at once when it
  ends.

#### Chip

Images: [light](references/images/data-display/chip-light.png), [dark](references/images/data-display/chip-dark.png).

A status label with a dot, never a button: 24px high, 10px of padding at each side, `--radius-full`, a 1px border in
the tone's line, the tone's subtle fill and text, `--text-xs` strong, never wrapping, with an 8px dot in the tone's
text color 6px before the label. Tones `success`, `danger`, `warning`, `info` and `brand` (`--brand-text` on
`--brand-subtle`, with a `--brand-subtle` line); neutral is the default.

#### Empty state

Images: [light](references/images/data-display/empty-state-light.png),
[dark](references/images/data-display/empty-state-dark.png).

What a list shows when it has nothing to show, centered, its parts 8px apart, with 32px of padding above and below
and 16px at the sides: one 40px icon in `--text-disabled`, one heading, one sentence in `--text-muted` saying why, and
at most one action, 8px further down, such as clearing a filter that matched nothing.

#### Fields

Images: [light](references/images/data-display/fields-light.png),
[dark](references/images/data-display/fields-dark.png).

Label-over-value pairs, as a description list with each pair grouped, filling as many columns at least 260px wide as
fit (two or three in a card, one in the side panel), 16px between rows and 24px between columns. A wide pair spans
the row.
- **Label**: `--text-sm` `--text-muted`. **Value**: 2px below, breaking anywhere rather than overflowing.
- Email and web addresses are links; another record is a related-record link; a value never set reads "Not set" in
  `--text-muted`; identifiers and numbers are in `--font-mono`.

#### Related-record link

Images: [light](references/images/data-display/related-record-link-light.png),
[dark](references/images/data-display/related-record-link-dark.png).

Another record, always as a link led 4px before by its type's 16px icon: `user` for a person, `users` for a group,
`building` for an organization. A link to an archived record stays a link, in `--text-muted`, followed 8px later by a
neutral "Archived" chip, in a field or a grid cell alike.

#### Side panel

Images: closed [light](references/images/data-display/side-panel-light.png),
[dark](references/images/data-display/side-panel-dark.png); open
[light](references/images/data-display/side-panel-open-light.png),
[dark](references/images/data-display/side-panel-open-dark.png).

A quick look at one record without leaving the page. It covers the right edge of the window, full height and 420px
wide (the window's width when that is less), `--surface` with a left `--border` and `--shadow-popup`. It slides in
from the right as it opens, and back out as it closes.
- **Header**: the record's name and its status chip, 8px apart one above the other, and a close button at the top
  right, 12px from them; 16px of padding and a bottom `--border`.
- **Body**: the record's fields, one column, with 16px of padding; it scrolls on its own.
- **Footer**: Open full record and the one primary action, at the right, 8px apart; 12px by 16px of padding and a top
  `--border`.
- **Behavior**: not modal; the page beside it stays usable. Escape, the close button or a click outside closes it.
  The row it shows is the grid's selected row. It is named by its heading, and the close button takes focus as it
  opens.
- The link that opens it is still a link to the record's page, so it works without script.

#### Version badge

Images: [light](references/images/data-display/version-badge-light.png),
[dark](references/images/data-display/version-badge-dark.png).

A version number in the brand violet, "v4": at least 32px wide, 22px high, 6px of padding at each side,
`--radius-sm`, `--brand-subtle` with `--brand-text`, `--font-mono` `--text-xs` strong, centered. The current version,
the one a record is at now, is filled: `--brand` with `--on-brand`.

#### Timeline

Images: [light](references/images/data-display/timeline-light.png),
[dark](references/images/data-display/timeline-dark.png).

A record's versions, newest first, as an ordered list numbered in reverse, so the newest has the highest number.
- **The line**: each entry is indented 32px; a 2px `--border` line runs down 11px from the left, from the first dot to
  the last. Each entry's dot is 14px, 5px from the left and 14px from the entry's top: `--surface` ringed 2px in
  `--border-input`, or filled with `--brand` for the current version.
- **An entry** opens and closes on its summary row: a 16px chevron in `--icon` that turns a quarter right when open,
  the version badge, what happened, and who and when at the right end in `--text-sm` `--text-muted`, 8px apart down
  and 12px across, wrapping; 8px by 12px of padding, `--radius-sm`, `--surface-hover` under the pointer.
- **Open**, below the summary with 8px by 12px of padding and 20px below: the diff, and the actions on that version
  in a row 8px apart, 12px between the two.
- The current version has no actions, and may start open.

#### Diff

Images: [light](references/images/data-display/diff-light.png), [dark](references/images/data-display/diff-dark.png).

Changed values before and after: a table with the columns Field, Before and After, listing only the fields that
changed, framed by a 1px `--border` at `--radius-md` that scrolls sideways when narrower than 440px.
- `--text-sm`. Field is 120px wide; Before and After share the rest equally. Every cell has 8px by 12px of padding
  and a bottom `--border` (none under the last row), aligned top, breaking anywhere rather than overflowing.
- The column headers are strong `--text-muted`; each field's name heads its row, strong.
- **The old value** is marked as a deletion: `--danger-subtle` behind `--text`, struck through in `--danger`. **The
  new value** is marked as an insertion: `--success-subtle` behind `--text`, not underlined.
- A missing value reads "Not set". The column headers carry the meaning; the colors only reinforce it.

### Inputs

#### Form

Images: [light](references/images/inputs/form-light.png), [dark](references/images/inputs/form-dark.png).

A whole form, as a page shows one: fields in a fields grid on a card, so a value keeps its place between reading and
editing, ending with the form action bar.

#### Field

Images: [light](references/images/inputs/field-light.png), [dark](references/images/inputs/field-dark.png).

One value in a form: the label, the control at the field's full width, an optional hint, then the validation
message, 4px apart.
- **Label**: `--text-sm` strong `--text`.
- **Hint**: `--text-sm` `--text-muted` under the control, which names it in its description.
- **Required**: the control says it is required, and its label is followed by " *" in `--danger`.
- **Invalid**: the control says `aria-invalid="true"`, takes the invalid look (Form controls), and names its message in
  its description. The message is `--text-sm` `--danger`, and takes no space when there is none.
- **Summary**: an error that belongs to no single field goes at the top of the form, 16px above it: `--danger-subtle`
  with `--danger` text, a 1px `--danger-line` border, `--radius-md`, 12px by 16px of padding, a list indented 20px.
  It takes no space when there are no errors.
- **Checked as you type**: the message is a polite live region, so a screen reader reads a message that appears or
  changes while the user is still in the field. Nothing else changes: the control and the message look as in any
  other state.
- **Disabled and read-only**: the control's own states (Form controls).
- **A group of controls**, such as a switch, choices, rows or a relation field, is a fieldset with no frame of its
  own, whose legend names it and looks like a label, 4px above what it names.

#### Switch

Images: [light](references/images/inputs/switch-light.png), [dark](references/images/inputs/switch-dark.png).

A yes-or-no value: a checkbox with the switch role, drawn as a track 36px by 20px at `--radius-full`, `--border-input`
when off and `--accent-fill` when on, with a 16px knob in `--on-accent` 2px inside it that slides 16px. Its label text
follows 8px after, and the label, at least `--control-height` high, is the click target. The focus ring goes round
the track. It sits in a fieldset that names the setting.

#### Choices

Images: [light](references/images/inputs/choices-light.png), [dark](references/images/inputs/choices-dark.png).

Several values from a short fixed list: a fieldset with its legend and a row of checkboxes drawn as pills, 8px apart
and wrapping. A pill is 28px high, 12px of padding at each side, with a 1px `--border-input` border at `--radius-full`
and its box 6px before the label. Checked: `--accent-subtle` with an `--accent` border and text. The focus ring goes
round the pill, not the box. For one value from a list, use a select.

#### Rows

Images: [light](references/images/inputs/rows-light.png), [dark](references/images/inputs/rows-dark.png).

Several values of one kind that the user writes, such as telephone numbers: a fieldset whose legend names them, a list
with a row for each value, 8px apart, then an Add button 4px below the list, at its own width.
- **A row**: its control, filling the row, and its Remove button, 8px apart. Remove is ghost and icon-only with the
  `x` icon, and says which it removes ("Remove phone number 2"); the control's accessible name counts it ("Phone
  number 2"). Every row's control has the same name, so the rows are sent as one list.
- **Add** is ghost with the `plus` icon and names what it adds ("Add a phone number"). It goes once the field holds
  the most it may; Remove shows only when there is more than one row.
- **Invalid**: the message belongs to the whole field, after the list, named in the fieldset's description; each row
  whose value is wrong is invalid and names the message too.
- Use it for values the user types, any number of them up to a limit. For values chosen from a short fixed list, use
  choices.

#### Relation field

Images: [light](references/images/inputs/relation-field-light.png),
[dark](references/images/inputs/relation-field-dark.png); picker
[light](references/images/inputs/relation-picker-open-light.png),
[dark](references/images/inputs/relation-picker-open-dark.png).

Another record as a value, in a fieldset with its legend.
- **The box** looks like a control: at least `--control-height` high, a 1px `--border-input` border, `--radius-sm`,
  `--surface`, 12px of padding at the left. It holds the chosen record as a related-record link, with its id sent as
  the field's value, and at the right end, 8px from the link, a secondary Change button joined to the box: no border
  of its own but a `--border-input` line at its left, square on the left and rounded on the right, filling the box's
  height inside its border. With nothing chosen it holds "Not set" in `--text-muted` and a Choose button.
- **The picker**, which Change and Choose open, is a dialog built from the list components: a filter box and a grid
  whose selection column holds radios. It lists only the records that can be chosen.

#### Filter box

Images: [light](references/images/inputs/filter-box-light.png), [dark](references/images/inputs/filter-box-dark.png).

A search form that narrows a list as you type: a form with the search role, holding a search input with an accessible
name, and the 18px `filter` icon inside the input, 10px from its left, in `--text-muted`, with 36px of padding at the
input's left to clear it. It grows from 240px and stops at 360px, Apply included. Its placeholder reads "Filter …",
never "Search …", and its icon is `filter`, not `search`. It sends on Enter, and has an Apply button 8px after the
input for when it cannot apply as you type (Conventions).

#### Form action bar

Images: [light](references/images/inputs/form-action-bar-light.png),
[dark](references/images/inputs/form-action-bar-dark.png).

The last thing in a form, 16px below the fields, sticking 16px above the bottom of the window while a long form
scrolls: `--surface`, a 1px `--border`, `--radius-md`, `--shadow-popup`, 12px by 16px of padding, buttons 8px apart
and wrapping. Actions on the whole record sit at the left as ghost buttons; Cancel and the one primary action sit at
the right end.

### Feedback

An action that can be undone runs at once and says so in a toast with Undo. Only an action that cannot be undone asks
first, in a confirm dialog. A state that lasts is an alert on the page.

#### Dialog

Images: closed [light](references/images/feedback/dialog-light.png), [dark](references/images/feedback/dialog-dark.png);
open [light](references/images/feedback/dialog-open-light.png), [dark](references/images/feedback/dialog-open-dark.png).

A modal dialog, 560px wide and never nearer than 16px to the window's sides, at most the window's height less 64px:
`--surface`, a 1px `--border`, `--radius-md` and `--shadow-popup`, over a `--scrim`.
- **Header**: the title, an h2 at `--text-lg`, and a close button (ghost, icon-only, `x`) at the right; 12px by 16px of
  padding and a bottom `--border`.
- **Body**: 16px of padding; it scrolls on its own.
- **Footer**: the buttons, at the right, 8px apart and wrapping, ending with the one primary action; 12px by 16px of
  padding and a top `--border`.
- **Behavior**: it is named by its title. Focus moves into it as it opens and returns to what opened it as it closes;
  Escape closes it, and the page behind cannot be used.
- Use it for a short task that must finish before the page goes on, such as a short form or a picker.

#### Confirm dialog

Images: open [light](references/images/feedback/confirm-dialog-open-light.png),
[dark](references/images/feedback/confirm-dialog-open-dark.png); several records
[light](references/images/feedback/confirm-dialog-many-open-light.png),
[dark](references/images/feedback/confirm-dialog-many-open-dark.png); not allowed
[light](references/images/feedback/confirm-dialog-not-allowed-open-light.png),
[dark](references/images/feedback/confirm-dialog-not-allowed-open-dark.png); discarding changes
[light](references/images/feedback/confirm-dialog-discard-open-light.png),
[dark](references/images/feedback/confirm-dialog-discard-open-dark.png); over the page
[light](references/images/feedback/confirm-dialog-over-page-light.png),
[dark](references/images/feedback/confirm-dialog-over-page-dark.png).

Only for an action that cannot be undone: a dialog 420px wide, described by its message.
- **Title**: asks the question and names the record ("Delete Pat Example permanently?").
- **Body**: a 40px circle in the dialog's tone (`--danger-subtle` with a 20px `--danger` icon by default; warning or
  info), aligned to the top of the message beside it, 12px apart; the message's parts 8px apart, and any list
  indented 20px.
- **Footer**: the safe choice first, taking focus as the dialog opens, and the destructive danger button last.
- **Not allowed**: when the action cannot be done at all, the dialog says why and what to do instead, with a warning
  icon, a single primary OK button, and no way to force it.

#### Preview dialog

Images: [light](references/images/feedback/preview-dialog-open-light.png),
[dark](references/images/feedback/preview-dialog-open-dark.png).

Shows the result of a change before it is made: a dialog whose body, its parts 12px apart, says what will happen,
warns in a warning alert about anything that cannot be carried out, and shows the diff from now to the result. What
it previews can be undone, so its button is primary, not danger.

#### Busy bar

Images: [light](references/images/feedback/busy-bar-light.png), [dark](references/images/feedback/busy-bar-dark.png).

A 2px line along the bottom edge of a card's toolbar, over its border, that runs while a request from the card is
running: a segment 30% of the card's width in `--accent` crosses from left to right every 1.1s, and nothing shows when
idle. The card says `aria-busy="true"` while it runs, the grid's rows fade to half strength, and the toolbar stays
usable. A button that sent a request shows its own spinner instead.

#### Progress bar

Images: [light](references/images/feedback/progress-bar-light.png),
[dark](references/images/feedback/progress-bar-dark.png).

How far a task with a known end has got, such as a file being sent or a job's steps: a track 8px high at
`--radius-full`, as wide as the field it sits in, in `--neutral-line`, with the part done in `--accent` from its left,
rounded at its end too.
- **As a field**: its label sits 4px above it, as a field's does, and says what is under way and how far it has got,
  in words or figures ("Sending the file: 42%", "Working: 3 of 10 steps done"), so the bar is never the only way to
  tell.
- **It moves only as the task does**, with no motion of its own. Before anything is done it is the empty track; once
  the task ends, what the task answers takes its place.
- **Accessibility**: it has the progressbar role, named by its label, with its value and its maximum, so a screen
  reader reads how far it has got when asked. It is not a live region, which would speak at every step; what ends the
  task says so, in a toast. In forced colors the track keeps a border and the part done a fill (Forced colors).
- Use it only when how far the task has got is known. A request whose end cannot be measured shows the busy bar, or
  its button's spinner.

#### Bulk bar

Images: [light](references/images/feedback/bulk-bar-light.png), [dark](references/images/feedback/bulk-bar-dark.png).

What can be done to the rows checked in a grid's selection column: a labelled region after the grid, in its card,
centered and as wide as its content (at most the card's width less 32px), 12px from what is above and below, sticking
16px above the bottom of the window. `--surface`, a 1px `--border`, `--radius-md`, `--shadow-popup`, 8px of padding
(16px at the left), its parts 8px apart and wrapping.
- It appears with the first checked row and leads with the count, strong, in figures of equal width ("2 selected").
- Then Clear, which unchecks every row, and the actions, each sending the checked rows; one that cannot be undone is
  danger and comes last.

#### Alert

Images: [light](references/images/feedback/alert-light.png), [dark](references/images/feedback/alert-dark.png).

A banner between the page header and the content about a state that lasts, 16px above the content: the tone's subtle
fill, a 1px border in the tone's line, `--radius-md`, 12px by 16px of padding, its parts 12px apart and wrapping.
- A 20px icon in the tone's text color, 1px lower than the text's top; the content, taking the room (at least 280px
  before it wraps), its title and text 2px apart; then at most one or two buttons, 8px apart.
- Tones `info`, `success`, `warning` and `danger`; neutral is the default. The text keeps `--text`; the tone colors
  the icon, fill and line.
- A failure that needs attention now has the alert role; the rest have the status role.

#### Toast

Images: [light](references/images/feedback/toast-light.png), [dark](references/images/feedback/toast-dark.png).

A short message about an action that just ran.
- **Where**: toasts appear in one polite live region with the status role, which sits empty in every page until a
  toast arrives. It is fixed 24px above the bottom of the window, centered, never nearer than 16px to the
  window's sides, and stacks its toasts 8px apart. It lets clicks through around its toasts.
- **A toast**: at least 44px high, `--inverse` with `--on-inverse` text, `--radius-md`, `--shadow-popup`,
  8px of padding (16px at the left), its parts 12px apart. The message takes the room. Undo, when the action can be
  undone, is a ghost button in `--accent-on-inverse`, washed with `--on-inverse` at 12% under the pointer; like every
  action it works without script (Conventions). Dismiss is ghost and icon-only with the `x` icon in `--on-inverse`,
  washed the same way under the pointer, and needs script (Conventions).
- **Motion**: it rises 8px as it fades in, and fades away by itself after 8 seconds, waiting while the pointer or
  the keyboard is on it.
