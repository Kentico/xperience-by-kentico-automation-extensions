# Implementation spec: anniversary offset verification

## Goal

Add deterministic unit coverage for the date calculation used by `AnniversaryTask` when firing a contact date anniversary a configured number of days early. The plan identifies the 7-days-before case as untested.

## Scope

- Keep the date calculation in the existing `Triggers/ContactDateAnniversary/AnniversaryCalendar` helper.
- If required to test the production path cleanly, add a small pure helper that calculates the anniversary date from the task's run date and `daysBefore`, then have `AnniversaryTask` use it.
- Add NUnit coverage proving a run on 2026-10-04 with `daysBefore = 7` targets 2026-10-11, and that the zero-day case still targets the run date.
- Follow the existing test naming, assertions, namespace, and formatting in `AnniversaryCalendarTests`.
- Do not alter trigger identifiers, properties, localization, configuration behavior, contact querying, or task scheduling.

## Acceptance criteria

- `AnniversaryTask` uses the same tested calculation covered by the new unit test.
- The 7-day scenario asserts the full target date, not only a month/day or a duplicated test-only calculation.
- Existing zero-day behavior remains covered.
- The targeted anniversary tests pass with the repository's existing test project and conventions.
- Report the exact files changed and validation command/result.

## Out of scope

- A running-app/database verification with the pre-existing sample contact.
- Same-day repeat protection or other anniversary edge-case changes.
- Updating `.agent-resources/automation-extensions/PLAN.md`; the caller will update it after reviewing and validating the implementation.
