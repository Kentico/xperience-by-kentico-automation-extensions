# Implementation spec: Activity frequency

## Selection

Implement condition #8 from `PLAN.md` (3.2), **reframed** as *Activity frequency*. Read-only, one count query, no task,
external service, trigger data, or prerequisite. Reuses `NumberComparisonOperator` / `NumberComparison` from
*Compare contact number field* (#22). Validate in DancingGoat by logging activities (page visits, form submissions).

Rejected now: #11 trigger-data design; #17 token design; #18 API verification; #1/#3 scheduled tasks; #15/#19/#20 providers/state.

## Why reframed (user confirmed 2026-10-05)

Built-in *Rule-based condition* already covers existence: "Contact has performed activity X with value that contains Y in
the last N days", "visited page in the last N days", "clicked link in email in the last N days". It can't check
**how often**. This condition only adds the frequency gap: "visited pricing ≥ 3× in 7 days", "< 2 sessions in 30 days",
"exactly 1 form submission". Description must point marketers to the built-in rule for plain "has done X" checks.

## User decisions (2026-10-05)

- Single activity type (dropdown). No multi-select, no "any activity".
- Optional **value contains** filter (matches built-in rule wording), empty = no filter.
- Rolling window: activities created since now − N days. Days 1–365, default 7.
- Defaults: operator `GreaterThanOrEqual`, count 3.
- Disabled → **No** (`return false`) – existing final decision, don't revisit.

## Identity

- Name `ActivityFrequency`; identifier `Labs.AutomationExtensions.ActivityFrequency`
  (`AutomationComponentCatalog.IDENTIFIER_PREFIX + NAME`). Never change after release.
- Kind: Condition. Add descriptor `AutomationComponentCatalog.ActivityFrequency` (+ `All`).
- Folder `Conditions/ActivityFrequency/`. Follow `ContactNumberFieldCompareCondition` structure closely.
- Icon: existing `Icons.*` constant distinct from other conditions (`CheckCircle`, `Cogwheel`, `Clock`, `Gauge`) and
  actions; e.g. a repeat/counter/chart-like icon. Verify it exists in 31.9.1 (dotnet-inspect / reflection).
- Display name "Activity frequency". Description (one sentence + hint): checks how many times the contact performed an
  activity type in the last N days; for "has performed at least once" use the built-in Rule-based condition.

## Properties (`ActivityFrequencyProperties : SkippableConditionProperties`)

| Property | Type | Component | Default | Notes |
| --- | --- | --- | --- | --- |
| `ActivityType` | string | `DropDownComponent`, new `ActivityTypeOptionsProvider` | `""` | Required. Value = `ActivityTypeName` (code name). Text = localized display name (resolve macros/resource keys via `ILocalizationService` if display names are keys). List **enabled** types sorted by text. |
| `Operator` | string | `DropDownComponent`, reuse `NumberComparisonOperatorOptionsProvider` | `GreaterThanOrEqual` | Required. Reuse texts; if the provider is tied to compare-condition keys, keep reusing them (same wording) – don't duplicate keys. Make provider reusable (move to shared place only if cleaner; don't change persisted values). |
| `Count` | int | `NumberInputComponent` | 3 | Min 0 (`MinimumIntegerValueValidationRule`). |
| `Days` | int | `NumberInputComponent` | 7 | 1–365 (min/max validation rules). |
| `ValueContains` | string | `TextInputComponent` | `""` | Optional. Case-insensitive contains on `ActivityValue` (SQL default collation). Explanation: e.g. page URL part / form name depending on activity type; empty = all. |

Orders 10/20/30/40/50, no category (match compare condition). All labels/explanations/validation messages in
`AutomationExtensionsResources.resx` with `labs.automationextensions.activityfrequency.*` keys, macro form in attributes.
`ExplanationTextAsHtml` + `<br>` only for ≥ 3-sentence texts (PLAN 5.3). Don't override `StepDisplayName`.

