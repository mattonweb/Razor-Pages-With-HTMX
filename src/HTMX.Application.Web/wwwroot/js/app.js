// The script the application keeps. The menu button switches the side nav between its full width and an icon rail,
// and a toast goes when Dismiss is pressed or when it has faded out. Every listener is on the document rather than on
// an element, because when the back button restores a page, HTMX swaps in a new body and with it new elements.
(() => {
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
})();
