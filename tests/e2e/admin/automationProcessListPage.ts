import { expect, type Locator, type Page } from "@playwright/test";
import { adminBaseUrl } from "../shared/config";

// Page object for the built-in automation process list (Automation application).
// Selectors for the built-in listing and create form live here.
const automationListPath = "automation/list";

// Created by admin/automation.setup.ts when missing, so admin specs have a process to open
export const e2eProcessName = "E2E automation";

export class AutomationProcessListPage {
  constructor(private readonly page: Page) {}

  private get rows(): Locator {
    return this.page.locator('[role="row"][data-testid="table-row"]');
  }

  async goto(): Promise<void> {
    await this.page.goto(`${adminBaseUrl}/${automationListPath}`);
    await expect(this.page.getByTestId("button-create")).toBeVisible({ timeout: 30_000 });
  }

  // Link of the process row whose name is exactly `name` (the link wraps the whole row)
  processLink(name: string): Locator {
    return this.rows
      .filter({ has: this.page.getByTestId("table-cell-WorkflowDisplayName").getByText(name, { exact: true }) })
      .locator(`a[href*="/admin/${automationListPath}/"]`);
  }

  // Creates the process with the built-in create form unless the list already has it
  async ensureProcess(name: string): Promise<void> {
    await this.goto();
    await this.page.getByTestId("search-input").fill(name);
    await this.page.getByTestId("search-input").press("Enter");

    // The listing reloads after the search; give it time before deciding the process is missing
    const exists = await this.processLink(name)
      .first()
      .waitFor({ timeout: 5_000 })
      .then(() => true)
      .catch(() => false);
    if (exists) {
      return;
    }

    await this.page.goto(`${adminBaseUrl}/${automationListPath}/create`);
    await this.page.getByTestId("WorkflowDisplayName").fill(name);
    await this.page.getByTestId("submit-form-button").click();

    await expect(this.page).toHaveURL(new RegExp(`/admin/${automationListPath}/\\d+/`, "i"), { timeout: 30_000 });
  }

  async openProcess(name: string): Promise<void> {
    await this.goto();
    await this.page.getByTestId("search-input").fill(name);
    await this.page.getByTestId("search-input").press("Enter");

    const link = this.processLink(name).first();
    await expect(link, `Automation process '${name}' not found; admin/automation.setup.ts creates it.`).toBeVisible({
      timeout: 30_000,
    });
    await link.click();
  }
}