**[verify]** what `ActivityValue` holds for common types (page visit, form submission, custom) in 31.9.1 and how the
built-in rule's "with value that contains" matches (only `ActivityValue`?). Report findings; if built-in matches other
columns, match the same columns and document. If unclear, implement `ActivityValue` only and report.

## Runtime behavior (`Evaluate`)

1. Disabled → `DisabledComponentLog.Write(…, "sends all contacts through the No branch")`, return `false`.
2. `SkippableStep.TryGetSkippedResult` → log skipped, return selected branch.
3. Validate (pure helper, e.g. `ActivityFrequencyConfiguration.GetConfigurationError`): activity type non-empty and exists
   (via provider/cached lookup; disabled type = still valid – contact may have old activities), operator parses (reuse
   compare parsing), count ≥ 0, days 1–365. Invalid → warning `InvalidActivityFrequencyConfiguration` once/hour per
   process+type (`LogWithIntervalPolicy`, same as compare), return `false`, no contact read/query.
4. `cancellationToken.ThrowIfCancellationRequested()`; `await context.GetProcessedObject(cancellationToken)`.
5. Count via `IInfoProvider<ActivityInfo>` (or the documented equivalent): `ActivityContactID = contact.ContactID`,
   `ActivityType = type`, `ActivityCreated >= since`, optional `ActivityValue LIKE %value%` (parameterized – never
   concatenate SQL), count only (`GetCountAsync` / `.Count` – no row load), propagate cancellation.
   **[verify]** whether `ActivityCreated` is stored in server-local time or UTC; compute `since` in the same basis
   (pure helper `ActivityWindow.GetSince(DateTime now, int days)` with injected clock/`TimeProvider` if existing code has one).
6. Result via `NumberComparison.Compare(count, op, properties.Count)`. Return it.
7. Read-only. Debug log `ActivityFrequencyEvaluated` (process, contact ID, type, days, count, operator, value, result; no
   PII, don't log `ValueContains`? – it's marketer config, not PII; OK to omit). No caching (fresh per contact).

Note: activity logging may be asynchronous (queued) – activities logged seconds before evaluation may not be counted yet.
Document in Usage Guide if confirmed by docs; otherwise mention as possible delay.

DI: `IAutomationComponentStatus`, `IInfoProvider<ActivityInfo>` (or query abstraction for testability – e.g. internal
`IActivityCounter` with DB implementation, registered in DI, so gating/compare paths are unit-testable; match existing DI
registration style in `AutomationExtensionsServiceCollectionExtensions`), `ILoggerFactory` with
`LogCategories.For(AutomationComponentCatalog.ActivityFrequency)` – **never `ILogger<T>`**.

New `LogEvents`: block 41060 `InvalidActivityFrequencyConfiguration`, 41061 `ActivityFrequencyEvaluated`. Must pass length tests.

## Documentation

- `docs/Usage-Guide.md`: component section (what/when, built-in rule for existence vs. this for frequency), properties,
  operators, window semantics (rolling, time basis), value filter, disabled → No, skip, possible logging delay;
  enablement example + component/log tables; composition example (page visit trigger/process → ≥ 3 pricing visits in 7
  days → sales notification). Follow existing compare-condition section style.
- `examples/DancingGoat/appsettings.Development.json`: enable `ActivityFrequency` (keep other entries).
- Don't touch README.

## Tests (existing NUnit project/patterns, no new packages)

- Window helper: since = now − days; boundaries 1/365; time basis.
- Configuration validation: empty/unknown type, bad operator, negative count, days 0/366.
- Options provider: enabled types only, values = code names, texts localized, sorted.
- Gating (disabled → `false` even with skip = Yes; skipped → selected branch; invalid → `false` without contact read/count),
  following `ContactNumberFieldCompareConditionTests`; counter abstraction faked for compare path if reachable; report
  limits of `GetProcessedObject` without Xperience container.
- Existing catalog/registration, localization key coverage, skippable, log category/event-name length tests; update
  `StepRegistryPageTests` counts (components +1, conditions +1).
