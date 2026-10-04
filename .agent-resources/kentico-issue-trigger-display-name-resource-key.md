# Issue report: Custom automation trigger display name stored as unresolved resource key

Status: draft (not submitted). Xperience by Kentico 31.9.1.

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
- Event log: `CREATEOBJ` / `UPDATEOBJ` entries read `Create Trigger 'acme.automation.mytrigger.displayname'`
- CI/CD repository: the trigger file is named after the key and stores it as the display name

Observed in this repository with `labs.automationexperiments.contactdateanniversary.displayname`
(Event log entries #953 and #963, process *Contact Anniversary*). CI store output:

```text
App_Data/CIRepository/@global/cms.objectworkflowtrigger/contactanniversary@74d07f7e39/
  labs.automationexp..ersary.displayname@a258e8ccd8.xml
```

```xml
<TriggerDisplayName>
  <![CDATA[labs.automationexperiments.contactdateanniversary.displayname]]>
</TriggerDisplayName>
```

For comparison, a built-in form trigger in the same repository: `coffee_sample_list@eb8c411f9c.xml` with
`<TriggerDisplayName>Coffee sample list</TriggerDisplayName>`.

## Expected result

One of:

- the stored `TriggerDisplayName` is the resolved (default culture) text, or
- the stored value stays a key, but every place that displays it resolves it (Event log descriptions, CI/CD repository
  file names and content, other admin listings).

And the documentation states whether `displayName` / `Description` of `RegisterAutomationTrigger` (and the action /
condition attributes) support resource keys. The
[Custom automation triggers](https://docs.kentico.com/documentation/developers-and-admins/digital-marketing-setup/automation-customization/automation-custom-triggers)
page currently shows only plain-text values.

## Impact

- Cosmetic for marketers – the Automation Builder shows resolved text.
- Confusing for administrators and developers – Event log entries and CI/CD repository files for
  `cms.objectworkflowtrigger` are named after the raw key.
- Pushes library authors towards non-localizable plain-text trigger names.

## Notes

- Built-in triggers (e.g. form submission) store a readable name (the form's display name).
- Custom **steps** (actions, conditions) registered with resource keys are not affected in the same way: the step name is
  entered/pre-filled in the builder with the resolved text and stored in `StepDisplayName`.
- Not verified: other admin screens that list triggers.
