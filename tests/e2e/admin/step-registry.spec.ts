import { expect, test } from "@playwright/test";
import { columnCaptions, StepRegistryPage } from "./stepRegistryPage";

// Covers the library's admin extension end to end: the Step registry tab under every
// automation process lists the registered triggers, conditions, and actions.
// Statuses depend on configuration (all on in Development, mostly off in CI), so the
// specs only require a status that matches the kind of registration.
test.describe("automation Step registry tab", () => {
  test("is reachable from an automation process and shows the registry columns", async ({ page }) => {
    const registry = new StepRegistryPage(page);

    await registry.gotoViaNavigation();

    expect(await registry.columnHeaders()).toEqual(columnCaptions);
  });

  test("lists library components and third-party steps with their status", async ({ page }) => {
    const registry = new StepRegistryPage(page);
    await registry.gotoViaNavigation();

    // Library component: managed by AutomationExtensions configuration
    const writeToLog = registry.row("Labs.AutomationExtensions.WriteToLog");
    await expect(writeToLog).toHaveCount(1);
    await expect(registry.cell(writeToLog, "Name")).toHaveText("Write to log");
    await expect(registry.cell(writeToLog, "Kind")).toHaveText("Action");
    await expect(registry.cell(writeToLog, "Status")).toHaveText(/^(Enabled|Disabled)$/);

    // DancingGoat's own sample step: registered, but not managed by the library
    const sendNotification = registry.row("DancingGoat.SendNotification");
    await expect(sendNotification).toHaveCount(1);
    await expect(registry.cell(sendNotification, "Status")).toHaveText("Not managed");
  });

  test("filters rows with the listing search", async ({ page }) => {
    const registry = new StepRegistryPage(page);
    await registry.gotoViaNavigation();

    await registry.search("Labs.AutomationExtensions.SendWebhook");

    await expect.poll(() => registry.identifiers()).toEqual(["Labs.AutomationExtensions.SendWebhook"]);
  });
});
