# Implementation spec: Contact in contact group (daily)

## Selection

Implement trigger #1 from `PLAN.md` (section 3.1, scheduled task decisions in 4.1). Segment-based nurture. Reuses the
#2 / #3 pattern (`Triggers/ContactDateAnniversary/`, `Triggers/ContactInactive/`): scheduled task, appsettings-driven
dropdown, batching, disabled → *Last result* text.

## User decisions (2026-10-11)

- **Groups scanned = appsettings list** `ContactGroups` (code names). Dropdown lists only configured groups that exist.
  Rejected: all groups (unbounded load); reading process trigger configs (couples to internal tables). Future option:
  custom settings object managed in the admin UI replaces/extends the appsettings list – keep group lookup behind one small
  class so the source can change later.
- **Contact groups only (dynamic).** Recipient lists (static, also contact groups) are **excluded** – a separate future
  trigger (*Contact in recipient list (daily)*, new catalog item, not in this slice).
- No stored state (PLAN 4.1): task fires for **every** current member each run; process recurrence prevents re-entry.
- Implement after #17 (*Set contact field*) is finished, in the current working tree (slices 8 + #17 uncommitted – keep them).

## Identity

- Name `ContactGroupMember`; identifier `Labs.AutomationExtensions.ContactGroupMember`; task `ContactGroupMemberTrigger.IDENTIFIER + "Task"`.
  Never change after release.
- Kind: Trigger. Descriptor `AutomationComponentCatalog.ContactGroupMember` (+ `All`).
- Folder `Triggers/ContactGroupMember/`. Mirror `Triggers/ContactInactive/`.
- Icon: existing `Icons.*` constant not used by other library steps (verify in 31.9.1; e.g. a users/people icon).
- Display name "Contact in contact group (daily)". Description (one sentence): starts for contacts in the selected contact
  group, checked once per day – requires a scheduled task set up by your developers.

## Configuration (`AutomationExtensions:ContactGroupMember`, class `ContactGroupMemberOptions`)

| Setting | Type | Default | Notes |
| --- | --- | --- | --- |
| `ContactGroups` | `List<string>` | `[]` | Contact group code names. Trimmed, distinct (case-insensitive). Empty → task returns a result text "No contact groups configured in '…'" and fires nothing; dropdown empty. Collections default empty (binding appends). |
| `BatchSize` | int | 500 | `Math.Max(1, …)`. |

Bind like the other trigger options in `AutomationExtensionsOptions`.

## Group resolution (`ContactGroupResolver` – single seam for a future settings object)

- Resolve configured code names to `ContactGroupInfo` (via `IInfoProvider<ContactGroupInfo>`), skipping:
  - unknown code names → warning `ContactGroupNotFound` (task run, with code name),
  - **recipient lists** → warning `ContactGroupIsRecipientList`. **[verify]** how XbyK 31.9.1 marks recipient lists on
    `ContactGroupInfo` (e.g. `ContactGroupIsRecipientList`); stop and report if unclear.
- Used by both the options provider (dropdown: display name + code name) and the task.

## Trigger

- `ContactGroupMemberTrigger : AutomationTrigger<ContactGroupMemberTriggerData, ContactGroupMemberTriggerProperties>`.
- Properties: `ContactGroup` (string code name, `DropDownComponent`, `ContactGroupOptionsProvider`), required, no default.
  `ExplanationText` (`<br>`, `ExplanationTextAsHtml`): lists come from appsettings; fires every day for **every** member;
  use recurrence *If not already running* or *Only once*; *Always* restarts every member daily; membership is updated by
  Xperience's contact group recalculation (can lag).
- Trigger data (public, persisted – stable names): `ContactGroupName` (code name), `ContactGroupDisplayName`.
- `Evaluate`: component enabled **and** code name equals (ordinal ignore case) – static `Matches` helper.
- Add the data type to `TriggerDetailsDescriber` (Write to log) and the #17 `{triggerData.X}` known-trigger list if those
  registries exist in the tree.

