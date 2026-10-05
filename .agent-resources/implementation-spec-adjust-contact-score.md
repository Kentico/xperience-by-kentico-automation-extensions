# Implementation spec: Adjust contact score

## Selection

Implement action #16 from `PLAN.md`. Lowest remaining complexity that is easy to validate: one contact read + one field write, no scheduled task, external service, trigger data, or prerequisite step. Score math is a pure helper and fully unit-testable.

Rejected: #7 *Has consent agreement* – user confirmed the built-in *Rule-based condition* already offers "Contact has agreed with consent". #11 needs trigger/field comparison decisions; #8 needs activity query design.

## User decisions

- Marketers pick the score field from a **dropdown of integer contact fields** (no free text).
- Built-in XbyK has no lead scoring; this action is the lightweight alternative (see plan 3.3).

## Identity

- Name `AdjustContactScore`; identifier `Labs.AutomationExtensions.AdjustContactScore` (via `AutomationComponentCatalog.IDENTIFIER_PREFIX`). Never change after release.
- Kind: Action. Add descriptor to `AutomationComponentCatalog` (+ `All`).
- Folder `Actions/AdjustContactScore/`. Follow `WriteToLogAction` / `SendWebhookAction` structure.
- Icon: pick an existing `Icons.*` constant that fits (e.g. a chart/graph or plus/minus icon); verify it exists in 31.9.1.
- Display name "Adjust contact score". Description: one sentence – changes a number field of the contact (add, subtract, or set), optional min/max limit; requires an integer custom contact field.

## Properties (`AdjustContactScoreActionProperties : SkippableActionProperties`)

Categories: *General* (`labs.automationextensions.category.general`, existing key) and *Limits* (collapsible), same pattern as `WriteToLogActionProperties`.

