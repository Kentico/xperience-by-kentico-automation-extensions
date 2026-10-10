# Kentico.Xperience.Labs.AutomationExtensions – Plan

Labs library of reusable Xperience by Kentico (XbyK) automation **triggers**, **conditions**, and **actions** for common marketing scenarios, plus appsettings-driven enable/disable with admin UI visibility.

Research date: 2026-10-04. Source: Kentico docs MCP (docs.kentico.com). Items marked **[verify]** are inferred or not confirmed by docs.

**Current status, open items, and pending decisions: `STATUS.md`** (single source – don't track status here). This file holds research, design rules, the component catalog, decisions, and the historical slice log (7, 8).

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
- `src/Kentico.Xperience.Labs.AutomationExtensions` – empty placeholder (`Class1.cs`).
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
  "AutomationExtensions": {
    "EnabledByDefault": false,
    "Components": {
      "Labs.AutomationExtensions.Action.SendWebhook": { "Enabled": true },
      "Labs.AutomationExtensions.Trigger.InactiveContact": { "Enabled": true, "Schedule": "daily" }
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
- Registration: `services.AddAutomationExtensions(configuration)` binds `IOptionsMonitor<AutomationExtensionsOptions>` (supports reload).

### 2.3 Runtime behavior when disabled

| Kind | Behavior | Why |
| --- | --- | --- |
| Action | `Execute` no-op, log warning (component + process name) | contact continues, nothing external happens |
| Condition | return `false` (**No**) without evaluating | Fails closed, matches Xperience (condition error/timeout → No); administrator disablement overrides marketer skip settings. **Final decision 2026-10-05** (see below) |
| Trigger | firing code (handler, scheduled task, webhook endpoint) skips dispatch; `Evaluate` returns `false` as backstop | avoids filling shared trigger queue |
| Scheduled task / endpoint | task returns early; endpoint returns 404 | no hidden work |

Implementation: thin base classes (`LabsAutomationAction<TProps>`, `LabsAutomationCondition<TProps>`, `LabsAutomationTrigger<TData,TProps>`) that check `IAutomationComponentStatus` then call an inner abstract method. Keeps each component focused on its logic.

### 2.4 Admin UI visibility (options, pick in implementation)

1. **Custom admin application** "Automation extensions" (Configuration category):
   - Lists every package component: kind, display name, identifier, enabled state, config source, and **processes using it** (query `ma.automationstep` / trigger rows) **[verify storage of custom identifiers]**.
   - Callout warning when a disabled component is used by an enabled process.
   - `ListingPage` requires an object type; list is code-defined → likely needs a small custom React template or a simple page with a table **[verify cheapest built-in template option]**.
   - Read-only; config changes happen in appsettings/deployment.
2. ~~**Builder-level hint** via localization ("… (disabled)")~~ – **rejected**. Admin localization uses static `.resx` files registered with `RegisterLocalizationResource` (see 5.5); no documented hook for config-dependent values. Disabled state is shown via option 1, scheduled task *Last result*, and logs instead.
3. **Startup log**: list enabled/disabled components + warnings for disabled-but-used.

Recommendation: do 3 first (trivial), then 1.

---

## 3. Proposed components (20)

Identifier prefix: `Labs.AutomationExtensions.` (final prefix TBD – never change after release).

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
| 8 | **Activity count in window** (reframed as *Activity frequency*: existence is covered by the built-in rule) | C | "Visited pricing ≥ 3× in 7 days" → sales path | Count `ActivityInfo` by type (+ optional page/value) since now − N days | Activity type, operator, count, days | parameterized activity query, caching |
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
| 22 | **Contact number field compare** | C | Branch on lead score (#16) or any integer contact field, e.g. "score ≥ 50" → sales path | Read integer custom contact field (reuse `ContactScoreFieldResolver`), empty = 0 (or option), compare with operator | Field (dropdown), operator (=, ≠, >, ≥, <, ≤), value | **built-in Rule-based condition can't compare numbers (user confirmed 2026-10-05)** – pairs with #16 |
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
2. **Tie into appsettings enablement.** If the trigger component is disabled, task returns early with result text like `Disabled via appsettings (AutomationExtensions:Components:<id>:Enabled)`. Visible in *Last result* column = free admin visibility.
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
> `Kentico.Xperience.Labs.AutomationExtensions.` prefix (45 chars) full type names (`<Kind>.<Component>.<ClassName>`)
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
- ~~**Tooltips** on dropdown options~~ – `DropDownComponent` doesn't render `DropDownOptionItem.Tooltip` (user confirmed 2026-10-05; other components, e.g. object selector, might). Explain options in the property's `ExplanationText` instead – one line per option separated by `<br>` (encoded `&lt;br&gt;` in `.resx`) with `ExplanationTextAsHtml = true`.
- **Long explanation texts (≥ 3 sentences)**: one sentence per line, separated by `<br>` (`&lt;br&gt;` in `.resx`), with `ExplanationTextAsHtml = true` on the attribute. Applied 2026-10-05 to skip-step (action + condition base classes), Send webhook endpoint/contact details, Write to log message, Random split percentage/key, anniversary date field, time window time zone/end date, score field. Not used for registration `Description` (step selector tooltip).
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
- Register: `[assembly: RegisterLocalizationResource(typeof(AutomationExtensionsResources), LocalizationTarget.Server, SystemContext.SYSTEM_CULTURE_NAME)]`.
  - Targets: `Server` (default, resolved per request in API responses), `Client` (bundled to browser at admin load), `Builder` (Page/Form Builder components).
  - DancingGoat sample registers its automation strings with `LocalizationTarget.Builder` – **verified:** `Builder` is sufficient for step names/properties in the Automation Builder (DancingGoat coffee club step). The library registers both `Server` and `Builder` (open item 2).
- Usage patterns seen in `examples/DancingGoat/Samples/Automation`:
  - registration attribute `displayName` / `Description`: plain key (`"labs.automationextensions.sendwebhook.displayname"`).
  - property attributes (`Label`, `ExplanationText`, …): macro form `"{$labs.automationextensions.sendwebhook.endpoint.label$}"`.
- Key naming: `labs.automationextensions.<component>.<element>` (lowercase, matches identifier prefix).
- Fallback: user culture → `en-US` → raw key. So **ship `en-US` for every key**; a missing key shows the raw key to marketers.
- Other languages: optional, add `.<culture>.resx` + registration; map multiple variants via `LogicalName` in `.csproj` or one class per culture (see Training guides repo `TrainingGuides.Admin`).
- Verify coverage with `dotnet run --no-build -- --kxp-export-localization-resources --export-path ./loc-export` (custom resources exported under `custom/`).
- Unit test: every key used in attributes exists in `en-US` `.resx`.

---

## 6. Proposed solution layout

```text
src/Kentico.Xperience.Labs.AutomationExtensions/
  Configuration/       options, AddAutomationExtensions(), component status service
  Core/                gated base classes, StepResultData, token resolver, option providers
  Triggers/            one folder per trigger (trigger, data, props, firing handler/task/endpoint)
  Conditions/
  Actions/
  Admin/               UI application + listing page (enable state, usage)
tests/                 unit tests per component (Evaluate/Execute with fakes)
examples/DancingGoat/  wire-up + appsettings + demo processes (CI/CD repository data)
```

---

## 7. Progress (historical log)

Append-only record of each slice as it was delivered. "Open"/"unverified" notes reflect that time – current state is in `STATUS.md`.

### Slice 1 (2026-10-04) – implemented and validated in DancingGoat

**Components:** #2 Contact date anniversary (+ `AnniversaryTask` scheduled task), #12 Previous step succeeded, #14 Send webhook, #21 Write to log.

**Core**

- Identifier prefix `Labs.AutomationExtensions.`; config section `AutomationExtensions`; components keyed by short name (`SendWebhook`). Disabled by default; opt-in/opt-out via `EnabledByDefault` + `Components`. Restart after changing (reload via `IOptionsMonitor` not verified).
- `AutomationComponentCatalog`, `IAutomationComponentStatus`, startup `ComponentStatus` log, `DisabledComponentLog` (once/hour/process).
- Step results: `StepResultData` process data keyed `<source>.<number>` (e.g. `SendWebhook.1`). Sources registered via DI (`AddStepResultSource`) so custom steps from other projects/packages opt in to *Previous step succeeded*; condition has two dropdowns (Step + Result number 1–5). Public `StepResultRecorder`, `StepResultNumberOptionsProvider`.
- Skippable steps: `SkippableActionProperties` / `SkippableConditionProperties` base classes ("Step behavior" category, order 10000); skipped actions save `StepResult.CreateSkipped`; skipped conditions follow the selected branch. Applied to all library steps + DancingGoat coffee club. Reason: Xperience blocks deleting steps once a process has contact history.
- Logging: named event IDs in `LogEvents` (41000–41031); loggers created via `ILoggerFactory` + `LogCategories.For(component)` – never `ILogger<T>` (see 5.1).
- Localization: all marketer-facing text in `AutomationExtensionsResources.resx`, registered for `Server` and `Builder`.
- Library assembly discoverable via `<AssemblyAttribute Include="CMS.AssemblyDiscoverableAttribute" />` in the `.csproj` (without it no `Register*` attribute is processed).

**Tests:** 84 unit tests (status, catalog vs. registrations, resx keys, discovery attribute, webhook sender/signature, step result keys/sources, skippable steps, anniversary math including the 7-day offset calculation, options binding, log categories/event names ≤ 100 chars).

**DancingGoat:** references the library; own `Directory.Build.props`, package versions from root `Directory.Packages.props` (all Kentico packages 31.9.1); Development config enables all components + `local-test` / `local-test-failing` webhook endpoints; dev-only receiver `/automation-extensions/webhook-test`; demo action `CoffeeClubEnrollmentAutomationAction` opting in to *Previous step succeeded*; *Example automation* uses the form submission trigger (CI repository).

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

### Slice 2 (2026-10-04) – Random split (A/B), implemented and unit-validated

**Selection:** #10 Random split (A/B) is the lowest-complexity remaining component: pure deterministic branching, no scheduled task, database query, external service, or prerequisite component. #9 needs time-zone/range semantics; #11 needs trigger/field comparison decisions; #16 needs contact field discovery and writes.

**Spec:** `.agent-resources/implementation-spec-random-split.md`. Implementation delegated to a subagent, reviewed and validated by the caller.

**Implementation**

- `Conditions/RandomSplit`: registered `RandomSplitCondition`, localized properties, and internal `RandomSplitBucket` helper; identifier `Labs.AutomationExtensions.RandomSplit`, config key `RandomSplit`.
- Existing catalog, disabled-component logging, skip controls, registration, localization, and log-length guard patterns reused. All disabled library conditions follow Yes (A) without evaluation; administrator disablement overrides marketer skip settings. When enabled, skipped conditions follow the selected branch before active evaluation.
- Integer Yes percentage 0–100 (default 50), required split key (default `default`). Runtime rejects invalid percentages, blank keys, and empty contact GUIDs; cancellation propagated.
- User-confirmed identity: contact GUID + split key, not process display name. Public process metadata exposes only `DisplayName`, not a stable process/step ID. Same key shares assignments across processes; different keys allow independent splits; renaming a process preserves assignments.
- Stable algorithm: UTF-8 of lowercase GUID `D` format + `:` + exact case-sensitive key → SHA-256 → first four bytes as unsigned big-endian integer modulo 100. Yes when bucket < percentage; 0 always No, 100 always Yes. Allocation approximate, not an exact population count.
- `docs/Usage-Guide.md` updated with enablement, component entry, properties, identity/change semantics, dependencies, and A/B email composition.

**Validation**

- `dotnet test .\tests\Kentico.Xperience.Labs.AutomationExtensions.Tests\Kentico.Xperience.Labs.AutomationExtensions.Tests.csproj --no-restore`: **108 passed, 0 failed/skipped** (24 new cases).
- Fixed hash vectors including UTF-8, case sensitivity, repeatability, exact thresholds, 0/100, monotonic percentages, invalid configuration/GUID, deterministic 10,000-contact allocation within 45–55%, disabled/skip precedence, and cancellation covered.
- `dotnet build .\examples\DancingGoat\DancingGoat.csproj --no-restore`: **0 warnings/errors**. Subagent's scoped formatting verification passed; caller's `git diff --check` passed.
- **Running-app feedback:** user reached the disabled condition in DancingGoat's *Sample requests* process and observed No (B). Requested disabled behavior is Yes (A); corrected the return value, warning text, docs, and regression coverage. Enabled split/contact retrieval remains unverified in the running app. Isolated tests construct the nonpublic platform context for gating, but reading `ContactGUID` needs an initialized Xperience container. Production evaluation calls the tested bucket helper; no duplicate calculation.
- **Disabled-branch correction validation:** 110 unit tests passed, including disabled behavior with skip off/Yes/No and invalid properties. DancingGoat `dotnet build --no-restore -t:Compile` passed; full build could not replace the DLL locked by the running app. Stop, rebuild, and restart DancingGoat to load the correction. `git diff --check` passed.
- **Marketer clarity:** localized Builder description and percentage help explain disabled Yes (A) routing regardless of percentage/skip settings. Shared condition skip help explains that administrator disablement takes precedence. These describe static rules, not live configuration status; marketers are directed to their administrator for current status. No configuration-dependent localization hook assumed (see 2.4).
- **Clarity validation:** 112 unit tests passed, including two Builder-help regression cases; DancingGoat compile target passed with 0 warnings/errors; whitespace check passed. Rendering of the updated help in the running Builder remains unverified.
- Existing anniversary work and other worktree changes preserved; no commit.

### Slice 3 (2026-10-04) – Step registry tab, implemented and unit-validated

**Spec:** `.agent-resources/implementation-spec-step-registry.md`.

**Research:** Kentico docs MCP was unavailable to the implementing session. Used official
web docs for UI pages/registration/routing, listing configuration, and callouts; fetched
the community-portal `ContentTypeGeneralExtender` example. `dotnet-inspect` skill and
installed 31.9.1 DLL inspection verified the real APIs (citations in the spec).

**Initial implementation (historical; display superseded 2026-10-04)**

- `Admin/StepRegistryPage`: discoverable assembly-level `UIPage`, parent
  `Kentico.Xperience.Admin.DigitalMarketing.UIPages.Internal.AutomationProcessEditSection`,
  slug `step-registry`, localized caption, built-in `TemplateNames.LISTING`.
- Installed registration metadata: Builder order 100, Statistics 200, Contacts 300.
  Registry order **1000** per user request, after all stock tabs including Contacts
  (other custom tabs with higher orders may follow). Its route inherits the selected
  process edit-section path/ID; content is explicitly **global**, not process usage.
- `DataContainerListingPage` supplies in-memory rows with built-in search/sort/paging.
  No custom database object or React module. Two localized `PageConfiguration.Callouts`
  above the table explain global/config scope and all disabled/skip precedence rules.
- Injected `ICustomAutomationTriggerProvider`, `ICustomAutomationActionProvider`, and
  `ICustomAutomationConditionProvider` supply definitions through `GetAll()`, replacing
  direct singleton definition-store access. Trigger definitions are
  `AutomationTriggerDefinition`; action/condition providers return their respective
  custom definition types. Metadata includes identifier, display name, and description.
- Join registered definitions to catalog by case-insensitive identifier and kind.
  Library status uses existing `IAutomationComponentStatus`; bound current options
  show per-component `true`/`false` overrides and explicit/default source paths.
  Default enablement remains false. Sources are options paths, not provider/file provenance.
- Other registrations are **Not managed**, never inferred disabled. Unknown options keys
  remain visible as **Unregistered configuration entry** rows with their values; they
  cannot control third-party registrations. Missing catalog registrations remain visible.
  Structural Builder steps outside the registration stores are not listed.
- Existing Server localization registration and resources reused; Usage Guide explains
  scope, status rules, navigation, configuration, and public-`Internal` version coupling.
- No process-usage/dependency inspection or configuration editing implemented.

**Initial validation (historical; original table UI)**

- Full NUnit suite: **136 passed, 0 failed/skipped** (24 new registry/page cases).
  Covers all three kinds, default-disabled/default-enabled, case-insensitive explicit
  overrides and identifier matching, custom status-service reuse, third-party isolation,
  unknown keys/values, missing registrations, current-option reads, localized row shape,
  assembly discovery, real stock tab order, and callout content.
- Built-in data-container listing tested through its protected platform load method:
  localized search, sorting, paging/counts, cancellation and store-error propagation.
  Tests inject registration definitions; user-supplied DancingGoat screenshot subsequently confirms live registration rows render.
- Library `dotnet build --no-restore`: **0 warnings/errors**.
- DancingGoat `dotnet build --no-restore -t:Compile`: **0 warnings/errors**.
  No full output-copy build attempted for this tab because the running app's DLL lock
  was reported by the caller.
- Scoped `dotnet format --no-restore --verify-no-changes` and `git diff --check` passed.
- Order changed to 1000 on user request; updated stock-tab ordering regression passed
  (1/1), library/test projects rebuilt successfully, whitespace check passed.

**User-supplied running-app evidence (2026-10-04; original table UI):** DancingGoat *Sample requests*
shows Step registry after Contacts, with the clipboard-checklist icon, localized callouts,
column headings, and eight registration rows. Random split displays Disabled / Not set /
Default: `AutomationExtensions:EnabledByDefault`; explicitly enabled library components
and Not managed DancingGoat registrations display correctly. Screenshot confirms
rendering and live store integration, not interactive search/paging or automation execution.
Existing anniversary/Random split work preserved; no commit.

**Filter follow-up (2026-10-04; original table UI):**

- Added localized **Kind** (Trigger/Condition/Action/Unknown) and **Library status**
  (Enabled/Disabled/Not managed/Not applicable) dropdowns, both defaulting to **All**.
  Both filters AND together and with search; filtered totals, sorting, and paging
  retain the built-in behavior. No column/registration/order/icon changes.
- Official `filter-listing-data.md` and installed 31.9.1 DLL inspection verified
  `ListingConfiguration.FilterConfiguration.FormModel`, `DropDownComponent`,
  `FilterCondition`, and `IWhereConditionBuilder.Build(string, object)`.
  The docs' `FilterFormModel` alias is obsolete. The stock data-container executor
  ignores SQL filter conditions, so the page filters the stock-bound typed model
  in `LoadDataContainers` before built-in search/sort/count/paging. A no-op condition
  builder preserves standard filter metadata without generating unused SQL.
  Filter predicates share displayed kind/status keys, including third-party and
  missing/unknown rows; no duplicated status rules.
- Targeted Admin/localization tests **51 passed**; full suite **154 passed**
  (18 new filter cases), 0 failed/skipped. Exact matching rows, all kinds/statuses,
  combined filters/search, null/empty All/reset, localization, filtered sorting,
  page counts/pages, third-party, unknown, and missing registration covered.
- DancingGoat `dotnet build --no-restore -t:Compile`: **0 warnings/errors**.
  Scoped formatting and `git diff --check` passed; existing dirty work preserved.
- Screenshot evidence above remains valid for the original rendered registry.
  It predates filters: live filter UI, apply/reset interactions, and form-command
  binding remain unverified. Running app untouched; no commit.

**Display revision (2026-10-04; current UI):**

- Current columns are **Name**, **Kind**, **Identifier**, **Status**, **Description**.
  Removed Registration, Configured component value, and Configuration source.
  The filter is labeled **Status**; Kind choices/behavior are unchanged.
- Kentico 31.9.1's shared `CustomAutomationStepDefinition` exposes `Description` for
  trigger/action/condition definitions, verified with `dotnet-inspect`. Description
  resource keys are localized; plain descriptions remain plain; null descriptions are
  empty. Description is last and is not searchable.
- Effective library state still comes from `IAutomationComponentStatus`. Third-party
  registrations show **Not managed**; unknown configuration rows and unregistered
  catalog entries show **Not applicable**. Configuration values/source are not displayed.
- Historical screenshot and test evidence above cover the original UI only. Current
  targeted Admin/localization tests passed (**52**), full NUnit suite passed (**157**),
  and DancingGoat `--no-restore -t:Compile` succeeded with **0 warnings/errors**.
  Scoped changed-file formatting and `git diff --check` passed. Live rendering remains
  unverified; running app stays untouched.

### Slice 4 (2026-10-05) – Adjust contact score, implemented and validated in DancingGoat

**Selection:** #16 lowest remaining complexity: one contact read + one field write, no task, external service, or prerequisite step. #7 *Has consent agreement* dropped – user confirmed built-in *Rule-based condition* already has "Contact has agreed with consent".

**Spec:** `.agent-resources/implementation-spec-adjust-contact-score.md`. Implemented by subagent, reviewed by caller.

**Implementation**

- `Actions/AdjustContactScore`: identifier `Labs.AutomationExtensions.AdjustContactScore`, config key `AdjustContactScore`, icon `Icons.Graph`.
- Properties: Field (dropdown), Operation (Add/Subtract/Set, tooltips), Amount (int ≥ 0, default 10), Limits category: Limit range + Minimum/Maximum (visible if checked). Inherits `SkippableActionProperties`.
- Field choice (user decision): dropdown of contact class fields that are Integer, not PK, not `System`, no `ReferenceToObjectType`, not dummy. Built-in contact fields are all `system="true"` (incl. `ContactOwnerUserID`, `ContactCountryID` etc. without reference metadata), so `FormFieldInfo.System` is the separator. `ContactScoreFieldResolver` (public, DI singleton) re-validates the field at runtime → column name safe.
- Runtime: disabled → no-op + `DisabledComponentLog`; skipped → no-op; invalid config (missing/ineligible field, unknown op, negative amount, min > max) → warning `InvalidContactScoreConfiguration` (41041, once/hour per process+field), no write. Empty value = 0; `long` math saturated to int range, then clamp. No write when unchanged and value not empty. Save via `IInfoProvider<ContactInfo>.SetAsync`; last write wins under concurrency. Info log `ContactScoreAdjusted` (41040): process, contact ID, field, old → new (no PII).
- No step result recording. Min ≤ max only checked at runtime (no cross-field validation attribute).
- Usage Guide: component entry, enablement, tables, how to add an integer custom contact field. DancingGoat Development config enables the component.

**Validation**

- Full NUnit suite: **241 passed**, 0 failed/skipped (82 new: calculator, resolver filtering, gating, invalid config, cancellation, localization). Step registry counts updated.
- Library build and DancingGoat `-t:Compile`: 0 warnings/errors. Scoped format (unrelated formatter changes reverted) and `git diff --check` passed.
- Running app: added integer column `ContactScore` to `OM.Contact` via Management MCP. **User confirmed field appears in the step's Field dropdown.** Field (caption "Score") then added via MCP to all four contact UI forms: Contact edit, Profile overview (NumberInput), Personal details - read only (NumberWithLabel; TextWithLabel rejected for integer), Set contact field value automation step (also enables built-in step).
- **Execution verified (2026-10-05):** form submission → *Newsletter sign up* process → step wrote score; user screenshot shows contact 3 Score = 10 in Personal details. App log: `ContactScoreAdjusted` [41040] "contact 3: field 'ContactScore' changed from (empty) to 10" and contact 4 (empty) → 25.
- User confirmed: Event log entry for contact 4 (empty) → 25 (Xperience Event log, not only console), and Limit range clamp works in running app.
- CI repository not enabled in DancingGoat → `ContactScore` column/UI form fields exist only in the local DB (recreate per environment; not shipped).
- Unit tests can't reach the write path without the Xperience container (covered by the running-app check above).
- User confirmed built-in rule conditions **cannot** do numeric comparisons → Usage Guide corrected; new condition #22 *Contact number field compare* added to 3.4 (library must solve this). Option tooltips don't render in `DropDownComponent` → removed; Add/Subtract/Set descriptions moved into the Operation `ExplanationText` (3 resx keys + test cases removed; suite 238 passed).

### Slice 5 (2026-10-05) – Compare contact number field, implemented and unit-validated

**Selection:** #22 lowest remaining complexity: one contact read + int compare, no task/external service/prerequisite. Reuses #16 field resolver; completes scoring (built-in rules can't compare numbers).

**Spec:** `.agent-resources/implementation-spec-contact-number-field-compare.md`. Implemented by subagent, reviewed by caller.

**User decisions:** empty field = 0 (no option); Field dropdown = same fields as *Adjust contact score* (custom `Integer` only, no LongInteger/Decimal).

**Implementation**

- `Conditions/ContactNumberFieldCompare`: identifier `Labs.AutomationExtensions.ContactNumberFieldCompare`, config key `ContactNumberFieldCompare`, display name "Compare contact number field", icon `Icons.Gauge` (verified in 31.9.1).
- Properties (no category, order 10/20/30): Field (reuses `ContactScoreFieldOptionsProvider`), Operator (enum `NumberComparisonOperator` persisted by name: Equal, NotEqual, GreaterThan, GreaterThanOrEqual (default), LessThan, LessThanOrEqual; localized texts, no tooltips), Value (int, default 50, negatives allowed). Inherits `SkippableConditionProperties`.
- Runtime: disabled → No; skipped → selected branch; invalid field/operator → No + warning `InvalidNumberCompareConfiguration` (41050, once/hour per process+field), no contact read; else read contact, empty = 0 (reuses `ContactScoreCalculator.GetStoredValue`), pure `NumberComparison.Compare`. Debug log `ContactNumberFieldCompared` (41051, no PII). Read-only.
- Usage Guide: component section, enablement/component/dependency/log tables, scoring example (+10 → ≥ 50 → sales path); removed "planned" note in Adjust contact score. DancingGoat Development config enables it.

**Validation**

- Full NUnit suite: **310 passed**, 0 failed/skipped (72 new: all operators, int bounds, negatives, operator parsing, config validation, empty = 0, gating incl. disabled → No with skip = Yes, invalid config returns before contact read, cancellation, option providers). Step registry counts +1. Re-run by caller: 310 passed.
- DancingGoat `-t:Compile`: 0 warnings/errors. Scoped format (11 unrelated formatter changes reverted) and `git diff --check` passed.
- Contact read path not reachable in unit tests (no Xperience container). **Running-app check open:** Field dropdown shows `ContactScore`, Yes/No per operator for scored + unscored contact, 41050 warning in Event log.

### Slice 6 (2026-10-05) – Activity frequency (#8 reframed), implemented and unit-validated

**Selection:** #8 lowest remaining complexity: read-only `COUNT` query, reuses `NumberComparisonOperator`/`NumberComparison` from #22, no task/external service/prerequisite.

**Reframe (user confirmed):** built-in *Rule-based condition* already covers existence ("performed activity X with value that contains … in the last N days", "visited page", "clicked link in email"). Gap = **how often**. Description points marketers to the built-in rule for "at least once" checks.

**Spec:** `.agent-resources/implementation-spec-activity-frequency.md`. Implemented by subagent, reviewed by caller.

**User decisions / findings**

- Single activity type (no multi-select, no "Any activity" – type-specific filters don't fit it). Rolling window 1–365 days (default 7), default ≥ 3.
- `ActivityCreated` is **server-local time** (user: [private cloud deployment docs](https://docs.kentico.com/documentation/developers-and-admins/deployment/deploy-to-private-cloud.md#deploy-the-database); subagent: `IDateTimeNowService` → `DateTime.Now`, built-in `NewerThan` uses local now − days). Window = `TimeProvider.GetLocalNow()` − N days.
- Detail columns per type (user-supplied): emailclick → `ActivityValue` (URL); pagevisit/landingpage → `ActivityURL` (+ `ActivityURLReferrer`); bizformsubmit → `ActivityURL`, `ActivityTitle`; memberregistration → `ActivityTitle`; others → `ActivityValue`. Built-in "value that contains" rule searches `ActivityValue` only → can't filter page visits.
- Per-type filter properties with **conditional visibility** (user request). Referrer never searched (a /contact visit from /pricing would count as a pricing visit).

**Implementation**

- `Conditions/ActivityFrequency`: identifier `Labs.AutomationExtensions.ActivityFrequency`, config key `ActivityFrequency`, icon `Icons.EkgLine` (verified 31.9.1).
- Properties: Activity type (dropdown, enabled types, localized, sorted, value = code name), Operator (reused provider), Count (≥ 0, default 3), Days (1–365, default 7), Page URL contains / Title contains / Value contains (optional, AND). Inherits `SkippableConditionProperties`.
- `ActivityDetailFilters` = single source of truth: URL for pagevisit/landingpage/bizformsubmit; Title for bizformsubmit/memberregistration; Value for all others; case-insensitive; nothing while no type selected. Used by custom `ActivityDetailFilterVisibilityCondition` (`VisibilityConditionWithDependency`, attribute `VisibleIfActivityDetailFilter`) and runtime `GetCriteria` → hidden (stale) values ignored.
- Platform facts: multiple visibility attributes combine with **AND** (can't express "one of" with `VisibleIfEqualTo`); custom conditions supported in automation step config, discovered via `[VisibilityConditionAttribute]`, created via DI, re-evaluated when dependency field changes ([docs](https://docs.kentico.com/documentation/developers-and-admins/customization/extend-the-administration-interface/ui-form-components/ui-form-component-visibility-conditions)).
- Runtime: disabled → No; skipped → selected branch; invalid config (missing/unknown type, bad operator, count < 0, days out of range) → No + warning `InvalidActivityFrequencyConfiguration` (41060, once/hour per process+type), no query; else public `IActivityCounter` (internal `ActivityCounter`: parameterized `WhereContains` per criterion, `GetCountAsync`, no row load) → `NumberComparison.Compare`. Debug log `ActivityFrequencyEvaluated` (41061). Read-only, no caching.
- Activities are queued and written every ~10 s (`CMSProcessContactActionsInterval`) → Usage Guide recommends a short *Wait* before the condition when the triggering activity must count.
- `LocalizationResourceTests` scans declared properties only (inherited platform visibility-condition property uses a platform key); library base types still scanned.
- Usage Guide: component section, filter-per-type table, built-in rule difference, enablement/tables, example (Page URL contains `/pricing` ≥ 3 in 7 days). DancingGoat Development config enables it.

**Follow-up: ordering + text match operators (user request)**

- Order: Activity type 10, `UrlOperator`/`Url` 20/30, `TitleOperator`/`Title` 40/50, `ValueOperator`/`Value` 60/70, Operator 80, Count 90, Days 100. Operator + text pairs share per-type visibility.
- Public enum `TextMatchOperator` (persisted by name): `Contains` (default), `DoesNotContain`, `Equal`, `NotEqual`, `StartsWith`, `EndsWith` (`Equal` avoids clash with `object.Equals`, matches `NumberComparisonOperator`). Shared `TextMatchOperatorOptionsProvider`.
- SQL: only DataEngine `WhereContains/NotContains/Equals/NotEquals/StartsWith/EndsWith` – parameterized, LIKE wildcards `[ % _` escaped; negated operators add `OR column IS NULL` (empty column counts as not matching). Where-condition shape tested without the Xperience runtime. Empty text = no filter; invalid operator on a visible filter with text → 41060 + No; hidden/empty filters ignored.
- Stored `ActivityURL` (31.9.1 DLLs): page visit/landing page = `window.location.href` unchanged (absolute, query string + `#fragment`); form submission = HTTP Referer (absolute, query, no fragment). Matching follows DB collation (case-insensitive by default).
- **Limitation:** home page via "is equal to `https://host/`" misses visits with query string/fragment. No path-only operator – separate user decision if needed.

**Validation**

- After follow-up: **446 passed** (re-run by caller), DancingGoat `-t:Compile` 0 warnings/errors, `git diff --check` clean. Running-app checks add: operator labels, *starts with* `https://<host>/pricing` ≥ 3 → Yes, *is equal to* `https://<host>/` counts `/` not `/?utm_source=x`, *does not contain* counts empty columns.
- Full NUnit suite (before follow-up): **413 passed**, 0 failed/skipped (103 new: window, config validation, options provider, filter mapping/visibility/stale values/trim, gating, count+compare via fake counter, cancellation). Re-run by caller: 413 passed.
- DancingGoat `-t:Compile`: 0 warnings/errors. Format (touched files only) and `git diff --check` passed.
- Not unit-testable: SQL `COUNT`, activity type cache/localized names, Builder dialog visibility. **Running-app check open:** dropdown names; fields per type (none / URL / URL+Title / Title / Value) switch without save; Yes/No for ≥ 3 `/pricing` visits (wait > 10 s); referrer not matched; stale hidden value ignored; 41060 on invalid config; disabled → No + 41003.

### Decision (2026-10-05): disabled library conditions → No

History: Yes (Slice 2) → texts said "No (A)" while code returned `true` (Yes) → final **No** for all library conditions (`return false`). Changed several times; don't revisit without the user.

Reasons (pros/cons reviewed with user): fails closed (gated emails/notifications/syncs don't run), matches Xperience condition error/timeout → No, single rule to document. Accepted downsides: failure-path conditions (*Previous step succeeded*) send everyone to the failure branch; Random split sends everyone to B. Possible later: appsettings `DisabledConditionBranch` override if a real case needs it.

Applied: `PreviousStepSucceeded`, `RandomSplit`, `WithinTimeWindow` return `false` + log "sends all contacts through the No branch"; resx/Usage Guide say "No" ("No (B)" for Random split); disabled-branch tests assert `false` with skip set to Yes to prove precedence. 238 tests passed.

Open items moved to `STATUS.md` (2026-10-10).

---

## 8. Within time window condition (historical log)

### 8.1 Completion date
2026-10-05 (subagent + post-implementation fixes; date from commit `3dbd19f`, was wrongly recorded as 2026-11-29)

### 8.2 What was built

**WithinTimeWindowCondition** (Component #9 from 3.2):
- Main condition class with full approved semantics (UTC default, start≥/end< boundaries, overnight windows, DST handling, optional date limits).
- Admin UI properties: weekday checkboxes, time text inputs, timezone dropdown (validated), optional date range fields.
- TimeWindowCalculator: pure-function calendar algorithm using `DateOnly`/`TimeOnly`, all edge cases tested.
- TimeZoneOptionsProvider: dropdown with system time zones.
- 50+ localization strings (display name, description, property labels, help text, error messages).
- Comprehensive unit tests (50+ test cases covering algorithm, edge cases, timezone conversions, DST transitions).
- Integration with AutomationComponentCatalog.
- Icon: `Icons.Clock` (Clock icon for time-based condition).

### 8.3 Post-implementation work completed

1. **Disabled condition routing fix**: texts changed to No per Xperience error-handling docs (code still returned `true` until 2026-10-05 fix – see decision below).
   - Updated: Usage-Guide.md (4 sections), AutomationExtensionsResources.resx (4 strings), LocalizationResourceTests.cs (2 test cases).
   - Applies to: PreviousStepSucceeded, RandomSplit, WithinTimeWindow conditions.

2. **Test fixture updates**: Added new condition to component count expectations.
   - Updated StepRegistryPageTests.cs (7 test fixtures).
   - Changed: component count 8→9, condition filter expectations 2→3.

3. **Pre-existing bug fix**: UpsellFreeShippingService.cs build error.
   - Fixed incomplete switch statement in `BuildMessage()` (case FreeShippingMinimumRequirementType.None was missing return).

4. **Icon differentiation**: Improved custom condition icons for visual distinction.
   - PreviousStepSucceeded: `Icons.CheckCircle` (checkmark – decision confirmed).
   - RandomSplit: `Icons.Cogwheel` (settings – represents configuration branching).
   - WithinTimeWindow: `Icons.Clock` (time icon – represents temporal logic).

5. **Form component improvements**: Replaced basic text inputs with Xperience built-in visual components.
   - Start/End date: `DateTime?` + `DateInputComponent` (docs: component requires `DateTime?`; condition formats to `yyyy-MM-dd` for the calculator).
   - Start/End time: custom `TimeInputComponent` (React `TimeInputFormComponent` in `Client/src/form-components`, adapted from the admin design reference's `DateTimeInput/components/TimeInput`). React Aria segmented input and styled dropdown replace the unstyleable native HTML5 popup. Values remain `HH:mm`, with no browser-time-zone conversion.
   - Dropdown offers 15-minute suggestions plus the current value when between suggestions; segment editing supports any minute. `useGlobalization().uiLocale` supplies React Aria's locale. Disabled mode prevents editing/opening; read-only mode uses admin `TextWithLabel`.
   - Scoped styles follow the reference's `InputBase`, `TimeInput`, and `ListPopover`: full-width pill, admin spacing/typography, purple selected options, and focus/invalid/disabled states. Admin `Paper` and `Icon` components render the popup and trigger.
   - Verified in an isolated Chrome fixture using the actual React component and admin package: mouse/keyboard selection, arbitrary-minute editing and persistence, Escape/outside dismissal, disabled/read-only/invalid states, empty values, locale switching, and popup width/selection color. Client typecheck/production build and Admin library build pass; webpack reports bundle-size advisories after adding React Aria.
   - Time zone dropdown sorts by `BaseUtcOffset` ascending, then standard name for equal offsets.
   - Previously saved dates (string) are not migrated; feature is unreleased.
   - Not verified in a running admin UI.

### 8.4 Test and build results

- **Tests**: 160/160 passing; 17 isolated browser checks passing ✅
- **.NET build**: Admin library builds with 0 errors, 0 warnings ✅
- **Client build**: Typecheck and production build pass; webpack reports size advisories for the 261 KiB React Aria bundle.
- **Format validation**: Passed ✅

### 8.5 Decisions made (with user approval)

1. **Time zone**: Default UTC, persisted as zone ID (not label).
2. **Boundaries**: Start inclusive (≥), end exclusive (<).
3. **Overnight windows**: Allowed; weekday refers to window start day.
4. **Equal start/end times**: Treated as 24-hour window.
5. **Date limits**: Optional, both inclusive; applied to window start date.
6. **DST handling**: Convert UTC to local; both occurrences of repeated fall-back hours match.
7. **Disabled routing**: No branch (Random split: No (B)).
8. **UI component**: Built-in date picker and custom styled React Aria time picker.

### 8.6 Documentation

- **Usage-Guide.md**: 80+ line configuration guide with examples, decision rationale, timezone migration notes.
- **AutomationExtensionsResources.resx**: All marketer-facing text (display name, descriptions, labels, help text, validation messages).
- **Code comments**: Algorithm complexity explained; edge cases documented.

---

## 9. Resolved questions

Unresolved questions live in `STATUS.md` (pending decisions).

1. Identifier prefix: `Labs.AutomationExtensions.`; display text via localization resources (5.5).
2. Default enabled state: disabled by default, opt-in/opt-out via appsettings.
3. Admin UI: read-only global Step registry; no DB-stored overrides.
4. Scheduled tasks: per-trigger tasks, docs-only setup (section 4).
5. Datetime picker UX: built-in date picker and custom styled time picker (section 8.3).
6. Disabled library conditions → No (section 7, decision 2026-10-05).
7. #7 *Has consent agreement* dropped – built-in rule covers it (Slice 4).