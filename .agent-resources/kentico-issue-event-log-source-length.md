# Issue report: Event log silently drops ILogger entries with categories longer than 100 characters

Status: submitted to Kentico core product (2026-10-04). Xperience by Kentico 31.9.1.

## Summary

Entries logged via `ILogger<T>` are not written to the Xperience event log (`CMS_EventLog`) when the logger category
(the full name of `T`) is longer than 100 characters. The entries still reach other logging providers (console),
no error is visible in the Event log application, and the documentation doesn't mention the limit.

`CMS_EventLog.Source` is `nvarchar(100)`; the category is used as the Source value.

## Steps to reproduce

1. Create a class whose full type name is longer than 100 characters, e.g.
   `Kentico.Xperience.Labs.AutomationExperiments.Triggers.ContactDateAnniversary.ContactDateAnniversaryScheduledTask` (112).
2. Inject `ILogger<ThatClass>` and log a Warning (passes the default `XperienceEventLog` filter for any code).
3. In the same method, log a Warning via `ILoggerFactory.CreateLogger("Acme.Test")`.

## Actual result

- Console: both entries.
- Event log: only the `Acme.Test` entry.

## Expected result

Either:

- the Source value is truncated to 100 characters and the entry is written, or
- a fallback entry / error is written so the loss is visible, and
- the 100-character limit is documented on the Logging page (custom code uses `ILogger<T>`, so long namespaces in
  class libraries hit it easily).

## Suggested documentation change

On [Logging](https://docs.kentico.com/documentation/developers-and-admins/development/logging), section *Logging in custom code*,
next to "The category also determines the **Source** value for events in the Xperience event log", add:

> The event log stores the Source (logger category) and Event code (`EventId` name) in 100-character columns. Events whose
> category or event name exceed 100 characters are not written to the Xperience event log (other logging providers are not
> affected). Keep the fully qualified names of types used as `TCategoryName` within this limit, or create loggers with a
> shorter category via `ILoggerFactory.CreateLogger(string)`.

This matters especially for AI-assisted development (KentiCopilot skills, agents following the docs), which generate
deeply nested namespaces and descriptive class names by default.

## Notes

- `CMS_EventLog.EventCode` is also `nvarchar(100)` (`EventId.Name`); likely the same behavior.
- Investigation initially looked like a scheduled-task issue because the first affected class was a custom `IScheduledTask`;
  the same happens for any class (e.g. a custom automation condition with a 108-character name).
