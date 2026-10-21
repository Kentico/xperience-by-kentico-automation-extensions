import { expect, type Locator, type Page } from "@playwright/test";
import { adminBaseUrl } from "../shared/config";

// The Step registry tab registered by src/Kentico.Xperience.Labs.AutomationExtensions.Admin:
// Admin/StepRegistryPage.cs ([assembly: UIPage] under AutomationProcessEditSection, slug
// "step-registry", built-in listing template). It is a tab of every automation process,
// so the page object opens a process first. Selectors for the built-in listing UI live here.
const automationListPath = "automation/list";
const tabSlug = "step-registry";
const tabCaption = "Step registry";

export const columnCaptions = ["Name", "Kind", "Identifier", "Status", "Description"];

export class StepRegistryPage {
  constructor(private readonly page: Page) {}

  private get rows(): Locator {
    return this.page.locator('[role="row"][data-testid="table-row"]');
  }

  private get searchInput(): Locator {
    return this.page.getByTestId("search-input");
  }

  // Navigates the way a user would: Automation -> first process -> Step registry tab.
  // The DancingGoat template database ships at least one automation process.
  async gotoViaNavigation(): Promise<void> {
    await this.page.goto(`${adminBaseUrl}/${automationListPath}`);

    const firstProcess = this.rows.locator(`a[href*="/admin/${automationListPath}/"]`).first();
    await expect(firstProcess, "The automation process list is empty; the tab needs a process.").toBeVisible({
      timeout: 30_000,
    });
    await firstProcess.click();

    await this.page.locator(`a[href$="/${tabSlug}"]`).filter({ hasText: tabCaption }).first().click();

    await expect(this.page).toHaveURL(new RegExp(`/admin/${automationListPath}/\\d+/${tabSlug}$`, "i"));
    await this.expectLoaded();
  }

  async expectLoaded(): Promise<void> {
    await expect(this.page.getByRole("columnheader").first()).toBeVisible({ timeout: 30_000 });
    await expect(this.rows.first()).toBeVisible();
  }

  async columnHeaders(): Promise<string[]> {
    return (await this.page.getByRole("columnheader").allInnerTexts()).map((text) => text.trim());
  }

  // The row whose Identifier cell is exactly `identifier`
  row(identifier: string): Locator {
    return this.rows.filter({
      has: this.page.getByTestId("table-cell-Identifier").getByText(identifier, { exact: true }),
    });
  }

  cell(row: Locator, column: string): Locator {
    return row.getByTestId(`table-cell-${column}`);
  }

  // The listing applies the search term on Enter
  async search(term: string): Promise<void> {
    await this.searchInput.fill(term);
    await this.searchInput.press("Enter");
  }

  async rowCount(): Promise<number> {
    return this.rows.count();
  }

  async identifiers(): Promise<string[]> {
    return (await this.rows.getByTestId("table-cell-Identifier").allInnerTexts()).map((text) => text.trim());
  }
}
