---
name: code-htmx-pages
description: How ASP.NET Core Razor Pages and HTMX 4 work together. Use whenever adding hx- attributes to a Razor page or partial view, writing a page handler that answers HTMX requests, choosing whether a handler returns a partial or the whole page, wiring history, forms, validation or antiforgery for HTMX, testing a page model against HTMX's headers, or reviewing any of those. Covers the HTMX 4 changes that break HTMX 2 habits, loading HTMX, one handler answering with a partial or the whole page by HX-Request-Type, list views kept in the query string, Back and Forward, testing, and the patterns still open (forms that post, more than one target). Written against HTMX 4.0.1. Draft; the status table says which sections are proven.
owner: Matt
version: 0.1.0
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

## Status: draft 0.1.0, HTMX 4.0.1

| Section | State |
|---|---|
| 1. HTMX 4 against HTMX 2 habits | Checked |
| 2. Loading HTMX | Proven |
| 3. One handler, two answers | Proven |
| 4. A list in the query string | Proven |
| 5. Back and Forward | Proven |
| 6. Testing | Proven |
| 7. Forms that post | Open |
| 8. More than one target | Open |

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
  Razor Pages side: handlers that render fragments, validation as you type, modals, confirmation, paging and polling.
  It is written for htmx 1.9.12, so its event names, its test of the `HX-Request` header and its scripts at the end of
  `<body>` do not carry over to 4. Take the Razor Pages shape from it and the HTMX details from this skill.

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
  root, each new copy carries them.

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

- **The list's whole state is in the query string**: filter, status, sort, page and page size. Every view of the list
  has its own URL; the GET handler reads it through one bound `InputModel` with `[BindProperty(SupportsGet = true)]`,
  and a value it does not recognize falls back to its default. A URL that HTMX pushes into history must load the
  whole page on its own ([Browser history](https://four.htmx.org/docs#browser-history-support)), and this is what
  makes that true.
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
- **What a unit test cannot see is checked in the browser:** which element a swap replaces, inheritance, focus, and
  the URL that is pushed. Read the request headers and the response body in the developer tools' network panel, then
  press Back and Forward after a few swaps.

## 7. Forms that post (open)

Nothing in this section is built yet. What is known, to start from:

- **Antiforgery.** Razor Pages checks the antiforgery token on every POST to a page handler, and the form tag helper
  writes the token into every `<form method="post">`. A boosted form, or an `hx-post` inside the form, sends the
  form's fields and the token with them. A request with no form around it has to send the token another way, such as
  in the `RequestVerificationToken` header through an inherited `hx-headers`
  ([CSRF prevention](https://four.htmx.org/docs#csrf-prevention)). To be verified on the first handler that needs it.
- **Prefer a POST to a named handler over `hx-delete` and `hx-put`.** A browser form can only GET or POST, so a button
  that must work without HTMX, such as Archive, posts to `asp-page-handler="Archive"`, answered by
  `OnPostArchiveAsync`. HTMX 4's `hx-delete` also leaves out the form's fields (section 1), and the token with them.
- **A form that fails validation** comes back with its messages. HTMX 4 swaps a 422, so the handler can answer a
  `partial` request with the form's partial and status 422, with `hx-status:422` to retarget it if needed, and a plain
  POST with `Page()`. The workshop instead answers 200 and retargets with `HX-Retarget` and `HX-Reswap`, because htmx
  1 would not swap a 4xx. Choose one when the first form is built. After every swap, HTMX focuses the first element
  with `autofocus` in the new content (4.0.1 source, `#handleAutoFocus`), which can put the cursor on the first field
  in error.
- **After a successful post** the plain form needs Post/Redirect/Get. Under HTMX, `fetch` follows a redirect by itself
  and HTMX swaps whatever the final URL returns; find out which headers the redirected GET carries, and so which
  answer section 3 gives it, before relying on it, and compare the `HX-Location` and `HX-Redirect` response headers.
- **A submit button that is busy** says `hx-disable="this"` to disable itself while the request runs (HTMX 2's
  `hx-disabled-elt`).
- **A select that applies when it changes** (`hx-trigger="change"` on its GET form) does nothing without HTMX unless
  the form also has a submit button.

## 8. More than one target (open)

- **A response that updates the target and something else**, such as a toast after an archive or the count on a tab,
  can carry the extra content as elements marked `hx-swap-oob`, matched by `id`, or wrapped in
  `<hx-partial hx-target="…" hx-swap="…">`. In 4 both swap after the main content, and a response with nothing but
  extra content leaves the main target alone
  ([Multi-target updates](https://four.htmx.org/docs#multi-target-updates)). Check that Razor writes `<hx-partial>`
  through unchanged; the `<template hx-type="partial">` form is the fallback.
- **An `HX-Trigger` response header** raises a client-side event, for something on the page that should load itself
  again. Compare it with an out-of-band swap when the first toast is built.
- **Confirmation.** `hx-confirm` uses the browser's `confirm()` and needs HTMX; a confirm page, or a `<dialog>` with
  its own form, works without it. Choose when the first action that cannot be undone is built.

## Changing this skill

- Each sentence must hold for any Razor Pages application on this HTMX version. How one application uses HTMX belongs
  in that application's code, tests and README.
- Every HTMX fact cites the reference page or the source method it came from. A section moves to Proven only when it
  has been built and shown working.
- On a new HTMX version, read its What's new page and its `dist/htmx.js`, check section 1 against them, update the
  version in the status heading, and only then move the pin.
