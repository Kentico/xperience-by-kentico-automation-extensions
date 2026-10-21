# Implementation spec: Contact inactive for N days

## Selection

Implement trigger #3 from `PLAN.md` (section 3.1). Win-back scenario (PLAN 3.5 composition). Reuses the #2 anniversary
pattern (scheduled task, appsettings-defined values, batching, disabled → *Last result* text). No pending decision blocks it.

Fallback rejected for now: #17 (token design).

## User decisions (2026-10-11)

- **Exact-day match, no state:** task fires for contacts whose last activity local date is exactly today − N days. Each
  contact fires once per threshold crossing (until it becomes active again and later re-crosses).
- **Missed contacts accepted:** server restart / aborted run / skipped day → contacts not reached that day are never fired
  for that crossing (in-memory trigger queue also lost on restart). Document it. No catch-up window, no stored state
  (extending contact / custom object type rejected for now).
- **Multiple runs per day are allowed** if the process uses recurrence *If not already running* and keeps the contact
  in the process ≥ 1 day (e.g. a Wait step). User confirmed: a contact in a Wait step counts as running. Re-runs the same
  day fire the same contacts again; recurrence ignores them. Without a Wait step (short process) → duplicate runs. Document
  in trigger property `ExplanationText` and Usage Guide.
- Contacts **without any activity are skipped** (no fallback to `ContactCreated`).
- **No activity type filter** in v1 (all activity types count).

## Identity

- Name `ContactInactive`; identifier `Labs.AutomationExtensions.ContactInactive` (`AutomationComponentCatalog.IDENTIFIER_PREFIX + NAME`).
  Task identifier `ContactInactiveTrigger.IDENTIFIER + "Task"`. Never change after release.
- Kind: Trigger. Add descriptor `AutomationComponentCatalog.ContactInactive` (+ `All`).
- Folder `Triggers/ContactInactive/`. Mirror `Triggers/ContactDateAnniversary/` structure (trigger, data, properties,
  options, task, days options provider).
- Icon: existing `Icons.*` constant, distinct from `Gift`; verify it exists in 31.9.1 (e.g. a sleep/clock/user icon).
- Display name "Contact inactive for N days". Description (one sentence): starts when a contact's last activity was exactly
  the selected number of days ago; requires a scheduled task configuration.

## Configuration (`AutomationExtensions:ContactInactive`, class `ContactInactiveOptions`)

| Setting | Type | Default | Notes |
| --- | --- | --- | --- |
| `InactiveDays` | `List<int>` | `[]` → effective `[90]` | Valid 1 – 3650; invalid ignored; distinct ascending via `GetInactiveDays()`. Same "collections default empty" rule as `ContactDateAnniversaryOptions`. Each value = one query set per run. Marketers pick only these. |
| `BatchSize` | int | 500 | `Math.Max(1, …)`. |

Bind like `ContactDateAnniversary` options in `AutomationExtensionsOptions`.

## Trigger

- `ContactInactiveTrigger : AutomationTrigger<ContactInactiveTriggerData, ContactInactiveTriggerProperties>`.
- Properties: `InactiveDays` (string, `DropDownComponent`, provider listing `GetInactiveDays()` – mirror
  `DaysBeforeOptionsProvider`), default `"90"`, required. Option text localized ("90 days"). `ExplanationText`
  (`<br>`-separated, `ExplanationTextAsHtml = true`): values come from appsettings; fires once when the last activity
  reaches N days; recommended recurrence *If not already running* + Wait step if the task runs more than once per day;
  contacts without activity never start; missed runs aren't caught up.
- Trigger data (public, persisted – keep names stable): `InactiveDays` (int), `LastActivityDate` (DateTime, date only).
- `Evaluate`: component enabled **and** `int.TryParse(properties.InactiveDays, invariant) == triggerData.InactiveDays`
  (static `Matches` helper like anniversary).
- Not a step → no skippable base class.

## Scheduled task (`ContactInactiveTask : IScheduledTask`)

1. Disabled → return `ScheduledTaskExecutionResult("Disabled via configuration ('<ConfigurationPath>'). No triggers fired.")`.
2. `today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime)`. `ActivityCreated` is **server local time**
   (see `Conditions/ActivityFrequency/ActivityWindow.cs`) – compare in local time.
