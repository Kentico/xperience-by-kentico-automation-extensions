# Implementation spec: Contact number field compare

## Selection

Implement condition #22 from `PLAN.md` (section 3.4). Lowest remaining complexity, easy to validate: one contact read, one
integer compare, no task, external service, trigger data, or prerequisite. Reuses `ContactScoreFieldResolver` from
*Adjust contact score* (#16) and completes lightweight scoring: built-in *Rule-based condition* can't compare numbers
(user confirmed 2026-10-05).

Rejected: #11 needs trigger/field comparison decisions; #8 needs activity query design; #17 needs token design;
#1/#3 need scheduled tasks + batching.

## User decisions (2026-10-05)

- Empty field value = **0** (same as *Adjust contact score*). No marketer option.
- Field dropdown = **same fields as Adjust contact score**: reuse `ContactScoreFieldResolver` / `ContactScoreFieldOptionsProvider`
  unchanged (custom `Integer` fields only; no system/PK/reference/dummy). No LongInteger/Decimal.
- Disabled → **No** (`return false`) – existing final decision, don't revisit.

## Identity

- Name `ContactNumberFieldCompare`; identifier `Labs.AutomationExtensions.ContactNumberFieldCompare`
  (`AutomationComponentCatalog.IDENTIFIER_PREFIX + NAME`). Never change after release.
- Kind: Condition. Add descriptor `AutomationComponentCatalog.ContactNumberFieldCompare` (+ `All`).
- Folder `Conditions/ContactNumberFieldCompare/`. Follow `RandomSplitCondition` / `WithinTimeWindowCondition` structure.
- Icon: an existing `Icons.*` constant distinct from the other conditions (`CheckCircle`, `Cogwheel`, `Clock`), e.g. a
  scale/balance/compare/chart icon; verify it exists in 31.9.1 (dotnet-inspect / reflection). Not `Graph` (used by score action).
- Display name "Compare contact number field". Description (one sentence): checks whether an integer custom contact field
  (e.g. a score from *Adjust contact score*) is equal to, greater than, or less than a value; empty counts as 0.

## Properties (`ContactNumberFieldCompareProperties : SkippableConditionProperties`)

| Property | Type | Component | Default | Notes |
| --- | --- | --- | --- | --- |
| `Field` | string | `DropDownComponent`, `ContactScoreFieldOptionsProvider` (reuse) | `""` | Required. Explanation: lists integer custom contact fields; empty = 0; how to add a field (`<br>` + `ExplanationTextAsHtml` if ≥ 3 sentences). |
| `Operator` | string | `DropDownComponent`, new `NumberComparisonOperatorOptionsProvider` | `GreaterThanOrEqual` | Required. Persist enum name. Option texts localized: "is equal to", "is not equal to", "is greater than", "is greater than or equal to", "is less than", "is less than or equal to". Don't rely on option tooltips (don't render). |
| `Value` | int | `NumberInputComponent` | 50 | Any int (negatives allowed). |

Enum `NumberComparisonOperator` (public, persisted by name – never rename): `Equal`, `NotEqual`, `GreaterThan`,
`GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`.

Orders 10/20/30 (no category needed – match `RandomSplitConditionProperties`), or *General* category like the score action;
pick whichever matches the Step behavior section placement of existing conditions. All labels/explanations/validation
messages/option texts in `AutomationExtensionsResources.resx` with `labs.automationextensions.contactnumberfieldcompare.*`
keys, macro form in attributes. Don't override `StepDisplayName`.

## Runtime behavior (`Evaluate`)

1. Disabled → `DisabledComponentLog.Write(logger, …, "sends all contacts through the No branch")`, return `false`.
2. `SkippableStep.TryGetSkippedResult` → log skipped, return the selected branch.
3. Validate: field resolves via `ContactScoreFieldResolver.ResolveFieldName`, operator parses (exact name, ordinal, numeric
   rejected – same as `ContactScoreCalculator.TryParseOperation`). Invalid → warning event `InvalidNumberCompareConfiguration`
   once/hour per process+field (`LogWithIntervalPolicy`, same as score action) with process name + reason; return `false`.
4. `cancellationToken.ThrowIfCancellationRequested()`; `await context.GetProcessedObject(cancellationToken)`.
5. Stored value: reuse `ContactScoreCalculator.GetStoredValue` (null/DBNull → null) → `?? 0`.
6. Result via pure helper `NumberComparison.Compare(int current, NumberComparisonOperator op, int value)`; return it.
7. Read-only: never write the contact. Optional `Debug`-level log (process, contact ID, field, value, result); no PII.

`ContactScoreCalculator` is `internal` in the same assembly – reuse it; if sharing a helper reads cleaner, move the
stored-value conversion to a small shared internal helper, but don't change behavior of the score action.

DI: `IAutomationComponentStatus`, `ContactScoreFieldResolver`, `ILoggerFactory` (logger via
`LogCategories.For(AutomationComponentCatalog.ContactNumberFieldCompare)` – **never `ILogger<T>`**). Resolver is already
registered; options providers resolved like existing ones.

New `LogEvents`: next free block 41050 `InvalidNumberCompareConfiguration` (+ 41051 for the optional debug event). Names
must pass existing length tests.

## Documentation

- `docs/Usage-Guide.md`: component entry (what/when), properties, operators, empty = 0, integer custom fields only (link to
  the Adjust contact score field setup section), disabled → No, skip behavior, dependency/works-with (*Adjust contact score*),
  scoring composition example (score +10 → compare ≥ 50 → sales path). Add to enablement example + component table.
  Update any text saying numeric branching is unavailable / must use built-in rules.
- `examples/DancingGoat/appsettings.Development.json`: enable `ContactNumberFieldCompare` (keep other entries).
- Don't touch README.

## Tests (existing NUnit project, existing patterns, no new packages)

- `NumberComparisonTests`: every operator for less/equal/greater; int.MinValue/MaxValue edges; negative values.
- Operator parsing: valid names, wrong case, numeric strings, null/empty, unknown.
- Configuration validation: empty field, ineligible field, unknown operator.
- Empty stored value treated as 0 (helper layer).
- Options provider: six options, enum-name values, localized texts.
- Gating (disabled → `false` even with skip = Yes; skipped → selected branch; invalid config → `false`, no contact read)
  following `RandomSplitConditionTests`; report limits of reaching `GetProcessedObject` without Xperience container.
- Existing catalog/registration, localization key coverage, skippable properties, log-category/event-name length tests;
  update `StepRegistryPageTests` counts (components +1, conditions +1).
- Run `dotnet test .\tests\Kentico.Xperience.Labs.AutomationExtensions.Admin.Tests\Kentico.Xperience.Labs.AutomationExtensions.Admin.Tests.csproj --no-restore`; report counts.
- `dotnet build .\examples\DancingGoat\DancingGoat.csproj --no-restore -t:Compile` (running app may lock the DLL).
- Format per `AGENTS.md`: `dotnet format Kentico.Xperience.Labs.AutomationExtensions.slnx --no-restore --exclude ./examples/**`
  – keep formatter changes only in files touched by this task. `git diff --check`.

## Work boundaries

- No commits, no dependency changes, don't edit `PLAN.md`.
- Don't modify the DancingGoat database / CI repository – caller handles running-app validation (`ContactScore` field
  already exists in the local DB).
- Stop and report any unresolved platform question instead of guessing.
- Report changed files, commands + results, limitations.
