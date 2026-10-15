import { expect, test } from "@playwright/test";

// Alpha acceptance loop:
// sign in → create project → enter data → calculate → save →
// reopen → edit → recalculate → save, with change history recorded.

test("full Alpha loop", async ({ page }) => {
  await page.goto("/");
  await expect(page).toHaveURL(/\/login$/);
  await page.getByRole("button", { name: "Sign in with Microsoft" }).click();
  await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();
  await page.getByRole("button", { name: "Reset sample data" }).click();
  await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();

  // Create with missing fields: validation blocks calculating
  await page.getByRole("link", { name: "New project" }).click();
  await page.getByRole("button", { name: "Calculate" }).click();
  await expect(page.getByText("Enter a project name.")).toBeVisible();
  await expect(page.getByText("Describe the item.")).toBeVisible();

  // Enter data and calculate
  await page.fill("#name", "E2E strongroom door");
  await page.fill("#client", "Bayview Accountants");
  await page.getByLabel("Item 1 description").fill("Vault door, fire rated");
  await page.getByLabel("Item 1 unit cost").fill("3800");
  await page.getByLabel("Item 1 unit price").fill("5900");
  await page.fill("#labourHours", "10");
  await page.fill("#labourRate", "55");
  await page.fill("#overheadPct", "12");
  await page.getByRole("button", { name: "Calculate" }).click();
  await expect(page.getByText("$1,028.00", { exact: true })).toBeVisible(); // net profit

  // Save (create) and confirm
  await page.getByRole("button", { name: "Create project" }).click();
  await expect(page).toHaveURL(/\/projects\/[0-9a-f-]{36}$/);
  await expect(page.getByRole("status").filter({ hasText: "Project created" })).toBeVisible();
  const url = page.url();

  // Find it from the list with search, and reopen it
  await page.goto("/projects");
  await page.getByPlaceholder("Search by name, client or number").fill("strongroom");
  await expect(page.locator("tbody tr")).toHaveCount(1);
  await page.getByRole("link", { name: "E2E strongroom door" }).click();
  await expect(page).toHaveURL(url);
  await expect(page.locator("#name")).toHaveValue("E2E strongroom door");
  await expect(page.getByText("Calculated", { exact: true })).toBeVisible();

  // Edit: results become out of date until recalculated
  await page.fill("#labourHours", "16");
  await expect(page.getByText("Needs recalculating")).toBeVisible();
  await page.getByRole("button", { name: "Recalculate" }).click();
  await expect(page.getByText("$658.40", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Save project" }).click();
  await expect(page.getByRole("status").filter({ hasText: "Project saved" })).toBeVisible();

  // Survives a reload, and the change is in the history
  await page.reload();
  await expect(page.locator("#labourHours")).toHaveValue("16");
  await expect(page.getByText("$658.40", { exact: true })).toBeVisible();
  const history = page.getByRole("region", { name: "Change history" });
  await expect(history.getByText("Labour hours:")).toBeVisible();
  await expect(history.getByText("created the project")).toBeVisible();
});

test("sign out returns to the sign-in screen", async ({ page }) => {
  await page.goto("/login");
  await page.getByRole("button", { name: "Sign in with Microsoft" }).click();
  await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();
  await page.getByRole("button", { name: "Sign out" }).last().click();
  await expect(page).toHaveURL(/\/login$/);
  await page.goto("/projects");
  await expect(page).toHaveURL(/\/login$/);
});
