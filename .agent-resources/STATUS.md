# Status

Single source for current state. Update after every slice (and when a check or decision closes).
Design, research, decisions with reasons, and the slice log: `PLAN.md`. Per-slice scope: `implementation-spec-*.md`.

Last updated: 2026-10-10. Xperience 31.9.1. Unit tests: 446 passed.

## Components

Numbers = `PLAN.md` section 3. Validation: **app** = verified in running DancingGoat; **unit** = unit tests only.

| # | Name | Kind | State | Validation | Open checks |
| --- | --- | --- | --- | --- | --- |
| 2 | Contact date anniversary (+ scheduled task) | T | Done | app | *7 days before* firing (contact 9, birthday 10-11) |
| 12 | Previous step succeeded | C | Done | app | – |
| 14 | Send webhook | A | Done | app | – |
| 21 | Write to log | A | Done | app | – |
| 10 | Random split (A/B) | C | Done | unit | Builder + enabled split branching |
| 9 | Within time window | C | Done | unit + isolated browser | Time picker/date fields in running Builder |
| 16 | Adjust contact score | A | Done | app | – |
| 22 | Compare contact number field | C | Done | unit | Field dropdown, Yes/No per operator, 41050 warning |
| 8 | Activity frequency | C | Done | unit | See below |
| 1 | Contact in contact group (daily) | T | Not started | | |
| 3 | Contact inactive for N days | T | Not started | | |
| 4 | Content engagement threshold | T | Not started | | |
| 5 | Inbound webhook received | T | Not started | | |
| 6 | Order completed | T | Not started | | |
| 11 | Started by trigger / trigger data matches | C | Not started | | |
| 13 | Purchase history meets threshold | C | Not started | | |
| 15 | Post to Teams / Slack | A | Not started | | |
| 17 | Set contact field (dynamic value) | A | Not started | | |
| 18 | Subscribe / unsubscribe recipient list | A | Not started | | |
| 19 | Assign contact owner (round-robin) | A | Not started | | |
| 20 | Enrich contact from external API | A | Not started | | |
| 7 | Has consent agreement | C | Dropped | – | Built-in rule covers it |

## Infrastructure

| Feature | State | Open checks |
| --- | --- | --- |
| Appsettings enable/disable, startup status log, disabled-step warning | Done (app) | – |
| Step results (process data) + result sources | Done (app) | – |
| Skippable steps | Done (app) | – |
| Localization (`Server` + `Builder`) | Done (app) | Decision: drop `Server`? |
| Log category / event name length guards | Done (unit) | – |
| Step registry tab (Kind/Status filters, current columns) | Done (unit; original UI seen in app) | Current UI + filters in running app |
| Process-usage / dependency checks, separate admin app (PLAN 2.4) | Not started | |
| README | Placeholder | |

## Activity frequency (#8) – open

1. Running-app checks: dropdown names; operator + text fields per type (none / URL / URL+Title / Title / Value) switch without save; *starts with* `https://<host>/pricing` ≥ 3 → Yes (wait > 10 s); *is equal to* `https://<host>/` counts `/` not `/?utm_source=x`; *does not contain* counts empty columns; invalid config → No + 41060; disabled → No + 41003.
2. **Decision pending:** add **Page** filter (web page selector + optional language, matches `ActivityWebPageItemGUID`) for reliable page matching; regex rejected (SQL support/perf/UX). First verify which activity types store page GUID + language.
## Pending decisions

1. Localization: keep `Server` + `Builder` registration or drop `Server`.
2. Trigger display names: submit `kentico-issue-trigger-display-name-resource-key.md`; plain-text names vs. keys + known-issue note.
3. Commerce components (#6, #13): core package or separate `.Commerce` package.
4. #15 Teams/Slack, #20 enrichment: real provider or sample/abstract only.
5. Minimum supported Xperience version (only 31.9.1 tested).

## Other open items

1. Anniversary task same-day re-run protection (deferred; option: store last processed date).
2. Local test data cleanup: contacts 8 and 9, *Contact Anniversary* process, Email queue.
3. `ContactScore` field exists only in local DB (CI repository off) – recreate per environment.
4. Event log length issue reported (`kentico-issue-event-log-source-length.md`); no response tracked.
