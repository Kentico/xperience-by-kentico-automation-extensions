# Implementation spec: Within time window

## Status

**Draft; blocked pending user approval of time-window semantics.**

The caller requested a decision through `ask_user`, but the user was unavailable.
Do not implement or delegate implementation until the rules below are confirmed.
`PLAN.md` remains unchanged because no implementation has been validated.

## Selection

Implement condition **#9 Within time window** from `PLAN.md`.

Of the remaining complete components, this is the simplest to validate without a
running Xperience application: clock-driven branching with a pure calendar helper,
no contact retrieval, database query, scheduled firing, external service, writes,
or shared action-result prerequisite.

- #7 Has consent agreement looks smaller, but the installed 31.9.1
  `CMS.DataProtection.dll` includes `CMSContactHasAgreedWithConsentTranslator`.
  This is evidence of built-in rule overlap, not proof of its availability in
  Automation Builder. Resolve that overlap before adding a duplicate component.
- #11 Started by trigger / trigger data matches includes typed field comparisons,
  operator/value validation, and supported-trigger decisions. Trigger matching
  alone would be a reduced slice, not the entire proposed component.
- #1 Contact in contact group requires audience queries, scheduled task setup,
  dispatch/batching tests, and queue-load validation.
- Other remaining triggers/actions introduce persistence, contact-field discovery,
  consent/commerce mapping, or external integration.

The time-window component has semantic decisions, but its full behavior can be
covered with fixed instants and injected `TimeProvider`, once those are settled.

## Decisions requiring approval

These are **proposals, not approved behavior**:

| Topic | Proposed rule |
| --- | --- |
| Time zone | Default UTC; marketer selects a time zone supported by the host. Persist its ID, not its display label. Invalid/unavailable IDs are explicit errors, never server-local fallback. |
| Default window | All seven weekdays, 00:00 to 00:00, no date limits: a full-day window. |
| Boundaries | Start inclusive, end exclusive. |
| Overnight | Allow end earlier than start. Selected weekday refers to the day the window starts. Monday 22:00 to 02:00 includes Tuesday 01:00. |
| Equal times | A 24-hour window anchored at the selected start time, not an empty interval. |
| Date range | Optional inclusive local start/end dates. Check against the window's start date, including the previous date during an overnight spillover. |
| DST | Convert the current UTC instant to local time. Evaluate wall-clock membership: both occurrences of repeated fall-back times qualify; skipped spring-forward times never occur. No conversion of configured boundaries into fabricated instants. |
| Invalid settings | Throw explicit argument/configuration errors; Xperience routes condition errors through No. Never silently substitute values. |

The approval request described UTC, weekday/start-date anchoring, inclusive/exclusive
boundaries, overnight support, equal-time full days, date limits, and invalid-setting
behavior. Defaults and time-zone persistence should also be confirmed before work.

Existing library gating rules are already established, not new decisions:
administrator disablement returns **Yes (A)** before validation/evaluation and
overrides marketer skipping; enabled/skipped returns the configured branch.
This condition branches once when evaluated. It does not wait or schedule a retry.

## Architecture

- Add `Conditions/WithinTimeWindow/WithinTimeWindowCondition`, properties, and an
  internal pure calendar helper. Follow `RandomSplitCondition` for registration,
  enablement, skip precedence, cancellation, and explicit runtime validation.
- Stable name/configuration key: `WithinTimeWindow`.
  Identifier: `Labs.AutomationExtensions.WithinTimeWindow`.
- Add the descriptor to `AutomationComponentCatalog.All`. The global Step registry,
  component status logger, registration, localization, and log-length tests must
  discover the new component through existing mechanisms.
- Inherit properties from `SkippableConditionProperties`.
- Inject `IAutomationComponentStatus`, `TimeProvider`, and `ILoggerFactory`.
  Reuse the existing `TryAddSingleton(TimeProvider.System)` registration.
  Use `LogCategories.For(AutomationComponentCatalog.WithinTimeWindow)`.
  Reuse `DisabledComponentLog` and `SkippableStep`; no `ILogger<T>`.
- Active evaluation validates saved settings, checks cancellation, reads
  `TimeProvider.GetUtcNow()` exactly once, resolves the selected time zone, and
  calls the same calendar helper exercised by tests.
- No processed contact, process data, static clock, background service, cache, or
  new public abstraction is needed.
- Do not add gated base classes or refactor existing components for this slice.

## Builder properties

Use existing admin UI form components and validation patterns. Inspect installed
31.9.1 APIs before choosing a weekday multi-select or date/time input type; do not
assume a component exists.

- Selected weekdays, with localized labels and at least one required.
- Start and end time, minute precision.
- Required time-zone selector; UTC default under the proposal.
- Optional start/end local dates.
- Existing inherited Step behavior controls.

