# Implementation spec: Activity frequency

## Selection

Implement condition #8 from `PLAN.md` (3.2), **reframed** as *Activity frequency*. Read-only, one count query, no task,
external service, trigger data, or prerequisite. Reuses `NumberComparisonOperator` / `NumberComparison` from
*Compare contact number field* (#22). Validate in DancingGoat by logging activities (page visits, form submissions).

Rejected now: #11 trigger-data design; #17 token design; #18 API verification; #1/#3 scheduled tasks; #15/#19/#20 providers/state.

## Why reframed (user confirmed 2026-10-05)

Built-in *Rule-based condition* already covers existence: "Contact has performed activity X with value that contains Y in
the last N days", "visited page in the last N days", "clicked link in email in the last N days". It can't check
**how often**. This condition only adds the frequency gap: "visited pricing ≥ 3× in 7 days", "< 2 sessions in 30 days",
"exactly 1 form submission". Description must point marketers to the built-in rule for plain "has done X" checks.

## User decisions (2026-10-05)

- Single activity type (dropdown). No multi-select, no "any activity".
- Optional **value contains** filter (matches built-in rule wording), empty = no filter.
- Rolling window: activities created since now − N days. Days 1–365, default 7.
- Defaults: operator `GreaterThanOrEqual`, count 3.
- Disabled → **No** (`return false`) – existing final decision, don't revisit.

## Identity

- Name `ActivityFrequency`; identifier `Labs.AutomationExtensions.ActivityFrequency`
  (`AutomationComponentCatalog.IDENTIFIER_PREFIX + NAME`). Never change after release.
- Kind: Condition. Add descriptor `AutomationComponentCatalog.ActivityFrequency` (+ `All`).
- Folder `Conditions/ActivityFrequency/`. Follow `ContactNumberFieldCompareCondition` structure closely.
- Icon: existing `Icons.*` constant distinct from other conditions (`CheckCircle`, `Cogwheel`, `Clock`, `Gauge`) and
  actions; e.g. a repeat/counter/chart-like icon. Verify it exists in 31.9.1 (dotnet-inspect / reflection).
- Display name "Activity frequency". Description (one sentence + hint): checks how many times the contact performed an
  activity type in the last N days; for "has performed at least once" use the built-in Rule-based condition.

## Properties (`ActivityFrequencyProperties : SkippableConditionProperties`)

| Property | Type | Component | Default | Notes |
| --- | --- | --- | --- | --- |
| `ActivityType` | string | `DropDownComponent`, new `ActivityTypeOptionsProvider` | `""` | Required. Value = `ActivityTypeName` (code name). Text = localized display name (resolve macros/resource keys via `ILocalizationService` if display names are keys). List **enabled** types sorted by text. |
| `Operator` | string | `DropDownComponent`, reuse `NumberComparisonOperatorOptionsProvider` | `GreaterThanOrEqual` | Required. Reuse texts; if the provider is tied to compare-condition keys, keep reusing them (same wording) – don't duplicate keys. Make provider reusable (move to shared place only if cleaner; don't change persisted values). |
| `Count` | int | `NumberInputComponent` | 3 | Min 0 (`MinimumIntegerValueValidationRule`). |
| `Days` | int | `NumberInputComponent` | 7 | 1–365 (min/max validation rules). |
| `ValueContains` | string | `TextInputComponent` | `""` | Optional. Case-insensitive contains on `ActivityValue` (SQL default collation). Explanation: e.g. page URL part / form name depending on activity type; empty = all. |

Orders 10/20/30/40/50, no category (match compare condition). All labels/explanations/validation messages in
`AutomationExtensionsResources.resx` with `labs.automationextensions.activityfrequency.*` keys, macro form in attributes.
`ExplanationTextAsHtml` + `<br>` only for ≥ 3-sentence texts (PLAN 5.3). Don't override `StepDisplayName`.

**[verify]** what `ActivityValue` holds for common types (page visit, form submission, custom) in 31.9.1 and how the
built-in rule's "with value that contains" matches (only `ActivityValue`?). Report findings; if built-in matches other
columns, match the same columns and document. If unclear, implement `ActivityValue` only and report.

## Runtime behavior (`Evaluate`)

