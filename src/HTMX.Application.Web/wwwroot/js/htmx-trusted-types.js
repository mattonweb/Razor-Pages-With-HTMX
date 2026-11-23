// Gives HTMX the Trusted Types policy the Content Security Policy asks for. The policy says require-trusted-types-for
// 'script', so a string becomes HTML or script only through a policy, and trusted-types htmx default, so the page may
// create two policies, these. HTMX parses every answer through the htmx policy's createHTML (4.0.1 source,
// #parseHTML), which passes it on: an answer is the application's own, whose text Razor encoded. HTMX gives each script
// in an answer its text through createScript (#processScripts), which gives it none, so a script in an answer never
// runs, even under a policy that would trust a script HTMX creates. A browser without Trusted Types enforces neither
// the directives nor the policies, and HTMX hands each string on as it always does.
//
// HTMX copies the rest of each script's attributes as strings with setAttribute (#processScripts), and a script's src
// is a script address, which the browser hands to the policy named default, or refuses, failing the whole swap. A
// script from a file gets into an answer that way when Visual Studio runs the application: Browser Link and hot reload
// write theirs into every page, an answer to a boosted link included. The default policy makes script addresses and
// nothing else, and makes each an empty one, which the browser never asks for, so such a script never runs either and
// the rest of the answer swaps in. A string that would become HTML or script text still has no way in but HTMX's.
(() => {
  if (!window.trustedTypes || !window.htmx) {
    return;
  }

  const policy = window.trustedTypes.createPolicy("htmx", {
    createHTML: (html) => html,
    createScript: () => "",
  });
  window.trustedTypes.createPolicy("default", {
    createScriptURL: () => "",
  });
  window.htmx.registerExtension("trusted-types", {
    init: (api) => api.initSecurity(policy),
  });
})();
