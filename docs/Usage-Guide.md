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

Commerce components (*Order completed*) are in the separate `Kentico.Xperience.Labs.AutomationExtensions.Commerce` package.
Add it and call `AddAutomationExtensionsCommerce()` after `AddAutomationExtensions(...)` – see [Trigger – Order completed](#trigger--order-completed).

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
| Disabled | Never starts processes (its scheduled task fires nothing) | All contacts follow **No**; condition isn't evaluated and disablement overrides skip settings | Does nothing, the contact moves to the next step |

Disabled components used in a process log a warning (at most once per hour per process).
At application start, the library logs which components are enabled and warns about unknown component names in configuration.

### Opt in (recommended)

```json
{
  "AutomationExtensions": {
    "EnabledByDefault": false,
    "Components": {
      "ActivityFrequency": true,
      "AdjustContactScore": true,
      "ContactDateAnniversary": true,
      "ContactGroupMember": true,
      "ContactInactive": true,
      "ContactNumberFieldCompare": true,
      "PreviousStepSucceeded": true,
      "RandomSplit": true,
      "SendWebhook": true,
      "SetContactField": true,
      "StartedByTrigger": true,
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
**No** without evaluation; administrator disablement overrides marketer skip settings;
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
| Trigger – Contact in contact group (daily) | `ContactGroupMember` | `Labs.AutomationExtensions.ContactGroupMember` |
| Trigger – Contact inactive for N days | `ContactInactive` | `Labs.AutomationExtensions.ContactInactive` |
| Trigger – Order completed (`.Commerce` package) | `OrderCompleted` | `Labs.AutomationExtensions.OrderCompleted` |
| Condition – Activity frequency | `ActivityFrequency` | `Labs.AutomationExtensions.ActivityFrequency` |
| Condition – Compare contact number field | `ContactNumberFieldCompare` | `Labs.AutomationExtensions.ContactNumberFieldCompare` |
| Condition – Previous step succeeded | `PreviousStepSucceeded` | `Labs.AutomationExtensions.PreviousStepSucceeded` |
| Condition – Random split (A/B) | `RandomSplit` | `Labs.AutomationExtensions.RandomSplit` |
| Condition – Started by trigger | `StartedByTrigger` | `Labs.AutomationExtensions.StartedByTrigger` |
| Condition – Within time window | `WithinTimeWindow` | `Labs.AutomationExtensions.WithinTimeWindow` |
| Action – Adjust contact score | `AdjustContactScore` | `Labs.AutomationExtensions.AdjustContactScore` |
| Action – Send webhook | `SendWebhook` | `Labs.AutomationExtensions.SendWebhook` |
| Action – Set contact field (dynamic value) | `SetContactField` | `Labs.AutomationExtensions.SetContactField` |
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

### Trigger – Contact in contact group (daily)

Starts a process for contacts in the selected contact group, checked once per day – for example a nurture series for a *High intent* group.
The scheduled task fires the trigger every day for **every** current member of the group (not only for contacts that joined since the last run); the process recurrence decides whether a contact starts again.

**Requires** a scheduled task configuration – without it the trigger never fires:

1. Open the **Scheduled tasks** application and select **New scheduled task configuration**.
2. Set **Task implementation** to `Labs.AutomationExtensions.ContactGroupMemberTask`.
3. Set the **Task schedule** to run **once per day**, preferably off-peak.
4. Select **Enabled** and **Save**.

When the component is disabled, the task does nothing and its **Last result** shows `Disabled via configuration (…)`.
When no contact groups are configured, it shows `No contact groups configured in 'AutomationExtensions:ContactGroupMember:ContactGroups'. …`; when none of the configured groups exists, `None of the contact groups configured … exist as contact groups …`.

Configuration:

```json
{
  "AutomationExtensions": {
    "ContactGroupMember": {
      "ContactGroups": [ "HighIntent", "TrialUsers" ],
      "BatchSize": 500
    }
  }
}
```

| Setting | Default | Description |
| --- | --- | --- |
| `ContactGroups` | `[]` | Code names of the contact groups scanned by the task (trimmed; empty entries and duplicates ignored, case-insensitive). Marketers can select only these groups. Each group adds one set of queries and one trigger per member to every run. Empty: the task fires nothing. |
| `BatchSize` | `500` | Contacts loaded per database query. |

Unknown code names (`ContactGroupNotFound`) and recipient lists (`ContactGroupIsRecipientList`) are skipped with a warning on every task run and aren't listed in the drop-down.
Other object types stored in the contact group table (Xperience 31.9 *segments*) are skipped the same way (`ContactGroupNotFound`).

Marketer properties:

- **Contact group** – one of the configured contact groups that exist, shown as *Display name (code name)*.

Trigger data (`ContactGroupMemberTriggerData`, available to custom steps via `GetTriggerData`): `ContactGroupName` (code name), `ContactGroupDisplayName`.

How the task finds contacts: on each run, for every configured group, it reads the group's contact members (`OM_ContactGroupMember`, member type *contact*) in batches ordered by contact ID and fires the trigger for each existing contact. A contact in several configured groups fires once per group; each process starts only for its selected group.

Process recurrence:

| Recurrence | Behavior |
| --- | --- |
| **If not already running** (recommended) | A member starts when it isn't currently in the process – again the day after it finishes, while it is still in the group. Keep members in the process (e.g. *Wait* steps) as long as they shouldn't restart. |
| **Only once** (recommended) | Each contact starts once, ever – even if it leaves and rejoins the group. |
| **Always** | Every task run starts the process again for every member – daily duplicates. Avoid. |

Limitations:

- **No "newly joined" detection.** No state is stored: the task doesn't know when a contact joined. Every member fires on every run; recurrence prevents repeated starts.
- **Recipient lists aren't supported.** A separate trigger (*Contact in recipient list (daily)*) is planned.
- **Membership lags.** Group membership is maintained by Xperience's [contact group recalculation](https://docs.kentico.com/documentation/business-users/digital-marketing/contact-groups#recalculate-contact-groups). Contacts that recently started or stopped matching the conditions, or members of a group whose conditions changed but which wasn't recalculated yet, are fired according to the stored membership.
- **Missed runs are not caught up.** If the task doesn't run on a day, or fired triggers are lost from the in-memory trigger queue (application restart), no start happens for that day – members are fired again on the next run.
- Leaving the group doesn't stop a running process.

> **Load:** each task run fires one trigger per member per configured group into Xperience's shared in-memory trigger queue (also used by the *Form* trigger).
> When the queue is full, Xperience drops further triggers and logs a warning. Keep groups targeted, run the task off-peak, and for large groups raise the `CMSAutomationTriggerQueueCapacity` application setting (default 5000, see [configuration keys](https://docs.kentico.com/documentation/developers-and-admins/configuration/reference-configuration-keys#automation-settings)).
> Recurrence settings don't reduce the load: triggers are fired for every member and filtered when processes evaluate them.

Example – high-intent nurture:

1. Contact group *High intent* (e.g. visited pricing pages and submitted a form), code name added to `ContactGroups`.
2. Trigger: *Contact in contact group (daily)* (*High intent*), recurrence **Only once**.
3. *Send email* – case study, *Wait* 3 days, *Send email* – demo offer.
4. *Adjust contact score* (+20) or internal notification for sales.

### Trigger – Contact inactive for N days

Starts a process when a contact's last activity was exactly the selected number of days ago – for example a win-back email after 90 days without activity.
Each contact starts once per inactivity period: on the day its last activity reaches the threshold. If it becomes active again and later reaches the threshold again, it starts again.

**Requires** a scheduled task configuration – without it the trigger never fires:

1. Open the **Scheduled tasks** application and select **New scheduled task configuration**.
2. Set **Task implementation** to `Labs.AutomationExtensions.ContactInactiveTask`.
3. Set the **Task schedule** to run **once per day** (see the recurrence notes below for more frequent runs).
4. Select **Enabled** and **Save**.

When the component is disabled, the task does nothing and its **Last result** shows `Disabled via configuration (…)`.

Configuration:

```json
{
  "AutomationExtensions": {
    "ContactInactive": {
      "InactiveDays": [ 30, 90, 180 ],
      "BatchSize": 500
    }
  }
}
```

| Setting | Default | Description |
| --- | --- | --- |
| `InactiveDays` | `[90]` | Numbers of days without activity scanned by the task (1 – 3650; invalid values are ignored). Marketers can select only these values. Each value adds one set of queries to every run. |
| `BatchSize` | `500` | Contacts loaded per database query. |

Marketer properties:

- **Days without activity** – one of the configured `InactiveDays` values.

Trigger data (`ContactInactiveTriggerData`, available to custom steps via `GetTriggerData`): `InactiveDays`, `LastActivityDate` (date of the last activity, UTC midnight of the server-local date).

How the task matches contacts: on each run it takes today's date (server local time) and, for every configured value N, selects contacts whose **latest** activity was logged on the day exactly N days ago (at least one activity that day and none after it).

Process recurrence:

| Recurrence | Behavior |
| --- | --- |
| **If not already running** (recommended) | Starts once per inactivity period. If the task runs more than once per day, keep contacts in the process for at least one day (e.g. a *Wait* step) – later runs on the same day fire the same contacts again and only a running process prevents a second start. |
| **Always** | Every task run on the threshold day starts the process again – duplicates if the task runs more than once per day. |
| **Only once** | The contact never enters the process again, even after later inactivity periods. |

Limitations:

- **Missed runs are not caught up.** No state is stored: if the task doesn't run on a day (server down, task disabled, aborted run), or fired triggers are lost from the in-memory trigger queue (application restart), contacts that reached the threshold that day never start the process for this inactivity period.
- **Contacts without any activity never start the process** (the contact creation date isn't used).
- **All activity types count** – any logged activity (page visit, form submission, email click, custom activity, …) resets inactivity.
- Dates use the **server local time** (the time zone `ActivityCreated` is stored in), not the contact's time zone.

> **Load:** each task run fires one trigger per matching contact into Xperience's shared in-memory trigger queue (also used by the *Form* trigger).
> The query reads all activities since the oldest scanned day, so large `InactiveDays` values (e.g. 365+) on big activity tables take longer. Run the task off-peak.

**Optional index (large activity tables).** The query filters `OM_Activity` by `ActivityCreated` and groups by `ActivityContactID`.
The built-in indexes cover each column separately, so SQL Server joins two index scans. A covering index removes that join.

Add it with a [CI/CD database migration script](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/ci-cd-database-migration-scripts)
(also applied automatically when [deploying to SaaS](https://docs.kentico.com/documentation/developers-and-admins/deployment/deploy-to-the-saas-environment)):

1. Add `@migrations/AddActivityCreatedContactIndex.sql` to your CI/CD repository folder:

    ```sql
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OM_Activity_ActivityCreated_ActivityContactID' AND object_id = OBJECT_ID('OM_Activity'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_OM_Activity_ActivityCreated_ActivityContactID
            ON OM_Activity (ActivityCreated)
            INCLUDE (ActivityContactID);
    END
    ```

2. Add `AddActivityCreatedContactIndex` on a new line in `After.txt` (create indexes after the restore).
3. Restore with `--enable-migrations` (`--kxp-ci-restore` / `--kxp-cd-restore`). Applied migrations are recorded in `CI_Migration` / `CD_Migration` and run once.

- Measured without it on ~298k activities: 40–80 ms for 1–90 days, ~190 ms for 365 days, ~280 ms for 1000 days. Add it only if the task gets slow.
- `OM_Activity` is an Xperience system table: the index is a custom schema change. Check it still exists after hotfixes/upgrades, and expect slightly slower activity inserts.
- To remove it: a new migration that drops the index if it exists, listed in `Before.txt` (see the docs' rollback example).

Example – win-back:

1. Trigger: *Contact inactive for N days* (90 days), recurrence **If not already running**.
2. *Send email* – "We miss you" with an offer.
3. *Wait* 7 days.
4. *Activity frequency* (*Page visit*, 1 or more in the last 7 days) → **Yes**: *Finish*; **No**: *Adjust contact score* or internal notification.

### Trigger – Order completed

Starts a process when a visitor completes an order – for example a thank-you email, review request, or cross-sell.
Package `Kentico.Xperience.Labs.AutomationExtensions.Commerce`.

**Requires** checkout code that fires the trigger – Xperience doesn't fire it on its own:

```csharp
builder.Services
    .AddAutomationExtensions(builder.Configuration)
    .AddAutomationExtensionsCommerce();
```

```csharp
// In the checkout controller, after IOrderCreationService.CreateOrder (inject IOrderCompletedTriggerDispatcher)
var order = await orderInfoProvider.GetAsync(orderId, cancellationToken);
var contact = ContactManagementContext.GetCurrentContact(createAnonymous: false);
await orderCompletedTriggerDispatcher.Fire(order, contact, "USD", cancellationToken);
```

- **Contact is passed by your code.** The library never looks up a contact from the customer, member, or email – anyone can check out with an email they don't own, and a lookup would start processes (and send emails) for someone else's contact.
- `GetCurrentContact(createAnonymous: false)` returns `null` when the visitor isn't tracked (cookie level below *Visitor*, no consent, bot) – the trigger is then not fired (Debug log, no exception).
- **Contacts and consent:** every automation process runs for one contact, so no contact means no process. The library doesn't check consent – but Xperience usually only has a contact for visitors who allowed tracking. Orders from untracked visitors don't start processes.
  - To cover them, your checkout code must create or pick a contact itself. Don't look one up by checkout email (see above). Creating a new contact per order is safe from that, but adds duplicate contacts and stores personal data outside tracking consent – your site needs a legal basis for that (e.g. contract).
  - Transactional messages (order confirmation, shipping updates) belong in [order status notifications](https://docs.kentico.com/documentation/developers-and-admins/digital-commerce-setup/configure-order-statuses#configure-notifications-for-order-statuses), which need no contact or consent. Use this trigger for marketing follow-ups.
- `Fire` never throws for failures (the order already exists); it returns `Fired`, `Disabled`, `NoContact`, or `Failed` (Error log).
- **Currency:** Xperience orders store no currency. Pass the currency code your site uses (optional).

Marketer properties:

- **Minimum order total** – grand total (incl. tax and shipping) at least this amount; `0` = any order.
- **Currency** – empty = any currency; otherwise must equal the code passed by the checkout code (case-insensitive).
- **First order only** – starts only when the customer had no earlier order (matched by customer record, not contact). Evaluated when the trigger fires, stored in the trigger data.

Trigger data (`OrderCompletedTriggerData`): `OrderId`, `CustomerId`, `Total`, `Currency`, `ItemCount` (sum of item quantities, rounded up), `IsFirstOrder`. No personal data and no product references.
Usable in [*Started by trigger*](#condition--started-by-trigger) data checks, `{triggerData.…}` placeholders of [*Set contact field*](#action--set-contact-field-dynamic-value), and [*Write to log*](#action--write-to-log) trigger details (`order 42, customer 7, total 25.00 USD, 3 item(s), first order`).

### Action – Adjust contact score

Adds points to, subtracts points from, or sets a number field of the contact. Use it for lightweight lead scoring (Xperience has no built-in lead scoring): for example, add 10 points after a form submission and 5 after a webinar registration, then branch on the score.

**Requires:** an integer custom contact field. Developers add it in **Modules → Contact management → Classes → Contact management - Contact → Database columns** with **Data type** *Integer number* (for example `AcmeContactScore`, field caption *Lead score*). See [Add custom fields to contacts](https://docs.kentico.com/documentation/developers-and-admins/digital-marketing-setup/contact-configuration#add-custom-fields-to-contacts). To show the score in the contact's admin pages, also add the field to the contact UI forms described there.

Marketer properties:

- **Score field** – lists custom contact fields of the *Integer number* type, sorted by caption. Built-in contact fields (including IDs such as owner, status, country, state), primary key, references to other objects, and other number types (long integer, decimal, floating-point) are never listed. The list is empty until a developer adds a field.
- **Operation** – *Add* (default), *Subtract*, or *Set*. *Set* replaces the current score with the amount and ignores the current value.
- **Amount** – default 10. Must be 0 or more; the operation sets the direction. For *Set* it is the new score.
- **Limit score to a range** (*Limits* section, off by default) / **Minimum** (default 0) / **Maximum** (default 100) – the new score is clamped to the range (both ends included), for all operations. For example, *Add 20* to 90 with maximum 100 gives 100; *Set 500* gives 100. When the minimum is greater than the maximum, the step does nothing (the Builder can't check this, see below).

Behavior:

- An empty field counts as 0. An empty field is always written, even when the result is 0; an unchanged value is not saved again.
- Results beyond the integer range (about ±2.1 billion) stop at the range end instead of failing.
- The contact is read, changed, and saved without locking. When several processes adjust the same field of the same contact at the same moment, the last save wins and one change can be lost.
- Each change logs an Information entry (`ContactScoreAdjusted`, event ID `41040`) with process name, contact ID, field name, and old → new value – no email or name.
- Invalid settings (field deleted or changed to another type, unknown operation, negative amount, minimum above maximum) log an `InvalidContactScoreConfiguration` warning (event ID `41041`, at most once per hour per process and field). Nothing is saved and the contact moves to the next step.
- Disabled component: does nothing, warning logged (see [Enable components](#enable-components)). **Skip this step**: does nothing.

**Works with:** [*Compare contact number field*](#condition--compare-contact-number-field) to branch on the score (for example *score ≥ 50*). The built-in *Rule-based condition* can't compare numbers.

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

### Action – Set contact field (dynamic value)

Sets a contact field to a value built from placeholders, or calculates a number from the field's current value. The built-in *Set contact field* step only sets fixed values.

Examples:

- Stamp the campaign: field *Last campaign* (custom text field), **Set to value** `{processName}`.
- Remember how long a contact was inactive: field *Last inactive days* (custom integer field), **Set to value** `{triggerData.InactiveDays}` (process started by *Contact inactive for N days*).
- Count something: field *Webinar count* (custom integer field), **Calculate** `+ 1`.
- Stamp a date once: field *First contacted* (custom date field), **Set to value** `{today}`, **Only if the field is empty** on.

**Requires:** nothing for system fields; for custom fields, a developer adds them in **Modules → Contact management → Classes → Contact management - Contact → Database columns** (see [Add custom fields to contacts](https://docs.kentico.com/documentation/developers-and-admins/digital-marketing-setup/contact-configuration#add-custom-fields-to-contacts)).

Marketer properties:

- **Field** – custom contact fields first (shown as *Caption (CodeName)*), then built-in fields marked *system field*. Supported data types: text, long text, integer, long integer, decimal, floating-point number, boolean (yes/no), date, date and time. Never listed: primary key (`ContactID`), `ContactGUID`, `ContactCreated`, `ContactLastModified`, `ContactBounces` (maintained by bounce handling), the legacy `ContactSalesForce*` replication fields, fields that aren't stored in the database, and other data types (GUID, binary, time span, references to content, …). Also never listed: foreign keys (`ContactStatusID`, `ContactOwnerUserID`, `ContactCountryID`, `ContactStateID`, and custom fields referencing another object) and `ContactGender` (enum).
- **Mode** – *Set to value* (default) or *Calculate (number fields)*.
- **Value** (*Set to value*) – text with placeholders (see below). An empty result clears the field.
- **Only if the field is empty** (*Set to value*, off by default) – fields that already have a value aren't changed; the step still counts as succeeded.
- **Operator** (*Calculate*) – *+ (add)* (default), *− (subtract)*, *× (multiply)*, *÷ (divide)*, *% (remainder)*.
- **Operand** (*Calculate*) – default `1`. A number with a dot as decimal separator, or placeholders that resolve to one (for example `{triggerData.Years}`).
- **Result number** (*Result for later steps* section) – the step always saves whether it succeeded (*Result 1–5*) for the [*Previous step succeeded*](#condition--previous-step-succeeded) condition.

Placeholders (case-insensitive):

| Placeholder | Value |
| --- | --- |
| `{now}` | Current server-local date and time, `yyyy-MM-ddTHH:mm:ss` (Xperience stores contact dates in server-local time) |
| `{today}` | Current server-local date, `yyyy-MM-dd` |
| `{processName}` | Name of the automation process |
| `{triggerIdentifier}` | Identifier of the trigger that started the process (empty when unknown) |
| `{contactId}`, `{contactGuid}` | ID / GUID of the processed contact |
| `{contact.<Field>}` | Current value of another contact field (any database field, for example `{contact.ContactEmail}`); dates `yyyy-MM-dd` (date fields) or `yyyy-MM-ddTHH:mm:ss` |
| `{triggerData.<Property>}` | Data of the trigger that started the process – see below |

`{triggerData.<Property>}` works for every trigger that provides trigger data: built-in triggers, this library's triggers, and triggers registered by your project or other packages (`RegisterAutomationTrigger`). Properties = public readable properties of the trigger's data class (without `Identifier`), the same list as the *Trigger data field* of [*Started by trigger*](#condition--started-by-trigger). Built-in and library triggers:

| Trigger | Properties |
| --- | --- |
| Form (built-in) | `BizFormId`, `BizFormItemId` |
| Registration (built-in) | `MemberId` |
| Custom activity (built-in) | `ActivityType`, `ActivityItemId`, `ActivityItemDetailId`, `ActivityValue`, `ActivityChannelId` |
| Contact date anniversary | `FieldName`, `DaysBefore`, `AnniversaryDate`, `Years` |
| Contact inactive for N days | `InactiveDays`, `LastActivityDate` |
| Contact in contact group (daily) | `ContactGroupName`, `ContactGroupDisplayName` |
| Order completed (`.Commerce`) | `OrderId`, `CustomerId`, `Total`, `Currency`, `ItemCount`, `IsFirstOrder` |

- An unknown placeholder, an unknown field or property, or a `{triggerData.…}` placeholder in a process started by another trigger (or by a trigger that is no longer registered or provides no data) is replaced by empty text and logs a `SetContactFieldTokenUnresolved` warning (once per hour per process and placeholder). The step continues.
- Text in braces that doesn't look like a placeholder (for example `{}` or `{a b}`) is kept as typed. There's no escaping: a literal `{word}` can't be written.

Conversion of the resolved value (culture-invariant):

| Field type | Accepted value |
| --- | --- |
| Text, long text | As is. Text longer than the field size is truncated (`SetContactFieldValueTruncated` warning, once per hour per process and field). |
| Integer, long integer | Whole number, for example `-12` (no thousands separators) |
| Decimal, floating-point number | Number with a dot as decimal separator, for example `12.75` or `1e3` (the database rounds decimals to the field's decimal places) |
| Boolean | `true`, `false`, `1`, `0` (case-insensitive) |
| Date | `yyyy-MM-dd`; a time part (`yyyy-MM-ddTHH:mm[:ss]`) is dropped |
| Date and time | `yyyy-MM-ddTHH:mm:ss`, `yyyy-MM-ddTHH:mm`, or `yyyy-MM-dd` (midnight); no time zone offsets – values are server-local time |

An empty result clears the field (stores no value). Fields that don't allow empty values get empty text (text fields) or fail the step (other types).

*Calculate*: `current value <operator> operand` in the field's number type; an empty field counts as 0. Integer fields divide without decimals (truncated toward zero, `7 ÷ 2 = 3`, `-7 ÷ 2 = -3`); `%` returns the remainder with the sign of the current value. Only number fields can be calculated – *Calculate* on another field type is an invalid configuration.

Behavior:

- **Fails without changing the field** (step result *failed*, `SetContactFieldFailed` warning with process, contact ID, field, and reason) when the value can't be converted to the field type, the operand isn't a valid number, division or remainder by zero, the result is outside the field type's range (overflow), or saving the contact fails. The contact continues to the next step.
- **Invalid settings** (no field, field deleted or no longer changeable, unknown mode or operator, *Calculate* on a non-number field, empty operand) log an `InvalidSetContactFieldConfiguration` warning (event ID `41080`, at most once per hour per process and field) and save a *failed* result. Nothing is changed.
- Unchanged values aren't saved again (step succeeds). The contact is read, changed, and saved without locking – when several processes change the same field of the same contact at the same moment, the last save wins.
- Values are never logged (they may be personal data). Successful changes log at Debug level (`ContactFieldSet`) with process, contact ID, and field name.
- Disabled component: does nothing and saves no result, warning logged (see [Enable components](#enable-components)). **Skip this step**: does nothing and saves a *skipped* result.

**Works with:** [*Previous step succeeded*](#condition--previous-step-succeeded) (step *Set contact field (dynamic value)*, same result number) to branch on failures; triggers providing data for `{triggerData.…}` placeholders; [*Compare contact number field*](#condition--compare-contact-number-field) for counters kept in integer custom fields.

#### Risks of changing system fields

Built-in contact fields drive Xperience features. Prefer custom fields; change system fields only when you know the effect:

- **Email** (`ContactEmail`) – the address all contact emailing is sent to (marketing emails, recipient lists, automation emails). A wrong or duplicate address sends emails to the wrong person. Membership emails use the member's email and customer emails the customer's email – changing the contact email doesn't change those.
- **Names, company, phone, address** – shown in the admin UI and used in email personalization.
- **Boolean flags** (for example `ContactMonitored`) and `ContactCampaign` can also be set by Xperience itself and may be overwritten later.

Changes apply immediately and the process can't undo them. The step keeps no history of previous values. Test on a staging copy before using it on system fields in production.

### Action – Write to log

Writes a message to the application log for every contact that reaches the step. Use it to see which path contacts take and why – for example after each branch of a *Previous step succeeded* condition.

Marketer properties:

- **Message** – supports `{processName}`, `{contactId}`, `{contactGuid}`, `{triggerIdentifier}`. Unknown placeholders are kept as typed. New steps start with a default message that uses these placeholders.
- **Level** – Information, Warning, or Error.
- **Include results of earlier steps** (default on) – e.g. `SendWebhook.1: failed (HTTP 500, 2026-10-04T12:00:00Z)`.
- **Include trigger details** (default on) – trigger identifier plus non-personal trigger data:
  - Built-in triggers: form and record ID, member ID, activity type and item ID.
  - Library triggers and other registered triggers whose data implements `ITriggerDataDescription` (see [Describe custom trigger data in Write to log](#describe-custom-trigger-data-in-write-to-log)) – for example `order 42, customer 7, total 25.00 USD, 3 item(s), first order`.
  - Any other trigger: identifier only.

Log entry (event ID `41000`, name `AutomationLog`):

```text
Automation 'Example automation', contact c382…: Contact 4 reached this step. | Step results: DancingGoat.CoffeeClubEnrollment.1: succeeded (Enrolled, …); SendWebhook.1: failed (HTTP 500, …) | Trigger: Kentico.FormSubmissionTrigger (form 1, record 3)
```

Entries use the logger category `Kentico.Xperience.Labs.AutomationExtensions.WriteToLog`. With the default Xperience logging configuration (`Logging:XperienceEventLog:LogLevel:Kentico` = `Information`) they appear in the **Event log** application, and in any other logging provider configured in the application.

Only the contact ID/GUID is logged – never email or name. Keep personal data out of messages. The message has no `{triggerData.…}` or `{contact.…}` placeholders on purpose – trigger data can contain personal data (for example custom activity values); trigger details only log what the data type describes itself.

> **Volume:** the step logs once per contact. Enable it in development or for short troubleshooting periods (`"WriteToLog": true` in `appsettings.Development.json`) and keep it disabled in production. Disabled steps do nothing.
>
> **Auditing:** the Event log is trimmed over time and isn't tamper-proof. For a per-contact audit trail use the built-in *Log custom activity* step (visible in the contact's activities); for long-term retention route the `AutomationLog` category to a dedicated logging provider.

### Condition – Activity frequency

Follows the **Yes** branch when the number of activities of one type the contact performed in the last N days matches a comparison (for example *3 or more page visits in 7 days*, *fewer than 2 email clicks in 30 days*, *exactly 1 form submission*), otherwise **No**.

Use it for **how often**. To check whether a contact performed an activity **at all** (for example "visited page X in the last 7 days" or "performed activity with value that contains Y"), use the built-in *Rule-based condition* instead.

Marketer properties:

- **Activity type** – lists enabled activity types (built-in and custom), sorted by name.
- **Detail filters** – optional. The dialog shows only the filters that apply to the selected activity type; several filled filters must all match.
  - **Pages** – page selector (any number of pages from a website channel's content tree; an activity on any selected page counts). Empty selection = no filter.
  - **Language** – *Any* (default, no filter) or one content language; counts only activities on that language version of the pages.
  - **URL rules** – up to 5 rules on parts of the URL, all must match (see [URL rules](#url-rules)). No rules = no filter.
  - Text filters (**Title**, **Value**) – a match operator plus a text. Empty text = no filter (whatever the operator).

  | Activity type | Filters shown | Searched column |
  | --- | --- | --- |
  | Page visit, Landing page | **Pages**, **Language**, **URL rules** (page URL), **Title** (page name) | `ActivityWebPageItemGUID`, `ActivityLanguageID`, `ActivityURL`, `ActivityTitle` |
  | Form submission | **URL rules** (page the form was submitted on), **Title** (form name) | `ActivityURL`, `ActivityTitle` |
  | Member registration | **Title** (member name or email) | `ActivityTitle` |
  | Email link click | **URL rules** (clicked link), **Title** (email name) | `ActivityValue`, `ActivityTitle` |
  | Custom and all other types | **Value** | `ActivityValue` |

  Text match operators: *contains* (default), *does not contain*, *is equal to*, *is not equal to*, *starts with*, *ends with*. Matching follows the database collation: not case-sensitive with a case-insensitive collation (the SQL Server default); a case-sensitive collation makes matching case-sensitive. *Does not contain* and *is not equal to* also match activities where the column is empty.
- **Operator** – same options as *Compare contact number field*: *is equal to*, *is not equal to*, *is greater than*, *is greater than or equal to* (default), *is less than*, *is less than or equal to*.
- **Number of activities** – whole number ≥ 0 to compare the count with, default 3.
- **In the last (days)** – 1–365, default 7.

Behavior:

- **Window:** rolling, from exactly N × 24 hours before the evaluation up to now – not calendar days. Xperience stores activity times in the server time zone (`DateTime.Now`, see [Deploy the database](https://docs.kentico.com/documentation/developers-and-admins/deployment/deploy-to-private-cloud#deploy-the-database)), and the window uses server local time too, like the built-in "in the last N days" rules. On Xperience SaaS the server time is UTC (the admin UI shows times converted to your time zone – a contact created at 15:45 server time shows as 11:45 AM EDT); self-hosted servers use their own time zone. The window length is the same either way.
- **Pages filter:** matches the page an activity was logged on (`ActivityWebPageItemGUID`), so URL slug changes, query strings, and fragments don't matter. Only page visits and landing pages store the page, and only for pages from the content tree tracked by the activity logging script – visits of other URLs (custom routes, cross-site or headless tracking) have no page and never match; use **URL rules** for them. In real data, about 10 % of page visits had no page. The selector offers only pages with a URL (no folders). A page matches in all its language variants. Pages deleted after selection never match (no error; if all selected pages are gone the count is 0); the dialog flags them when you open the step again and asks to remove them before saving.
- **Language filter:** matches the content language the page was visited in (`ActivityLanguageID`, stored with the page by page visits and landing pages). Combine with **Pages** for "visited the Spanish pricing page". The language is stored by code name, so the step keeps working after deployment to another environment. A language deleted after configuration makes the step invalid (see below), like a deleted activity type.
- **Titles** are stored as text built from a template, for example `Page visit 'Home'`, `Form submitted 'Contact us'`, `Member registered as 'jane'`, `Clicked link in email 'Newsletter'`. The **Title** filter compares only the **name in quotes**: *is equal to* `Home` matches `Page visit 'Home'` but not `'Home page'` or `'My Home'`; *contains* `visit` never matches the prefix. The prefixes come from Xperience localization resources (`activity.activitytitle.*`, system culture), so they may differ in other cultures or after customization – matching relies only on the quotes (the name starts after the first apostrophe, the title ends with an apostrophe). Names may contain apostrophes (`O'Brien`); one edge case remains: *starts with* also matches after an apostrophe inside the name (*starts with* `Brien` matches `'O'Brien'`). Page titles are the page's name in the content tree (the title sent by the tracking script for pages outside the content tree).
  > **Personal data:** member registration titles contain the member name or email. Don't copy them into exports or logs; this step never logs filter texts.
- **Detail filters:** Xperience stores the details of built-in activities in different columns – page visits and form submissions have no *Value* – so each type gets the filters that match where its data lives (table above). The referrer URL is never searched: it would count a visit of `/contact` coming from `/pricing` as a pricing visit. Unlike this step, the built-in rule's "with value that contains" searches only the *Value* column. Filters hidden for the current type are ignored, even if they still hold text from a previously selected type.
- **Stored page URLs** are the full browser address: page visits and landing pages store `window.location.href` from the tracking script (scheme, host, path, query string, and `#fragment`); form submissions store the URL of the page the form was submitted from (scheme, host, path, query string, no fragment); email link clicks store the clicked link. The same page shows up in many URL variants (query strings, fragments, trailing slashes, hosts) – **URL rules** compare normalized parts so the variants match consistently.
- **Pages or URL rules?** Prefer **Pages** for pages from the content tree: it counts every visit regardless of URL, survives slug changes, and is the cheapest query. Use **URL rules** for URLs without a page (custom routes, other sites, form submissions, email links), groups of paths (`/blog/*`), hosts, and query parameters. Both can be combined (AND).
- **Activity logging delay:** Xperience saves logged activities to the database in batches, every 10 seconds by default (`CMSProcessContactActionsInterval`, see [Set up activities](https://docs.kentico.com/documentation/developers-and-admins/digital-marketing-setup/set-up-activities#activity-and-contact-update-processing)). Activities logged just before the step runs may not be counted yet – for example the page visit that started the process. Add a short *Wait* step before the condition if that activity must count.
- **Disabled activity types** stay valid: activities logged before the type was disabled still count. Disabled types aren't listed for new selections.
- Read-only: one `COUNT` query per contact (with URL rules: one grouped count per distinct URL, see [URL rules](#url-rules)); no activities are changed.
- Invalid settings (no activity type, activity type deleted, unknown operator or match operator of a filled filter, invalid URL rules, selected language deleted, negative number, days outside 1–365) follow **No** and log an `InvalidActivityFrequencyConfiguration` warning (event ID `41060`, at most once per hour per process and activity type). No contact or activity is read.
- Each evaluation logs a Debug entry (`ActivityFrequencyEvaluated`, event ID `41061`) with process name, contact ID, activity type, days, count, operator, and result – no email, name, URLs, or filter texts.
- Disabled component: all contacts follow **No** without evaluating the condition, regardless of skip settings (see [Enable components](#enable-components)). When enabled, **Skip this step** follows the selected skip branch without counting.

**Example** – notify sales about engaged leads:

1. Trigger: *Form* (contact form submitted).
2. *Activity frequency* (*Page visit*, **Pages** = *Pricing*, *is greater than or equal to* 3, last 7 days) → **Yes**: notify sales (for example *Send webhook* to the CRM or a built-in internal notification email); **No**: *Finish*.

#### URL rules

Each rule picks a **URL part**, an **operator**, and a **value**. All rules must match (up to 5). **Test a URL** below the rules shows, for any URL you paste, whether it matches, the result per rule, and the normalized host, path, and query parameters the step compares – checked on the server with the same logic the step uses.

| URL part | Compares | Example |
| --- | --- | --- |
| *Full URL* | `scheme://host/path?query` (normalized, query as stored) | *starts with* `https://shop.example.com/` |
| *Host* | host name without port | *ends with* `example.com` |
| *Path* | path without query string | *is equal to* `/pricing` |
| *Query parameter* | value(s) of the named parameter | `utm_campaign` *is equal to* `spring` |

Operators: *is equal to*, *is not equal to*, *contains*, *does not contain*, *starts with*, *ends with*, *matches pattern* (`*` = any characters, including `/`; the whole part must match), and for query parameters also *is present* / *is not present*. No regular expressions.

Normalization (applied to stored URLs and to rule values):

- Not case-sensitive.
- `#fragment` is always ignored; default ports (`:443`, `:80`) are dropped; `www.` is **not** removed (use *ends with* `example.com` or `*.example.com` for all subdomains).
- Path: percent-decoded (`/caf%C3%A9` = `/café`), trailing slash ignored (`/pricing/` = `/pricing`), empty path = `/`. For *is equal to*, *is not equal to*, *starts with*, and *matches pattern* a missing leading `/` is added to the value. With *starts with*, a trailing slash in the value is kept: `/blog/` matches `/blog/post` but not `/blog` itself.
- Query parameters: names not case-sensitive, values percent-decoded (`+` = space); a parameter without a value (`?preview`) is present with an empty value; a repeated parameter matches if any of its values matches.
- *Full URL* with *is equal to* / *is not equal to*: the value is normalized like a stored URL.
- Relative URLs (`/pricing?x=1`, rare) have a path and query but no host. Unrecognized values (empty, `mailto:` links, malformed) have no parts.
- Negated operators (*is not equal to*, *does not contain*, *is not present*) also match when the part is missing – a URL without that parameter, without a host, or not recognized at all – like the text filters match empty columns.

Examples:

- Home page by URL (any host, query string, or fragment): *Path* *is equal to* `/`.
- Pricing across hosts and query strings: *Path* *is equal to* `/pricing` (sub-pages: *Path* *starts with* `/pricing/`).
- Blog posts: *Path* *matches pattern* `/blog/*`.
- Campaign traffic: *Query parameter* `utm_campaign` *is equal to* `spring`.
- Form submitted on a contact page (*Form submission*): *Path* *starts with* `/contact`.
- Email clicks on pricing links (*Email link click*): *Path* *starts with* `/pricing`.
- Only the main site: *Host* *is equal to* `www.example.com`.

Performance: the database narrows the contact's activities (type, window, other filters) and, for rules that must find a text (not the negated ones), a parameterized text prefilter; it then returns each distinct URL once with its count, and the step matches those URLs in .NET and sums the counts – rows read are bounded by distinct URLs, not activities (in production data the heaviest contact had 764 distinct URLs in a year, about 54 ms). Safety limits:

- More than 10,000 distinct URLs for one contact in the window → that contact follows **No** and an `ActivityUrlLimitExceeded` warning is logged (event ID `41062`, at most once per hour per process and activity type).
- A *matches pattern* comparison taking longer than 100 ms counts as "no match" and logs a `UrlMatchTimeout` warning (event ID `41063`, at most once per hour per process).
- The prefilter assumes a case-insensitive database collation (Xperience default) and URLs that don't percent-encode plain letters, digits, `-`, `.`, `_`, `~` (browsers don't).

### Condition – Compare contact number field

Follows the **Yes** branch when an integer custom contact field matches a comparison (for example *score ≥ 50*), otherwise **No**. Use it to branch on a lead score kept by [*Adjust contact score*](#action--adjust-contact-score) or on any other integer custom contact field. The built-in *Rule-based condition* can't compare numbers.

**Requires:** an integer custom contact field – the same fields as *Adjust contact score* (see its **Requires** section for how developers add one).

Marketer properties:

- **Number field** – lists custom contact fields of the *Integer number* type, sorted by caption (same list as the *Score field* of *Adjust contact score*). Built-in contact fields, primary key, references to other objects, and other number types (long integer, decimal, floating-point) are never listed. The list is empty until a developer adds a field.
- **Operator** – *is equal to*, *is not equal to*, *is greater than*, *is greater than or equal to* (default), *is less than*, *is less than or equal to*.
- **Value** – whole number to compare with, default 50. Negative numbers are allowed.

Behavior:

- An empty field counts as 0 (same as *Adjust contact score*). For example, *is less than 10* follows **Yes** for a contact without a score.
- Read-only: the contact is never changed.
- Invalid settings (no field, field deleted or changed to another type, unknown operator) follow **No** and log an `InvalidNumberCompareConfiguration` warning (event ID `41050`, at most once per hour per process and field). The contact isn't read.
- Each evaluation logs a Debug entry (`ContactNumberFieldCompared`, event ID `41051`) with process name, contact ID, field name, value, operator, and result – no email or name.
- Disabled component: all contacts follow **No** without evaluating the condition, regardless of skip settings (see [Enable components](#enable-components)). When enabled, **Skip this step** follows the selected skip branch without reading the contact.

**Works with:** *Adjust contact score* – example lead scoring process:

1. Trigger: *Form* (contact form submitted).
2. *Adjust contact score* (*Score field*: Lead score, *Add* 10).
3. *Compare contact number field* (*Number field*: Lead score, *is greater than or equal to* 50) → **Yes**: notify sales (for example *Send webhook* to the CRM or a built-in internal notification email); **No**: *Finish*.

Add more processes that adjust the same field (for example +5 for a webinar registration); every process can end with the same comparison.

### Condition – Previous step succeeded

Follows the **Yes** branch when an earlier step saved a successful result, otherwise **No**.

Marketer properties:

- **Step** – type of the earlier step (e.g. *Send webhook*). Lists library steps and [custom steps registered by developers](#support-previous-step-succeeded-in-custom-steps).
- **Result number** – the number selected on the earlier step (*Result 1–5*).

**Works with:** *Send webhook* (or a registered custom step) with **Record result for later steps** turned on, or *Set contact field (dynamic value)* (always saves its result), and the same **Result number**.
If no earlier step saved that result (missing step, different number, or the earlier step was disabled), an enabled condition follows **No** and logs a warning. If this condition itself is disabled, all contacts follow **No** without reading the saved result.

Each step type has its own numbered results, so different step types never overwrite each other. Within a process, give each step of the same type a different number. Steps can't detect which other steps exist in a process, so the step and number must be matched manually.

### Condition – Random split (A/B)

Splits contacts between the **Yes** and **No** branches using a stable bucket calculated from each contact GUID and a split key. It needs no additional setup, database query, or external service.

Marketer properties:

- **Yes branch percentage** – integer from 0 to 100, default 50. A contact follows **Yes** when its bucket is below this percentage. `0` always follows **No**; `100` always follows **Yes**.
- **Split key** – required, default `default`. Use a different key for an independent split.

The allocation is approximate, not an exact count or guaranteed 50/50 distribution. A contact keeps the same branch across process runs and across processes when its GUID and split key are the same. The process display name is not part of the identity, so renaming a process does not move contacts between branches. There is no automatic per-step identity.

The split key is used exactly as entered: matching is case-sensitive and whitespace in a nonblank key is significant. Changing the key or percentage can change branch outcomes. Increasing the percentage never moves a contact from **Yes** to **No**.

When the component is disabled globally, all contacts follow **No** without evaluating the condition. This takes precedence over **Skip this step** for every library condition. For Random split (A/B), this also means no contact is read and no bucket is calculated. The disabled-component warning records this branch behavior. When enabled, **Skip this step** follows the selected skip branch without reading the contact.

The Builder's step description and percentage help explain this disabled behavior. This is static guidance, not a live enabled/disabled indicator; ask your administrator to confirm the current configuration. Marketers can choose the skip branch per step only while the component is enabled.

To compose a simple A/B test, add **Random split (A/B)** after the process trigger. Connect **Yes** to a built-in **Send email** step with variant A and **No** to a second **Send email** step with variant B. No new assignment is made on a later run; use a new split key when an independent assignment is wanted.

### Condition – Started by trigger

Follows the **Yes** branch when the process was started by the selected trigger and, optionally, a value of its trigger data matches (for example *Form submission* with *BizFormId is equal to 3*), otherwise **No**. Use it when one process has several triggers and each start source needs its own path. Read-only.

**Requires:** a process started by the selected trigger; for the data check, a trigger that provides trigger data.

Marketer properties:

- **Trigger** – the built-in *Form submission*, *Member registration*, and *Custom activity* triggers plus every trigger registered in the application with `RegisterAutomationTrigger` (this library's triggers, your project's custom triggers, other packages), sorted by name. The step saves the trigger identifier. If a saved trigger is no longer registered, the step still matches its identifier but the trigger isn't in the list and its data can't be checked.
- **Also check trigger data** (default off) – in the *Trigger data* section; shown only for triggers that provide trigger data.
- **Trigger data field** – public readable properties of the selected trigger's data type (without `Identifier`). The list changes with the trigger; changing the trigger clears the field. See the table below.
- **Operator** – *is equal to*, *is not equal to*, *contains*, *does not contain*, *starts with*, *ends with*, *is greater than*, *is greater than or equal to*, *is less than*, *is less than or equal to*, *is empty*, *is not empty*.
- **Value** – hidden for *is empty* / *is not empty*. Don't enter personal data.

Fields of built-in and library triggers (custom triggers list the properties of their own trigger data class):

| Trigger | Fields |
| --- | --- |
| Form submission (built-in) | `BizFormId`, `BizFormItemId` |
| Member registration (built-in) | `MemberId` |
| Custom activity (built-in) | `ActivityType`, `ActivityItemId`, `ActivityItemDetailId`, `ActivityValue`, `ActivityChannelId` |
| Contact date anniversary | `FieldName`, `DaysBefore`, `AnniversaryDate`, `Years` |
| Contact inactive for N days | `InactiveDays`, `LastActivityDate` |
| Contact in contact group (daily) | `ContactGroupName`, `ContactGroupDisplayName` |
| Order completed (`.Commerce`) | `OrderId`, `CustomerId`, `Total`, `Currency`, `ItemCount`, `IsFirstOrder` |

| Field type | Operators and value format |
| --- | --- |
| Number (`BizFormId`, `InactiveDays`, …) | All operators. *Equal*, *not equal*, and order operators compare numbers – value with a dot as decimal separator, for example `12.5`. Text operators compare the number as text. |
| Date (`AnniversaryDate`, `LastActivityDate`) | All operators. *Equal*, *not equal*, and order operators compare the date only (time ignored) – value `yyyy-MM-dd`, for example `2026-10-11`. Text operators compare `yyyy-MM-ddTHH:mm:ss` text. |
| Text (`ActivityType`, `ActivityValue`, `FieldName`, `ContactGroupName`, …) | Equality and text operators, ignoring case and leading/trailing spaces of the value. Order operators aren't allowed. |
| Any | *is empty* / *is not empty* – no value; missing values and empty or whitespace text count as empty. |

Behavior:

- A process started by another trigger follows **No** (the data check isn't evaluated).
- A missing value (for example `ActivityValue` not set) is never equal to, greater than, or less than a value – *is not equal to* follows **Yes**.
- Selected trigger without trigger data in the process (or data of another type) → **No** and a `StartedByTriggerDataMissing` warning (event ID `41101`, at most once per hour per process and trigger).
- Invalid settings → **No** and an `InvalidStartedByTriggerConfiguration` warning (event ID `41100`, at most once per hour per process and trigger): no trigger; data check for an unregistered trigger or a trigger without data; no field or a field the trigger's data type doesn't have; unknown operator, no value, value that isn't a number or `yyyy-MM-dd` date for that field, order operator on a text field. Settings are checked before the trigger is read. Entered values are never logged.
- Disabled component: all contacts follow **No** without evaluating the condition, regardless of skip settings (see [Enable components](#enable-components)). When enabled, **Skip this step** follows the selected skip branch.

**Works with:** processes with several triggers – example nurture process:

1. Triggers: *Form* (newsletter form) and *Contact inactive for N days* (90 days).
2. *Started by trigger* (*Form submission*) → **Yes**: welcome email; **No**: next condition.
3. *Started by trigger* (*Contact inactive for N days*, *Also check trigger data*: `InactiveDays` *is greater than or equal to* 90) → **Yes**: re-engagement email; **No**: *Finish*.

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
- **Start time** – required, chosen with a time picker (e.g., 09:00). Boundary is inclusive (≥).
- **End time** – required, chosen with a time picker. Boundary is exclusive (<). A time equal to start time creates a 24-hour window (e.g., 14:00–14:00 includes all times from 14:00 today through 13:59:59 tomorrow).
- **Time zone** – required, selected from host-supported time zones, sorted by standard UTC offset (lowest first), then name. Controls local time conversion for window boundary and DST handling.
- **Start date (optional)** – chosen with the date picker. If set, the contact must reach this condition on or after the specified date (inclusive). Validation: if both start and end date are set, start date must be ≤ end date.
- **End date (optional)** – chosen with the date picker. If set, the contact must reach this condition on or before the specified date (inclusive). Validation: if both start and end date are set, end date must be ≥ start date.

The time picker offers a styled dropdown of 15-minute suggestions. Edit the hour/minute segments for any minute; saved values between suggestions remain selectable. Display follows the admin locale, while values are stored as `HH:mm` and interpreted in the selected time zone, not the browser time zone.

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

- **Actions** – **Skip this step**: contacts move on without the step running. If the step saves results (*Record result for later steps*; always for *Set contact field (dynamic value)*), it saves a *skipped* result, which *Previous step succeeded* treats as not succeeded (No branch, no warning), and *Write to log* shows as `skipped`.
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
| Activity frequency | Logged activities of the selected type (activity tracking enabled; custom activities logged by code) | Count is 0 |
| Adjust contact score | Integer custom contact field | Field list empty; invalid step does nothing, warning logged |
| Compare contact number field | Integer custom contact field (usually kept by *Adjust contact score*) | Field list empty; invalid step follows **No** branch, warning logged |
| Contact date anniversary | Scheduled task configuration | Trigger never fires |
| Contact in contact group (daily) | Scheduled task configuration; contact group code names in `ContactGroups`; contact group recalculation | Trigger never fires; drop-down empty; stale membership |
| Contact inactive for N days | Scheduled task configuration; logged activities | Trigger never fires; contacts without activity never start |
| Order completed | `.Commerce` package; checkout code calling `IOrderCompletedTriggerDispatcher.Fire` with the visitor's contact | Trigger never fires; untracked visitors (no contact) never start |
| Previous step succeeded | Earlier step of the selected type saving the same result number | Follows **No** branch, warning logged |
| Random split (A/B) | None | No additional setup required |
| Send webhook | Endpoint in configuration | Does nothing, warning logged |
| Set contact field (dynamic value) | Custom contact field (recommended); trigger providing data for `{triggerData.…}` placeholders | Field list has system fields only; placeholder empty, warning logged |
| Started by trigger | Process started by the selected trigger; trigger data of that trigger for the data check | Follows **No** branch; missing data logs a warning
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

Results are stored in the `StepResultData` automation process data (identifier `Labs.AutomationExtensions.StepResultData`): a dictionary keyed by `<step name>.<result number>`, e.g. `SendWebhook.1`, `SetContactField.1`, or `Acme.SyncToCrm.2`.
Values: `Succeeded`, `ActionIdentifier`, `RecordedAt` (UTC), `Message`. The data is deleted together with the contact.

## Describe custom trigger data in Write to log

*Write to log* (*Include trigger details*) logs only the identifier of your own triggers. To add data, implement `ITriggerDataDescription` (namespace `Kentico.Xperience.Labs.AutomationExtensions.Triggers`) on the trigger data class:

```csharp
public sealed class AcmeQuoteTriggerData : IAutomationTriggerData, ITriggerDataDescription
{
    public string Identifier => "Acme.QuoteTriggerData";

    public int QuoteId { get; init; }

    public decimal Amount { get; init; }

    // Logged for every contact – identifiers and non-personal values only, invariant culture.
    public string Describe() => string.Format(CultureInfo.InvariantCulture, "quote {0}, amount {1:0.00}", QuoteId, Amount);
}
```

- The text is written to the application log: return IDs, code names, counts, amounts, or dates only – never names, emails, addresses, or text entered by visitors.
- Used only when the trigger is registered (`RegisterAutomationTrigger`) and the trigger data of the process is of the trigger's data type. Empty text → identifier only. An exception → `(description failed: <exception type>)`; the message isn't logged.
- `{triggerData.…}` placeholders and *Started by trigger* need no extra code – they read public properties of any registered trigger's data class.

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
| 41040 | `ContactScoreAdjusted` | Information | *Adjust contact score* changed a contact's score |
| 41041 | `InvalidContactScoreConfiguration` | Warning | *Adjust contact score* settings can't be applied (once per hour per process and field) |
| 41050 | `InvalidNumberCompareConfiguration` | Warning | *Compare contact number field* settings can't be evaluated, contacts follow **No** (once per hour per process and field) |
| 41051 | `ContactNumberFieldCompared` | Debug | *Compare contact number field* result for a contact |
| 41060 | `InvalidActivityFrequencyConfiguration` | Warning | *Activity frequency* settings can't be evaluated, contacts follow **No** (once per hour per process and activity type) |
| 41061 | `ActivityFrequencyEvaluated` | Debug | *Activity frequency* count and result for a contact |
| 41062 | `ActivityUrlLimitExceeded` | Warning | *Activity frequency* URL rules skipped: contact has more than 10,000 distinct URLs in the window, contact follows **No** (once per hour per process and activity type) |
| 41063 | `UrlMatchTimeout` | Warning | A *matches pattern* URL rule took too long; affected URLs counted as not matching (once per hour per process) |
| 41070 | `ContactInactiveTaskCompleted` | Information | *Contact inactive for N days* scheduled task run summary |
| 41080 | `InvalidSetContactFieldConfiguration` | Warning | *Set contact field (dynamic value)* settings can't be applied, nothing changed (once per hour per process and field) |
| 41081 | `SetContactFieldTokenUnresolved` | Warning | *Set contact field (dynamic value)* placeholder replaced by empty text (once per hour per process and placeholder) |
| 41082 | `SetContactFieldValueTruncated` | Warning | *Set contact field (dynamic value)* text longer than the field size was truncated (once per hour per process and field) |
| 41083 | `SetContactFieldFailed` | Warning | *Set contact field (dynamic value)* didn't change a contact's field: conversion, calculation, or save failed (no values logged) |
| 41084 | `ContactFieldSet` | Debug | *Set contact field (dynamic value)* outcome for a contact (updated, unchanged, or not empty) |
| 41090 | `ContactGroupMemberTaskCompleted` | Information | *Contact in contact group (daily)* scheduled task run summary (total and per-group trigger counts) |
| 41091 | `ContactGroupNotFound` | Warning | Configured contact group doesn't exist or isn't a contact group (e.g. a segment); skipped (every task run) |
| 41092 | `ContactGroupIsRecipientList` | Warning | Configured contact group is a recipient list; skipped (every task run) |
| 41100 | `InvalidStartedByTriggerConfiguration` | Warning | *Started by trigger* settings can't be evaluated, contacts follow **No** (once per hour per process and trigger) |
| 41101 | `StartedByTriggerDataMissing` | Warning | *Started by trigger* found no trigger data of the selected trigger, contact follows **No** (once per hour per process and trigger) |
| 41110 | `OrderCompletedFired` | Debug | *Order completed* trigger fired for an order |
| 41111 | `OrderCompletedNotFired` | Debug | *Order completed* not fired: component disabled or no contact passed |
| 41112 | `OrderCompletedFailed` | Error | *Order completed* failed (loading order details or firing); checkout continues |

## Localization

All marketer-facing texts are in `Resources/AutomationExtensionsResources.resx` (English). To translate, add `AutomationExtensionsResources.<culture>.resx` files in your project and register them with `RegisterLocalizationResource` – see [Admin UI localization](https://docs.kentico.com/documentation/developers-and-admins/customization/admin-ui-localization).
