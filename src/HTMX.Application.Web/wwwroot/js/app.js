// The one piece of script the brand asks the application to keep: the menu button switches the side nav between its
// full width and an icon rail. It listens on the document rather than on the button, because when the back button
// restores a page, HTMX swaps in a new body and with it a new menu button.
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
})();
