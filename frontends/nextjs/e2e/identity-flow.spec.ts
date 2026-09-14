import { expect, test } from "@playwright/test";

import { registerIdentityFlowSpec } from "../../../e2e/identity-flow";

// The real test body lives in the shared `e2e/identity-flow.ts` — this file
// only supplies this project's own `test`/`expect` (resolved from this
// project's own `node_modules`) so Playwright's runner, which loads spec
// files by requiring them directly, can find `@playwright/test` the normal
// way. See `../../../e2e/README.md`.
registerIdentityFlowSpec(test, expect);
