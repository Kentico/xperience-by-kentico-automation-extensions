# Kentico.Xperience.Labs.AutomationExperiments – Plan

Labs library of reusable Xperience by Kentico (XbyK) automation **triggers**, **conditions**, and **actions** for common marketing scenarios, plus appsettings-driven enable/disable with admin UI visibility.

Research date: 2026-10-04. Source: Kentico docs MCP (docs.kentico.com). Items marked **[verify]** are inferred or not confirmed by docs.

---

## 1. Research summary

### 1.1 Automation basics (marketer view)

Source: [Automation](https://docs.kentico.com/documentation/business-users/digital-marketing/automation)

- Process = trigger + steps, built in **Automation Builder**. Runs per **contact**.
- **Recurrence**: `Always`, `Only once`, `If not already running` (contact must reach *Finish* to re-run).
- **Built-in triggers**: Form submission, Registration (member becomes enabled), Custom activity, Custom (developer).
- **Built-in steps**: Send email (Form autoresponder / Automation purpose), Wait (interval or date), Log custom activity, Set contact field value (single fixed value only), Rule-based condition (contact attributes + activities, e.g. "Contact is in recipient list"), Finish, Custom actions/conditions.
- **Statistics**: per-step passed/waiting counts, refreshed every 30 min. Contacts view shows current step.
- **Disable** = "Disable new triggers". Contacts already inside continue (incl. Wait steps).
- No process versioning. Steps can't be deleted once process enabled / has stats.
- Chaining processes: docs recommend *Log custom activity* → *Custom activity* trigger rather than firing triggers from actions.
- Automation not intended for ongoing newsletters (fixed number of email steps).

### 1.2 Custom actions and conditions

Source: [Custom automation steps](https://docs.kentico.com/documentation/developers-and-admins/digital-marketing-setup/automation-customization/automation-custom-steps)

| Concept | API |
| --- | --- |
| Action (no props) | `AutomationAction`, override `Execute(context, ct)` |
| Action (props) | `AutomationAction<TProperties>`, `TProperties : IAutomationActionProperties` |
| Condition (no props) | `AutomationCondition`, override `Evaluate(context, ct)` → `Task<bool>` |
| Condition (props) | `AutomationCondition<TProperties>`, `TProperties : IAutomationConditionProperties` |
| Registration | `[assembly: RegisterAutomationAction<T>(identifier, displayName, Description, IconName)]` / `RegisterAutomationCondition<T>` |
| Contact | `await context.GetProcessedObject(ct)` → `ContactInfo` |
| Process name | `context.Process.DisplayName` |
| Trigger data | `context.GetTriggerData<T>(ct)`; built-in: `FormSubmissionTriggerData`, `RegistrationTriggerData`, `CustomActivityTriggerData`; helpers `TriggeredByFormSubmission/Registration/CustomActivity`; custom: `GetTriggerIdentifier(ct)` |
| Share data across steps | `SetProcessData<T>` / `GetProcessData<T>`, `T : IAutomationProcessData` (stable `Identifier`), stored as JSON, survives Wait/restarts. One shared slot per type per process. |

Key rules:

- Properties use admin UI form components (`TextInputComponent`, `DropDownComponent` + `IDropDownOptionsProvider`, `CheckBoxComponent`, `NumberInputComponent`, etc.), `FormCategory`, validation and visibility attributes. Optional `StepDisplayName` property overrides the step name input.
- Timeout 2 min + 30 s grace. Action timeout/exception → contact moves on. Condition timeout/exception → **false** branch. False branch = safe default.
- Conditions must be read-only, deterministic, idempotent (may be re-evaluated).
- Steps resolved from DI (constructor injection).
- **Never change identifiers. Unregistering a used step/trigger breaks the process and the Builder can't fix it** until re-registered. Versioning = new `_V2` identifier, keep old registered.
- GDPR: don't put personal data/secrets in properties; process data deleted with contact on erasure; no export of process data.
- `displayName` / `Description` can be localization keys (seen in `examples/DancingGoat/Samples/Automation/SendNotificationAutomationAction.cs`).

### 1.3 Custom triggers

Source: [Custom automation triggers](https://docs.kentico.com/documentation/developers-and-admins/digital-marketing-setup/automation-customization/automation-custom-triggers)

| Concept | API |
| --- | --- |
| Basic | `AutomationTrigger` |
| With data | `AutomationTrigger<TData>`, `TData : IAutomationTriggerData` (stable `Identifier`, `init` props) |
| With data + props | `AutomationTrigger<TData, TProperties>`, `TProperties : IAutomationTriggerProperties` (props-only needs dummy data class) |
| Gate start | override `Evaluate(...)` → `Task<bool>` (default true), 2 min timeout → not started |
| Fire | inject `IAutomationTriggerDispatcher`, `FireTrigger<T>(new AutomationTriggerDispatch(contact[, data]), ct)` |
| Register | `[assembly: RegisterAutomationTrigger<T>(...)]` |

Key rules:

- Triggers stateless (single shared instance). `Evaluate` lightweight and side-effect free.
- Firing enqueues into bounded in-memory queue shared with Form trigger; overflow drops + warns. Setting: `CMSAutomationTriggerQueueCapacity`.
- No process using the trigger → silent no-op. Unregistered trigger / wrong data type → `InvalidOperationException`.
- Fire sources: object event handlers (`IInfoObjectEventHandler<InfoObjectAfterUpdateEvent<ContactInfo>>`), controllers, webhook endpoints, scheduled tasks (`IScheduledTask` + `RegisterScheduledTask`). Scheduled firing **max once per day** recommended.
- Trigger data: small, IDs not PII, UTC dates, null on deserialization failure.

### 1.4 Related platform pieces used by the proposals

- Consents: `IConsentAgreementService.IsAgreed(contact, consent)` ([Consents API](https://docs.kentico.com/api/configuration/consents)).
- Admin UI apps: `UIApplication`, `UIPage`, `ListingPage` (needs object type), custom React templates ([UI pages](https://docs.kentico.com/documentation/developers-and-admins/customization/extend-the-administration-interface/ui-pages)).
- CI/CD object types: `ma.automationaction`, `ma.automationprocess`, `ma.automationstep`, `cms.objectworkflowtrigger` – usable to find which processes use which custom step **[verify how custom step identifiers are stored]**.
- Commerce: orders (`OrderInfo`, `IOrderCreationService`); customer↔contact link is not built in (custom field recommended by docs).
- Member registration maps member → contact by email; may target a different/untracked contact.

### 1.5 Existing repo state

- Package versions: `Kentico.Xperience.*` 31.9.1 (root `Directory.Packages.props`). Older versions not tested (see 8.6).
- `src/Kentico.Xperience.Labs.AutomationExperiments` – empty placeholder (`Class1.cs`).
- `examples/DancingGoat/Samples/Automation` – existing samples: `SendNotificationAutomationAction` (internal email notification), `ContactKeyFieldChangedAutomationTrigger` (+ handler). Library should not duplicate these 1:1.

---

## 2. Enable/disable via appsettings

### 2.1 Constraints

- Registration is via assembly attributes → **everything in the package is always registered**. No conditional registration.
- Must **not** try to unregister anyway: a used-but-unregistered step/trigger bricks the process in the Builder.
- So "disabled" = **registered but inert at runtime**, plus visible state in admin.

### 2.2 Proposed configuration

```json
{
  "AutomationExperiments": {
    "EnabledByDefault": false,
    "Components": {
      "Labs.AutomationExperiments.Action.SendWebhook": { "Enabled": true },
      "Labs.AutomationExperiments.Trigger.InactiveContact": { "Enabled": true, "Schedule": "daily" }
    },
    "Webhooks": {
      "Endpoints": {
        "crm": { "Url": "https://...", "SecretHeaderName": "X-Signature" }
      }
    }
  }
}
```

- Key = component identifier (stable). `EnabledByDefault` lets teams opt-in (safe default `false`) or opt-out.
- Component-specific settings (endpoints, secrets, API keys) live here, **not** in step properties (GDPR/secrets guidance). Step properties pick a **named** endpoint via `IDropDownOptionsProvider`.
- Registration: `services.AddAutomationExperiments(configuration)` binds `IOptionsMonitor<AutomationExperimentsOptions>` (supports reload).

### 2.3 Runtime behavior when disabled

| Kind | Behavior | Why |
| --- | --- | --- |
| Action | `Execute` no-op, log warning (component + process name) | contact continues, nothing external happens |
| Condition | return `false` | false = documented safe default |
| Trigger | firing code (handler, scheduled task, webhook endpoint) skips dispatch; `Evaluate` returns `false` as backstop | avoids filling shared trigger queue |
| Scheduled task / endpoint | task returns early; endpoint returns 404 | no hidden work |

Implementation: thin base classes (`LabsAutomationAction<TProps>`, `LabsAutomationCondition<TProps>`, `LabsAutomationTrigger<TData,TProps>`) that check `IAutomationComponentStatus` then call an inner abstract method. Keeps each component focused on its logic.

### 2.4 Admin UI visibility (options, pick in implementation)

1. **Custom admin application** "Automation experiments" (Configuration category):
   - Lists every package component: kind, display name, identifier, enabled state, config source, and **processes using it** (query `ma.automationstep` / trigger rows) **[verify storage of custom identifiers]**.
   - Callout warning when a disabled component is used by an enabled process.
   - `ListingPage` requires an object type; list is code-defined → likely needs a small custom React template or a simple page with a table **[verify cheapest built-in template option]**.
   - Read-only; config changes happen in appsettings/deployment.
2. ~~**Builder-level hint** via localization ("… (disabled)")~~ – **rejected**. Admin localization uses static `.resx` files registered with `RegisterLocalizationResource` (see 5.5); no documented hook for config-dependent values. Disabled state is shown via option 1, scheduled task *Last result*, and logs instead.
3. **Startup log**: list enabled/disabled components + warnings for disabled-but-used.

Recommendation: do 3 first (trivial), then 1.

---

## 3. Proposed components (20)

Identifier prefix: `Labs.AutomationExperiments.` (final prefix TBD – never change after release).

Legend: **T** trigger, **C** condition, **A** action. "Showcases" = XbyK automation capability demonstrated.

### 3.1 Triggers (6)

| # | Name | Kind | Scenario | How | Marketer properties | Showcases |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | **Contact in contact group (daily)** | T | Start nurture when contact is part of a segment (e.g. "High intent") | Daily scheduled task fires for **all** current group members; duplicates handled by process recurrence (no stored state – see 4.1) | Contact group (dropdown) | scheduled trigger, props-based `Evaluate` filter |
| 2 | **Contact date anniversary** | T | Birthday, signup anniversary, renewal reminder N days before a date field | Daily scheduled task over configured date fields | Date field, offset days (−30..+30), match year? | generalizes docs birthday example, trigger data (date, years) |
| 3 | **Contact inactive for N days** | T | Re-engagement / win-back | Daily task: contacts with last activity older than N days (bounded batch) | Days inactive, activity types to count | time-based audience trigger, queue-safe batching |
| 4 | **Content engagement threshold** | T | Contact views N pages tagged with taxonomy X → interest-based follow-up | `ActivityInfo` after-insert handler counts page visits by tag | Taxonomy tag(s), visit count, lookback days | event-handler trigger, "custom audience engagement" |
| 5 | **Inbound webhook received** | T | External system (CRM, event platform, webinar tool) starts a process for a contact | Authenticated minimal API endpoint, resolves contact by email/GUID, fires with small payload (event name + key/values) | Event name filter | webhook trigger, trigger data with typed payload |
| 6 | **Order completed** | T | Post-purchase thank-you, review request, cross-sell | Order created event / checkout hook; data: order ID, total, currency, item count | Min total, currency, first order only? | commerce integration, data + props trigger |

### 3.2 Conditions (7)

| # | Name | Kind | Scenario | Logic | Marketer properties | Showcases |
| --- | --- | --- | --- | --- | --- | --- |
| 7 | **Has consent agreement** | C | GDPR gate before email/sync steps | `IConsentAgreementService.IsAgreed` | Consent (dropdown) | dynamic dropdown, compliance branching **[verify not already a built-in rule]** |
| 8 | **Activity count in window** | C | "Visited pricing ≥ 3× in 7 days" → sales path | Count `ActivityInfo` by type (+ optional page/value) since now − N days | Activity type, operator, count, days | parameterized activity query, caching |
| 9 | **Within time window** | C | Only send/notify during business hours, weekdays, or a campaign date range; pair with Wait to retry | Compare UTC now (converted to configured time zone) | Days of week, start/end time, time zone, optional date range | process-state branching without contact data |
| 10 | **Random split (A/B)** | C | Test two email sequences | Deterministic hash of contact GUID + step/process → bucket % | Percentage for true branch, salt | deterministic/idempotent condition design |
| 11 | **Started by trigger / trigger data matches** | C | One process, branches per source (form vs. webhook vs. order over X) | `GetTriggerIdentifier`, `TriggeredBy…`, compare known trigger data fields | Trigger (dropdown), optional field + operator + value | trigger data access, built-in + custom trigger data |
| 12 | **Previous step succeeded** | C | Branch on result of webhook/sync/enrich action (e.g. retry or notify on failure) | Reads `StepResultData` process data written by library actions | Step key (matches action's "result key") | **process data sharing** between steps |
| 13 | **Purchase history meets threshold** | C | VIP / repeat customer path | Order count or lifetime total for contact (via customer↔contact mapping) | Metric (count/total), operator, value, lookback days | commerce data in automation **[verify contact↔customer link approach]** |

### 3.3 Actions (7)

| # | Name | Kind | Scenario | Logic | Marketer properties | Showcases |
| --- | --- | --- | --- | --- | --- | --- |
| 14 | **Send webhook** | A | Sync contact/member data to CRM, CDP, Zapier, Power Automate | POST JSON (selected contact fields + process/trigger context) to **named endpoint from appsettings**, HMAC signature, writes `StepResultData` | Endpoint (dropdown), fields to include, result key | external sync, process data, secrets out of props |
| 15 | **Post to Teams / Slack** | A | Notify sales channel about hot lead | Incoming-webhook message with contact summary + admin link | Channel (named in appsettings), message template | internal notification without email |
| 16 | **Adjust contact score** | A | Lightweight lead scoring (XbyK has no built-in scoring) | Increment/decrement/set numeric custom contact field, clamp min/max | Field (numeric), operation, amount, min/max | contact update API, pairs with built-in rule conditions |
| 17 | **Set contact field (dynamic value)** | A | Stamp "LastCampaign = {process}", "LeadStatus", dates – built-in step only sets fixed value | Token values: `{now}`, `{processName}`, `{triggerData.X}`, literal | Field, value template | extends built-in step, trigger data use |
| 18 | **Subscribe / unsubscribe recipient list** | A | Move contact between lists (e.g. remove from "Prospects" after purchase) | Recipient list subscription API; subscribe only if consent/opt-in rules allow **[verify API + double opt-in implications]** | List, operation, require consent (consent dropdown) | email marketing integration |
| 19 | **Assign contact owner (round-robin)** | A | Route new leads to sales reps evenly | Pick next owner from configured list, store in custom field, persisted counter | Owner pool (from appsettings or admin users/roles), field | stateful assignment, pairs with #15 |
| 20 | **Enrich contact from external API** | A | Fill company/country/industry from email domain | Provider abstraction (`IContactEnrichmentProvider`), default sample provider, only fill empty fields, writes `StepResultData` | Provider, field mapping, overwrite? | docs' "enrich contact profiles" use case, timeouts/cancellation |

### 3.4 Added after review

| # | Name | Kind | Scenario | Logic | Marketer properties | Showcases |
| --- | --- | --- | --- | --- | --- | --- |
| 21 | **Write to log** | A | Debug which branch contacts take; lightweight troubleshooting trail | `ILogger` entry (event `AutomationLog`) with process, contact GUID, message, saved step results, trigger details; no PII | Message with placeholders (pre-filled default), level, include step results, include trigger details | step data + trigger data inspection, enablement for dev-only use |

### 3.5 Example compositions (for README / demo processes)

- **Hot lead routing**: #4 Content engagement → #7 consent → #16 score +10 → #8 pricing visits? → #19 assign owner → #15 Teams post.
- **Win-back**: #3 inactive 90 days → #9 business hours → Send email → Wait 7d → #8 any visit? → else #17 set "Dormant".
- **Post-purchase**: #6 order → #13 VIP? → #10 A/B split → emails → #18 remove from prospects list.
- **CRM sync with retry**: Form trigger → #14 webhook → #12 succeeded? → false → Wait 1h → #14 → #12 → false → #15 alert.

---

## 4. Scheduled tasks for recurring triggers

Decision: **in scope.** Triggers #1, #2, #3 only fire from a recurring job. Without it they appear in the Builder but never fire (silent no-op).

Source: [Scheduled tasks](https://docs.kentico.com/documentation/developers-and-admins/customization/scheduled-tasks)

- Task = `IScheduledTask` class + `[assembly: RegisterScheduledTask(identifier, type)]` (ships in package) **and** a task configuration created in the **Scheduled tasks** app (schedule, enabled flag, last result) – per environment, not shippable via attributes.
- Runs on worker thread, no request context. Schedules use **server-local time**.
- Trigger docs: fire from scheduled tasks **at most once per day**.

### 4.1 Decisions

1. **One task per trigger** (not one shared task). Own schedule, own enable toggle, own *Last result*; matches per-component appsettings enablement.
2. **Tie into appsettings enablement.** If the trigger component is disabled, task returns early with result text like `Disabled via appsettings (AutomationExperiments:Components:<id>:Enabled)`. Visible in *Last result* column = free admin visibility.
3. **Task configuration setup: documentation only** (Usage Guide: create configuration, pick implementation, set daily schedule, enable). No auto-creation, no CI/CD-shipped config for now.
4. **Duplicate firing / state:**
   - #2 date anniversary – no state needed (date match is naturally once per period).
   - #3 inactive contact – rely on process recurrence (`Only once` / `If not already running`); document recommended recurrence.
   - Found in testing (#2): "If not already running" lets a finished contact restart, so extra same-day task runs (manual, retry, short interval) re-fire. Documented "run once per day" for now; option later: task stores last processed date and skips repeats.
   - #1 contact group – **no custom object type** for now. Task fires for **every** current group member each run; process recurrence prevents re-entry. Document infra impact:
     - each run enqueues one trigger per member → large groups can saturate the shared in-memory trigger queue (also used by the built-in *Form* trigger); overflow drops triggers + logs warning.
     - mitigations: keep groups targeted, run off-peak, raise `CMSAutomationTriggerQueueCapacity`, use `Only once`/`If not already running` recurrence.
     - `Always` recurrence on such a process re-runs daily for every member – warn explicitly in docs and in trigger `Description`/property `ExplanationText`.
     - future option: store membership snapshot (custom object type) to fire only for new members.
5. **Queue safety:** fire in batches, propagate `CancellationToken`, log counts (fired/skipped) and return them in task result, document `CMSAutomationTriggerQueueCapacity`.

Event-driven triggers (#4 activity handler, #5 endpoint, #6 order event) need no scheduled task.

---

## 5. Implementation guidelines (for implementing agents)

### 5.1 Follow Xperience docs

> **Event log length limits (not in Xperience docs – found in testing):** the `XperienceEventLog` provider stores the logger
> category in `CMS_EventLog.Source` and `EventId.Name` in `CMS_EventLog.EventCode`, both `nvarchar(100)`. Entries exceeding
> either limit are **silently dropped** from the Event log (they still reach other providers, e.g. console). With the
> `Kentico.Xperience.Labs.AutomationExperiments.` prefix (45 chars) full type names (`<Kind>.<Component>.<ClassName>`)
> easily exceed this. **Rule: never inject `ILogger<T>` in the library** – inject `ILoggerFactory` and call
> `CreateLogger(LogCategories.For(AutomationComponentCatalog.<Component>))` (or `LogCategories.CONFIGURATION`).
> Enforced by `LogCategoryLengthTests`. Reported to Kentico core product (2026-10-04):
> `.agent-resources/kentico-issue-event-log-source-length.md`.

Follow best practices from the docs above for automations, logging, error handling – specifically:

- [Custom automation steps – Best practices](https://docs.kentico.com/documentation/developers-and-admins/digital-marketing-setup/automation-customization/automation-custom-steps#best-practices): performance/caching (`IProgressiveCache`), 2 min timeout, cancellation propagation, error handling, GDPR, versioning, condition guidelines, property guidelines, DI.
- [Custom automation triggers – Best practices](https://docs.kentico.com/documentation/developers-and-admins/digital-marketing-setup/automation-customization/automation-custom-triggers#best-practices): stateless, lightweight `Evaluate`, trigger data guidelines, versioning.
- Logging via `ILogger<T>` ([Logging](https://docs.kentico.com/documentation/developers-and-admins/development/logging)): include component identifier, process name, contact ID (not PII). Catch expected errors (`HttpRequestException`, timeouts) when not cancelled; let conditions fail to the false branch.
- Never change identifiers after release; version with `_V2`.
- Use the KentiCopilot `automation-action`, `automation-condition`, `automation-trigger` skills as reference patterns.

### 5.2 Ask when unsure

If unsure about specific features, constraints of automations, or behavior of built-in steps/triggers (e.g. what a *Rule-based condition* already covers, recurrence semantics, Wait behavior, API availability), **stop and ask the user** instead of guessing. Items marked **[verify]** in this plan need confirmation (docs or user) before implementation.

### 5.3 Marketer-friendly UX

Steps are used by marketers, not developers. For every trigger/condition/action:

- **Names**: plain-language, action-oriented (`Send webhook`, `Has consent agreement`, `Contact inactive for N days`). No technical jargon or class-like names.
- **Description** (tooltip in step selector): one sentence on what it does + key requirement (e.g. "Requires a scheduled task configuration.").
- **Properties**: `Label`, `ExplanationText`, sensible defaults, `Order` (10 / 100 increments), `FormCategory` groups (e.g. *General*, *Advanced* collapsed).
- **Tooltips** on dropdown options (`DropDownOptionItem.Tooltip`) for non-obvious choices.
- **Visibility conditions** (`VisibleIfTrue`, `VisibleIfEqualTo`, …) to hide options that don't apply (e.g. date range fields only when "Limit to date range" is checked).
- **Validation rules** with clear messages (required, min/max, email format).
- Don't override `StepDisplayName` – it replaces the builder's pre-filled step name and the value isn't passed to the step's properties.
- Dropdowns populated from real data (`IDropDownOptionsProvider`: consents, contact groups, activity types, named endpoints) instead of free text.
- All marketer-facing text (display names, descriptions, labels, explanation texts, tooltips, validation messages) in localization resources – see 5.5.

### 5.4 Step dependencies

Some components only work with others. Known dependencies:

| Component | Depends on | Failure mode if missing |
| --- | --- | --- |
| #1, #2, #3 triggers | scheduled task configuration (4.1) | trigger never fires |
| #11 Started by trigger / trigger data | trigger data of selected trigger | false branch |
| #12 Previous step succeeded | earlier #14 Send webhook or #20 Enrich with same *result key* | false branch (no process data) |
| #17 Set field – `{triggerData.X}` tokens | process started by trigger providing that data | token empty, warning logged |
| #13 Purchase history | contact↔customer mapping (custom field) | false branch |
| #6 Order completed | firing code in checkout / order event | trigger never fires |
| #15 Teams post with owner | #19 Assign owner earlier (optional) | owner omitted from message |
| #14, #15 | named endpoint/channel in appsettings | action no-op + warning |

Required:

1. **Usage Guide** (`docs/Usage-Guide.md`): per-component "Requires / Works with" section + the dependency table + example compositions (3.4).
2. **Make dependencies apparent in admin UI** (proposal):
   - `Description` tooltip states requirement ("Use after *Send webhook* step.").
   - Property `ExplanationText` on the linking property (e.g. *result key*: "Must match the result key of a preceding *Send webhook* or *Enrich contact* step.").
   - Dropdowns list only statically compatible options (e.g. #11 trigger list = triggers that provide data; #12 = registered result sources (DI, `IEnumerable<StepResultSource>`) + result number 1–5). **Constraint (confirmed by user): steps don't know which process they're in or which other steps it contains**, so the Builder can't validate dependencies per process – rely on descriptions, `ExplanationText`, docs, the admin app check below, and runtime warnings.
   - Prefer fixed, well-known keys over free text for linking steps (e.g. #14/#20 write result under a key chosen from the same dropdown #12 reads from) to reduce misconfiguration.
   - Admin app (2.4) shows a **Dependencies** column and flags enabled processes that use a dependent step without its prerequisite (e.g. #12 without #14/#20, #17 trigger tokens with incompatible trigger) **[verify step/trigger storage for process inspection]**.
   - Runtime: log a warning with process name when a prerequisite is missing (docs' "handle missing process data" guidance).

### 5.5 Admin UI localization

Sources: [Admin UI localization](https://docs.kentico.com/documentation/developers-and-admins/customization/admin-ui-localization), [Integrate custom code – resource files](https://docs.kentico.com/documentation/developers-and-admins/customization/integrate-custom-code#store-application-resources-in-resource-files)

- Strings live in `.resx` files in the package assembly. One resource class per `.cs` file (empty class encapsulating the `.resx`).
- Register: `[assembly: RegisterLocalizationResource(typeof(AutomationExperimentsResources), LocalizationTarget.Server, SystemContext.SYSTEM_CULTURE_NAME)]`.
  - Targets: `Server` (default, resolved per request in API responses), `Client` (bundled to browser at admin load), `Builder` (Page/Form Builder components).
  - DancingGoat sample registers its automation strings with `LocalizationTarget.Builder` – **verified:** `Builder` is sufficient for step names/properties in the Automation Builder (DancingGoat coffee club step). The library registers both `Server` and `Builder` (open item 2).
- Usage patterns seen in `examples/DancingGoat/Samples/Automation`:
  - registration attribute `displayName` / `Description`: plain key (`"labs.automationexperiments.sendwebhook.displayname"`).
  - property attributes (`Label`, `ExplanationText`, …): macro form `"{$labs.automationexperiments.sendwebhook.endpoint.label$}"`.
- Key naming: `labs.automationexperiments.<component>.<element>` (lowercase, matches identifier prefix).
- Fallback: user culture → `en-US` → raw key. So **ship `en-US` for every key**; a missing key shows the raw key to marketers.
- Other languages: optional, add `.<culture>.resx` + registration; map multiple variants via `LogicalName` in `.csproj` or one class per culture (see Training guides repo `TrainingGuides.Admin`).
- Verify coverage with `dotnet run --no-build -- --kxp-export-localization-resources --export-path ./loc-export` (custom resources exported under `custom/`).
- Unit test: every key used in attributes exists in `en-US` `.resx`.

---

## 6. Proposed solution layout

```text
src/Kentico.Xperience.Labs.AutomationExperiments/
  Configuration/       options, AddAutomationExperiments(), component status service
  Core/                gated base classes, StepResultData, token resolver, option providers
  Triggers/            one folder per trigger (trigger, data, props, firing handler/task/endpoint)
  Conditions/
  Actions/
  Admin/               UI application + listing page (enable state, usage)
tests/                 unit tests per component (Evaluate/Execute with fakes)
examples/DancingGoat/  wire-up + appsettings + demo processes (CI/CD repository data)
```

---

## 7. Progress

### Slice 1 (2026-10-04) – implemented and validated in DancingGoat

**Components:** #2 Contact date anniversary (+ `AnniversaryTask` scheduled task), #12 Previous step succeeded, #14 Send webhook, #21 Write to log.

**Core**

- Identifier prefix `Labs.AutomationExperiments.`; config section `AutomationExperiments`; components keyed by short name (`SendWebhook`). Disabled by default; opt-in/opt-out via `EnabledByDefault` + `Components`. Restart after changing (reload via `IOptionsMonitor` not verified).
- `AutomationComponentCatalog`, `IAutomationComponentStatus`, startup `ComponentStatus` log, `DisabledComponentLog` (once/hour/process).
- Step results: `StepResultData` process data keyed `<source>.<number>` (e.g. `SendWebhook.1`). Sources registered via DI (`AddStepResultSource`) so custom steps from other projects/packages opt in to *Previous step succeeded*; condition has two dropdowns (Step + Result number 1–5). Public `StepResultRecorder`, `StepResultNumberOptionsProvider`.
- Skippable steps: `SkippableActionProperties` / `SkippableConditionProperties` base classes ("Step behavior" category, order 10000); skipped actions save `StepResult.CreateSkipped`; skipped conditions follow the selected branch. Applied to all library steps + DancingGoat coffee club. Reason: Xperience blocks deleting steps once a process has contact history.
- Logging: named event IDs in `LogEvents` (41000–41031); loggers created via `ILoggerFactory` + `LogCategories.For(component)` – never `ILogger<T>` (see 5.1).
- Localization: all marketer-facing text in `AutomationExperimentsResources.resx`, registered for `Server` and `Builder`.
- Library assembly discoverable via `<AssemblyAttribute Include="CMS.AssemblyDiscoverableAttribute" />` in the `.csproj` (without it no `Register*` attribute is processed).

**Tests:** 82 unit tests (status, catalog vs. registrations, resx keys, discovery attribute, webhook sender/signature, step result keys/sources, skippable steps, anniversary math, options binding, log categories/event names ≤ 100 chars).

**DancingGoat:** references the library; own `Directory.Build.props`, package versions from root `Directory.Packages.props` (all Kentico packages 31.9.1); Development config enables all components + `local-test` / `local-test-failing` webhook endpoints; dev-only receiver `/automation-experiments/webhook-test`; demo action `CoffeeClubEnrollmentAutomationAction` opting in to *Previous step succeeded*; *Example automation* uses the form submission trigger (CI repository).

**Validated in the running app**

- Builder: steps, condition, trigger, dropdowns, localized texts (`Builder` target sufficient for steps), Step behavior section (hidden required fields not validated).
- All three *Example automation* paths (success, enrollment failure, webhook failure) incl. skipped condition/action, checked against `CMS_EventLog` and `CMS_AutomationHistory`.
- Webhook round trip: signature valid, payload correct, failure → No branch, single `WebhookFailed` warning.
- Anniversary task: fires triggers, trigger data reaches steps, `AnniversaryTaskCompleted` in Event log, disabled component → `Last result` "Disabled via configuration…".
- Disabled component (after restart): `ComponentDisabled` warning, contact continues.

**Findings**

- Event log silently drops entries with logger category / event name > 100 chars → `LogCategories` + guard tests. Reported to Kentico core product: `.agent-resources/kentico-issue-event-log-source-length.md`.
- Custom trigger display name registered as resource key is stored unresolved (`CMS_ObjectWorkflowTrigger`, Event log, CI file names). Report drafted: `.agent-resources/kentico-issue-trigger-display-name-resource-key.md`.
- `StepDisplayName` override in step properties blanks the builder's pre-filled step name and isn't passed to the step → removed.
- "If not already running" lets finished contacts restart → extra same-day anniversary task runs re-fire. Documented "run once per day".
- Recommended anniversary recurrence is "If not already running" only ("Only once" = first year only).

### Open items

1. *7 days before* timing not tested (contact 9, birthday 10-11, ready).
2. Localization: keep `Server` + `Builder` registration or drop `Server`.
3. Trigger display name issue: submit report; decide plain-text trigger names vs. keys + known-issue note.
4. Same-day re-run protection for the anniversary task (deferred; option: store last processed date).
5. Local test data cleanup: contacts 8 and 9, *Contact Anniversary* process, Email queue.
6. Admin UI app (2.4) – needs the [verify] items checked first.
7. Remaining components (16 of the original 20).
8. README still template placeholder; nothing committed yet.

---

## 8. Open questions

1. ~~Final identifier prefix?~~ Resolved: `Labs.AutomationExperiments.`; display text via localization resources (5.5).
2. ~~Default enabled state?~~ Resolved: disabled by default, opt-in/opt-out via appsettings.
3. Admin UI: read-only status page enough, or also DB-stored overrides editable in admin?
4. Commerce components (#6, #13) – include in core package or separate `.Commerce` package?
5. Teams/Slack (#15) and enrichment (#20) – ship real provider or sample/abstract only?
6. Minimum supported XbyK version – currently 31.9.1 (all package references); older versions not tested.
7. ~~Shared vs. per-trigger scheduled tasks?~~ Resolved: per-trigger tasks, docs-only setup (section 4).