1. Disabled → `DisabledComponentLog.Write(…, "sends all contacts through the No branch")`, return `false`.
2. `SkippableStep.TryGetSkippedResult` → log skipped, return selected branch.
3. Validate (pure helper, e.g. `ActivityFrequencyConfiguration.GetConfigurationError`): activity type non-empty and exists
   (via provider/cached lookup; disabled type = still valid – contact may have old activities), operator parses (reuse
   compare parsing), count ≥ 0, days 1–365. Invalid → warning `InvalidActivityFrequencyConfiguration` once/hour per
   process+type (`LogWithIntervalPolicy`, same as compare), return `false`, no contact read/query.
4. `cancellationToken.ThrowIfCancellationRequested()`; `await context.GetProcessedObject(cancellationToken)`.
5. Count via `IInfoProvider<ActivityInfo>` (or the documented equivalent): `ActivityContactID = contact.ContactID`,
   `ActivityType = type`, `ActivityCreated >= since`, optional `ActivityValue LIKE %value%` (parameterized – never
   concatenate SQL), count only (`GetCountAsync` / `.Count` – no row load), propagate cancellation.
   **[verify]** whether `ActivityCreated` is stored in server-local time or UTC; compute `since` in the same basis
   (pure helper `ActivityWindow.GetSince(DateTime now, int days)` with injected clock/`TimeProvider` if existing code has one).
6. Result via `NumberComparison.Compare(count, op, properties.Count)`. Return it.
7. Read-only. Debug log `ActivityFrequencyEvaluated` (process, contact ID, type, days, count, operator, value, result; no
   PII, don't log `ValueContains`? – it's marketer config, not PII; OK to omit). No caching (fresh per contact).

Note: activity logging may be asynchronous (queued) – activities logged seconds before evaluation may not be counted yet.
Document in Usage Guide if confirmed by docs; otherwise mention as possible delay.

DI: `IAutomationComponentStatus`, `IInfoProvider<ActivityInfo>` (or query abstraction for testability – e.g. internal
`IActivityCounter` with DB implementation, registered in DI, so gating/compare paths are unit-testable; match existing DI
registration style in `AutomationExtensionsServiceCollectionExtensions`), `ILoggerFactory` with
`LogCategories.For(AutomationComponentCatalog.ActivityFrequency)` – **never `ILogger<T>`**.

New `LogEvents`: block 41060 `InvalidActivityFrequencyConfiguration`, 41061 `ActivityFrequencyEvaluated`. Must pass length tests.

## Documentation

- `docs/Usage-Guide.md`: component section (what/when, built-in rule for existence vs. this for frequency), properties,
  operators, window semantics (rolling, time basis), value filter, disabled → No, skip, possible logging delay;
  enablement example + component/log tables; composition example (page visit trigger/process → ≥ 3 pricing visits in 7
  days → sales notification). Follow existing compare-condition section style.
- `examples/DancingGoat/appsettings.Development.json`: enable `ActivityFrequency` (keep other entries).
- Don't touch README.

## Tests (existing NUnit project/patterns, no new packages)

- Window helper: since = now − days; boundaries 1/365; time basis.
- Configuration validation: empty/unknown type, bad operator, negative count, days 0/366.
- Options provider: enabled types only, values = code names, texts localized, sorted.
- Gating (disabled → `false` even with skip = Yes; skipped → selected branch; invalid → `false` without contact read/count),
  following `ContactNumberFieldCompareConditionTests`; counter abstraction faked for compare path if reachable; report
  limits of `GetProcessedObject` without Xperience container.
- Existing catalog/registration, localization key coverage, skippable, log category/event-name length tests; update
  `StepRegistryPageTests` counts (components +1, conditions +1).
- `dotnet test .\tests\Kentico.Xperience.Labs.AutomationExtensions.Tests\Kentico.Xperience.Labs.AutomationExtensions.Tests.csproj --no-restore`; report counts.
- `dotnet build .\examples\DancingGoat\DancingGoat.csproj --no-restore -t:Compile` (running app may lock DLL).
- Format per `AGENTS.md` (`dotnet format --no-restore`); keep formatter changes only in touched files; `git diff --check`.

## Work boundaries

- No commits, no dependency changes, don't edit `PLAN.md`.
- Don't modify DancingGoat database / CI repository – caller handles running-app validation.
- Stop and report unresolved platform questions instead of guessing.
- Report changed files, commands + results, [verify] findings, limitations.
