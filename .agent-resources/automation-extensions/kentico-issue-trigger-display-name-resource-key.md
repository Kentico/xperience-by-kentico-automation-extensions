# Issue report: Custom automation trigger display name stored as unresolved resource key

Status: submitted 2026-10-21; no response yet. Revert trigger display names to resource keys when fixed. Xperience by Kentico 31.9.1; rechecked on 31.9.3 (2026-10-20). Library switched trigger display names to plain text meanwhile.

## Summary

`RegisterAutomationTrigger<T>` accepts a localization resource key as `displayName` (the Automation Builder resolves it
correctly in the trigger selection dialog). When a marketer adds the trigger to a process, the system stores the
**unresolved key** in `CMS_ObjectWorkflowTrigger.TriggerDisplayName`. Places that show the stored trigger name display the
raw key instead of the localized text.

## Steps to reproduce

1. Register a localization resource (`RegisterLocalizationResource`) containing
   `acme.automation.mytrigger.displayname` = `My trigger`.
2. Register a custom trigger using the key as display name:

   ```csharp
   [assembly: RegisterAutomationTrigger<MyTrigger>(
       MyTrigger.IDENTIFIER,
       "acme.automation.mytrigger.displayname",
       Description = "acme.automation.mytrigger.description")]
   ```

3. In the Automation Builder, create a process and select **My trigger** as its trigger. The selection dialog shows
   `My trigger` (resolved), and the trigger step name is pre-filled with the resolved text.
4. Save the process.

## Actual result

- `CMS_ObjectWorkflowTrigger.TriggerDisplayName` = `acme.automation.mytrigger.displayname`
- CI/CD repository: the trigger file is named after the key and stores it as the display name
- Automation process listing (*Trigger* column, 31.9.3): `Custom: labs.automationextensions.ordercompleted.displayname`

Not affected: the Event log. `CREATEOBJ` / `UPDATEOBJ` entries use the trigger's **step name** in the process (pre-filled with
resolved text, editable by the marketer), not the registered display name (rechecked on 31.9.3, 2026-10-20). CI/CD
repository not rechecked on 31.9.3.

Observed in this repository with `labs.automationextensions.contactdateanniversary.displayname`
(process *Contact Anniversary*). CI store output:

```text
App_Data/CIRepository/@global/cms.objectworkflowtrigger/contactanniversary@74d07f7e39/
  labs.automationexp..ersary.displayname@a258e8ccd8.xml
```

```xml
<TriggerDisplayName>
  <![CDATA[labs.automationextensions.contactdateanniversary.displayname]]>
</TriggerDisplayName>
```

For comparison, a built-in form trigger in the same repository: `coffee_sample_list@eb8c411f9c.xml` with
`<TriggerDisplayName>Coffee sample list</TriggerDisplayName>`.

## Expected result

One of:

- the stored `TriggerDisplayName` is the resolved (default culture) text, or
- the stored value stays a key, but every place that displays it resolves it (automation process listing, CI/CD
  repository file names and content, other admin listings).

And the documentation states whether `displayName` / `Description` of `RegisterAutomationTrigger` (and the action /
condition attributes) support resource keys. The
[Custom automation triggers](https://docs.kentico.com/documentation/developers-and-admins/digital-marketing-setup/automation-customization/automation-custom-triggers)
page currently shows only plain-text values.

## Impact

- Marketers see the raw key in the automation process listing (*Trigger* column). The Automation Builder shows resolved text.
- Confusing for administrators and developers – CI/CD repository files for `cms.objectworkflowtrigger` are named after
  the raw key.
- Pushes library authors towards non-localizable plain-text trigger names.

## Notes

- Built-in triggers (e.g. form submission) store a readable name (the form's display name).
- Custom **steps** (actions, conditions) are not affected: their registered display name is not stored in the database or
  CI/CD repository and is shown only in the step selection dialog, where it is resolved correctly.
- Triggers differ: the registered display name is stored in `CMS_ObjectWorkflowTrigger.TriggerDisplayName` because it is
  shown in the automation process listing and names the trigger's CI/CD repository file.
- Not verified: other admin screens that list triggers.
