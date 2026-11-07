// Specimen pages only. The navigation toggle is the one piece the application keeps, in its own script.
(() => {
  const root = document.documentElement;
  const app = document.querySelector(".app");
  const nav = document.querySelector(".app-nav");

  // Opened from disk: browsers will not load the icon sprite into a file:// page, so every icon is blank.
  // Say so at the top of the page, with the address that works.
  if (location.protocol === "file:") {
    const page = location.pathname.split("/").pop();
    const notice = document.createElement("p");
    notice.className = "file-notice";
    notice.innerHTML =
      `Icons do not load when this page is opened from a file. Open ` +
      `<a href="http://localhost:5180/references/specimens/${page}">http://localhost:5180/references/specimens/${page}</a> instead, ` +
      `after starting the server from the repository root: ` +
      `<code>python .claude/skills/brand-htmlx-pages/scripts/serve_specimens.py</code>`;
    document.querySelector(".app-main").prepend(notice);
  }

  // Navigation: switch between the full menu and the icon rail. The application uses these same lines.
  const navButton = document.querySelector("[data-action='toggle-nav']");
  navButton.setAttribute("aria-expanded", String(nav.offsetWidth > 100));
  navButton.addEventListener("click", () => {
    const isRail = nav.offsetWidth <= 100;
    app.dataset.nav = isRail ? "full" : "rail";
    navButton.setAttribute("aria-expanded", String(isRail));
  });

  // Theme: lets a reviewer see dark mode without changing the operating system setting.
  // The button shows the mode it switches to: a moon in light mode, a sun in dark mode.
  const themeButton = document.querySelector("[data-action='toggle-theme']");
  const currentTheme = () => root.dataset.theme ?? (matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light");
  const showThemeIcon = () => themeButton.querySelector("use").setAttribute("href", `../../assets/icons.svg#${currentTheme() === "dark" ? "sun" : "moon"}`);
  themeButton.addEventListener("click", () => {
    root.dataset.theme = currentTheme() === "dark" ? "light" : "dark";
    showThemeIcon();
    showColorValues();
  });
  showThemeIcon();

  // Color values: each [data-hex="accent"] shows var(--accent) as resolved in the current mode.
  function showColorValues() {
    const probe = document.createElement("i");
    probe.hidden = true;
    document.body.append(probe);
    for (const label of document.querySelectorAll("[data-hex]")) {
      probe.style.backgroundColor = `var(--${label.dataset.hex})`;
      label.textContent = toHex(getComputedStyle(probe).backgroundColor);
    }
    probe.remove();
  }

  function toHex(rgb) {
    const channels = rgb.match(/[\d.]+/g).slice(0, 3).map(Number);
    return "#" + channels.map((n) => Math.round(n).toString(16).padStart(2, "0")).join("");
  }

  // Icons: one tile per symbol in the sprite, so the page never lists them twice.
  const iconGrid = document.querySelector("[data-icon-grid]");
  if (iconGrid) {
    fetch("../../assets/icons.svg")
      .then((response) => response.text())
      .then((text) => {
        const ids = [...text.matchAll(/<symbol id="([^"]+)"/g)].map((match) => match[1]).filter((id) => id !== "logo");
        iconGrid.innerHTML = ids
          .map((id) => `<div><svg class="icon" aria-hidden="true"><use href="../../assets/icons.svg#${id}"></use></svg><code>${id}</code></div>`)
          .join("");
      });
  }

  // Side panel: in the application HTMX loads the record into the panel and opens it. Here a click on an
  // a[data-panel] link opens the panel already on the page, and marks that link's row as selected.
  const panel = document.getElementById("record-panel");
  const clearSelection = () => document.querySelectorAll(".grid tr[aria-selected]").forEach((row) => row.removeAttribute("aria-selected"));
  if (panel) {
    for (const link of document.querySelectorAll("a[data-panel]")) {
      link.addEventListener("click", (event) => {
        event.preventDefault();
        clearSelection();
        link.closest("tr")?.setAttribute("aria-selected", "true");
        panel.showPopover();
      });
    }
    panel.addEventListener("toggle", (event) => {
      if (event.newState === "closed") clearSelection();
    });
  }

  // Specimen only: example rows start checked by script rather than by the checked attribute, so the Clear
  // (reset) button empties them as it does in the application, where rows always start unchecked.
  document.querySelectorAll("[data-start-checked]").forEach((box) => (box.checked = true));

  // Select all: the one piece of bulk selection that needs script, so the application keeps these lines.
  // The header box checks or clears every row on the page; a row change sets the header to checked, clear
  // or mixed (indeterminate).
  for (const all of document.querySelectorAll("[data-select-all]")) {
    const rows = [...all.closest("table").querySelectorAll("tbody .select input[type='checkbox']")];
    const showHeader = () => {
      const checked = rows.filter((box) => box.checked).length;
      all.checked = checked === rows.length;
      all.indeterminate = checked > 0 && checked < rows.length;
    };
    all.addEventListener("change", () => rows.forEach((box) => (box.checked = all.checked)));
    rows.forEach((box) => box.addEventListener("change", showHeader));
    rows[0]?.form?.addEventListener("reset", () => setTimeout(showHeader));
    showHeader();
  }

  // Toasts: in the application the server adds a toast to the .toasts region after an action. Here the demo
  // button adds one from a template, and Undo or Dismiss on a live toast removes it (the held ones stay).
  const region = document.querySelector("body > .toasts");
  document.querySelector("[data-demo-toast]")?.addEventListener("click", () => {
    region.append(document.getElementById("demo-toast").content.cloneNode(true));
  });
  document.addEventListener("click", (event) => {
    const button = event.target.closest("body > .toasts .toast .btn");
    if (button) button.closest(".toast").remove();
  });
  region?.addEventListener("animationend", (event) => {
    if (event.animationName === "toast-out") event.target.remove();
  });

  // The specimen pages post nowhere: any submit other than a dialog form closing its dialog stops here.
  for (const form of document.querySelectorAll("form")) {
    form.addEventListener("submit", (event) => {
      if (form.method !== "dialog") event.preventDefault();
    });
  }

  showColorValues();
})();
