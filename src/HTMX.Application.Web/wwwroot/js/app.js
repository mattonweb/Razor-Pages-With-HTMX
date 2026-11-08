// The one piece of script the brand asks the application to keep: the menu button switches the side nav between its
// full width and an icon rail. These are the brand's own lines, from its specimens.
(() => {
  const app = document.querySelector(".app");
  const nav = document.querySelector(".app-nav");
  const navButton = document.querySelector("[data-action='toggle-nav']");
  navButton.setAttribute("aria-expanded", String(nav.offsetWidth > 100));
  navButton.addEventListener("click", () => {
    const isRail = nav.offsetWidth <= 100;
    app.dataset.nav = isRail ? "full" : "rail";
    navButton.setAttribute("aria-expanded", String(isRail));
  });
})();
