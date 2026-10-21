import { test } from "@playwright/test";
import { AutomationProcessListPage, e2eProcessName } from "./automationProcessListPage";

// Runs once (the `admin-data` project) after sign-in and before `admin-tests`: the Step
// registry tab lives under an automation process, and a new database (CI) has none.
test("ensure the e2e automation process exists", async ({ page }) => {
  const processes = new AutomationProcessListPage(page);

  await processes.ensureProcess(e2eProcessName);
});
