# Implementation spec: Started by trigger (trigger data matches)

## Selection

Implement condition #11 from `PLAN.md` (section 3.2, dependency table 5.4). One process, several start sources → branch per
trigger and optionally on one trigger data value. Reuses the known trigger data registry from #17
(`Actions/SetContactField/TriggerDataTokens.cs`) and Write to log (`Actions/WriteToLog/TriggerDetailsDescriber.cs`).
User approved slice 2026-10-11. Design defaults below chosen by caller – flag anything that doesn't fit the platform.

## Identity

- Name `StartedByTrigger`; identifier `Labs.AutomationExtensions.StartedByTrigger`. Never change after release.
- Kind: Condition. Descriptor `AutomationComponentCatalog.StartedByTrigger` (+ `All`).
- Folder `Conditions/StartedByTrigger/`. Mirror `Conditions/ContactNumberFieldCompare/` (skippable condition base,
  disabled → **No**, PLAN decision 2026-10-05).
- Icon: existing `Icons.*` constant not used by other library steps (verify in 31.9.1; e.g. a flag/signpost/branch icon).
- Display name "Started by trigger". Description (one sentence): checks which trigger started the process and, optionally,
  a value of its trigger data.

## Known triggers (shared registry)

Extract one small internal registry (e.g. `Core/KnownTriggers.cs`): key, display label (resx key), trigger data type,
match function. Use it from the new condition **and** refactor `TriggerDataTokens.KnownTypes` + `TriggerDetailsDescriber`
to read from it where it simplifies without behavior change (existing tests must stay green).

| Key | Label | Match | Data type |
| --- | --- | --- | --- |
| `FormSubmission` | Form submission | `context.TriggeredByFormSubmission` | `FormSubmissionTriggerData` |
| `Registration` | Member registration | `context.TriggeredByRegistration` | `RegistrationTriggerData` |
| `CustomActivity` | Custom activity | `context.TriggeredByCustomActivity` | `CustomActivityTriggerData` |
| `ContactDateAnniversary` | Contact date anniversary | identifier == `ContactDateAnniversaryTrigger.IDENTIFIER` | library data |
| `ContactInactive` | Contact inactive for N days | identifier == `ContactInactiveTrigger.IDENTIFIER` | library data |
| `ContactGroupMember` | Contact in contact group (daily) | identifier == `ContactGroupMemberTrigger.IDENTIFIER` | library data |
| `Other` | Other trigger (enter identifier) | identifier == entered text (ordinal ignore case, trimmed) | none |

Keys are persisted in step properties → stable, never rename. Library trigger labels reuse existing display name resx keys.
**[verify]** built-in trigger data public properties in 31.9.1 (reflection on referenced assembly); fields come from the
type's public readable properties (same rule as `TriggerDataTokens`, excluding `Identifier`).

## Properties (`StartedByTriggerProperties`)

Order 10/100 steps, `FormCategory` like other conditions; skip properties from the base last.

