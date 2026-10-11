# Implementation spec: Set contact field (dynamic value)

## Selection

Implement action #17 from `PLAN.md` (section 3.3). Built-in *Set contact field* step only sets fixed values. No pending
decision, no external service/task. Reuses placeholder formatting (`Actions/WriteToLog/LogMessageFormatter.cs`), trigger data
access (`Actions/WriteToLog/TriggerDetailsDescriber.cs`), field resolver + stored value handling
(`Actions/AdjustContactScore/`), step results (`Core/StepResult*.cs`).

## User decisions (2026-10-11)

- **Two modes:** *Set to value* (template with tokens) and *Calculate* (`current value <operator> operand`, operators
  `+ − × ÷ %`) for number fields. Adjust contact score (#16) stays unchanged (score-specific, clamp).
- **Fields:** all contact fields that can be modified – system and custom. Exclude non-editable ones (see Fields). Document risks.
- **Unresolved token → empty string + warning.** Fail the action only when the error is unrecoverable (value can't be
  converted to the field type, division by zero, overflow, invalid configuration, save error).
- Implemented in the current working tree (slice 8 already in the tree, uncommitted – don't revert or reformat it).

## Identity

- Name `SetContactField`; identifier `Labs.AutomationExtensions.SetContactField`. Never change after release.
- Kind: Action. Descriptor `AutomationComponentCatalog.SetContactField` (+ `All`).
- Folder `Actions/SetContactField/`. Mirror `AdjustContactScore` / `SendWebhook` structure.
- Icon: existing `Icons.*` constant not used by other library steps (verify in 31.9.1; e.g. `Edit`/`Pencil`-like).
- Display name "Set contact field (dynamic value)". Description (one sentence): sets a contact field to a value built from
  placeholders (date, process, trigger data, other fields) or calculates a number from the current value.
- Step result source: register `SetContactField` via `AddStepResultSource` (like Send webhook) so *Previous step succeeded*
  can branch on it. Properties get a *Result number* (1–5) like Send webhook.

## Fields (new `ContactFieldResolver` / `ContactFieldOptionsProvider`)

- Source: `OM_Contact` class form definition (same approach as `ContactScoreFieldResolver`), system + custom fields.
- Exclude: primary key (`ContactID`), `ContactGUID`, `ContactCreated`, `ContactLastModified` (or any system-maintained
  timestamp), dummy/non-DB fields, binary/unsupported types, and fields maintained by the system (inspect the definition
  and list what you exclude + why in the report; e.g. merge/bounce/score-like internal columns if present).
- Supported data types: text, long text, integer, long integer, decimal, double/float, boolean, date, date and time.
  Others (GUID, binary, etc.) excluded. Reference/ID fields (e.g. country, state, status, owner): include only if their
  type is supported; document they need a valid ID.
- Option text: field caption (localized) + code name, like the score field dropdown. Group system vs. custom if simple.

## Properties (`SetContactFieldActionProperties : SkippableActionProperties`)

| Property | Type | Component | Default | Notes |
| --- | --- | --- | --- | --- |
| `Field` | string | `DropDownComponent`, `ContactFieldOptionsProvider` | `""` | Required. |
| `Mode` | string | `DropDownComponent` (enum `SetContactFieldMode`: `SetValue`, `Calculate`, persisted by name) | `SetValue` | Explanation: Calculate only for number fields. |
| `ValueTemplate` | string | `TextAreaComponent` (or text input) | `""` | Visible if `Mode == SetValue`. Explanation lists tokens (`<br>`, `ExplanationTextAsHtml`). Empty result clears the field. |
| `OnlyIfEmpty` | bool | checkbox | false | Visible if `SetValue`. Skip (no write, success result) when field already has a value. |
| `Operator` | string | `DropDownComponent` (enum `ArithmeticOperator`: `Add`, `Subtract`, `Multiply`, `Divide`, `Modulo`) | `Add` | Visible if `Calculate`. Option texts "+ (add)", "− (subtract)", "× (multiply)", "÷ (divide)", "% (remainder)". |
| `Operand` | string | text input | `"1"` | Visible if `Calculate`. Number literal or tokens (e.g. `{triggerData.Years}`); resolved, then parsed invariant. |
| `ResultNumber` | string | dropdown (reuse `StepResultNumberOptionsProvider`) | `"1"` | Same as Send webhook. |

Categories/orders: follow Adjust contact score / Send webhook. All texts in `.resx`, keys
`labs.automationextensions.setcontactfield.*`. Don't override `StepDisplayName`.

## Tokens (`ContactFieldValueTemplate`)

Case-insensitive `{name}` / `{name.part}` (new regex allowing one dotted segment; don't change `LogMessageFormatter`
behavior – reuse constants where sensible):

| Token | Value |
| --- | --- |
| `{now}` | server local date and time (Xperience stores contact dates as `DateTime.Now`; verify and document) – ISO `yyyy-MM-ddTHH:mm:ss` |
| `{today}` | server local date `yyyy-MM-dd` |
| `{processName}` | `context.Process.DisplayName` |
| `{triggerIdentifier}` | trigger identifier or empty |
| `{contactId}`, `{contactGuid}` | processed contact |
| `{contact.<Field>}` | current value of another contact field (invariant format; dates ISO) |
| `{triggerData.<Property>}` | public property of the typed trigger data for known triggers: form submission, registration, custom activity (built-in data types used in `TriggerDetailsDescriber`), contact date anniversary, contact inactive. Reflection over public readable properties, invariant format. Unknown trigger / property → empty + warning. |

Unknown/unresolved token → empty string + one warning (event `SetContactFieldTokenUnresolved`, rate-limited once/hour per
process + token via `LogWithIntervalPolicy`) with process name and token name. Literal braces: no escaping in v1 – document.

## Runtime behavior (`Execute`)

1. Disabled → `DisabledComponentLog.Write(...)`, return (same as other actions).
2. Skipped → `SkippableStep` (save skipped result) like other actions.
3. Validate config: field resolves; mode/operator parse (exact enum name, numeric rejected – same as
   `ContactScoreCalculator.TryParseOperation`); `Calculate` requires a number field. Invalid → warning
   `InvalidSetContactFieldConfiguration` (rate-limited), save failed step result, no write.
4. Load contact (`GetProcessedObject`).
5. **SetValue:** resolve template → convert to field type:
   - text/long text: as is; longer than field size → truncate + warning (`SetContactFieldValueTruncated`).
   - number/boolean/date: empty → null (clear); else parse invariant (boolean: `true/false/1/0`, dates ISO). Parse failure
     → failed result, no write, warning with field + reason (no value content – may be PII).
   - `OnlyIfEmpty` and current value non-empty → no write, success result.
6. **Calculate:** current value (null → 0, like #16) `op` operand → checked arithmetic. Integer fields: integer division
   (truncate), `%` remainder. Decimal/double: normal. Division/modulo by zero, overflow, non-numeric operand → failed result,
   no write, warning.
7. Write via `contact.SetValue` + update through the provider (same as #16 – honour cancellation, no write if unchanged).
8. Save success/failure step result (`StepResultRecorder`). Log success at Debug (process, contact ID, field) – never log
   values (PII).

DI: `IAutomationComponentStatus`, resolver, `IInfoProvider<ContactInfo>` (as #16), `StepResultRecorder`, `TimeProvider`,
`ILoggerFactory` (`LogCategories.For(AutomationComponentCatalog.SetContactField)` – **never `ILogger<T>`**). Keep
conversion, arithmetic and token resolution in pure testable helpers.

New `LogEvents`: block 41080 (`InvalidSetContactFieldConfiguration`, `SetContactFieldTokenUnresolved`,
`SetContactFieldValueTruncated`, `SetContactFieldFailed`, `ContactFieldSet`). Names must pass length tests.

## Documentation

- `docs/Usage-Guide.md`: component entry, modes, token table, conversion rules, failure behavior (empty token vs. failed
  result), OnlyIfEmpty, step result + *Previous step succeeded* pairing, examples (stamp `LastCampaign = {processName}`,
  `LastInactiveDays = {triggerData.InactiveDays}`, counter `+ 1`). **Risks section:** system fields drive built-in
  behavior – email (recipient lists, consent, contact matching/merging), name fields, status/owner/country IDs (must be
  valid IDs), boolean flags; changes apply immediately and aren't reversible by the process; recommend custom fields.
  Add to enablement example, component table, dependency table, log events table.
- `examples/DancingGoat/appsettings.Development.json`: enable `SetContactField`.
- Don't touch README.

## Tests (existing NUnit project, existing patterns, no new packages)

- Token resolution: each token, case-insensitivity, unknown token → empty + reported, dotted names, no tokens, braces.
- Trigger data reflection for each known data type; unknown property.
- Conversion per data type: valid, empty → null, invalid, truncation, boolean variants, ISO dates.
- Arithmetic: each operator for int/long/decimal/double; null current = 0; divide/modulo by zero; overflow (checked);
  negative values; integer division truncation.
- Mode/operator parsing; config validation (Calculate on text field).
- Field resolver: excluded fields absent, supported types present (use a test form definition like the score resolver tests).
- Gating (disabled / skipped / invalid config) like existing action tests.
- Catalog/registration, localization keys, skippable properties, step result source registration, log name lengths;
  `StepRegistryPageTests` counts (components +1, actions +1).
- Run `dotnet test .\tests\Kentico.Xperience.Labs.AutomationExtensions.Tests\Kentico.Xperience.Labs.AutomationExtensions.Tests.csproj --no-restore`; report counts.
- `dotnet build .\examples\DancingGoat\DancingGoat.csproj --no-restore -t:Compile`.
- Format per `AGENTS.md`: `dotnet format Kentico.Xperience.Labs.AutomationExtensions.slnx --no-restore --exclude ./examples/**`
  – keep formatter changes only in files touched by this task. `git diff --check`.

## Work boundaries

- No commits, no dependency changes, don't edit `PLAN.md` / `STATUS.md`.
- Don't modify the DancingGoat database / CI repository. Caller handles running-app validation.
- Stop and report unresolved platform questions instead of guessing (e.g. how contact date values are stored, update API).
- Report changed files, excluded-field list with reasons, commands + results, limitations.
