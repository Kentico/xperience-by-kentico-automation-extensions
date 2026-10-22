# Implementation spec: Contact in recipient list (daily)

## Selection

Implement trigger #23 from `STATUS.md` (split from #1, `implementation-spec-contact-group-member.md`). Recipient lists are
static contact groups (`ContactGroupIsRecipientList`), excluded by #1. Same pattern as #1
(`Triggers/ContactGroupMember/`): scheduled task, appsettings list, dropdown, keyset batches, disabled → *Last result*.

## Decisions (caller, 2026-10-21)

- **Lists scanned = appsettings list** `RecipientLists` (code names), like #1. Dropdown lists only configured lists that exist.
- No stored state (PLAN 4.1): task fires for **every** current subscribed member each run; process recurrence prevents
  re-entry. Same queue load notes as #1.
- **Only subscribed members.** A contact counts when it is a contact member of the list **and** not unsubscribed / not
  pending double opt-in confirmation. **[verify]** in 31.9.1 how membership, pending confirmation and unsubscribe are
  stored (e.g. `ContactGroupMemberInfo` row added only after confirmation? `EmailSubscriptionConfirmationInfo`? unsubscribe
  removes the member row or sets a flag?). Use the platform's own definition of "subscribed" if a public API exists
  (e.g. a recipient list / subscription service). Stop and report if unclear.
- Work on `main`, in the working tree. Share code with #1 where it stays simple (e.g. resolver lookup, batching), but don't
  change #1 behavior, identifiers, log events or texts.

## Identity

- Name `RecipientListMember`; identifier `Labs.AutomationExtensions.RecipientListMember`; task
  `RecipientListMemberTrigger.IDENTIFIER + "Task"`. Never change after release.
- Kind: Trigger. Descriptor `AutomationComponentCatalog.RecipientListMember` (+ `All`).
- Folder `Triggers/RecipientListMember/` in `.Admin`.
- Icon: existing `Icons.*` constant not used by other library steps (verify in 31.9.1; e.g. a list/envelope icon).
- Display name "Contact in recipient list (daily)" – **plain text** in the registration (like the other triggers, see
  `kentico-issue-trigger-display-name-resource-key.md`); add the `*.displayname` resx key too for step registry fallback,
  same as the other 4 triggers. Description (one sentence): starts for contacts subscribed to the selected recipient list,
  checked once per day – requires a scheduled task set up by your developers.

## Configuration (`AutomationExtensions:RecipientListMember`, class `RecipientListMemberOptions`)

| Setting | Type | Default | Notes |
| --- | --- | --- | --- |
| `RecipientLists` | `List<string>` | `[]` | Recipient list code names. Trimmed, distinct (case-insensitive). Empty → task result text "No recipient lists configured in '…'", fires nothing; dropdown empty. Collections default empty (binding appends). |
| `BatchSize` | int | 500 | `Math.Max(1, …)`. |

Bind like `ContactGroupMemberOptions`.

## List resolution

- Resolve configured code names to recipient lists, skipping:
  - unknown code names → warning `RecipientListNotFound`,
  - contact groups that are **not** recipient lists → warning `RecipientListIsContactGroup` (point to #1).
- Single seam used by the dropdown options provider (display name + code name) and the task.

## Trigger

- `RecipientListMemberTrigger : AutomationTrigger<RecipientListMemberTriggerData, RecipientListMemberTriggerProperties>`.
- Property `RecipientList` (code name, `DropDownComponent`, own options provider), required, no default.
  `ExplanationText` (`<br>`, `ExplanationTextAsHtml`): lists come from appsettings; fires every day for **every**
  subscribed contact; use recurrence *If not already running* or *Only once*; *Always* restarts every contact daily;
  unsubscribed / unconfirmed contacts are skipped.
- Trigger data (public, persisted – stable names): `RecipientListName` (code name), `RecipientListDisplayName`.
- `Evaluate`: component enabled **and** code name equals (ordinal ignore case).
- Implement `ITriggerDataDescription` (Write to log details) and make sure the trigger shows in the trigger store
  (`Core/RegisteredTriggers`) so #11 fields and #17 `{triggerData.X}` work.

## Scheduled task (`RecipientListMemberTask : IScheduledTask`)

1. Disabled → `"Disabled via configuration ('<ConfigurationPath>'). No triggers fired."` (same as #1).
2. No resolvable lists → result text, no triggers.
3. Per list: subscribed contact IDs in keyset batches ordered by contact ID, `TopN(batchSize)`; load `ContactInfo` via
   `WhereIn`; skip missing; `FireTrigger<RecipientListMemberTrigger>`. Contact in several lists fires once per list.
   Propagate `CancellationToken`.
4. Log `RecipientListMemberTaskCompleted` with total + per-list counts; return `Success` with summary text.

Logger via `LogCategories.For(AutomationComponentCatalog.RecipientListMember)` – **never `ILogger<T>`**.
New `LogEvents` block: next free block in `.Admin` `Core/LogEvents.cs` (41130 looks free – Commerce uses 41110–41112;
verify). Names must pass length tests.

## Localization

`.resx` keys `labs.automationextensions.recipientlistmember.*`; macro form in property attributes, like #1.

## Documentation

- `docs/Usage-Guide.md`: component entry mirroring #1 (appsettings, scheduled task setup, recurrence, load, limitations:
  no "newly subscribed only", missed runs not caught up, subscription rules found in verification), trigger data,
  example (newsletter list → welcome series). Enablement example, component/dependency/log event tables.
- #1 Usage Guide text: replace "separate trigger planned" with a link to this component.
- `README.md`: add the component to the component list.
- `examples/DancingGoat/appsettings.Development.json`: enable component, `RecipientLists` with an existing DancingGoat
  recipient list code name if visible in the CI repository (else placeholder + note in report).

## Tests (existing NUnit project, existing patterns, no new packages)

- Options: defaults, trimming, duplicates, empty entries, binding.
- Trigger `Matches`: equal, case-insensitive, different, empty.
- Resolver pure logic: unknown / non-recipient-list filtering.
- Options provider: only resolved lists, text format.
- Subscribed-member query logic (pending / unsubscribed excluded) as far as testable like `ContactGroupMemberTask`.
- Task: disabled → result text, no queries; no lists → result text.
- Trigger store / `ITriggerDataDescription`, catalog/registration, localization keys, log name lengths,
  `StepRegistryPageTests` counts (+1 component, +1 trigger), plain-text display name test.
- `dotnet test` for the Admin and Commerce test projects (`--no-restore`); report counts.
- `dotnet build .\examples\DancingGoat\DancingGoat.csproj --no-restore -t:Compile`.
- Format per `AGENTS.md` (`dotnet format --no-restore`), only touched files; check format diffs (it once inserted
  `throw new NotImplementedException()`). `git diff --check`.

## Work boundaries

- No commits, no dependency changes, don't edit `PLAN.md` / `STATUS.md`.
- Don't modify the DancingGoat database / CI repository. Caller handles running-app validation.
- Stop and report unresolved platform questions (subscription / confirmation / unsubscribe storage) instead of guessing.
- Report changed files, commands + results, verification findings, limitations.
