# Status

Single source for current state. Update after every slice (and when a check or decision closes).
Design, research, decisions with reasons, and the slice log: `PLAN.md`. Per-slice scope: `implementation-spec-*.md`.

Last updated: 2026-10-13. Xperience 31.9.1. Unit tests: 1375 passed (Admin 1309, Commerce 66).

## Components

Numbers = `PLAN.md` section 3. Validation: **app** = verified in running DancingGoat; **unit** = unit tests only.

| # | Name | Kind | State | Validation | Open checks |
| --- | --- | --- | --- | --- | --- |
| 2 | Contact date anniversary (+ scheduled task) | T | Done | app | *7 days before* firing (contact 9, birthday 10-11) |
| 12 | Previous step succeeded | C | Done | app | – |
| 14 | Send webhook | A | Done | app | – |
| 21 | Write to log | A | Done | app | Trigger details via `ITriggerDataDescription` for #1, #2, #3 in running app (Order completed checked 2026-10-13) |
| 10 | Random split (A/B) | C | Done | unit | Builder + enabled split branching |
| 9 | Within time window | C | Done | unit + isolated browser | Time picker/date fields in running Builder |
| 16 | Adjust contact score | A | Done | app | – |
| 22 | Compare contact number field | C | Done | unit | Field dropdown, Yes/No per operator, 41050 warning |
| 8 | Activity frequency | C | Done | app | Invalid config → No + 41060; disabled → No + 41003 |
| 1 | Contact in contact group (daily) (+ scheduled task) | T | Done | unit | Type filter excludes recipient lists (`UseObjectTypeCondition = false` lookup), end-to-end firing (group has 0 members), dropdown, *Last result* texts |
| 23 | Contact in recipient list (daily) | T | Not started | | Split from #1 (recipient lists = static groups); spec #1 excludes them |
| 3 | Contact inactive for N days (+ scheduled task) | T | Done | unit + PROD-DB query check | Task config, firing (`InactiveDays: [1]`), *If not already running* + Wait blocks same-day re-run, disabled *Last result* shown as failure? |
| 4 | Content engagement threshold | T | Not started | | |
| 5 | Inbound webhook received | T | Not started | | |
| 6 | Order completed (`.Commerce` project) | T | Done | app (2026-10-13: dialog renders; checkout with consent fires, without consent doesn't; Min total / Currency / First order only filters work; #17 `{triggerData.X}` sets value; Write to log details incl. `item(s)`; 2nd order of same customer → `repeat order`) | *Started by trigger* lists it + data fields |
| 24 | Order contains products ("Items purchased") | C | Not started | | Design settled 2026-10-13: resolver hook (order ID → product GUIDs), default `[]`. Pending decision 3 |
| 11 | Started by trigger (trigger data matches) | C | Done | unit | Trigger list (built-ins + registered, labels), data fields hide for no-data triggers, Field narrows/clears on Trigger change + save passes, end-to-end data check (library + built-in trigger), unregistered saved identifier fails resave? |
| 13 | Purchase history meets threshold | C | Not started | | |
| 15 | Post to Teams / Slack | A | Not started | | |
| 17 | Set contact field (dynamic value) | A | Done | unit | Clear field (null), system field save (email side effects), Mode visibility, Previous step succeeded pairing, `{triggerData.*}` per trigger (now any registered trigger, incl. project triggers) |
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
| Components from other assemblies (`AddAutomationComponent`: status log + step registry) | Done (unit; startup log seen in app) | Step registry row for `OrderCompleted` in running app |
| Trigger store for #17 `{triggerData.X}` + Write to log details (`Core/RegisteredTriggers`; `KnownTriggers` removed; public `ITriggerDataDescription`) | Done (app: Order completed via #17 + Write to log, 2026-10-13) | See #17, #21 |
| Process-usage / dependency checks, separate admin app (PLAN 2.4) | Not started | |
| README | Placeholder | |

## Activity frequency (#8) – open

1. Running-app checks left: invalid config → No + 41060; disabled → No + 41003.
2. Slice 7 (spec: `implementation-spec-activity-frequency.md` → Slice 7): **7A Page filter** done (app, 2026-10-11): Pages (page visit + landing page), Language (same types, code name, "Any" default), Title also for page visit/landing page/email click, Title operators match the quoted name only. **7C** URL match component done (app, 2026-10-11; spec "7C as built"): URL rules editor, Test a URL, property switching verified. PROD data check done (spec "Real-data check – PRODUCTION"). **7B benchmark** deferred (PROD timings sufficient). Regex rejected (SaaS compat level 160 < 170). No UTM filters (columns not populated by default).
## Pending decisions

1. Localization: keep `Server` + `Builder` registration or drop `Server`.
2. Trigger display names: submit `kentico-issue-trigger-display-name-resource-key.md`; plain-text names vs. keys + known-issue note.
3. Package split. Proposal under consideration (user, 2026-10-13, not decided): `Kentico.Xperience.Labs.AutomationExtensions` (general steps), `.Admin` (admin UI extensions: form components, pages, client module), `.Commerce` (commerce steps #6, #13, #24). Now: `.Commerce` project exists (#6) and references `.Admin`; existing projects not restructured. Blockers for a clean split: see `PLAN.md` slice 12.
4. #15 Teams/Slack, #20 enrichment: real provider or sample/abstract only.
5. Minimum supported Xperience version (only 31.9.1 tested).

## Other open items

1. Anniversary task same-day re-run protection (deferred; option: store last processed date).
2. Local test data cleanup: contacts 8 and 9, *Contact Anniversary* process, Email queue.
3. `ContactScore` field exists only in local DB (CI repository off) – recreate per environment.
4. Local DBs `Kentico.SaaS.Analysis` (QA env) and `Kentico.SaaS.Prod.Analysis` (PROD, 298k activities): `OM_Contact` PII, `ActivityComment`, member registration titles cleared; members/users/form/email tables **not** scrubbed. Kept for real-data checks and 7B benchmark. Local only, never share. Backup files deleted.
5. Event log length issue reported (`kentico-issue-event-log-source-length.md`); no response tracked.