3. For each N in `GetInactiveDays()`: day = today − N; range `[day 00:00, day+1 00:00)`. Contacts whose
   `MAX(ActivityCreated)` falls in that range (i.e. ≥ 1 activity on that day and none after it).
4. Load contact IDs in batches ordered by `ActivityContactID` (keyset on last ID, `TopN(batchSize)`), then load
   `ContactInfo` for the batch (skip IDs with no contact row), fire `FireTrigger<ContactInactiveTrigger>` with data.
   Propagate `CancellationToken`.
5. Log `ContactInactiveTaskCompleted` (count, today); return `Success`.

Query: prefer `ActivityInfo` provider with `GROUP BY ActivityContactID HAVING MAX(ActivityCreated) >= @start AND
MAX(ActivityCreated) < @end` (or equivalent `NOT EXISTS` later activity) via ObjectQuery/DataQuery; avoid raw SQL string
concatenation of values (parameters only). **Perf check** on local DB `Kentico.SaaS.Prod.Analysis` (298k activities –
read-only, never copy data out): report query time + plan notes (index on `ActivityCreated`/`ActivityContactID`?). If
too slow, report options instead of adding indexes.

DI: `IAutomationTriggerDispatcher`, `IInfoProvider<ActivityInfo>`, `IInfoProvider<ContactInfo>`,
`IAutomationComponentStatus`, `IOptionsMonitor<AutomationExtensionsOptions>`, `TimeProvider`, `ILoggerFactory` (logger via
`LogCategories.For(AutomationComponentCatalog.ContactInactive)` – **never `ILogger<T>`**). Put pure date math in a small
testable helper (e.g. `InactivityCalendar.GetRange(today, days)`).

New `LogEvents`: next free block 41070 `ContactInactiveTaskCompleted`. Names must pass length tests.

## Localization

All texts in `AutomationExtensionsResources.resx`, keys `labs.automationextensions.contactinactive.*`; macro form in
property attributes, plain keys in registration attribute (same as anniversary).

## Documentation

- `docs/Usage-Guide.md`: component entry; appsettings (`InactiveDays`, `BatchSize`); scheduled task setup (create config,
  pick task, daily schedule, enable – same as anniversary section); recurrence guidance (*If not already running* + Wait
  step for multiple daily runs; *Always* = duplicates; *Only once* = never re-enters later); limitations (missed runs not
  caught up, no-activity contacts skipped, all activity types count, local server time); trigger data fields; win-back
  example. Add to enablement example + component table + dependency table (needs scheduled task).
- `examples/DancingGoat/appsettings.Development.json`: enable `ContactInactive`, e.g. `InactiveDays: [1, 7, 90]` (small
  values make local testing possible).
- Don't touch README.

## Tests (existing NUnit project, existing patterns, no new packages)

- Options: defaults, invalid/duplicate values, ordering, binding from configuration.
- Calendar/range helper: range boundaries, month/year/leap-day crossings.
- Trigger `Matches`: equal, different, non-numeric/empty property.
- Days options provider: lists effective values, localized text.
- Task: disabled → result text, no queries (follow existing anniversary task tests if any; report limits of testing
  `ObjectQuery` without Xperience DB).
- Existing catalog/registration, localization key coverage, options binding, log category/event name length tests;
  update `StepRegistryPageTests` counts (components +1, triggers +1).
- Run `dotnet test .\tests\Kentico.Xperience.Labs.AutomationExtensions.Admin.Tests\Kentico.Xperience.Labs.AutomationExtensions.Admin.Tests.csproj --no-restore`; report counts.
- `dotnet build .\examples\DancingGoat\DancingGoat.csproj --no-restore -t:Compile` (running app may lock the DLL).
- Format per `AGENTS.md`: `dotnet format Kentico.Xperience.Labs.AutomationExtensions.slnx --no-restore --exclude ./examples/**`
  – keep formatter changes only in files touched by this task. `git diff --check`.

## Work boundaries

- No commits, no dependency changes, don't edit `PLAN.md` / `STATUS.md`.
- Don't modify the DancingGoat database / CI repository; analysis DBs read-only. Caller handles running-app validation
  (task configuration, firing, recurrence + Wait behavior).
- Stop and report any unresolved platform question instead of guessing.
- Report changed files, commands + results, perf check numbers, limitations.
