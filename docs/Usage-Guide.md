# Usage Guide

## Installation

1. Add the package to your Xperience by Kentico application:

   ```powershell
   dotnet add package Kentico.Xperience.Labs.AutomationExtensions.Admin
   ```

2. Register the services in `Program.cs` (after `AddKentico`):

   ```csharp
   builder.Services.AddAutomationExtensions(builder.Configuration);
   ```

3. Enable the components you want to use (all are **disabled by default**) – see [Enable components](#enable-components).

The `.Admin` package includes the automation components and admin UI client module.
Existing C# namespaces remain under `Kentico.Xperience.Labs.AutomationExtensions`;
service registration and component identifiers are unchanged. Client assets are embedded
in the package, so consuming applications do not need Node.js.

## Enable components

All components are always registered, so every trigger, condition, and action appears in the Automation Builder once the package is installed.
Removing a registered component would break any process that uses it, so components are turned on and off through configuration instead.

| State | Trigger | Condition | Action |
| --- | --- | --- | --- |
| Enabled | Starts processes | Evaluates normally | Runs normally |
| Disabled | Never starts processes (its scheduled task fires nothing) | All contacts follow **No (A)**; condition isn't evaluated and disablement overrides skip settings | Does nothing, the contact moves to the next step |

Disabled components used in a process log a warning (at most once per hour per process).
At application start, the library logs which components are enabled and warns about unknown component names in configuration.

### Opt in (recommended)

```json
{
  "AutomationExtensions": {
    "EnabledByDefault": false,
    "Components": {
      "ContactDateAnniversary": true,
      "PreviousStepSucceeded": true,
      "RandomSplit": true,
      "SendWebhook": true,
      "WriteToLog": true
    }
  }
}
```

### Opt out

```json
{
  "AutomationExtensions": {
    "EnabledByDefault": true,
    "Components": {
      "SendWebhook": false
    }
  }
}
```

Component names are case-insensitive. Restart the application after changing these settings. (The library reads them via `IOptionsMonitor`, so changes may also apply on configuration reload, but this hasn't been verified.)

### Step registry tab

Open **Automation → a process → Step registry**, immediately after **Contacts**.
The tab is read-only and **global**: every process shows the same application registry,
not the steps used by that process. No process-specific usage or dependency check is performed.

The built-in listing supports search, sorting, paging, and two optional filters:

- **Kind:** Trigger, Condition, Action, or Unknown (unknown configuration entries).
- **Status:** Enabled, Disabled, Not managed, or Not applicable.

Both default to **All**. Applied filters combine with each other and the search term;
sorting, page totals, and paging use the matching rows. For example, **Condition** +
**Disabled** shows only disabled library conditions. Clear an applied filter or reset
the listing filters to return to **All**; the search term still applies.
**Not applicable** includes missing library registrations and unknown configuration
entries, while **Not managed** includes registered application/third-party definitions.

The listing combines registered trigger/action/condition definitions with the library
catalog and current `AutomationExtensionsOptions`. Columns are **Name**, **Kind**,
**Identifier**, **Status**, and **Description**. Descriptions come from each
registration definition; resource keys are localized, plain text is shown as provided,
and missing descriptions are blank.

- **Library components:** effective **Enabled**/**Disabled** comes from `IAutomationComponentStatus`.
  The default is disabled unless application configuration enables components.
- **Other registrations:** **Not managed**. This library neither controls nor infers
  their runtime state. This includes steps from the application or third-party packages;
  their own runtime gates may still apply.
- **Unknown component keys:** appear as **Unknown** rows with **Not applicable** status.
  Their configured values are not shown and do not control third-party registrations,
  even if a key matches that registration's identifier/name.
- **Missing library registration:** the catalog entry remains visible with **Not applicable**
  status; no effective runtime status is claimed.

The registry covers definitions exposed by Xperience's injected custom automation providers, not structural
Builder steps such as Wait and Finish. Configuration values, endpoint secrets, and
step-property values are not listed.

The top callouts explain scope and disablement: all disabled library conditions follow
**No (A)** without evaluation; administrator disablement overrides marketer skip settings;
disabled actions no-op; disabled triggers cannot start processes.

No database object or custom frontend is required. The package's discoverable assembly
registers `StepRegistryPage` under `AutomationProcessEditSection` with the built-in listing
template, slug `step-registry`, order **1000**, after all stock tabs (Contacts is **300**
in Xperience **31.9.1**). Other custom tabs with higher orders can still appear after it.
Routing inherits the selected process's edit-section path/ID. Parent pages and registration
providers are public APIs in `Internal` namespaces: this integration is version-coupled;
verify signatures, tab order, and routing when upgrading Xperience.

## Components

| Component | Name (configuration key) | Identifier |
| --- | --- | --- |
| Trigger – Contact date anniversary | `ContactDateAnniversary` | `Labs.AutomationExtensions.ContactDateAnniversary` |
| Condition – Previous step succeeded | `PreviousStepSucceeded` | `Labs.AutomationExtensions.PreviousStepSucceeded` |
| Condition – Random split (A/B) | `RandomSplit` | `Labs.AutomationExtensions.RandomSplit` |
| Condition – Within time window | `WithinTimeWindow` | `Labs.AutomationExtensions.WithinTimeWindow` |
| Action – Send webhook | `SendWebhook` | `Labs.AutomationExtensions.SendWebhook` |
| Action – Write to log | `WriteToLog` | `Labs.AutomationExtensions.WriteToLog` |

### Trigger – Contact date anniversary

Starts a process once a year on (or a number of days before) the anniversary of a contact date field, such as a birthday or signup date.
Only the month and day of the date are compared. February 29 dates match on February 28 in non-leap years.

**Requires** a scheduled task configuration – without it the trigger never fires:

1. Open the **Scheduled tasks** application and select **New scheduled task configuration**.
2. Set **Task implementation** to `Labs.AutomationExtensions.ContactDateAnniversaryTask`.
3. Set the **Task schedule** to run **once per day** (more frequent runs are not recommended by Xperience and fire duplicate triggers).
4. Select **Enabled** and **Save**.

When the component is disabled, the task does nothing and its **Last result** shows `Disabled via configuration (…)`.

Configuration:

```json
{
  "AutomationExtensions": {
    "ContactDateAnniversary": {
      "DateFields": [ "ContactBirthday", "ContactSignupDate" ],
      "DaysBefore": [ 0, 7, 30 ],
      "BatchSize": 500
    }
  }
}
```

| Setting | Default | Description |
| --- | --- | --- |
| `DateFields` | `["ContactBirthday"]` | Contact fields scanned by the task. Custom contact fields of the *Date* or *Date and time* type are supported. Invalid fields are ignored and logged. |
| `DaysBefore` | `[0]` | Day offsets scanned by the task (0 = on the date, max 365). Marketers can select only these values. Each value adds one query per date field to every run. |
| `BatchSize` | `500` | Contacts loaded per database query. |

Marketer properties:

- **Contact date** – one of the configured `DateFields`.
- **When to start** – *On the date* or one of the configured `DaysBefore` values.

Trigger data (`ContactDateAnniversaryTriggerData`, available to custom steps via `GetTriggerData`): `FieldName`, `DaysBefore`, `AnniversaryDate`, `Years`.

Recommended process recurrence: **If not already running**. Avoid **Only once** – it starts the process only for the first anniversary, never in later years.

> **Run the task once per day.** A contact that finished the process can start it again, so every additional task run on the same day (manual run, retry, or an interval shorter than a day) starts the process again for the same contacts – for example sending a birthday email twice.

> **Load:** each task run fires one trigger per matching contact into Xperience's shared in-memory trigger queue (also used by the *Form* trigger).
> For very large contact bases, run the task off-peak and consider increasing the `CMSAutomationTriggerQueueCapacity` application setting.

### Action – Send webhook

Posts a JSON payload about the processed contact to a named endpoint (CRM, CDP, Zapier, Power Automate, …).
Endpoint URLs and secrets stay in configuration and are never stored in the automation process.

Configuration:

```json
{
  "AutomationExtensions": {
    "Webhooks": {
      "Endpoints": {
        "crm": {
          "DisplayName": "CRM",
          "Url": "https://example.com/hooks/xperience",
          "Secret": "<store in user secrets, environment variables, or a key vault>",
          "SignatureHeaderName": "X-Automation-Signature",
          "TimeoutSeconds": 30
        }
      }
    }
  }
}
```

| Setting | Default | Description |
| --- | --- | --- |
| `DisplayName` | endpoint name | Name shown to marketers. |
| `Url` | – | Absolute URL. Required. |
| `Secret` | – | Optional. When set, the body is signed with HMAC-SHA256 and sent as `sha256=<hex>` in the signature header. |
| `SignatureHeaderName` | `X-Automation-Signature` | Signature header name. |
| `TimeoutSeconds` | `30` | Request timeout. |

Marketer properties:

- **Destination** – one of the configured endpoints.
- **Event name** – sent as `eventName`, e.g. `lead-qualified`.
- **Include contact details** – adds email, first name, last name, company name. Off by default (only the contact GUID is sent).
- **Record result for later steps** / **Result number** – saves the outcome (*Result 1–5*) for the *Previous step succeeded* condition.

Payload:

```json
{
  "eventName": "lead-qualified",
  "occurredAt": "2026-10-04T12:00:00+00:00",
  "processName": "Lead nurture",
  "triggerIdentifier": "Labs.AutomationExtensions.ContactDateAnniversary",
  "contact": {
    "guid": "8f6c2b8e-6d33-4c4a-9c47-0e2b1d1f6a10",
    "email": null,
    "firstName": null,
    "lastName": null,
    "companyName": null
  }
}
```

Failures (non-success status, network error, timeout) are logged once as a `WebhookFailed` warning (with the exception, if any); the contact always continues to the next step.

### Action – Write to log

Writes a message to the application log for every contact that reaches the step. Use it to see which path contacts take and why – for example after each branch of a *Previous step succeeded* condition.

Marketer properties:

- **Message** – supports `{processName}`, `{contactId}`, `{contactGuid}`, `{triggerIdentifier}`. Unknown placeholders are kept as typed. New steps start with a default message that uses these placeholders.
- **Level** – Information, Warning, or Error.
- **Include results of earlier steps** (default on) – e.g. `SendWebhook.1: failed (HTTP 500, 2026-10-04T12:00:00Z)`.
- **Include trigger details** (default on) – trigger identifier plus non-personal data of built-in triggers (form/record ID, member ID, activity type) and library triggers.

Log entry (event ID `41000`, name `AutomationLog`):

```text
Automation 'Example automation', contact c382…: Contact 4 reached this step. | Step results: DancingGoat.CoffeeClubEnrollment.1: succeeded (Enrolled, …); SendWebhook.1: failed (HTTP 500, …) | Trigger: Kentico.FormSubmissionTrigger (form 1, record 3)
```

Entries use the logger category `Kentico.Xperience.Labs.AutomationExtensions.WriteToLog`. With the default Xperience logging configuration (`Logging:XperienceEventLog:LogLevel:Kentico` = `Information`) they appear in the **Event log** application, and in any other logging provider configured in the application.

Only the contact ID/GUID is logged – never email or name. Keep personal data out of messages.

> **Volume:** the step logs once per contact. Enable it in development or for short troubleshooting periods (`"WriteToLog": true` in `appsettings.Development.json`) and keep it disabled in production. Disabled steps do nothing.
>
> **Auditing:** the Event log is trimmed over time and isn't tamper-proof. For a per-contact audit trail use the built-in *Log custom activity* step (visible in the contact's activities); for long-term retention route the `AutomationLog` category to a dedicated logging provider.

### Condition – Previous step succeeded

Follows the **Yes** branch when an earlier step saved a successful result, otherwise **No**.

Marketer properties:

- **Step** – type of the earlier step (e.g. *Send webhook*). Lists library steps and [custom steps registered by developers](#support-previous-step-succeeded-in-custom-steps).
- **Result number** – the number selected on the earlier step (*Result 1–5*).

**Works with:** *Send webhook* (or a registered custom step) with **Record result for later steps** turned on and the same **Result number**.
If no earlier step saved that result (missing step, different number, or the earlier step was disabled), an enabled condition follows **No** and logs a warning. If this condition itself is disabled, all contacts follow **No (A)** without reading the saved result.

Each step type has its own numbered results, so different step types never overwrite each other. Within a process, give each step of the same type a different number. Steps can't detect which other steps exist in a process, so the step and number must be matched manually.

### Condition – Random split (A/B)

Splits contacts between the **Yes** and **No** branches using a stable bucket calculated from each contact GUID and a split key. It needs no additional setup, database query, or external service.

Marketer properties:

- **Yes branch percentage** – integer from 0 to 100, default 50. A contact follows **Yes** when its bucket is below this percentage. `0` always follows **No**; `100` always follows **Yes**.
- **Split key** – required, default `default`. Use a different key for an independent split.

The allocation is approximate, not an exact count or guaranteed 50/50 distribution. A contact keeps the same branch across process runs and across processes when its GUID and split key are the same. The process display name is not part of the identity, so renaming a process does not move contacts between branches. There is no automatic per-step identity.

The split key is used exactly as entered: matching is case-sensitive and whitespace in a nonblank key is significant. Changing the key or percentage can change branch outcomes. Increasing the percentage never moves a contact from **Yes** to **No**.

When the component is disabled globally, all contacts follow **No (A)** without evaluating the condition. This takes precedence over **Skip this step** for every library condition. For Random split (A/B), this also means no contact is read and no bucket is calculated. The disabled-component warning records this branch behavior. When enabled, **Skip this step** follows the selected skip branch without reading the contact.

The Builder's step description and percentage help explain this disabled behavior. This is static guidance, not a live enabled/disabled indicator; ask your administrator to confirm the current configuration. Marketers can choose the skip branch per step only while the component is enabled.

To compose a simple A/B test, add **Random split (A/B)** after the process trigger. Connect **Yes** to a built-in **Send email** step with variant A and **No** to a second **Send email** step with variant B. No new assignment is made on a later run; use a new split key when an independent assignment is wanted.

### Condition – Within time window

Evaluates whether the current time (when the condition runs) falls within a configured business hours window. Useful for routing contacts to marketing activities that only run during business hours, without needing a **Wait** step.

This condition checks the time when the step is evaluated, not when the contact was added to the process or joined a **Wait** step. If you need to delay execution until a specific time, use the built-in **Wait** step.

**Time zone behavior:**

- Configured time zone ID is saved and must exist on the deployment host. If the time zone disappears (e.g., due to OS update or configuration change), the condition logs an error and contacts follow the **No** branch.
- Time is converted from UTC to the configured local time zone before evaluation.
- Daylight Saving Time (DST) transitions are handled automatically: both occurrences of repeated hours (fall back) are included; skipped hours (spring forward) never match.
- Default is UTC; time zone is required and must be selected from the host-supported list.

**Marketer properties:**

- **Weekdays** – at least one day must be selected (Monday–Sunday). Checked days determine which days the window is active. For overnight windows (e.g., 22:00–06:00), the start day is the anchor: if Monday is selected and the window is 22:00–06:00, the window includes Monday 22:00 through Tuesday 06:00.
- **Start time** – required, selected from a dropdown in 15-minute steps (e.g., 09:00). Boundary is inclusive (≥).
- **End time** – required, selected from a dropdown in 15-minute steps. Boundary is exclusive (<). A time equal to start time creates a 24-hour window (e.g., 14:00–14:00 includes all times from 14:00 today through 13:59:59 tomorrow).
- **Time zone** – required, selected from host-supported time zones. Controls local time conversion for window boundary and DST handling.
- **Start date (optional)** – chosen with the date picker. If set, the contact must reach this condition on or after the specified date (inclusive). Validation: if both start and end date are set, start date must be ≤ end date.
- **End date (optional)** – chosen with the date picker. If set, the contact must reach this condition on or before the specified date (inclusive). Validation: if both start and end date are set, end date must be ≥ start date.

**Example: Business hours (9 AM–5 PM, Mon–Fri, any date):**
- Weekdays: Monday, Tuesday, Wednesday, Thursday, Friday
- Start time: 09:00
- End time: 17:00
- Time zone: Your local zone (e.g., Eastern Time)
- Start date: (leave empty)
- End date: (leave empty)

When enabled and a contact reaches this step at 3 PM on a Tuesday, the condition is **Yes**. At 6 PM or on a Saturday, the condition is **No**.

**Example: Overnight shift (10 PM–6 AM, Monday–Friday):**
- Weekdays: Monday, Tuesday, Wednesday, Thursday, Friday
- Start time: 22:00
- End time: 06:00
- Time zone: Your local zone
- Start date: (leave empty)
- End date: (leave empty)

When a contact reaches this step on Wednesday at 3 AM, the condition is **Yes** (because Wednesday 22:00–Thursday 06:00 includes Wednesday night). When reached on Wednesday at 11 AM, the condition is **No**.

**Disabled component behavior:**

When the component is disabled globally, all contacts follow the **No** branch without evaluating the condition or reading the time zone. This takes precedence over **Skip this step**. The disabled-component warning records this behavior. When enabled, **Skip this step** follows the selected skip branch without evaluating the condition.

**Configuration example:**

```json
{
  "AutomationExtensions": {
    "EnabledByDefault": false,
    "Components": {
      "WithinTimeWindow": true
    }
  }
}
```

## Skip steps

Xperience doesn't allow deleting steps from a process that is enabled or has contact history. All library actions and conditions have a **Step behavior** section (at the bottom of the step configuration) to take a step out of such a process instead:

- **Actions** – **Skip this step**: contacts move on without the step running. If the step saves results (*Record result for later steps*), it saves a *skipped* result, which *Previous step succeeded* treats as not succeeded (No branch, no warning), and *Write to log* shows as `skipped`.
- **Conditions** – **Skip this step** + **Always follow** (*No branch* by default, or *Yes branch*): the condition isn't evaluated and all contacts follow the selected branch.

Changes apply immediately, also to contacts already in the process (including contacts in *Wait* steps). Skipped steps log at debug level (`StepSkipped`).

The builder doesn't show the skipped state on the step tile, so mark it in the step name. Recommended prefixes:

| Step | Prefix | Example |
| --- | --- | --- |
| Action | `[Skipped]` | `[Skipped] Send webhook` |
| Condition | `[Skipped → Yes]` / `[Skipped → No]` | `[Skipped → No] Previous step succeeded` |

The step tile truncates long names, so keep prefixes short. Emoji (e.g. `⏭️`) also work as a more compact prefix, but they are harder to search for, render differently across operating systems, and are read out by screen readers. Prefer the plain-text prefix.

When a condition is skipped, only the selected branch is used. Keep the other branch connected to further steps (or a *Finish* step): if you turn skipping off later, contacts sent to a branch end with no following step stay stuck there.

The per-step skip is for marketers and applies to one step. To turn a component off everywhere, developers use [configuration](#enable-components).

Custom steps can reuse the same behavior – inherit their properties class from `SkippableActionProperties` or `SkippableConditionProperties`, then check it first in `Execute`/`Evaluate`:

```csharp
// Action
if (properties.SkipStep)
{
    SkippableStep.LogSkipped(logger, IDENTIFIER, context.Process.DisplayName);
    return;
}

// Condition
if (SkippableStep.TryGetSkippedResult(properties, out bool skippedResult))
{
    return skippedResult;
}
```

Keep your own property and category `Order` values below `SkippableStep.CATEGORY_ORDER` (10000) so the Step behavior section stays at the bottom. Actions that save results should save `StepResult.CreateSkipped(...)` when skipped.

## Component dependencies

| Component | Depends on | If missing |
| --- | --- | --- |
| Contact date anniversary | Scheduled task configuration | Trigger never fires |
| Previous step succeeded | Earlier step of the selected type saving the same result number | Follows **No** branch, warning logged |
| Random split (A/B) | None | No additional setup required |
| Send webhook | Endpoint in configuration | Does nothing, warning logged |
| Write to log | Logging configuration routing `Kentico*` categories at the selected level | Nothing visible in the Event log |

## Example process – CRM sync with retry

1. Trigger: *Contact date anniversary* (Birthday, on the date).
2. *Send webhook* (CRM, `birthday`, record result as *Result 1*).
3. *Previous step succeeded* (*Send webhook*, *Result 1*) → **Yes**: *Finish*.
4. **No**: *Wait* 1 hour → *Send webhook* (same settings) → *Previous step succeeded* (*Send webhook*, *Result 1*) → **No**: internal notification.

## Support Previous step succeeded in custom steps

Custom automation actions from your project or other packages can save results and appear in the **Step** list of the *Previous step succeeded* condition.

1. Register the step type (after `AddAutomationExtensions`):

   ```csharp
   builder.Services
       .AddAutomationExtensions(builder.Configuration)
       .AddStepResultSource("Acme.SyncToCrm", "acme.automation.synctocrm.displayname");
   ```

   - **Name** – stable, unique, prefixed (e.g. `Acme.`). It is part of saved result keys – never change it once used.
   - **Display name** – an admin localization resource key or plain text.
   - Registering the same name twice with the same display name is ignored; a different display name throws.

2. Let marketers choose a result number in the action's properties:

   ```csharp
   [CheckBoxComponent(Label = "Record result for later steps", Order = 210)]
   public bool RecordResult { get; set; }

   [DropDownComponent(Label = "Result number", DataProviderType = typeof(StepResultNumberOptionsProvider), Order = 220)]
   [VisibleIfTrue(nameof(RecordResult))]
   public string ResultNumber { get; set; } = StepResultKeys.DEFAULT_RESULT_NUMBER;
   ```

3. Save the outcome in the action's `Execute`:

   ```csharp
   if (properties.RecordResult)
   {
       await StepResultRecorder.Record(context, "Acme.SyncToCrm", properties.ResultNumber, new StepResult
       {
           Succeeded = succeeded,
           ActionIdentifier = IDENTIFIER,
           RecordedAt = DateTime.UtcNow,
           Message = "HTTP 200" // no personal data
       }, cancellationToken);
   }
   ```

If a registered step type is removed later, conditions that reference it keep their stored value, find no result, and follow **No** (with a warning).

### Saved result format

Results are stored in the `StepResultData` automation process data (identifier `Labs.AutomationExtensions.StepResultData`): a dictionary keyed by `<step name>.<result number>`, e.g. `SendWebhook.1` or `Acme.SyncToCrm.2`.
Values: `Succeeded`, `ActionIdentifier`, `RecordedAt` (UTC), `Message`. The data is deleted together with the contact.

## Logging

> **Known issue – long logger categories:** the Xperience event log stores the logger category (full type name of `T` in
> `ILogger<T>`) in a 100-character **Source** column and the event name in a 100-character **Code** column. Entries that
> exceed either limit are silently dropped from the Event log – they still appear in the console and other logging
> providers. The library uses short component-based categories (`Kentico.Xperience.Labs.AutomationExtensions.<Component>`, shown as **Source**) to stay within the limit. If you write custom steps (e.g. to
> [support Previous step succeeded](#support-previous-step-succeeded-in-custom-steps)), keep the full names of classes
> that log via `ILogger<T>` at 100 characters or less.

All library log entries use named event IDs, shown in the **Code** column of the Event log:

| ID | Name | Level | Meaning |
| --- | --- | --- | --- |
| 41000 | `AutomationLog` | selected | Entry written by *Write to log* |
| 41001 | `ComponentStatus` | Information | Enabled/disabled components at application start |
| 41002 | `UnknownComponent` | Warning | Unknown component name in configuration |
| 41003 | `ComponentDisabled` | Warning | Disabled component reached in a process (once per hour per process) |
| 41004 | `StepSkipped` | Debug | Contact passed a step with *Skip this step* turned on |
| 41010 | `WebhookFailed` | Warning | *Send webhook* request failed |
| 41011 | `WebhookResponse` | Debug | Webhook endpoint status code |
| 41020 | `StepResultMissing` | Warning | *Previous step succeeded* found no saved result (once per hour per process) |
| 41030 | `InvalidDateField` | Warning | Configured anniversary date field is missing or not a date |
| 41031 | `AnniversaryTaskCompleted` | Information | Anniversary scheduled task run summary |

## Localization

All marketer-facing texts are in `Resources/AutomationExtensionsResources.resx` (English). To translate, add `AutomationExtensionsResources.<culture>.resx` files in your project and register them with `RegisterLocalizationResource` – see [Admin UI localization](https://docs.kentico.com/documentation/developers-and-admins/customization/admin-ui-localization).