| Property | Component | Notes |
| --- | --- | --- |
| `Trigger` | `DropDownComponent` + options provider (table above) | Required. |
| `OtherTriggerIdentifier` | `TextInputComponent` | Visible if `Trigger` = `Other`. Required then (validate in `Evaluate` → invalid config). Explanation: ask developers for the identifier; `{triggerIdentifier}` in *Write to log* shows it. |
| `CheckTriggerData` | `CheckBoxComponent` | Default false. Explanation: *Other* triggers have no known data. |
| `Field` | `DropDownComponent` | Visible if `CheckTriggerData`. Options: every known trigger's fields, text "`<Trigger label>`: `<Property>`", value `<Key>.<Property>`. Explanation (`<br>`, `ExplanationTextAsHtml`): pick a field of the selected trigger. If 31.9.1 offers a documented way to filter options by another property, prefer it; else this combined list + runtime check. |
| `Operator` | `DropDownComponent` | Visible if `CheckTriggerData`. `Equal`, `NotEqual`, `Contains`, `DoesNotContain`, `StartsWith`, `EndsWith`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `IsEmpty`, `IsNotEmpty`. New enum (don't reuse `TextMatchOperator` / `NumberComparisonOperator` – persisted names must stay independent). Explanation: one line per group (text ops, number/date ops, empty ops). |
| `Value` | `TextInputComponent` | Visible if `CheckTriggerData` and operator not empty-check (custom visibility like `VisibleIfActivityDetailFilterAttribute` if needed). Explanation: numbers invariant (`12.5`), dates `yyyy-MM-dd`, text case-insensitive. |

No personal data in properties (GDPR) – mention in Value explanation ("don't enter personal data").

## Evaluation

1. Disabled → `false` + existing disabled log. Skip handling as other conditions.
2. Trigger match (table). `Other` with empty identifier → invalid config.
3. No match → `false`. Match and `!CheckTriggerData` → `true`.
4. Data check:
   - Field empty / unknown / belongs to another trigger key / `Other` selected → invalid config.
   - `GetTriggerData<IAutomationTriggerData>`; null or wrong type → `false` + warning (missing data).
   - Compare (pure static class `TriggerDataComparison`, unit tested):
     - number types (int/long/decimal/double, nullable) → numeric; `Value` parsed invariant; parse failure → invalid config.
     - `DateTime` → date part vs `yyyy-MM-dd` value (time ignored); parse failure → invalid config.
     - string/Guid/bool/other → formatted text (`ContactFieldValueConverter.Format`), ordinal ignore case. `Equal`/`NotEqual`
       on numbers/dates compare values, text ops on numbers/dates compare formatted text.
     - order ops on text → invalid config. Empty ops: null or whitespace text.
5. Invalid config → `false` + warning with process name, no PII. Exceptions → let fail (Xperience → No), but don't swallow
   cancellation.

Logging: `ILoggerFactory` + `LogCategories.For(AutomationComponentCatalog.StartedByTrigger)` – **never `ILogger<T>`**.
New `LogEvents` block 41100 (`InvalidStartedByTriggerConfiguration`, `StartedByTriggerDataMissing`). Name length tests must pass.

## Localization

`.resx` keys `labs.automationextensions.startedbytrigger.*`; macro form in property attributes, plain keys in registration.
Option labels localized.

## Documentation

- `docs/Usage-Guide.md`: component entry (what, properties, operator/value formats, per-trigger field list incl. built-in,
  *Other* + how to find identifiers, disabled → No, invalid config → No + event), dependency table row (#11), example
  (one nurture process started by Form submission and Contact inactive → branch), enablement example, component table, log
  events table.
- `examples/DancingGoat/appsettings.Development.json`: enable component.
- Don't touch README.

## Tests (existing NUnit project, existing patterns, no new packages)

- Comparison: every operator × text/number/date/null/bool/Guid; invalid number/date value; order op on text.
- Field options: one per known property, value format, excludes `Identifier`; built-in types included.
- Config validation: `Other` empty, field of another trigger, unknown field, `Other` + data check.
- Registry: keys unique, match identifiers equal trigger `IDENTIFIER`s; `TriggerDataTokens` / describer behavior unchanged.
- Condition: disabled → false (skip set to Yes, precedence); trigger mismatch → false; match without data check → true;
  missing data → false. Use fakes like other condition tests; if `AutomationProcessContext` can't be faked, test the pure
  parts and report.
- Catalog/registration, localization keys, log name lengths; `StepRegistryPageTests` counts (components +1, conditions +1).
- `dotnet test .\tests\Kentico.Xperience.Labs.AutomationExtensions.Admin.Tests\Kentico.Xperience.Labs.AutomationExtensions.Admin.Tests.csproj --no-restore`; report counts.
- `dotnet build .\examples\DancingGoat\DancingGoat.csproj --no-restore -t:Compile`.
- Format per `AGENTS.md`: `dotnet format Kentico.Xperience.Labs.AutomationExtensions.slnx --no-restore --exclude ./examples/**`
  – only files touched by this task. `git diff --check`.

## Work boundaries

- No commits, no dependency changes, don't edit `PLAN.md` / `STATUS.md`.
- Don't modify the DancingGoat database / CI repository. Caller handles running-app validation.
- Stop and report unresolved platform questions (built-in trigger data properties, dependent dropdown options) instead of
  guessing.
- Report changed files, commands + results, limitations.

## Revision 1 (2026-10-11, user): list registered triggers

User: use the registered trigger definitions (`CMS.Automation.Internal.CustomAutomationTriggerDefinitionStore.Instance.GetAll()`)
instead of a fixed list. Use the injectable `ICustomAutomationTriggerProvider.GetAll()` (same store, already used by
`Admin/StepRegistry.cs`) for testability. Internal API – accepted, same as the step registry.

- Trigger dropdown = built-in triggers (Form submission, Member registration, Custom activity) + **every registered custom
  trigger** (library #1/#2/#3, DancingGoat samples, any project trigger). Label = localized `DisplayName` (resolve keys like
  the step registry does), sorted. **[verify]** whether built-ins are in the store; if not, keep them from
  `DefaultTriggerIdentifiers`.
- Stored `Trigger` value = **trigger identifier** for all entries (built-ins: `DefaultTriggerIdentifiers` constants). Match =
  ordinal identifier compare. Drop *Other* + `OtherTriggerIdentifier` (unreleased – no migration). Unregistered saved
  identifier still matches by identifier; dropdown/runtime treat it as unknown data type.
- Fields = public readable properties of the definition's `TriggerDataType` (excluding `Identifier`; same rule as
  `TriggerDataTokens`); none when no data type. Field value = property name; configurator narrows options to the selected
  trigger. Runtime: property must exist on the actual trigger data type, else invalid config.
- Data check with a trigger without data type → invalid config (hide *Check trigger data* if feasible, else explain).
- `KnownTriggers` registry: keep only what's still needed (#17 tokens / Write to log stay unchanged in this revision;
  generalizing them to all registered triggers is a separate follow-up).
- Update tests, resx, Usage Guide (registered-trigger list, no *Other*), and drop obsolete keys.
