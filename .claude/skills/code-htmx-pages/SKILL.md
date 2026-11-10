---
name: code-htmx-pages
description: How ASP.NET Core Razor Pages and HTMX 4 work together. Use whenever adding hx- attributes to a Razor page or partial view, writing a page handler that answers HTMX requests, choosing whether a handler returns a partial or the whole page, wiring history, forms, validation or antiforgery for HTMX, testing a page model against HTMX's headers, or reviewing any of those. Covers the HTMX 4 changes that break HTMX 2 habits, loading HTMX, one handler answering with a partial or the whole page by HX-Request-Type, list views kept in the query string, Back and Forward, testing, forms that post and what a save answers with HTMX and without it, one answer that updates the page and a toast, actions on a row that draw the list again as it was, a field that works without HTMX and is enhanced as the page loads, an action that cannot be undone, asked for first in a dialog that works without HTMX, an action that takes its row out of a list and where the keyboard goes then, and the patterns still open (fields checked as you type, polling). Written against HTMX 4.0.1. Draft; the status table says which sections are proven.
owner: Matt
version: 0.9.0
visibility: public
---

# HTMX 4 in Razor Pages

How a Razor Pages application uses HTMX 4: which attributes go in the markup, how a page handler tells an HTMX
request from a browser's and what it sends back, how history and forms behave, and how to test it. It is a baseline
to copy into any repository that pairs the two. A copy belongs to the repository that takes it; nothing keeps the two
in step.

