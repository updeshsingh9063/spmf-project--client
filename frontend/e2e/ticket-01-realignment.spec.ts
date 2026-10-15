import { expect, test } from "@playwright/test";

// Ticket 1 — Freeze the wrong-direction workflow and preserve the reusable shell.
//
// These assert the ticket's own acceptance criteria. Criterion 2 (the prototype is
// recoverable from source control) is verified by the git tag `prototype-job-costing`
// and the branch `prototype-job-costing-backup`, not by a browser.

async function signIn(page: import("@playwright/test").Page) {
  await page.goto("/login");
  await page.getByRole("button", { name: "Sign in with Microsoft" }).click();
  await expect(page.getByRole("heading", { name: "Work Queue" })).toBeVisible();
}

test("the user no longer lands in a gross-margin dashboard", async ({ page }) => {
  await signIn(page);

  // Criterion 1. The home route is the Work Queue.
  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByRole("heading", { name: "Work Queue", level: 1 })).toBeVisible();

  // None of the job-costing vocabulary should survive anywhere on the home screen.
  const body = await page.locator("body").innerText();
  for (const gone of ["Gross profit", "Gross margin", "Net profit after overhead", "Contract value"]) {
    expect(body).not.toContain(gone);
  }
});

test("primary navigation is Work Queue, Projects and Financial Review", async ({ page }) => {
  await signIn(page);
  const nav = page.getByRole("navigation", { name: "Main" });

  // Criterion 4.
  await expect(nav.getByRole("link", { name: "Work Queue" })).toBeVisible();
  await expect(nav.getByRole("link", { name: "Projects" })).toBeVisible();
  await expect(nav.getByText("Financial Review")).toBeVisible();

  // Financial Review may be disabled until Ticket 19 — it must be present but not a link.
  await expect(nav.getByRole("link", { name: "Financial Review" })).toHaveCount(0);
  await expect(nav.getByText("Financial Review")).toHaveAttribute("aria-disabled", "true");
});

test("authentication and navigation still work", async ({ page }) => {
  // Criterion 3.
  await page.goto("/projects");
  await expect(page).toHaveURL(/\/login$/);

  await signIn(page);

  const nav = page.getByRole("navigation", { name: "Main" });
  await nav.getByRole("link", { name: "Projects" }).click();
  await expect(page.getByRole("heading", { name: "Projects" })).toBeVisible();
  await expect(page.locator("tbody tr").first()).toBeVisible();

  await nav.getByRole("link", { name: "Work Queue" }).click();
  await expect(page.getByRole("heading", { name: "Work Queue" })).toBeVisible();

  await page.getByRole("button", { name: "Sign out" }).last().click();
  await expect(page).toHaveURL(/\/login$/);
});

test("the project page keeps identity and drops the costing form", async ({ page }) => {
  await signIn(page);
  await page.getByRole("navigation", { name: "Main" }).getByRole("link", { name: "Projects" }).click();
  await page.locator("tbody tr td:nth-child(2) a").first().click();

  // Kept: project identity. The client marked this KEEP / REWORK.
  await expect(page.locator("#name")).toBeVisible();
  await expect(page.locator("#client")).toBeVisible();
  await expect(page.locator("#status")).toBeVisible();

  // Removed: every manual cost input.
  for (const gone of ["#labourHours", "#labourRate", "#subcontractorCost", "#otherDirectCost", "#overheadPct"]) {
    await expect(page.locator(gone)).toHaveCount(0);
  }
  await expect(page.getByRole("button", { name: /Recalculate|Calculate/ })).toHaveCount(0);

  // The sections that replace it are named, so the direction is visible.
  for (const section of ["Financial summary", "Linked QuickBooks activity", "Shipments", "Open decisions"]) {
    await expect(page.getByRole("heading", { name: section })).toBeVisible();
  }
});

test("a project can still be created, edited and saved", async ({ page }) => {
  // The shell has to keep working — this is a reset, not a teardown.
  page.on("dialog", (d) => d.accept());
  await signIn(page);

  await page.goto("/projects/new");
  await page.getByRole("button", { name: "Create project" }).click();
  await expect(page.getByText("Enter a project name.")).toBeVisible();

  await page.fill("#name", "Ticket 1 realignment check");
  await page.fill("#client", "Bayview Logistics");
  await page.getByRole("button", { name: "Create project" }).click();

  await expect(page).toHaveURL(/\/projects\/[0-9a-f-]{36}$/);
  const url = page.url();

  await page.fill("#site", "12 Dock Road");
  await page.getByRole("button", { name: "Save project" }).click();
  await expect(page.getByRole("status").filter({ hasText: "Project saved" })).toBeVisible();

  await page.reload();
  await expect(page.locator("#site")).toHaveValue("12 Dock Road");

  // Change history survives the reset.
  await expect(page.getByRole("heading", { name: "Change history" })).toBeVisible();
  await expect(page.getByText("Site address:")).toBeVisible();

  expect(page.url()).toBe(url);
});

test("Financial Review is reachable directly but says it is not built", async ({ page }) => {
  await signIn(page);
  await page.goto("/financial-review");

  await expect(page.getByRole("heading", { name: "Financial Review", level: 1 })).toBeVisible();
  await expect(page.getByText("Not built yet")).toBeVisible();
});