## Scheduled task (`ContactGroupMemberTask : IScheduledTask`)

1. Disabled → `ScheduledTaskExecutionResult("Disabled via configuration ('<ConfigurationPath>'). No triggers fired.")`
   (same as #2/#3).
2. No resolvable groups → result text, no triggers.
3. Per group: load member contact IDs in keyset batches (`ContactGroupMemberInfo` where group ID and member type =
   contact – **[verify]** member type values in 31.9.1), ordered by contact ID, `TopN(batchSize)`; load `ContactInfo` via
   `WhereIn`; skip missing; fire `FireTrigger<ContactGroupMemberTrigger>`. A contact in several configured groups fires
   once per group (Evaluate filters by group). Propagate `CancellationToken`.
4. Log `ContactGroupMemberTaskCompleted` with total and per-group counts; return `Success`.

DI: `IAutomationTriggerDispatcher`, `IInfoProvider<ContactGroupMemberInfo>`, `IInfoProvider<ContactInfo>`, resolver,
`IAutomationComponentStatus`, `IOptionsMonitor<AutomationExtensionsOptions>`, `ILoggerFactory`
(`LogCategories.For(AutomationComponentCatalog.ContactGroupMember)` – **never `ILogger<T>`**).

New `LogEvents`: block 41090 (`ContactGroupMemberTaskCompleted`, `ContactGroupNotFound`, `ContactGroupIsRecipientList`).
Check the block is free after #17. Names must pass length tests.

## Localization

`.resx` keys `labs.automationextensions.contactgroupmember.*`; macro form in property attributes, plain keys in registration.

## Documentation

- `docs/Usage-Guide.md`: component entry; appsettings; scheduled task setup (same as #2/#3); recurrence table (*If not
  already running* / *Only once* recommended, *Always* = daily re-run per member); load section (one trigger per member per
  run into the shared in-memory trigger queue also used by *Form* trigger; overflow drops triggers + logs warning; keep
  groups targeted, run off-peak, raise `CMSAutomationTriggerQueueCapacity` – verify setting name in docs, recurrence);
  limitations (no stored state → not "newly joined" only; recipient lists excluded, separate trigger planned; membership
  recalculation lag; missed runs not caught up); trigger data; example (high-intent group → nurture). Enablement example,
  component table, dependency table, log events table.
- `examples/DancingGoat/appsettings.Development.json`: enable component, `ContactGroups` with an existing DancingGoat group
  code name if one is visible in the CI repository (else placeholder + note in report).
- Don't touch README.

## Tests (existing NUnit project, existing patterns, no new packages)

- Options: defaults, trimming, duplicates, empty entries, binding.
- Trigger `Matches`: equal, case-insensitive, different, empty.
- Resolver logic (pure part: filtering unknown / recipient-list groups) with fakes where possible.
- Options provider: only resolved groups, text format.
- Task: disabled → result text, no queries; no groups → result text; query shape if testable like `ContactInactiveTask`.
- Catalog/registration, localization keys, options binding, log name lengths; `StepRegistryPageTests` counts (components +1, triggers +1).
- `dotnet test .\tests\Kentico.Xperience.Labs.AutomationExtensions.Tests\Kentico.Xperience.Labs.AutomationExtensions.Tests.csproj --no-restore`; report counts.
- `dotnet build .\examples\DancingGoat\DancingGoat.csproj --no-restore -t:Compile`.
- Format per `AGENTS.md`: `dotnet format Kentico.Xperience.Labs.AutomationExtensions.slnx --no-restore --exclude ./examples/**`
  – only files touched by this task. `git diff --check`.

## Work boundaries

- No commits, no dependency changes, don't edit `PLAN.md` / `STATUS.md`.
- Don't modify the DancingGoat database / CI repository. Caller handles running-app validation.
- Stop and report unresolved platform questions (recipient list flag, member type values) instead of guessing.
- Report changed files, commands + results, limitations.