It is not the HTMX reference, which is linked throughout and is the place to look for anything this skill does not
cover. It says nothing about C# style (`code-csharp-standards`), tests in general (`code-csharp-mstest`) or how a
page looks (the repository's brand skill, if it has one).

**Every page works without HTMX.** Links are links, filters are GET forms, and actions are buttons in forms that
post. HTMX only narrows what a response replaces and keeps the address bar in step. A page that cannot be built that
way is a decision to make out loud, never a default.

## Status: draft 0.9.0, HTMX 4.0.1

| Section | State |
|---|---|
| 1. HTMX 4 against HTMX 2 habits | Checked |
| 2. Loading HTMX | Proven |
| 3. One handler, two answers | Proven |
| 4. A list in the query string | Proven |
| 5. Back and Forward | Proven |
| 6. Testing | Proven |
| 7. Forms that post | Proven |
| 8. More than one target | Proven |
| 9. Actions on a row | Proven |
| 10. A field HTMX enhances | Proven |
| 11. An action that cannot be undone | Proven |
| 12. An action that takes its row out of the list | Proven |
| 13. Still open | Open |

- **Proven:** built in a Razor Pages application on HTMX 4.0.1, and shown working by a unit test or in the browser.
- **Checked:** read in the 4.0.1 source and the reference, and true of that version, but not yet built.
- **Open:** a question, with what is known so far. Not a rule until it is proven.

A section moves up only when that is true of it, in a session Matt attends.

## Sources

- **The HTMX 4 site**: the [docs](https://four.htmx.org/docs), the [reference](https://four.htmx.org/reference),
  [What's new in htmx 4](https://four.htmx.org/docs/whats-new-in-htmx-4) and the
  [patterns](https://four.htmx.org/patterns), under the Zero-Clause BSD license. The site is built from the `four`
  branch of [bigskysoftware/htmx](https://github.com/bigskysoftware/htmx/tree/four), which runs ahead of any release,
  and some pages still describe HTMX 2: the [quirks](https://four.htmx.org/docs/quirks) page says error responses are
  not swapped and names config keys that 4 renamed. When the site and the pinned file disagree, the pinned file wins.
  Its unminified source is `https://unpkg.com/htmx.org@<version>/dist/htmx.js`; citations below to "4.0.1 source"
  name a method in that file.
- **Chris Woodruff's [htmx Razor Pages workshop](https://github.com/cwoodruff/htmx-razor-workshop)**, MIT. Good on the
  Razor Pages side, and sections 3, 6 to 9 and 13 take several ideas from it, each credited by chapter: a comment that
  states each partial's contract, what each request gets back, handlers named for what they do, a row action that
  draws the list again, checking a field as you type, form rows added by the server, polling the server stops, and
  what to look for in the network panel. It is written for htmx 1.9.12, and some of its central code does not carry
  over to 4. Its `IsHtmx()` tests `HX-Request`, which HTMX 4 also sends when Back and Forward need the whole page
  (section 3). It answers errors with 200 and `HX-Retarget`, because htmx 1 would not swap a 4xx (section 7). Its
  scripts load at the end of `<body>` (section 2), and its `Fragment()` helper repeats what `PageModel.Partial`
  already does. Take the Razor Pages shape from it and the HTMX details from this skill.

Everything here is in our own words; no text or code is copied from either source.

## 1. HTMX 4 against HTMX 2 habits (checked)

HTMX written from memory is usually HTMX 2, and most of it still runs on 4 while doing something else. Check this
table first when HTMX behaves oddly.

| HTMX 2 habit | HTMX 4 | Where it is stated |
|---|---|---|
| An attribute on a parent applies to its children | Only when written with `:inherited`, as in `hx-boost:inherited="true"`. `:append` adds to an inherited value. `hx-inherit` and `hx-disinherit` are gone | [Attribute inheritance](https://four.htmx.org/docs#attribute-inheritance) |
| `this` is the element that sent the request | In an inherited attribute, `this` is the element that declares it: `hx-target:inherited="this"` on a container makes every request inside it replace the container | 4.0.1 source, `#findThisElements` |
| A 4xx or 5xx response is not swapped | Every response is swapped except 204 and 304; 4xx and 5xx also raise `htmx:response:error`. `hx-status:422="…"` sets the target or swap for one code | [hx-status](https://four.htmx.org/reference/attributes/hx-status) |
| `hx-delete` sends the enclosing form | Like `hx-get`, it sends nothing from the form; add `hx-include="closest form"` | [What's new](https://four.htmx.org/docs/whats-new-in-htmx-4) |
| Back restores a snapshot from local storage | Back and Forward fetch the URL again and swap it into `<body>`, or into `[hx-history-elt]`; nothing is stored | [Browser history](https://four.htmx.org/docs#browser-history-support) |
| `htmx:afterSwap`, `htmx:afterOnLoad`, `htmx:load`, `htmx:configRequest` | `htmx:after:swap`, `htmx:after:request`, `htmx:after:process`, `htmx:config:request`: every event is named `htmx:phase:action` | [Events](https://four.htmx.org/reference/events) |
| The `HX-Trigger` request header names the element | `HX-Source`, written `tag#id`; `HX-Target` is `tag#id` too; `HX-Trigger-Name` is gone. New: `HX-Request-Type` (section 3) | [Request headers](https://four.htmx.org/reference/headers) |
| `HX-Trigger-After-Swap` and `HX-Trigger-After-Settle` response headers | Gone; `HX-Trigger` remains | [HX-Trigger](https://four.htmx.org/reference/headers/HX-Trigger) |
| `hx-disable` skips processing; `hx-disabled-elt` disables controls during a request | `hx-ignore` skips processing; `hx-disable` disables controls during a request. Rename `hx-disable` first, then `hx-disabled-elt` | [What's new](https://four.htmx.org/docs/whats-new-in-htmx-4) |
| `hx-ext`, `hx-vars`, `hx-params`, `hx-prompt`, `hx-request`, `hx-history` | Removed. An extension loads from its own `<script>` tag; `hx-vals` with a `js:` prefix replaces `hx-vars` | [What's new](https://four.htmx.org/docs/whats-new-in-htmx-4) |
| `from:(closest form)`; `click queue:all` | `from:'closest form'`, in single quotes; queuing belongs to `hx-sync` alone | [hx-trigger](https://four.htmx.org/reference/attributes/hx-trigger) |
| A request never times out | It times out after 60 seconds (`htmx.config.defaultTimeout`) | [What's new](https://four.htmx.org/docs/whats-new-in-htmx-4) |
| Out-of-band content swaps before the main content | The main content swaps first, then out-of-band content and `<hx-partial>` elements, in document order | [Multi-target updates](https://four.htmx.org/docs#multi-target-updates) |

Unchanged, and worth knowing: the default swap is still `innerHTML`; a boosted link or form with no target still
swaps into `<body>` and pushes its URL; and an `hx-get` from an element that is not a form still sends only its own
value, not its form's.

The HTMX package ships an upgrade checker that lists HTMX 2 attributes and event names in a folder of templates:
`npx htmx.org@<version> upgrade-check -- <folder>`. Its default file types leave out Razor; `--ext .cshtml` adds it.
Not yet tried on a Razor project.

## 2. Loading HTMX (proven)

- **Pin one exact version and serve it from `wwwroot`.** Download the release's `dist/htmx.min.js` once, keep it as
  `wwwroot/js/htmx.min.js`, and record the version and its source address in the README. A pinned file cannot change
  under the application, and section 1 shows how much a version can change.
- **Every script goes in `<head>`, with `defer`.** Back and Forward, and a boosted link with no target, swap a new
  `<body>` in, and HTMX runs every `<script>` in swapped content (4.0.1 source, `#processScripts`). A script in the
  body runs again on each such swap, and HTMX itself would load a second time. `defer` keeps the scripts from
  blocking the page, and 4.0.1 starts on `DOMContentLoaded`, or at once when the document is already parsed. The
  [quirks](https://four.htmx.org/docs/quirks) page warns that loading HTMX other than by a plain blocking tag is
  unreliable, mostly where a bundler is involved; `defer` has given no trouble so far.
- **A page's `Scripts` section is in the body.** The default layout renders `@RenderSectionAsync("Scripts")` at the
  end of `<body>`, so a script a page puts there runs again on each body swap. Move it into `<head>`, or write it so
  that running twice does no harm.
- **The application's own script listens on `document`.** A listener on an element is lost when a swap replaces the
  element. Delegate events from `document`, and refresh any state the script keeps on `htmx:after:swap`.

## 3. One handler, two answers (proven)

A page that HTMX updates in place answers one URL two ways: with the part being replaced when HTMX asks for it, and
with the whole page otherwise.

- **Decide on `HX-Request-Type`, never on `HX-Request`.** HTMX 4 sends `HX-Request: true` on every request it makes,
  including the ones that need the whole page: Back and Forward, a boosted link that swaps the body, and any request
  with `hx-select`. Those send `HX-Request-Type: full`, and a request that will swap one element sends `partial`
  ([HX-Request-Type](https://four.htmx.org/reference/headers/HX-Request-Type)). Return the partial only for `partial`;
  return the page for `full` and for a request with no HTMX headers at all.
- **Send `Vary: HX-Request-Type` from both branches**, so a cache keeps the two answers apart
  ([Caching](https://four.htmx.org/docs#caching)).
- **The page renders the same partial.** The page's `.cshtml` places it with `<partial name="_ProductList"
  model="Model" />` and the handler returns `this.Partial("_ProductList", this)`, so the two answers cannot differ.
  The partial's model is the page model.
- **The partial's root element is the target, and is replaced whole.** Give it an `id` and swap it with `outerHTML`,
  so that its attributes, counts and state come back with every response. Because the HTMX attributes sit on that
  root, each new copy carries them. A comment at the top of the partial says which element it replaces, how, and
  which handlers return it, so the contract can be read where it is kept (the workshop's "fragment inventory", in
  "Checkpoint: Make It Production-Livable").

```csharp
// HTMX 4 says on every request whether it will swap one element ("partial") or the whole body ("full").
private const string HtmxRequestTypeHeader = "HX-Request-Type";
private const string PartialRequestType = "partial";

public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
{
    ProductCollectionRequest request = CreateRequest(this.Input);
    this.Products = await this._products.ListAsync(request, cancellationToken).ConfigureAwait(false);

    // One URL, two bodies: a cache must keep them apart by the header that chooses between them.
    IHeaderDictionary responseHeaders = this.Response.Headers;
    responseHeaders.Vary = HtmxRequestTypeHeader;
    if (IsHtmxSwap(this.Request))
    {
        return this.Partial("_ProductList", this);
    }

    return this.Page();
}

private static bool IsHtmxSwap(HttpRequest request)
{
    IHeaderDictionary headers = request.Headers;
    return headers[HtmxRequestTypeHeader] == PartialRequestType;
}
```

## 4. A list in the query string (proven)

- **The list's whole state is in the query string**: filter, status, sort, page size, and where the page lies. Every
  view of the list has its own URL; the GET handler reads it through one bound `InputModel` with
  `[BindProperty(SupportsGet = true)]`, and a value it does not recognize falls back to its default. A URL that HTMX
  pushes into history must load the whole page on its own
  ([Browser history](https://four.htmx.org/docs#browser-history-support)), and this is what makes that true. Where
  the page lies is a cursor, `after=<id>` or `before=<id>`, not a page number: key-based paging, which seeks to the
  page through an index instead of skipping the rows before it. Paging this way is not HTMX's concern, but it is the
  pattern to copy, and every link and form in the list carries the cursor the same way it carries the rest.
- **The list's container declares the swap once, inherited:**

  ```html
  <div id="product-list"
       hx-boost:inherited="true" hx-target:inherited="this" hx-swap:inherited="outerHTML"
       hx-indicator:inherited="#product-list">
  ```

  Every link and GET form inside it becomes a request that replaces the container, sends `HX-Request-Type: partial`
  because its target is not the body, and pushes its URL, as boosted requests do. Without HTMX they are the same links
  and forms, written with `asp-page` and `asp-all-route-data` like any others.
- **A link that leaves the list says `hx-boost="false"`.** Otherwise it inherits the container as its target, and the
  next page would be swapped into the list.
- **Filter as you type** with a search input inside the list's GET form:

  ```html
  <input type="search" id="product-filter" name="filter" value="@Model.Input.Filter"
         hx-get="@Url.Page("./Index")" hx-trigger="input changed delay:300ms, search"
         hx-include="closest form" hx-push-url="true">
  ```

  The request goes 300 ms after the last key, and `search` catches the box's clear button. The input sends only its own
  value, so `hx-include` adds the form's hidden inputs, which carry the rest of the list's state; pressing Enter
  without HTMX submits the same values. An input is not a boost target, so it pushes its own URL. The `id` lets HTMX
  put focus and the caret back into the new copy of the box after the swap (4.0.1 source, `#insertContent`).
- **Show that a request is running** by naming the container in `hx-indicator`. HTMX adds the class `htmx-request` to
  it while the request runs, and CSS keyed on that class dims the rows or runs a progress bar.

## 5. Back and Forward (proven)

- Back or Forward to a URL that HTMX pushed makes HTMX GET that URL with `HX-Request-Type: full` and
  `HX-History-Restore-Request: true`, and swap the response over `<body>` (4.0.1 source, `#restoreHistory`). The
  handler in section 3 already answers with the whole page; nothing more is needed.
- Use `HX-History-Restore-Request` only to skip a side effect, such as counting a page view, never to send different
  HTML ([HX-History-Restore-Request](https://four.htmx.org/reference/headers/HX-History-Restore-Request)).
- Because the body is replaced, everything in it loses its listeners and runs its scripts again: section 2.
- Three alternatives exist, and a server-rendered page needs none of them: `hx-history-elt` restores one element
  instead of the body, `htmx.config.history = "reload"` reloads the page, and the
  [hx-history-cache](https://four.htmx.org/extensions/hx-history-cache) extension brings back a client-side cache.

## 6. Testing (proven)

- **A page model's HTMX branch is a unit test.** Give the page model a `PageContext` over a `DefaultHttpContext`, set
  the headers HTMX would send, call the handler and assert on the result. Three kinds of request cover it, and an enum
  of the three keeps each kind's headers in one place:

  | Kind | Headers | Result |
  |---|---|---|
  | None | no HTMX headers | `PageResult` |
  | Partial | `HX-Request: true`, `HX-Request-Type: partial` | `PartialViewResult` with the partial's name |
  | History restore | `HX-Request: true`, `HX-Request-Type: full`, `HX-History-Restore-Request: true` | `PageResult` |

  One more test checks that the response's `Vary` header names `HX-Request-Type`. `this.Partial(…)` reads the page's
  `ViewData` and `MetadataProvider`, so the test sets both: a `ViewDataDictionary` on the `PageContext`, and an
  `EmptyModelMetadataProvider` for each.
- **A handler that posts is tested the same way** (section 7). A plain post that succeeds gives a
  `RedirectToPageResult`; an HTMX one gives a `StatusCodeResult` of 204, with `HX-Location` among the response's
  headers; a refused one gives a `PageResult` or a `PartialViewResult`, with its status on the response. Two things
  the framework would have done need the test's hand:
  - `this.Url.Page("./Details", …)`, which writes the `HX-Location` address, needs `PageModel.Url`. A stand-in
    `IUrlHelper` will do, but its `ActionContext` must name the current page as the `page` route value: `Url.Page`
    resolves a relative name such as `./Details` against it, and throws without it.
  - A value the test posts and means to be valid: `ModelState.SetModelValue` leaves its entry unvalidated, and
    `ModelState.IsValid` is false while any entry is, so mark it with `MarkFieldValid`.
- **What a unit test cannot see is checked in the browser:** which element a swap replaces, inheritance, focus, and
  the URL that is pushed. Read the request headers and the response body in the developer tools' network panel: the
  body holds the target's own outer element, and the target's `id` appears once on the page (workshop, "Checkpoint:
  Troubleshooting + Pattern Review"). Then press Back and Forward after a few swaps.

## 7. Forms that post (proven)

Built as an edit form: one record's fields, posted to the page's own `OnPostAsync`, which HTMX sends from the form
itself.

- **What each request gets back** (the plan the workshop gives in "Checkpoint: Make It Production-Livable", with the
  answers HTMX 4 needs):

  | Request | Answer |
  |---|---|
  | A plain GET | `Page()` |
  | A plain POST that succeeds | `RedirectToPage()`, so that refreshing the next page does not post again |
  | A plain POST that is refused | `Page()`, with the messages and a status that says why |
  | An HTMX POST that succeeds | 204 No Content, with `HX-Location` naming the next page |
  | An HTMX POST that is refused | The form's partial, with the messages and the same status |

  The handler tells the two apart by section 3's test: the form's target is not the body, so HTMX sends
  `HX-Request-Type: partial`.
- **The form posts to one address, with HTMX or without it.**

  ```cshtml
  @{ string? action = Url.Page("./Edit", new { id = Model.Id }); }
  <form method="post" action="@action" asp-antiforgery="true" novalidate
        hx-post="@action" hx-target="#product-edit" hx-swap="outerHTML"
        hx-indicator="find button[type=submit]" hx-disable="find button[type=submit]">
  ```

  HTMX sends every field in the form, the antiforgery token among them, and Razor Pages checks the token as it would
  for a plain post. An `hx-post` pushes no URL unless told to (4.0.1 source, `#resolveHistoryAction`), so a refused
  save leaves the address bar and the history alone.
- **Antiforgery.** The form tag helper writes the token into a `<form method="post">` whose address it builds with
  `asp-page`, but not into one whose `action` is written by hand; `asp-antiforgery="true"` puts it back. Without the
  token Razor Pages answers 400 with an empty body, and HTMX 4 swaps an empty answer like any other (4.0.1 source,
  `swap`: empty content is swapped unless `swapEmpty` says otherwise), so the form vanishes without a message. When a
  form disappears on save, look for its token first.
- **A refused save answers 422 or 409, with the form's partial.** HTMX 4 swaps every status but 204 and 304
  (section 1), so the handler needs no `HX-Retarget` or `HX-Reswap`: 422 Unprocessable Entity when a value must change,
  409 Conflict when the record has moved on since the form was filled. A plain post gets `Page()` with the same status,
  which a browser shows like any page. HTMX raises `htmx:response:error` for each (4.0.1 source, `#issueRequest`), and
  the browser's console lists each one as a resource that failed to load; both are expected. `this.Partial(name, this)` hands the page's
  `ModelState` to the partial (ASP.NET Core 10 source, `PageModel.Partial`), so the messages render with no helper.
- **The swapped element holds everything a refused save can change.** That is more than the form: a page header that
  shows the record's name, version and who changed it last belongs inside it too, so that a save refused because the
  record moved on shows the record as it is now. The handler reads the record again before it answers.
- **The cursor goes to the first field to fix.** After a swap HTMX focuses the first element with `autofocus` in the
  new content (4.0.1 source, `#handleAutoFocus`), as a browser does when it loads a page. The framework's tag helpers
  mark an invalid control with the class `input-validation-error`, but write no `aria-invalid`, no
  `aria-describedby` naming the message, and no `autofocus`. A tag helper of the application's own on `input`,
  `select` and `textarea` with `asp-for` can add all three from the view's `ViewData.ModelState`, the dictionary the
  framework's tag helpers read, running after them (theirs run at the order -1000) and giving `autofocus` to the first
  invalid control in the response only. A second, on `span` with `asp-validation-for`, gives each message the `id`
  the control names.
- **A save that succeeds answers 204 with `HX-Location`, never a redirect.** `fetch` follows a redirect itself (4.0.1
  source, `#issueRequest` leaves `redirect` at its default), so HTMX would swap whatever the next page returns into the
  form's target. `HX-Location` makes HTMX GET the address as a boosted link would: into `<body>`, with
  `HX-Request-Type: full`, so that the next page's handler returns the whole page, then pushing the URL and setting the
  title (4.0.1 source, `#handleHeadersAndMaybeReturnEarly`, which reads the header whatever the status and before any
  swap). The status 204 says there is nothing to swap. `HX-Redirect` would reload the whole page instead.

  ```csharp
  private IActionResult ShowProduct(long id)
  {
      if (!IsHtmxSwap(this.Request))
      {
          return this.RedirectToPage("./Details", new { id });
      }

      // fetch would follow a redirect itself, and HTMX would swap the next page into the form.
      string url = this.Url.Page("./Details", new { id })
          ?? throw new InvalidOperationException("The product's page has no address.");
      IHeaderDictionary headers = this.Response.Headers;
      headers["HX-Location"] = url;
      return this.StatusCode(StatusCodes.Status204NoContent);
  }
  ```

  `PageModel` has no `NoContent()`; `StatusCode` gives the same result.
- **A save that another save overtook.** When the form carries the record's version, for optimistic concurrency, and
  the record has moved on, the form comes back with 409, keeps the values being edited, and starts from the saved
  version, so that saving again replaces it on purpose. Remove the posted version from `ModelState` before setting the
  new one on the input model: a tag helper writes the value that was posted ahead of the model's.
- **A submit button that is busy.** The form is the request's source, so HTMX marks the form with `htmx-request`
  unless told otherwise. `hx-indicator="find button[type=submit]"` marks the button instead, and
  `hx-disable="find button[type=submit]"` disables it while the request runs, which also stops Enter in a field from
  sending the form again: HTML's implicit submission does nothing when the default button is disabled. `find` is HTMX
  4's selector for the first matching descendant (4.0.1 source, `#findAllExt`). `hx-disable` costs the focus: a
  disabled button loses it, and HTMX can put back only an element that had it (section 8). On a form whose answer
  sets `autofocus` that does not matter; on a one-button form whose answer replaces the button, leave `hx-disable`
  off, and let a busy style keyed on `htmx-request` stop a second click.
- **A button that acts on the record posts to a named handler**, named for what it does. A browser form can only GET
  or POST, so Archive is a one-button form posting to `?handler=Archive`, answered by `OnPostArchiveAsync`, and
  Restore is a handler of its own beside it. `Url.Page("./Details", "Archive", new { id })` writes the address for
  both `action` and `hx-post`, and the handler binds the route's values as the GET handler does. `?handler=Archive` in
  the network panel leads straight to the method (workshop, "Checkpoint: Make It Production-Livable"). HTMX 4's
  `hx-delete` would leave out the form's fields (section 1), and the token with them, so the form posts.
- **Handlers that several pages share live on a base page model.** Razor Pages finds the handlers an abstract base
  class declares on every page that inherits it, so the tabs of one record, each a page with its own URL, share
  Archive and Restore while each posts to its own address and answers with its own content.
- **A form inside a `<dialog>` posts like any other.** The dialog opens with `commandfor` and `command="show-modal"`,
  which need no script, and its form says `hx-post` as any form does. When the answer replaces the element that holds
  the dialog, the modal dialog leaves the document with it, and nothing is left inert. Without HTMX the post
  redirects, and the next page loads with the dialog shut.

## 8. More than one target (proven)

Built as Archive and Restore on a record's page: one answer replaces the page's content and puts a toast with Undo in
a region of the layout.

- **One response, two places.** The answer renders the main content first, then the extra content inside
  `<hx-partial hx-target="#toasts" hx-swap="innerHTML">`. HTMX 4 rewrites each `<hx-…>` tag as a
  `<template hx type="…">` before it parses the answer (4.0.1 source, `#makeFragment`), swaps the main content into
  the request's target, then each partial into its own target with its own swap (`#processPartials`). Razor writes
  `<hx-partial>` through unchanged, so no `<template>` needs writing by hand. A partial names its own target and swap,
  so the extra content needs no `id` that matches an element on the page, as an `hx-swap-oob` element would
  ([Multi-target updates](https://four.htmx.org/docs#multi-target-updates)).
- **The page renders the same pieces.** The answer is a partial that holds two partials: the main content, which the
  page renders too, and the toast, which the page renders into the layout's region through a section
  (`@section Toasts`). The region sits empty in the layout, so a live region (`role="status"`) is in place before any
  toast arrives, and screen readers announce each one.
- **Replace the region's content, never add to it.** `hx-swap="innerHTML"` leaves one toast at a time, so an Undo
  never outlives the action it undoes.
- **Without HTMX the extra content waits for the redirect.** A plain post answers with `RedirectToPage()`, as section
  7 says, and leaves the toast's message in `TempData`; the page's GET handler reads it there, and a read removes it,
  so refreshing the page does not show it again. Undo is a real form, so it works the same way.
- **The focus survives the swap by `id`.** Before a swap HTMX notes the focused element if it has an `id`, and after
  it focuses the element in the document with that `id` (4.0.1 source, `#insertContent`). Give Archive and Restore
  one `id` between them, and the keyboard stays on the button that undoes what was just done.
- **An Undo that needs more than the record's identifier carries it in its address.** A roll-back's Undo is another
  roll-back, so its address holds the version to go back to and the version it starts from
  (`?handler=RollBack&to=2&version=3`); a POST handler's parameters bind from the query string as they do from a form.

## 9. Actions on a row (proven)

Built as Archive and Restore on each row of a paged list of a record's related records, under a header whose own
actions post to the page's address.

- **The row's form posts to the list's own address.** Its `action` and `hx-post` are the list's current URL, its
  state included, with the handler and the row's identifier added (`?after=10017&contact=10018&handler=ArchiveContact`).
  The POST handler binds where the list is from the query string through the same
  `[BindProperty(SupportsGet = true)]` model as the GET handler, reads the same page again, and answers with the
  list's partial (workshop, Lab 4). The identifier rides in the address, not in a hidden field, so the toast's Undo
  (section 8) is an address and nothing more.
- **The URL stays.** `hx-post` pushes nothing, so the address bar keeps the list's URL, which still loads the same
  page. Without HTMX the handler redirects to that URL (section 7), and the toast waits in `TempData` with the target
  its Undo replaces, since that is no longer the page's main content.
- **A form that says `hx-post` is not boosted.** Inside a container with `hx-boost:inherited`, HTMX sets up every
  element that names its own request first, and boosts only the links and forms not yet set up (4.0.1 source,
  `process` and `#maybeBoost`). The row's form keeps the container's inherited swap unless it names its own
  `hx-target`.
- **Whatever carries the URL's state is drawn again when the URL changes.** A form outside the list that posts to the
  page's address, such as Archive in the record's header, carries the list's state too. A Previous or Next that
  replaced only the list would leave that form posting from the old page, so paging replaces the list and the header
  together. An action on a row leaves the URL as it was, so it replaces the list alone. Choose each target as the
  smallest element that holds everything the request changes.
- **A row's menu is a popover, and its form posts like any other.** The answer replaces the list, the open popover
  with it, and the menu closes.
- **`autofocus` puts the keyboard back on the row.** The button pressed was inside the menu, and the answer's menu is
  closed, so putting the focus back by `id` would find a hidden button. The answer marks the row's menu button
  `autofocus` instead: after a swap HTMX focuses the first `[autofocus]` in the new content (4.0.1 source,
  `#handleAutoFocus`), and it does so after putting focus back by `id` (`#insertContent`), so the row's button takes
  the focus even when the toast's Undo sent the request.

## 10. A field HTMX enhances (proven)

Built as the organization a contact belongs to, one of about a thousand: a select without HTMX, and with it the
brand's relation field, whose picker filters the choices as you type.

- **The plain control comes first, and HTMX swaps it as the page loads.** The page draws a control that works
  without script, such as a select of every choice, in an element that says `hx-get`, `hx-trigger="load"` and
  `hx-swap="outerHTML"`. HTMX asks for the enhanced field as soon as it processes the page, and the answer replaces
  the plain one, keeping its `id`. Both post the value under the same name, so the handler binds it the same way. The
  load's address carries the value the control holds, since a GET sends nothing from the form around it (4.0.1
  source, `#handleTriggerEvent`).
- **An answer to HTMX can draw the enhanced field at once.** When HTMX sent the request, as for a refused save, it is
  known to be there, so the answer draws the enhanced field straight away and the page makes no second request.
- **A picker with a form of its own sits outside the form it fills.** A form inside another is not HTML, so the
  picker's dialog follows the edit form, and the field inside the edit form keeps the value in a hidden input. The
  picker's form answers with the field, as the main content, and a new dialog in an `<hx-partial>` (section 8); the new
  dialog replaces the open one, which closes it (section 7).
- **`hx-include` of a container sends the named controls inside it**, a radio or a checkbox only when it is checked
  (4.0.1 source, `#collectFormData` and `#addInputValues`). The filter's input sends its own value, and
  `hx-include` naming the list sends the radio that is checked, so the list comes back with it still checked.
- **HTMX 4 checks a form with the browser's own validation before sending it**, unless the form says `novalidate`
  (4.0.1 source, `#createRequestContext` and `#collectFormData`). Radios marked `required` stop an empty choice with
  the browser's message, and nothing is sent. The button that sent the form adds its own name and value, so
  `name="choose" value="true"` tells the handler a choice was made, and its answer marks the field's button
  `autofocus` to put the keyboard back there (section 9).
- **Enter in the picker's filter sends the picker's form**, because the filter is one of its fields. By then the list
  has kept up with the typing, so Enter chooses the checked radio, and with none checked the browser asks for one.

## 11. An action that cannot be undone (proven)

Built as Delete permanently on an archived record's page: a confirm dialog when the record can go, a dialog that says
why when it cannot, and the list afterwards, with a toast.

- **Ask first in a `<dialog>` with its own form, not with `hx-confirm`.** An action that can be undone runs at once
  and offers Undo (section 8); only one that cannot asks first. `hx-confirm` asks with the browser's `confirm()`
  ([hx-confirm](https://four.htmx.org/reference/attributes/hx-confirm)), after a cancelable `htmx:confirm` event,
  and only when HTMX sends the request (4.0.1 source, `#issueRequest`), so without HTMX the form posts unasked. A
  `<dialog>` opened by a `type="button"` with `commandfor` and `command="show-modal"` asks with HTMX and without it,
  and its form posts like any other (section 7). The safe choice says `autofocus`, which a modal dialog focuses as it
  opens (HTML's
  [dialog focusing steps](https://html.spec.whatwg.org/multipage/interactive-elements.html#dialog-focusing-steps)),
  so a key pressed at once does no harm.
- **The dialog's form carries the version it shows.** The handler refuses a request from an out-of-date dialog, as it
  refuses a save from an out-of-date form (section 7), so a confirmation never removes a record that has changed
  since it was asked for.
- **The page knows before it asks whether the action is allowed.** When the record cannot go, as when other records
  still name it, the page draws the same button with a different dialog: one that says why and what to do instead,
  closes with `<form method="dialog">`, and posts nothing. The handler checks again when the action is sent, since the
  page may be out of date, and answers a refusal as Archive does, with the page's content and a toast that says why
  (section 8). The answer replaces the element that holds the dialog, which closes it (section 7).
- **Success leaves the page, so the toast waits in `TempData`, with HTMX too.** The record's page is gone, so the
  handler answers as a save does (section 7): a redirect without HTMX, and 204 with `HX-Location` naming the list with
  it. Either way it leaves its toast in `TempData`, with no Undo. `HX-Location` makes HTMX GET the list with
  `HX-Request-Type: full` (4.0.1 source, `#handleHeadersAndMaybeReturnEarly`), and that `fetch` sends the
  application's cookies (`credentials: "same-origin"`, `#createRequestContext`), the `TempData` cookie among them
  ([TempData](https://learn.microsoft.com/aspnet/core/fundamentals/app-state#tempdata)), so the list's whole page
  shows the toast.
- **The list takes the toast only for its whole page.** A partial answer has no toast region to put it in, so the
  list's GET handler reads `TempData` after it has decided to answer with the page (section 3), and a swap of the
  list's card never spends a toast it cannot show. Leaving a toast and taking it belong in one small class that the
  record's pages and the list share, so the keys are written once.
- **Testing.** A test of the delete that HTMX sent asserts 204, `HX-Location` naming the list, and the message in
  `TempData`; the list's test leaves a message in `TempData` and expects it shown for a request with no HTMX headers
  and for a history restore, and not for a partial one. A page model that reads or writes `TempData` gets a
  `TempDataDictionary` over a stand-in `ITempDataProvider` in its test, as the framework would give it one.

## 12. An action that takes its row out of the list (proven)

Built as Delete permanently on an archived row of a paged list of a record's related records: the row's menu opens a
confirm dialog (section 11), and the answer draws the list again without the row.

- **Read the page again from the same cursor.** The dialog's form posts to the list's own address, its state
  included, as a row's action does (section 9), and the handler reads the page that address names once the action has
  run. With key-based paging the cursor names a row outside the page, so the rows after the one that left move up and
  the page fills from the next; only the last page comes back a row short. A page left with no row is a cursor that
  leaves nothing to show, and the list's own rule for one decides what is shown, such as its first page, so the
  answer needs no rule of its own. The workshop goes back a page instead (Lab 4), which paging by page number needs
  and paging by key does not.
- **The target holds every count the row was in.** An action that leaves its row in the list, such as Archive among
  rows that show archived ones, changes the list alone. One that takes the row out also changes any count of the
  list's records shown outside it, such as a count on a tab's label, so its target is the smallest element that holds
  the list and the count (section 9): here the element Previous and Next replace, which holds both.
- **Read the row's name and place before the action.** Once the row has gone, the page read again holds neither, so
  the handler reads the page as it was shown first, for the name its toast gives and the place the keyboard goes back
  to.
- **The keyboard goes to the row now in its place.** The menu button that opened the dialog went with its row, so the
  answer marks `autofocus` on the menu button of the row now at the same place, or of the last row when the page is
  shorter, and on the list's heading, made focusable with `tabindex="-1"`, when no row is left (section 9). A refused
  action leaves the row, and the keyboard goes back to its own menu button.
- **The rows' dialogs come after the table.** After a swap HTMX focuses the first `[autofocus]` in the new content
  only, whether or not it can take the focus (4.0.1 source, `#handleAutoFocus`). Each dialog's safe button says
  `autofocus` (section 11), and a button in a closed dialog cannot take the focus, so a dialog earlier in the content
  than the row's menu button keeps the button from getting it.
- **One dialog for each row, opened from its menu.** The menu's last item is a `type="button"` with `commandfor`
  naming the row's dialog, whose `id`, like the menu's, is built from the row's record. Showing a modal dialog hides
  the open popovers that are not its ancestors (HTML's
  [showModal()](https://html.spec.whatwg.org/multipage/interactive-elements.html#dom-dialog-showmodal) steps), so the
  menu closes as the dialog opens. The dialog's form carries the version the row shows (section 11), so the list's
  rows carry each record's version.
- **Without HTMX** the dialog opens the same way, its form posts, and the handler redirects to the list's address,
  which reads the same page without the row, with the toast waiting in `TempData` (section 8).
- **Testing.** A stand-in service that leaves a deleted record out of every page it answers afterwards lets a page
  test see the page read again: the row is gone, the row after it takes the focus, the last row does when the deleted
  row was last, and the heading does when it was the only one.

## 13. Still open (open)

Nothing in this section is built yet. What is known, to start from; an idea taken from the workshop names its
chapter.

- **A request with no form around it** has to send the antiforgery token another way, such as in the
  `RequestVerificationToken` header through an inherited `hx-headers`
  ([CSRF prevention](https://four.htmx.org/docs#csrf-prevention)). An input that posts on its own, such as one checked
  as you type, sends only its own value unless it says `hx-include="closest form"` (workshop, "Checkpoint:
  Troubleshooting + Pattern Review").
- **An action that takes its row out of the list and can be undone**, such as Archive on a list of active records
  only. The page read again is as section 12 says, and the toast's Undo posts the opposite from the same address,
  which puts the row back. Where the keyboard goes after that Undo, which was sent from the toast and not from a row,
  is not yet built.
- **Check one field as you type.** The input posts on its own to a handler of its own: `hx-post="?handler=CheckName"`,
  `hx-trigger="input changed delay:500ms"`, `hx-include="closest form"` for the token and the other fields, and the
  field's message element as its `hx-target`. The handler validates what was bound and returns only that field's
  message, as a partial whose root is the message element (workshop, Lab 3). Without HTMX the same rule runs when
  the form is posted.
- **Rows added to and removed from a form.** For a list of plain values, every row's input has the same name, such
  as `name="Input.Tags"`, which binds to a `List<string>` with no index numbers. The server renders one new row as a
  fragment that `hx-swap="beforeend"` appends, and removing a row only removes its element (workshop, Lab 5). A list
  of objects needs indexed names (`Input.Lines[0].Name`), and so more care when a row leaves.
- **A select that applies when it changes** (`hx-trigger="change"` on its GET form) does nothing without HTMX unless
  the form also has a submit button.
- **An `HX-Trigger` response header** raises a client-side event, and an element elsewhere can listen for it and load
  itself again: `hx-get="?handler=Messages" hx-trigger="showMessage from:body"`. That is how the workshop shows a
  message after a save (Lab 3). It costs a second request for each message, where section 8's `<hx-partial>` arrives
  with the first; it suits an element that reloads itself for reasons of its own. In HTMX 4 a `from:` selector that
  holds a space needs single quotes (section 1); `from:body` does not.
- **A long job reports its progress by polling.** While the job runs, its status element carries `hx-get` and
  `hx-trigger="every 1s"`, and each answer replaces the element. The answer for a finished job leaves the trigger out,
  and the polling stops (workshop, Lab 5). HTMX 4 still reads `every` (4.0.1 source, `#parseTriggerSpecs`). Without
  HTMX the status needs a link that loads the page again.

## Changing this skill

- Each sentence must hold for any Razor Pages application on this HTMX version. How one application uses HTMX belongs
  in that application's code, tests and README.
- Every HTMX fact cites the reference page or the source method it came from. A section moves to Proven only when it
  has been built and shown working.
- On a new HTMX version, read its What's new page and its `dist/htmx.js`, check section 1 against them, update the
  version in the status heading, and only then move the pin.