Use documented typed form controls where available. If string time/date inputs
are necessary, specify invariant formats (`HH:mm`, `yyyy-MM-dd`), validate in the
Builder and runtime, and explain the formats. Do not use culture-sensitive parsing
or encode multiple unrelated settings into one free-form string.

The time-zone dropdown should use `TimeZoneInfo.GetSystemTimeZones()`, ensuring
the persisted UTC value is selectable. Do not enumerate arbitrary identifiers
or invent Windows/IANA mappings. Document deployment portability and reject a
saved ID if the destination host cannot resolve it.

All marketer-facing text goes in `AutomationExtensionsResources.resx` under
`labs.automationextensions.withintimewindow.*`, using established resource-key
and macro conventions. Help must explain overnight weekday/date anchoring,
boundary behavior, disabled Yes routing, and that this is not a Wait step.

## Calendar algorithm under the proposal

1. Validate settings independently of the clock: selected weekdays, time ranges,
   date formats/order, and resolvable time zone.
2. Convert the supplied UTC instant with `TimeZoneInfo.ConvertTime`.
3. Determine the local window start date:
   - Same-day window: the current local date, if start <= local time < end.
   - Overnight window: current local date when local time >= start; previous
     local date when local time < end; otherwise outside.
   - Equal start/end: current local date when local time >= start; previous local
     date otherwise.
4. Check that this start date's weekday is selected and it falls within any
   inclusive date limits.
5. Return the result; do not mutate any state.

Use `DateOnly`/`TimeOnly` internally. Tests must cover extreme dates if calculating
a previous date could underflow. Surface invalid/unrepresentable values explicitly,
not via a success-shaped fallback.

## Tests and validation

Use the existing NUnit project and patterns, no new dependencies.

- Same-day inside/outside, exact start/end, and minute boundaries.
- Selected/unselected weekdays and all-week defaults.
- Overnight entry/spillover/end and start-day weekday anchoring.
- Equal-time windows before/at/after the anchor and prior-day anchoring.
- Optional single-sided dates; inclusive endpoints; overnight spillover across
  a date limit; month/year changes and leap day.
- UTC and a non-UTC time zone: the same instant may match different local dates.
- DST gaps and repeated hours using a deterministic custom test time zone rather
  than relying only on host time-zone IDs.
- Invalid weekdays/time values, blank/unknown zones, invalid dates, reversed
  date ranges, and cancellation. Provider failures propagate without broad catches.
- Disabled with skip off/Yes/No and invalid settings: always Yes without reading
  the clock or resolving a time zone.
- Enabled skip Yes/No with invalid settings: selected branch without evaluation.
- Test the active production `Evaluate` path with a fake `TimeProvider`; no
  duplicate calendar algorithm in the condition or tests. Assert one clock read.
- Verify dropdown option identities, UTC availability, saved-value validation,
  registration/catalog consistency, skippable properties, and resource coverage.
- Run the full existing suite:
  `dotnet test .\tests\Kentico.Xperience.Labs.AutomationExtensions.Admin.Tests\Kentico.Xperience.Labs.AutomationExtensions.Admin.Tests.csproj --no-restore`.
- Run existing scoped formatting verification on changed C# files and
  `git diff --check`.
- Build the library and DancingGoat with existing dependencies. If the running
  app locks output DLLs, report it and use `--no-restore -t:Compile` for compile
  evidence; do not claim that the full consumer build passed.

## Documentation and boundaries

Update `docs/Usage-Guide.md`: configuration opt-in, component table, properties,
time-zone portability, approved boundary/DST/date semantics, disabled/skip
precedence, and a business-hours example with an explicit Wait/retry path.
Explain that current time is evaluated when this step runs; a previous Yes does
not guarantee that a later action still runs inside the window.

No dependency changes, unrelated fixes, README replacement, example process
changes, database changes, application restart, or commits.
Preserve any existing worktree changes.

After approval, delegate implementation with this spec and existing reference
files. The implementer must stop and report any further unresolved product/API
decision. Deliver changed files, exact commands/results, test counts, and limits.
The caller reviews and validates the result, then updates `PLAN.md`; the
implementer must not edit the plan.

## Research references

- `PLAN.md`, sections 3.2 (#9), 5.1, 5.5, and completed slices in section 7.
- Existing `RandomSplitCondition` / properties and its gating tests.
- `AutomationExtensionsServiceCollectionExtensions`: injected `TimeProvider`
  registration already exists.
- Installed 31.9.1 DLL inspection via the `dotnet-inspect` skill:
  `IConsentAgreementService.IsAgreed(ContactInfo, ConsentInfo)` and the
  `CMSContactHasAgreedWithConsentTranslator` type.
- Official [Automation documentation](https://docs.kentico.com/documentation/business-users/digital-marketing/automation)
  and [Consents API examples](https://docs.kentico.com/api/configuration/consents).
