// The script the application keeps. The menu button switches the side nav between its full width and an icon rail,
// a toast goes when Dismiss is pressed or when it has faded out, a dialog opened from a menu gives the keyboard back
// to the menu's button, a request HTMX sends that fails says so in a toast, and a form that sends a file shows how
// much of it has gone. Every listener is on the document
// rather than on an element, because when the back button restores a page, HTMX swaps in a new body and with it new
// elements.
(() => {
  // HTMX loads before this script. When it has not, say so on the root, so that the brand's CSS shows the Apply
  // buttons that a form HTMX would otherwise send as you go needs without it.
  if (typeof window.htmx === "undefined") {
    document.documentElement.dataset.htmx = "absent";
  }

  const toggleSelector = "[data-action='toggle-nav']";
  const navIsFull = () => document.querySelector(".app-nav").offsetWidth > 100;
  const describeButton = () =>
    document.querySelector(toggleSelector)?.setAttribute("aria-expanded", String(navIsFull()));

  document.addEventListener("click", (event) => {
    const button = event.target.closest(toggleSelector);
    if (!button) {
      return;
    }

    const isRail = !navIsFull();
    document.querySelector(".app").dataset.nav = isRail ? "full" : "rail";
    button.setAttribute("aria-expanded", String(isRail));
  });
  document.addEventListener("DOMContentLoaded", describeButton);
  document.addEventListener("htmx:after:swap", describeButton);

  // The brand's CSS fades a toast out after 8 seconds, and pauses while the pointer or the keyboard is on it; once it
  // has faded, or Dismiss is pressed, the toast is removed, so that nothing invisible stays in the page.
  document.addEventListener("click", (event) => {
    event.target.closest("[data-action='dismiss-toast']")?.closest(".toast")?.remove();
  });
  document.addEventListener("animationend", (event) => {
    if (event.animationName === "toast-out") {
      event.target.remove();
    }
  });

  // A dialog gives the keyboard back, when it closes, to whatever had it when it opened. A button in a row's menu that
  // opens a dialog would leave it nowhere: the menu closes as the dialog opens, and takes that button out of reach. So
  // the keyboard moves to the button that opens the menu first, while the click is still being handled and before
  // the dialog opens, and Cancel, Escape and a request that fails all bring it back there.
  document.addEventListener("click", (event) => {
    const menu = event.target.closest("[command='show-modal']")?.closest("[popover]");
    if (menu) {
      document.querySelector(`[popovertarget="${CSS.escape(menu.id)}"]`)?.focus();
    }
  });

  // An answer the page planned for has content made for the request's target, with whatever status the handler chose,
  // such as 422 for a form to correct. An error status with no content, such as the 400 a stale antiforgery token
  // gets, and any 5xx are not planned for: HTMX 4 would swap them in like any other, emptying the target or putting a
  // whole error page inside it. Back and Forward are the exception: the address has already changed, so the error
  // page they get is shown, as the browser would show it without HTMX.
  const unplanned = (ctx) => {
    const status = ctx.response?.status ?? 200;
    if (status < 400 || ctx.request.headers["HX-History-Restore-Request"]) {
      return false;
    }

    return status >= 500 || ctx.text.trim() === "";
  };

  // What went wrong, by the template the layout holds for it: an answer the page did not plan for, no answer within
  // HTMX's timeout, or no answer at all, because the connection was lost.
  const failure = (ctx) => {
    if (ctx.response) {
      return "answer";
    }

    return ctx.request.signal.reason === "timeout" ? "timeout" : "connection";
  };

  // A request that fails changes nothing on the page. The toast region says what happened, and the keyboard goes
  // back where it was. A dialog the request came from closes, as Cancel would close it, because a modal dialog keeps
  // the toast out of sight and out of reach of a screen reader. A button that hx-disable turned off lost the focus,
  // so it gets it back once HTMX turns it on again, after this event, unless the user has moved on.
  const fail = (ctx) => {
    ctx.sourceElement.closest("dialog[open]")?.close();
    const toast = document.getElementById(`request-failed-${failure(ctx)}`);
    document.getElementById("toasts").replaceChildren(toast.content.cloneNode(true));
    const control = ctx.sourceEvent?.submitter ?? ctx.sourceElement;
    setTimeout(() => {
      if (document.activeElement === document.body && control.isConnected && !control.closest("dialog")) {
        control.focus();
      }
    });
  };

  // Cancelling the swap stops the history update too, which a boosted link would otherwise make: neither
  // htmx.config.noSwap nor hx-status does, since HTMX still runs the swap, with nothing to swap in.
  document.addEventListener("htmx:before:swap", (event) => {
    if (unplanned(event.detail.ctx)) {
      event.preventDefault();
      fail(event.detail.ctx);
    }
  });

  // HTMX raises htmx:error with the request's context when the request fails, and without one for an error in an
  // hx-on handler, which is not a request's.
  document.addEventListener("htmx:error", (event) => {
    if (event.detail.ctx) {
      fail(event.detail.ctx);
    }
  });

  // A form that sends a file says how much of it has gone, which HTMX 4 cannot: it sends with fetch, which reports
  // nothing of an upload. HTMX sends each request through ctx.fetch, the browser's fetch unless a listener of
  // htmx:before:request has put another there (4.0.1 source, #issueRequest). For a form that holds an element marked
  // data-upload-progress, around a <progress>, the request goes by XMLHttpRequest instead, whose upload reports its
  // progress, and HTMX gets a Response, as fetch would give it: rejected with a TypeError when the connection fails,
  // and with an AbortError when HTMX gives the request up, as at its timeout. HTMX does everything else as before:
  // the headers, the queue, the timeout, the busy button and the swap. The element is hidden until the upload starts,
  // so without script or without HTMX it never shows, and the browser reports the upload itself.
  const statusesWithoutBody = [101, 204, 205, 304];

  const responseOf = (xhr) => {
    const headers = new Headers();
    for (const line of xhr.getAllResponseHeaders().split("\r\n")) {
      const colon = line.indexOf(":");
      if (colon > 0) {
        headers.append(line.slice(0, colon), line.slice(colon + 1).trim());
      }
    }

    const body = statusesWithoutBody.includes(xhr.status) ? null : xhr.responseText;
    const response = new Response(body, { status: xhr.status, statusText: xhr.statusText, headers });
    Object.defineProperty(response, "url", { value: xhr.responseURL });
    return response;
  };

  const sendWithProgress = (url, request, progress) => new Promise((resolve, reject) => {
    const givenUp = () => new DOMException("The request was given up.", "AbortError");
    if (request.signal.aborted) {
      reject(givenUp());
      return;
    }

    const xhr = new XMLHttpRequest();
    xhr.open(request.method, url);
    for (const [name, value] of Object.entries(request.headers)) {
      xhr.setRequestHeader(name, value);
    }

    xhr.upload.addEventListener("progress", (event) => {
      if (event.lengthComputable) {
        progress.value = event.loaded / event.total;
      }
    });
    xhr.addEventListener("load", () => resolve(responseOf(xhr)));
    xhr.addEventListener("error", () => reject(new TypeError("The connection failed.")));
    xhr.addEventListener("abort", () => reject(givenUp()));
    request.signal.addEventListener("abort", () => xhr.abort(), { once: true });
    xhr.send(request.body);
  });

  const uploadProgressOf = (ctx) =>
    ctx.sourceElement.closest("form")?.querySelector("[data-upload-progress]");

  document.addEventListener("htmx:before:request", (event) => {
    const { ctx } = event.detail;
    const shown = uploadProgressOf(ctx);
    const progress = shown?.querySelector("progress");
    if (!progress || !(ctx.request.body instanceof FormData)) {
      return;
    }

    progress.value = 0;
    shown.hidden = false;
    ctx.fetch = (url, request) => sendWithProgress(url, request, progress);
  });

  // The answer replaces the form, its progress with it; a request that failed leaves the form, so it hides again.
  document.addEventListener("htmx:finally:request", (event) => {
    const shown = uploadProgressOf(event.detail.ctx);
    if (shown) {
      shown.hidden = true;
    }
  });
})();