| Property | Type | Component | Default | Notes |
| --- | --- | --- | --- | --- |
| `Field` | string | `DropDownComponent`, `ContactScoreFieldOptionsProvider` | `""` | Required. |
| `Operation` | string | `DropDownComponent`, operation options provider (Add / Subtract / Set, localized, tooltips optional) | `Add` | Required. Persist enum name. |
| `Amount` | int | `NumberInputComponent` | 10 | Min 0 validation for all operations (direction comes from Operation; for Set it's the new value). Document. |
| `LimitRange` | bool | `CheckBoxComponent` | false | Limits category. |
| `Minimum` | int | `NumberInputComponent`, `VisibleIfTrue(nameof(LimitRange))` | 0 | |
| `Maximum` | int | `NumberInputComponent`, `VisibleIfTrue(nameof(LimitRange))` | 100 | |

All labels/explanations/validation messages localized in `AutomationExtensionsResources.resx` with `labs.automationextensions.adjustcontactscore.*` keys, macro form in attributes (existing convention). Do not override `StepDisplayName`.

## Field discovery

`ContactScoreFieldResolver` (internal, singleton like `DateFieldResolver`) reads `FormHelper.GetFormInfo(ContactInfo.OBJECT_TYPE, false)` and returns fields that are:

- `FieldDataType.Integer` (not LongInteger, Decimal, Double),
- not the primary key,
- not a system field and not a foreign key / object reference (e.g. `ContactOwnerUserID`, `ContactStatusID`, `ContactCountryID`, `ContactStateID` must never be offered).

Verify in the installed 31.9.1 DLLs (dotnet-inspect skill / reflection) which `FormFieldInfo` members identify system and reference fields (e.g. `System`, `PrimaryKey`, `ReferenceToObjectType`). If no reliable member separates built-in from custom fields, **stop and report** – do not hard-code an exclusion list without approval.

Caption: localized `field.Caption`, fall back to `Name` (same as `DateFieldResolver`). Sort options by caption. Same resolver validates the field at runtime (field still exists and is still an eligible integer field) – this also keeps the column name safe.

## Runtime behavior (`Execute`)

Order, same as existing actions:

1. Disabled (`IAutomationComponentStatus`) → `DisabledComponentLog.Write(...)` (default "does nothing"), return.
2. `SkipStep` → `SkippableStep.LogSkipped`, return.
3. Validate properties: field eligible, operation known, `Amount >= 0`, when `LimitRange` then `Minimum <= Maximum`. Invalid → log warning (new event, once-per-period via `LogWithIntervalPolicy` keyed by process + field) with process name and reason, return without writing. Contact continues (action semantics).
4. `cancellationToken.ThrowIfCancellationRequested()`, `await context.GetProcessedObject(cancellationToken)`.
5. Current value: `contact.GetValue(field)`; null/DBNull → 0.
6. New value via pure helper `ContactScoreCalculator.Calculate(current, operation, amount, limitRange, min, max)`:
   - Add: `current + amount`; Subtract: `current - amount`; Set: `amount`.
   - Use `long` arithmetic, saturate to `int.MinValue..int.MaxValue` (no overflow exceptions).
   - Then clamp to `[min, max]` when `LimitRange`.
7. If new value equals current **and** stored value was not null → no write. Otherwise `contact.SetValue(field, newValue)` and save via injected `IInfoProvider<ContactInfo>` (`SetAsync` if available in 31.9.1, else `Set`). Last write wins under concurrent processes – document.
8. Log `Information` event `ContactScoreAdjusted` with process name, contact ID, field name, old → new value. No PII (no email/name).

No process data / step result recording in this slice.

DI: inject `IAutomationComponentStatus`, `ILoggerFactory` (logger via `LogCategories.For(AutomationComponentCatalog.AdjustContactScore)` – **never `ILogger<T>`**), `IInfoProvider<ContactInfo>`, resolver. Register resolver in `AutomationExtensionsServiceCollectionExtensions` with `TryAddSingleton`.

New `LogEvents` (next free block, e.g. 41040 `ContactScoreAdjusted`, 41041 `InvalidContactScoreConfiguration`); names must pass existing length tests.

## Documentation

`docs/Usage-Guide.md`: component entry (what/when), properties, how to add an integer custom contact field (Contact class field editor; link docs "Add custom fields to contacts"), null = 0, clamp semantics, Set ignores current value, concurrency note, disabled/skip behavior, works well with built-in *Rule-based condition* "Contact field value" rules for branching on the score. Add to enablement example and component table. Do not touch README.

`examples/DancingGoat/appsettings.Development.json`: enable `AdjustContactScore` (keep other entries).

## Tests (existing NUnit project, existing patterns, no new packages)

- `ContactScoreCalculatorTests`: Add/Subtract/Set, null-as-zero handled by caller (test whichever layer owns it), saturation at int bounds, clamp below/above/inside, min == max, Set clamped, limit disabled ignores min/max.
- Property validation helper tests: negative amount, min > max, unknown operation, empty field.
- Field resolver filtering: if `FormInfo` can be constructed in tests, verify integer custom field included and PK/system/reference/non-integer excluded. Report plainly if not testable without Xperience container.
- Gating tests (disabled, skipped) following existing condition/action test approach, as far as the context API allows; report limitations.
- Existing catalog/registration, localization key coverage, skippable properties, log-category/event-name length, and `StepRegistryPageTests` count expectations (update counts: actions +1).
- Run `dotnet test .\tests\Kentico.Xperience.Labs.AutomationExtensions.Tests\Kentico.Xperience.Labs.AutomationExtensions.Tests.csproj --no-restore`; report counts.
- `dotnet build .\examples\DancingGoat\DancingGoat.csproj --no-restore -t:Compile` (app may be running and lock the DLL).
- Format per `AGENTS.md`: `dotnet format Kentico.Xperience.Labs.AutomationExtensions.slnx --no-restore --exclude ./examples/**` – only keep formatter changes in files touched by this task. `git diff --check`.

## Work boundaries

- No commits, no dependency changes, do not edit `PLAN.md`.
- Don't modify the DancingGoat database / CI repository (no new contact field) – caller handles running-app validation.
- Stop and report any unresolved platform question instead of guessing.
- Report changed files, commands + results, limitations.
