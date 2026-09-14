// Runtime backend selection — edited (or generated), never rebuilt.
// `shared/config/env.ts` reads `window.__STACKBRAID_ENV__.apiBaseUrl`. Pointing
// this same compiled build at the .NET or the Python backend is a one-line
// edit to this file, not a code change or a rebuild.
window.__STACKBRAID_ENV__ = {
  apiBaseUrl: "http://127.0.0.1:8080",
};
