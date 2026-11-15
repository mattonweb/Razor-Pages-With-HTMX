// Gives HTMX the Trusted Types policy the Content Security Policy asks for. The policy says require-trusted-types-for
// 'script', so a string becomes HTML or script only through a policy, and trusted-types htmx, so the page may create
// one policy, this one. HTMX parses every answer through the policy's createHTML (4.0.1 source, #parseHTML), which
// passes it on: an answer is the application's own, whose text Razor encoded. HTMX gives each script in an answer its
// text through createScript (#processScripts), which gives it none, so a script in an answer never runs, even under a
// policy that would trust a script HTMX creates. A browser without Trusted Types enforces neither the directives nor
// the policy, and HTMX hands each string on as it always does.
(() => {
  if (!window.trustedTypes || !window.htmx) {
    return;
  }

  const policy = window.trustedTypes.createPolicy("htmx", {
    createHTML: (html) => html,
    createScript: () => "",
  });
  window.htmx.registerExtension("trusted-types", {
    init: (api) => api.initSecurity(policy),
  });
})();
