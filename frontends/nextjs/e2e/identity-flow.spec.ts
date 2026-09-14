import { expect, test, type Page } from "@playwright/test";

/**
 * The one Playwright happy path this frontend ships — register, sign in,
 * the httpOnly refresh cookie surviving a reload, sign out, and the admin
 * area being genuinely unreachable without the `admin` role, then genuinely
 * reachable with it. Screenshots are written outside the repository
 * (`SCREENSHOT_DIR`) purely as run evidence, never as a repo artifact.
 *
 * Talks to whatever backend `NEXT_PUBLIC_API_BASE_URL` was set to when the
 * `next dev`/`next start` under test was started — nothing here names
 * .NET or Python.
 */
const SEEDED_ADMIN = { email: "admin@stackbraid.local", password: "ChangeMe!123" };
const SCREENSHOT_DIR = process.env.SCREENSHOT_DIR;
const BACKEND_LABEL = process.env.BACKEND_LABEL ?? "backend";

async function shot(page: Page, name: string) {
  if (!SCREENSHOT_DIR) return;
  await page.screenshot({ path: `${SCREENSHOT_DIR}/${BACKEND_LABEL}-${name}.png`, fullPage: true });
}

test("register, sign in, survive a reload, sign out, and the admin area is role-gated", async ({ page }) => {
  const stamp = Date.now();
  const email = `sadia.islam+${stamp}@example.com`;
  const password = "correct-horse-battery-staple";
  const displayName = "Sadia Islam";

  // --- Signed out home ---------------------------------------------------
  await page.goto("/");
  await expect(page.getByRole("heading", { name: /working identity feature/i })).toBeVisible();
  await shot(page, "01-home-signed-out");

  // --- Register ------------------------------------------------------------
  await page.getByRole("link", { name: "Create account" }).click();
  await page.getByLabel("Display name").fill(displayName);
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill(password);
  await shot(page, "02-register-form");
  await page.getByRole("button", { name: "Create account" }).click();

  // Registration alone issues no session (the contract's own rule) — this
  // app signs the new account in right after, landing on its profile.
  await expect(page).toHaveURL(/\/profile$/);
  await expect(page.getByRole("heading", { name: displayName })).toBeVisible();
  await expect(page.getByText(email)).toBeVisible();
  // Self-registration grants no role — role membership is an administrator
  // action (assign-role), never something the register endpoint does on
  // its own. This account is deliberately roleless until the admin section
  // below assigns one.
  await shot(page, "03-profile-after-register");

  // --- The httpOnly refresh cookie survives a reload ------------------------
  await page.reload();
  await expect(page.getByRole("heading", { name: displayName })).toBeVisible();

  // --- The admin area is unreachable without the role -----------------------
  await expect(page.getByRole("link", { name: "Admin" })).toHaveCount(0);
  await page.goto("/admin/users");
  // AdminShell's RequireAuth bounces a non-admin back to the home page once
  // hydration confirms the session and its (lack of) permission — the
  // refresh cookie is scoped to the backend's own /v1/auth path, so no
  // Next.js middleware ever sees it to redirect any earlier than this.
  await expect(page).toHaveURL(/\/$/, { timeout: 10_000 });
  await shot(page, "04-admin-blocked-for-ordinary-user");

  // --- Sign out --------------------------------------------------------------
  await page.goto("/");
  const nav = page.getByRole("navigation");
  await page.getByRole("button", { name: "Sign out" }).click();
  await expect(nav.getByRole("link", { name: "Sign in" })).toBeVisible();

  // --- Sign back in with the same credentials ---------------------------------
  await nav.getByRole("link", { name: "Sign in" }).click();
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill(password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page).toHaveURL(/\/profile$/);
  await expect(page.getByRole("heading", { name: displayName })).toBeVisible();
  await page.getByRole("button", { name: "Sign out" }).click();

  // --- The seeded administrator can reach it -----------------------------------
  await page.goto("/login");
  await page.getByLabel("Email").fill(SEEDED_ADMIN.email);
  await page.getByLabel("Password").fill(SEEDED_ADMIN.password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page).toHaveURL(/\/profile$/);
  await expect(page.getByRole("link", { name: "Admin" })).toBeVisible();

  await page.getByRole("link", { name: "Admin" }).click();
  await expect(page).toHaveURL(/\/admin\/users$/);
  await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
  await expect(page.getByText(email)).toBeVisible(); // the account just registered shows up in the real list
  await shot(page, "05-admin-users-list");

  // --- Role management on the freshly registered user ---------------------------
  // Matched by email, not display name: repeated runs against the same
  // throwaway database accumulate several "Sadia Islam" rows, each with
  // its own timestamp-suffixed email.
  await page.getByRole("row", { name: new RegExp(email.replace(/[+.]/g, "\\$&")) }).getByRole("link", { name: "View" }).click();
  await expect(page.getByRole("heading", { name: displayName })).toBeVisible();
  await shot(page, "06-admin-user-detail");

  await page.getByRole("combobox").click();
  await page.getByRole("option", { name: "admin" }).click();
  await page.getByRole("button", { name: "Assign" }).click();
  await expect(page.getByRole("main").getByRole("listitem").filter({ hasText: "admin" })).toBeVisible();
  await shot(page, "07-admin-role-assigned");

  await page.getByRole("link", { name: "Roles" }).click();
  await expect(page.getByRole("heading", { name: "Roles" })).toBeVisible();
  await expect(page.getByText("users:write")).toBeVisible();
  await shot(page, "08-roles-catalogue");
});
