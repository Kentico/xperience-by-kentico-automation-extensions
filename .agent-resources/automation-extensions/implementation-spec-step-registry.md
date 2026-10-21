# Step registry

Read-only, global registry tab under `AutomationProcessEditSection`, after
all stock tabs. No process-usage inspection or configuration editing.

## Verified design (31.9.1)

- Contacts: `automation-contacts`, order **300**. Registry: `step-registry`, order **1000**
  per user request, after all stock tabs (custom tabs with higher orders may follow).
  Routing inherits the process edit section and its process ID.
- `DataContainerListingPage` + `TemplateNames.LISTING`: in-memory `IDataContainer`
  rows, built-in filter UI and search/sort/paging; no database object or React module.
- Current table columns, in order: **Name**, **Kind**, **Identifier**, **Status**,
  **Description**. Description is last; registration resource keys are localized,
  plain text remains plain, and missing descriptions are blank. Description is not
  searchable.
- Kind filter: **All** (default), **Trigger**, **Condition**, **Action**, **Unknown**
  (matches unknown configuration rows). Status filter: **All** (default),
  **Enabled**, **Disabled**, **Not managed**, **Not applicable**. Filters combine
  with each other and search; sorting/count/paging operate on matching rows.
  Clearing/resetting filters restores All without changing the search term.
- Installed 31.9.1 uses `PageConfiguration.FilterConfiguration.FormModel`;
  `FilterFormModel` in the current docs is obsolete. `DropDownComponent` defines
  localized labels/options and the All placeholder; null/empty values mean All.
  `FilterCondition(BuilderType = typeof(StepRegistryFilterConditionBuilder))`
  retains standard applied-filter metadata and uses an empty `WhereCondition`.
  **Important:** installed `DataContainerTableExecutor` ignores
  `LoadDataSettings.FilterWhereCondition`. The stock listing command first binds
  incoming values to the configured form model. `LoadDataContainers` applies that
  typed model before the stock executor runs search/sort/count/paging. Kind and
  status predicates use the exact keys displayed in those columns;
  no duplicated effective-status rules or SQL condition parsing.
- Inject `CMS.Automation.Internal.ICustomAutomationTriggerProvider`,
  `ICustomAutomationActionProvider`, and `ICustomAutomationConditionProvider`;
  their `GetAll()` methods supply definitions. Do not access singleton definition stores.
  Trigger definitions are `AutomationTriggerDefinition`; step metadata includes
  `DisplayName`, `Identifier`, and `Description` (inherited from the shared step
  definition base class).
- Join catalog identifiers/kinds to registrations (identifiers case-insensitive,
  matching the platform stores). Only catalog entries use
  `IAutomationComponentStatus`; short-name options keys are case-insensitive.
- Status is **Enabled**/**Disabled** for registered library components,
  **Not managed** for registered third-party definitions, and **Not applicable**
  for unknown configuration keys or catalog entries missing a registration.
- Registered non-library definitions: **Not managed** (never infer disabled).
  Unknown options keys remain visible as **Unknown** rows, but values are not shown
  and do not control third-party definitions. Catalog entries missing registration
  remain visible with **Not applicable** status.
- Rows contain only displayed fields plus `RowId` for unique listing identity.
  Configuration value/source details are not copied into rows.
- Localize headers, names, labels, callouts using existing Server resources.
  Localize description resource keys through `ILocalizationService`, show plain
  descriptions unchanged, and render null descriptions as empty.
  Callouts describe global scope, config ownership, and disabled/skip precedence:
  Random split → Yes (A); other library conditions → No; actions no-op; triggers
  cannot start.
- Public APIs in `Internal` namespaces are version-coupled. Recheck on upgrades.

## Evidence and validation

Kentico docs MCP unavailable in this session; official web fallback used:

- [UI pages / registration / routing](https://docs.kentico.com/documentation/developers-and-admins/customization/extend-the-administration-interface/ui-pages)
- [Listing template](https://docs.kentico.com/documentation/developers-and-admins/customization/extend-the-administration-interface/ui-pages/reference-ui-page-templates/listing-ui-page-template)
- [Columns and callouts](https://docs.kentico.com/documentation/developers-and-admins/customization/extend-the-administration-interface/ui-pages/reference-ui-page-templates/listing-ui-page-template/display-and-format-listing-data)
- [Listing filters (official Markdown)](https://docs.kentico.com/documentation/developers-and-admins/customization/extend-the-administration-interface/ui-pages/reference-ui-page-templates/listing-ui-page-template/filter-listing-data.md)
- [Public callout example](https://github.com/Kentico/community-portal/blob/main/src/Kentico.Community.Portal.Admin/UIPages/ContentTypeGeneralExtender.cs#L29)

`dotnet-inspect` skill/API inspection, installed DLL decompilation, and registration
metadata verified signatures and ordering. Historical baseline tests covered registration
joins, configuration values, status, third-party isolation, unknown keys, localization,
discovery, page configuration, and built-in listing behavior. Current validation follows.
Do not claim rendered runtime validation without actually doing it.

Historical baseline before the display revision: 136/136 full-suite tests (24 new),
library build and DancingGoat compile target both zero warnings/errors; scoped formatting
and whitespace checks passed.
Tests exercise real built-in listing search/sort/paging with injected definitions.
User-supplied DancingGoat screenshot confirms live registration rows, localized callouts,
the original status/configuration columns, tab placement after Contacts, and clipboard-
checklist icon. It predates the display revision below. Interactive search/paging remains
unit-tested, not verified through the screenshot. No app stopped or commit made.

Filter follow-up verified: targeted Admin/localization tests **51/51**, full NUnit
suite **154/154** (18 new filter cases), DancingGoat `--no-restore -t:Compile`
**0 warnings/errors**, scoped formatting and whitespace checks passed. Covers exact
rows for every kind/status, combinations, third-party/unknown/missing registrations,
empty and null All/reset, combined search, filtered sorting/page counts/pages, and
localized dropdown parsing via the installed platform parser. The existing screenshot
predates filters; rendered filter controls, applying/resetting them through the live
browser, and form-command binding remain unverified. No running app stopped or commit made.

## Display revision (2026-10-04)

- Replaced the original columns with **Name**, **Kind**, **Identifier**, **Status**,
  **Description**. Removed Registration, Configured component value, and Configuration
  source. Status is also the filter label; Kind filter behavior is unchanged.
- Kentico 31.9.1 exposes `Description` on the shared `CustomAutomationStepDefinition`
  base class inherited by trigger, action, and condition definitions, verified with
  `dotnet-inspect`. Resource-key descriptions are localized; plain text remains unchanged;
  null descriptions render empty.
- Status still uses `IAutomationComponentStatus` for registered library components.
  Third-party registrations remain **Not managed**; unknown configuration rows and
  unregistered catalog entries show **Not applicable**. Registration matching data is
  retained only where required for filtering, row identity, or matching. Obsolete
  configuration-value and source fields/derivation were removed.
- Earlier screenshot and test counts above document the prior UI, not this revision.
- Current verification: targeted Admin/localization tests **52 passed**; full NUnit
  suite **157 passed**, 0 failed/skipped; DancingGoat `--no-restore -t:Compile`
  succeeded with **0 warnings/errors**. Scoped changed-file formatting and
  `git diff --check` passed. Live rendering remains unverified; running app untouched.