- `dotnet test .\tests\Kentico.Xperience.Labs.AutomationExtensions.Admin.Tests\Kentico.Xperience.Labs.AutomationExtensions.Admin.Tests.csproj --no-restore`; report counts.
- `dotnet build .\examples\DancingGoat\DancingGoat.csproj --no-restore -t:Compile` (running app may lock DLL).
- Format per `AGENTS.md` (`dotnet format --no-restore`); keep formatter changes only in touched files; `git diff --check`.

## Work boundaries

- No commits, no dependency changes, don't edit `PLAN.md`.
- Don't modify DancingGoat database / CI repository – caller handles running-app validation.
- Stop and report unresolved platform questions instead of guessing.
- Report changed files, commands + results, [verify] findings, limitations.

---

# Follow-ups (as built, Slice 6 – committed `9ba1527`)

- Single activity type; filters directly below it. Per-type detail filters via custom visibility condition
  `ActivityDetailFilterVisibilityCondition` (built-in visibility attributes combine with AND): URL (pagevisit, landingpage,
  bizformsubmit), Title (bizformsubmit, memberregistration), Value (all others incl. emailclick). Referrer never searched.
- Each filter = `TextMatchOperator` (Contains, DoesNotContain, Equal, NotEqual, StartsWith, EndsWith) + text; only
  DataEngine parameterized `Where*` methods; negated operators include NULL. Hidden/empty filters ignored.
- `ActivityURL` = absolute URL as browser had it (page visit: `window.location.href` incl. query + fragment; form
  submission: HTTP Referer). → home page / path matching unreliable with text operators.

# Slice 7 – reliable page/URL matching (2026-10-10)

## Discussion record

- **Regex rejected as primary approach.** SQL Server 2022 adds nothing useful (no regex). `REGEXP_LIKE` exists in SQL Server
  2025, Azure SQL DB/MI, Fabric, but only at **compatibility level ≥ 170**. A recent Xperience SaaS backup (`.bacpac`,
  `model.xml` only) showed Azure SQL DB at **compat level 160**, collation `SQL_Latin1_General_CP1_CI_AS`. Xperience requires
  SQL Server 2019+. Also: no DataEngine regex API, marketers would write regex. Possible later: optional SQL regex when
  compat ≥ 170.
- **Matching in .NET is the portable approach** for URL parts (path, host, query parameter, wildcards) with normalization
  (case, trailing slash, default port, decoding, ignore query/fragment).
- **Custom URL-match UI form component is feasible.** User confirmed: form components used in step properties can store
  their value as a JSON string. Value would be a versioned JSON rule; invalid JSON → No + 41060. "Test a URL" preview should
  call the server to share one matcher. Client build pipeline exists (`TimeInputComponent`).
- **SaaS `OM_Activity` indexes** (schema only): `ActivityContactID`, `ActivityCreated`, `ActivityChannelID`,
  `ActivityItemDetailID`, `ActivityLanguageID`, composite starting with `ActivityType` (incl. `ActivityItemID`,
  `ActivityWebPageItemGUID`, `ActivityUTMSource`, `ActivityUTMContent`). Per-contact queries seek by contact.
