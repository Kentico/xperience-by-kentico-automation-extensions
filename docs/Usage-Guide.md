# Usage Guide

## Installation

1. Add the package to your Xperience by Kentico application:

   ```powershell
   dotnet add package Kentico.Xperience.Labs.AutomationExtensions
   ```

2. Register the services in `Program.cs` (after `AddKentico`):

   ```csharp
   builder.Services.AddAutomationExtensions(builder.Configuration);
   ```

3. Enable the components you want to use (all are **disabled by default**) – see [Enable components](#enable-components).

## Enable components

All components are always registered, so every trigger, condition, and action appears in the Automation Builder once the package is installed.
Removing a registered component would break any process that uses it, so components are turned on and off through configuration instead.

| State | Trigger | Condition | Action |
| --- | --- | --- | --- |
| Enabled | Starts processes | Evaluates normally | Runs normally |
| Disabled | Never starts processes (its scheduled task fires nothing) | Always follows the **No** branch | Does nothing, the contact moves to the next step |

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

## Components

| Component | Name (configuration key) | Identifier |
| --- | --- | --- |
| Trigger – Contact date anniversary | `ContactDateAnniversary` | `Labs.AutomationExtensions.ContactDateAnniversary` |
| Condition – Previous step succeeded | `PreviousStepSucceeded` | `Labs.AutomationExtensions.PreviousStepSucceeded` |
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
If no earlier step saved that result (missing step, different number, step disabled), the condition follows **No** and logs a warning.

Each step type has its own numbered results, so different step types never overwrite each other. Within a process, give each step of the same type a different number. Steps can't detect which other steps exist in a process, so the step and number must be matched manually.

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
