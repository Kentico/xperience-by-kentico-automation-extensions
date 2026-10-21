# Implementation spec: Random split (A/B)

## Selection

Implement condition #10 from `PLAN.md`. It is the lowest-complexity remaining component: no scheduled task, contact query, external service, consent decision, commerce mapping, or shared process result dependency. Its deterministic calculation can be validated without a running application.

Other small candidates require more decisions: #9 needs time-zone/date-range semantics; #11 combines trigger selection with typed field comparisons; #16 mutates contact fields and needs field discovery and validation.

## Decision

The public `AutomationProcessContext.Process` metadata exposes only `DisplayName`, not a stable process or step identifier (verified against the installed `CMS.Automation.dll`, version 31.9.1).

Use **contact GUID + split key** as the stable identity. Do not hash the process display name: renaming a process must not move contacts between branches. The split key is the plan's salt, with a marketer-friendly name and default `default`. Same contact/key means the same bucket across processes; different keys allow independent splits. There is no automatic per-step identity.

User confirmed this behavior after clarification: hash maps to a bucket, then the percentage selects the Yes (A) or No (B) branch.

## Architecture and behavior

- Add `Conditions/RandomSplit/RandomSplitCondition`, properties, and a small internal pure bucket helper, following `PreviousStepSucceededCondition` and existing testable helpers.
- Stable registration/configuration name: `RandomSplit`; identifier: `Labs.AutomationExtensions.RandomSplit`.
- Register through the existing assembly attribute pattern and add the descriptor to `AutomationComponentCatalog.All`. Existing catalog, configuration, startup logging, registration, localization, and skip tests must continue to cover it.
- Inherit properties from `SkippableConditionProperties`. All disabled library conditions return true (Yes/A) without evaluation or applying marketer skip settings, and log this branch through `DisabledComponentLog`; enabled/skipped returns the selected branch and logs through `SkippableStep`; only active evaluation reads the contact.
- Inject `IAutomationComponentStatus` and `ILoggerFactory`. Use `LogCategories.For(AutomationComponentCatalog.RandomSplit)`, not `ILogger<T>`.
- Fetch the processed contact with the existing context API and propagate cancellation. No contact writes, process data, random state, clock dependency, or additional DI services.
- Public properties: integer percentage (default 50, inclusive range 0..100) and required split key (default `default`). Use existing admin form components/validation, localized labels, explanations, and validation messages. Explain that the percentage is an approximate population allocation, not an exact count.
- Validate saved/runtime properties as well as Builder input. Invalid percentages, blank keys, or an empty contact GUID must throw explicit argument exceptions (no silent false fallback); Xperience handles condition errors through its false branch. Disabled/skipped checks still take precedence.

## Stable algorithm

- Canonical input: contact GUID in lowercase `D` format, followed by `:` and the split key exactly as entered; encode as UTF-8.
- Use SHA-256, not `GetHashCode`, `Random`, or platform-dependent byte interpretation.
- Read the first four digest bytes as an unsigned big-endian integer and take modulo 100 to produce a bucket in 0..99.
- Follow the Yes branch when `bucket < percentage`.
- 0 means always No; 100 means always Yes. Increasing the percentage never moves a Yes contact to No.
- Key matching is case-sensitive; whitespace is significant for nonblank keys. Document this. Reject all-whitespace keys.
- Keep this algorithm stable after release; changes require a new versioned component identifier.

## Localization and documentation

- All marketer-facing text in the existing `AutomationExtensionsResources.resx`, using `labs.automationextensions.randomsplit.*` keys and established attribute macro conventions.
- Builder description and percentage help must explain disabled Yes (A) behavior and its precedence over skip settings. Shared condition skip help distinguishes administrator disablement from marketer skipping. These are static rules, not a live configuration indicator.
- Update `docs/Usage-Guide.md`: enablement example, component table, properties, disabled/skipped behavior, stable identity rules, percentage boundaries, approximate allocation, and a simple A/B composition using built-in Send email.
- Explicitly state no additional setup/dependencies; no exact 50/50 guarantee, no new assignment each run, and changing the key or percentage can change branch outcomes.
- Do not modify the template README or unrelated example automation processes.

## Validation and acceptance

Use existing NUnit patterns and the existing test project; no new packages.

- Fixed GUID/key vectors assert exact buckets calculated independently of the production helper, including a non-ASCII key to lock down UTF-8 handling.
- Repeated evaluation gives identical results; a documented fixed set of different keys demonstrates independent bucket inputs (do not assume every pair has different buckets).
- Assert 0/100 boundaries, exact threshold behavior (`bucket` vs. `bucket + 1`), buckets in 0..99, and monotonic percentage behavior.
- Test out-of-range percentages, blank/null keys, and empty GUID rejection.
- Verify a deterministic population of at least 10,000 unique GUIDs allocates between 45% and 55% to Yes at percentage 50. This is deterministic coverage, not a randomized/flaky test.
- Cover production condition gating (disabled, enabled/skipped Yes and No), cancellation propagation, and the active evaluation path as far as the installed context API permits. Do not duplicate the calculation in the condition or tests to substitute for production-path coverage. Report any context-testing limitation plainly.
- Verify registration/catalog, localization coverage, skippable properties, and log-length guards with the full existing unit suite.
- Run `dotnet test .\tests\Kentico.Xperience.Labs.AutomationExtensions.Admin.Tests\Kentico.Xperience.Labs.AutomationExtensions.Admin.Tests.csproj --no-restore` and report counts.
- Run the existing formatting verification scoped to changed library/test files if supported by repository tooling.
- Build the DancingGoat consumer with existing dependencies to catch integration compile errors; do not change dependency manifests.

## Work boundaries

Preserve all existing worktree changes, including the anniversary offset helper/tests/spec and `PLAN.md`. Do not edit `PLAN.md`; the caller updates it after validation.

No commits. No dependency changes. Stop and report any unresolved implementation decision rather than inventing undocumented platform behavior.

Deliver the implementation, tests, and Usage Guide changes; report exact changed files, commands/results, and limitations. Running-app Builder verification is not required for this slice and must not be claimed.