- **User corrections:** `ActivityUTMSource`/`ActivityUTMContent` are **not populated** by Xperience out of the box (needs
  custom code – see [Simple Stats UTM capture](https://github.com/Kentico/xperience-by-kentico-simple-stats/blob/main/docs/Usage-Guide.md#utm-capture-optional));
  no other UTM columns exist → **no UTM filters**. **Not all activities populate `ActivityWebPageItemGUID` /
  `ActivityLanguageID`** → page/language filters only for types that do (verify).
- **Performance concern (user):** automations processing many contacts. Cost is per contact (queue-processed), risk is
  contacts with long histories when loading URL rows. Agreed mitigations for .NET URL matching:
  1. `SELECT ActivityURL, COUNT(*) … GROUP BY ActivityURL` (**DISTINCT** – user suggestion; many activities share pages) →
     match distinct URLs in .NET, sum counts. Rows bounded by distinct pages, not activity count.
  2. SQL `LIKE` prefilter from the rule's literal part (parameterized, escaped).
  3. Project only needed columns.
  4. Early stop where the result is decided (e.g. `≥ N` reached).
  5. Hard cap on distinct URLs scanned; hit → warning + No (fail closed).
  6. Prefer indexed columns (page GUID) over parsing.
  7. Keep 365-day max window.
- **Order agreed:** (A) page selector filter; (B) benchmark; (C) .NET URL-part matching (+ custom component) only after
  benchmark numbers are acceptable.

## 7A – Page filter (implement now)

Goal: reliable "visited page X" frequency for content-tree pages, independent of URL/query/fragment/slug changes.

- **[verify first]** which activity types populate `ActivityWebPageItemGUID` and `ActivityLanguageID` in 31.9.1 (DLL
  inspection of the loggers: page visit, landing page, form submission, member registration, email click, custom, others).
  Report a table. Show the page filter only for types that populate the GUID; language filter only if those types also
  populate `ActivityLanguageID` reliably.
- **[verify first]** a built-in web page selector form component works in automation step properties in 31.9.1
  (`WebPageSelectorComponent` or equivalent: property type, channel requirements, multi-select, stored value shape). Check
  docs + DLLs. If not usable in step properties, **stop and report** (fallback options for the user: custom component).
- Property `Pages` (selector, multiple allowed = "any of selected pages"), placed with the other filters below Activity
  type, same per-type visibility mechanism (extend `ActivityDetailFilter`/`ActivityDetailFilters` – single source of truth).
  Optional `Language` dropdown (site languages, "Any" default) only if verified useful.
- Runtime: `ActivityWebPageItemGUID IN (@guids)` via DataEngine parameterized `WhereIn`; combined with AND with the other
  filters; still one `COUNT`. Hidden → ignored. Selected page deleted later → no match for it (no error); all selected
  missing is still valid config (count 0) – document. Empty selection = no filter.
- Explanation text: works only for pages in the content tree tracked by Xperience; URL filter remains for other URLs.
- Usage Guide: page filter section, type table update, home page example now "Page = Home" (replaces unreliable URL
  equality advice; keep URL caveat).
- Tests: mapping/visibility for new filter, GUID parsing from stored value, criteria shape (`IN`, parameterized), hidden
  ignored, empty selection, localization keys, property order.
- Same validation/format/boundaries as above (full tests, DancingGoat `-t:Compile`, `dotnet format --no-restore` touched
  files only, `git diff --check`, no commits, no DB changes).

## 7B – Benchmark (later, user approval needed)

Seed synthetic activities in local DancingGoat DB only (e.g. 10k contacts, ~1M activities, some contacts 50k+ rows, many
repeated URLs). Measure per-evaluation median/p95/max for: current `COUNT`, page GUID filter, .NET URL matching via
`GROUP BY ActivityURL` with/without prefilter. Also a large process run to observe automation queue throughput.

## 7C – URL-part matching + custom component (after 7B)

Matcher engine (path/host/query parameter/full URL, operators, wildcards, normalization), JSON rule in custom form
component with server-side test preview, `GROUP BY` + prefilter + cap. Spec to be detailed after 7B.

## Real-data check (2026-10-10)

Recent Xperience SaaS **QA environment** backup (`db.bacpac`; not production – PROD analysis pending) imported locally as `Kentico.SaaS.Analysis` (SqlPackage; local server needed
`contained database authentication = 1`). `OM_Contact` PII columns (names, job title, address, city, ZIP, phones, email,
birthday, notes, company, Salesforce lead ID) and `OM_Activity.ActivityComment` set to NULL. Other tables (members, users,
form data, email) **not scrubbed** – local only, don't share. Scripts in session scratchpad.

- 2,590 activities (2022-04 → 2026-10), 255,740 contacts (most without activities). Compat level 160.
- Column fill per type (confirms DLL findings in 7A):

  | Type | Total | Page GUID | Language | URL | Title | Value | Referrer |
  | --- | --- | --- | --- | --- | --- | --- | --- |
  | pagevisit | 1962 | 1392 (71%) | 1392 | 1962 | 1962 | 0 | 1520 |
  | landingpage | 588 | 397 (68%) | 397 | 588 | 588 | 0 | 223 |
  | bizformsubmit | 24 | 0 | 0 | 24 | 24 | 0 | 0 |
  | emailclick | 14 | 0 | 0 | 0 | 14 | 14 | 0 |
  | memberregistration | 2 | 0 | 0 | 0 | 2 | 0 | 0 |

- **~30% of page visits/landing pages have no page GUID** → page filter alone isn't enough; URL matching (7C) still needed.
- **Page visits/landing pages have `ActivityTitle` (page title)** and email clicks have a title → candidate: show Title filter for
  pagevisit, landingpage, emailclick too (currently bizformsubmit, memberregistration only). Decision for user.
- URLs: all absolute `https://`, 4 hosts; **19% of page visits have a query string**; no `#fragment` stored in this data.
- Home-page-like page: 172 visits, **23 URL variants, 78 (45%) with query string** → URL equality misses almost half; page filter
  counts all.
- Per contact, page visits: p50 1, p95 3, p99 26, max 1,185 (all time); last 365 days p99 12, max 186.
  Heaviest contact: 1,375 activities → **113 distinct URLs**, 37 pages → `GROUP BY ActivityURL` cuts rows ~12×.
- Timing (heaviest contact, warm, 20 runs, local Docker SQL): `COUNT` type+window 2.1 ms median; + URL `LIKE` 1.8 ms;
  + page GUID `OR` 1.8 ms; `GROUP BY ActivityURL` (all time, 113 rows) 13 ms. Real data is small; 7B synthetic benchmark
  still needed for large-tenant behavior.

## Real-data check – PRODUCTION (2026-10-10)

Recent SaaS **production** backup imported locally as `Kentico.SaaS.Prod.Analysis` (backup file deleted after import).
Scrubbed: `OM_Contact` PII columns, `ActivityComment`, member registration `ActivityTitle` (holds name/email). Other tables
(members, users, form data, email) **not scrubbed** – local only. Aggregates only below.

- 298,056 activities (2023-01 → 2026-10), 895,694 contacts. Compat level 160.

  | Type | Total | Page GUID | Language | URL | Title | Value | Referrer | Channel |
  | --- | --- | --- | --- | --- | --- | --- | --- | --- |
  | pagevisit | 199,361 | 90% | 90% | ~100% | 100% | 0 | 56% | ~100% |
  | landingpage | 95,507 | 91% | 91% | ~100% | 100% | 0 | 23% | ~100% |
  | bizformsubmit | 1,632 | 0 | 0 | 100% | 100% | 0 | 0 | ~100% |
  | emailclick | 1,297 | 0 | 0 | 0 | 100% | 100% | 0 | 100% |
  | memberregistration | 259 | 0 | 0 | 0 | (scrubbed) | 0 | 0 | 0 |

- One custom type defined (`Chat`), no custom activities logged.
- Titles: 100% template `<prefix> '<name>'` for all types (confirms quote-anchored Title filter).
- URLs: 20 hosts; query string 15% page visits / 19% landing pages; **fragments present** (1.5% / 1.9%); ~0.2% not absolute.
- URL variants: top page 28,074 visits / **1,591 URL variants**; another 12,529 visits / **2,887 variants, 95% with query
  string** → URL equality unusable; page filter reliable for the ~90% with a GUID.
- Per contact page visits: p50 1, p95 3, p99 20, max 22,797 (all time); 365 days p99 16, max 7,309; 30 days p99 14, max 447.
  Heaviest contact 26,868 activities → 1,828 distinct URLs, 831 pages (likely bot/internal). `GROUP BY ActivityURL` cuts ~15×.
- Timing (local Docker SQL, warm, median of 20): typical heavy-ish contact `COUNT` 4.6 ms. Heaviest contact, 365 days: `COUNT`
  70 ms, + URL `LIKE` 18 ms, + page GUID 63 ms, `GROUP BY ActivityURL` 54 ms (764 rows), raw URL rows 52 ms (7,309 rows, max
  598 ms). All far below the 2-min step timeout; .NET URL matching with `GROUP BY` is viable without a cap for this site.
- QA-environment figures above are superseded by these.

## 7C – URL match component (spec, 2026-10-10)

Supersedes the 7C stub above. 7B synthetic benchmark deferred: PROD timings (see "Real-data check – PRODUCTION") show
`GROUP BY ActivityURL` + .NET matching is fast enough (heaviest contact, 365 days: 764 distinct URLs, 54 ms).

### Goal (user)

Let marketers pinpoint the exact URL part to match (host, path, query parameter) without regex, consistently across URL
variants (query strings, fragments, trailing slashes, case). Covers what the page picker can't: pages without a page GUID
(~10% of page visits), form submissions (URL only), path groups (`/blog/*`), query parameters (`utm_campaign`), hosts (20 in
PROD).

### Decisions (caller defaults – report if any proves wrong)

- **Replaces** the Page URL operator + text filter (feature unreleased). Same visibility: pagevisit, landingpage,
  bizformsubmit. Also used for **emailclick** against `ActivityValue` (the clicked link URL) instead of the Value text filter.
  Column per type comes from `ActivityDetailFilters` (single source of truth).
- **Rules:** list of up to 5 rules, **all must match** (AND). Empty list = no filter.
- **Rule model** (persisted JSON string, versioned, enum names never renamed):
  `{ "version": 1, "rules": [ { "part": "Path", "operator": "StartsWith", "value": "/blog/", "parameter": null } ] }`
  - `part`: `Url` (full, normalized), `Host`, `Path`, `QueryParameter` (needs `parameter` name).
  - `operator`: `Equal`, `NotEqual`, `Contains`, `DoesNotContain`, `StartsWith`, `EndsWith`, `Matches` (wildcard `*` = any
    characters incl. `/`), plus `Exists` / `DoesNotExist` for `QueryParameter` (no value).
- **Normalization** (shared engine, applied to stored URL and marketer value):
  - Case-insensitive everywhere (matches DB collation behavior marketers already see).
  - Fragment always ignored. Default ports dropped. Host without leading `www.`? **No** – keep host as is (marketers can use
    `EndsWith`/wildcard).
  - Path: percent-decoded, trailing slash ignored except root `/`, empty path = `/`. Query excluded from `Path`.
  - Query parameters: names case-insensitive, values percent-decoded; repeated parameter matches if any value matches.
  - `Url` part = normalized `scheme://host/path?query` (fragment removed, query kept as stored order).
  - Relative/malformed URLs (~0.2%): parse relative values as path (+ query) with no host; unparseable → never matches
    positive rules, matches negated rules (consistent with NULL handling).
- **Execution:** SQL narrows rows (contact, type, window, other filters) and, when a positive rule has a literal fragment,
  adds a parameterized `LIKE` prefilter on the column (escaped; skip for negated rules). Then
  `SELECT <column>, COUNT(*) … GROUP BY <column>` (DataEngine query, columns only), match distinct values in .NET, sum
  counts, compare with operator/count. Cap: 10,000 distinct values → warning (new event id) + **No** (fail closed).
  Wildcard → regex from escaped literal with `RegexOptions.IgnoreCase | CultureInvariant` and a match timeout (e.g. 100 ms);
  timeout → no match + warning once/hour.
- **Validation:** invalid JSON / unknown version / unknown enum / missing parameter name / > 5 rules / value required but
  empty → invalid config (41060, No). Value length limit (e.g. 400 chars).
- **Custom form component** (React, in `Client/src/form-components`, follow `TimeInputComponent` registration/build):
  - Value = JSON string property on `ActivityFrequencyProperties` (user confirmed form components can store JSON strings).
  - UI per rule: Part dropdown → Parameter name input (only for QueryParameter) → Operator dropdown (operators valid for the
    part) → Value input (hidden for Exists/DoesNotExist) → remove button. "Add rule" (max 5). Admin design-system components,
    styles consistent with time picker.
  - **Test a URL**: text input + "Test" button calling a **server-side form component command** (same .NET engine) that
    returns overall match, per-rule result, and the normalized parts (host, path, query parameters) so marketers see what is
    compared. No client-side duplicate matcher.
  - Read-only/disabled modes; validation messages localized; all texts in resx (`labs.automationextensions.urlmatch.*`).
  - **[verify]** form component command API in 31.9.1 (docs + DLLs) and how custom components in automation step dialogs
    receive/return values; stop and report if commands aren't supported in the automation dialog.
- **Engine reusable:** public `UrlMatchRule`/`UrlMatchRuleSet` model + internal matcher, independent of activities (future
  #4 Content engagement trigger).
- **Docs:** Usage Guide section with examples: home page by path `/`, `/pricing` across hosts and query strings, `/blog/*`,
  `utm_campaign = spring`, form submitted on a page with path starting `/contact`, email click on links to `/pricing`;
  normalization rules; when to prefer Pages vs URL rules; performance note.

### Tests

- Engine: every part × operator, normalization (case, trailing slash, root, percent-encoding, default ports, fragments,
  repeated parameters, missing parameter, relative/malformed URLs), wildcards incl. escaping of regex characters, timeout.
- Rule-set JSON: round-trip, version handling, all invalid cases.
- Counting path: grouped counts summed, prefilter only for positive literal rules, cap behavior, negated rules, NULL column.
- SQL shape: parameterized prefilter, grouped column projection; no marketer text in SQL.
- Form component: command returns expected payload; server-side model/attribute wiring; property order/visibility.
- Client: typecheck + production build (`npm run build` or existing script); no new npm packages unless already used.
- Full NUnit suite, DancingGoat `-t:Compile`, `dotnet format --no-restore` (touched files), `git diff --check`.

### Boundaries

No commits, no NuGet changes, don't edit PLAN.md/STATUS.md, no DancingGoat DB changes. Read-only aggregate queries against
local `Kentico.SaaS.Prod.Analysis` are allowed only if needed to validate normalization assumptions – never output URLs or
personal data, counts only.

### 7C as built (2026-10-10, unit-validated, uncommitted)

- `UrlRules` JSON property + `UrlMatchComponent` (React `UrlMatchFormComponent`) replaces Page URL operator/text and the
  email click Value filter. Engine in `UrlMatching/` (public `UrlMatchRule`/`UrlMatchRuleSet`, internal matcher).
- Form component commands verified for automation step dialogs (31.9.1: `AutomationProcessBuilder.ComponentCommand` →
  `IFormComponentCommandInvoker`); `TestUrl` command returns match, per-rule results, normalized parts.
- Grouped query `col, COUNT(*) GROUP BY col TOP 10001`; > 10,000 distinct → No + 41062 `ActivityUrlLimitExceeded`;
  wildcard timeout 100 ms → 41063 `UrlMatchTimeout`. Prefilter: parameterized contains on longest ≥ 3-char plain run of
  positive rules.
- Extra normalization: `+` = space in query values; leading `/` added to path values; trailing slash kept for StartsWith.
- **Prefilter assumption checked on PROD data (caller):** percent-encoded letters/digits in only 6 of ~295k page/landing
  URLs; 8% of URLs contain other `%` encodings, 15% contain uppercase (collation case-insensitive). Email click values:
  1,199 of 1,297 absolute http(s); the rest won't parse (only negated rules match them).
- 727 unit tests; client typecheck/build OK (bundle 270 KiB, warning pre-existed at ~261 KiB); DancingGoat compile OK.
- Not verified: real grouped SQL against a DB, the dialog UI and command round-trip in the running app.
