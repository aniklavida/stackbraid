import { defineConfig, devices } from "@playwright/test";

/**
 * One happy-path Identity flow, run against whichever backend
 * `NEXT_PUBLIC_API_BASE_URL` (baked into the running `next dev`/`next start`
 * this points at) currently answers to — this file has no backend-specific
 * knowledge, the same way `contract/conformance/` doesn't. Chromium only:
 * the point is proving the flow works against each backend, not cross-
 * browser coverage, and no browser beyond what is already installed on
 * this machine is downloaded to run it.
 */
export default defineConfig({
  testDir: "./e2e",
  fullyParallel: false,
  retries: 0,
  workers: 1,
  reporter: "list",
  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL ?? "http://127.0.0.1:3000",
    trace: "retain-on-failure",
    screenshot: "off",
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
