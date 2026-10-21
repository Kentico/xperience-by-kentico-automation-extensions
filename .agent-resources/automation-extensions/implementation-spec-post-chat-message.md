# Implementation spec: Post to Teams / Slack (#15)

## Selection

Action #15 from `PLAN.md`. Decision 2026-10-20: real implementation (not sample/abstract only). Use case: notify a sales
channel about a hot lead without an email.

## Dependencies

**No new NuGet packages.** Everything needed is already used by *Send webhook* (#14):

- `IHttpClientFactory` / `AddHttpClient` (already referenced, `AutomationExtensionsServiceCollectionExtensions`)
- `System.Text.Json` (BCL)
- Payloads are plain JSON (Slack incoming webhook, Teams Workflows webhook with an Adaptive Card). No Slack/Teams SDK,
  no Adaptive Cards package – build the card as anonymous objects / small internal records.

If implementation turns out to need a package, **stop and report** – do not add it.

## Identity

- Name `PostChatMessage`; identifier `Labs.AutomationExtensions.PostChatMessage` (via `AutomationComponentCatalog.IDENTIFIER_PREFIX`). Never change after release.
- Kind: Action. Descriptor in `AutomationComponentCatalog` (+ `All`).
- Folder `Actions/PostChatMessage/` in `.Admin`. Follow `Actions/SendWebhook/` structure (sender interface + internal sender, options, options provider, result record).
- Display name key `labs.automationextensions.postchatmessage.displayname` = "Post to Teams / Slack". Description: posts a message about the contact to a Microsoft Teams or Slack channel configured by your developers.
- Icon: pick an existing `Icons.*` constant that fits (message/bubble/bell); verify it exists in the installed version.

## Configuration (appsettings, never in step properties)

New `AutomationExtensionsOptions.ChatChannels` (`ChatChannelOptions`), same shape idea as `Webhooks`:

```json
"AutomationExtensions": {
  "ChatChannels": {
    "AdminBaseUrl": "https://localhost:5001/admin",
    "Channels": {
      "SalesTeams": { "Provider": "Teams", "Url": "<secret>", "DisplayName": "Sales (Teams)", "TimeoutSeconds": 30 },
      "SalesSlack": { "Provider": "Slack", "Url": "<secret>", "DisplayName": "#sales (Slack)" }
    }
  }
}
```

- `Channels`: dictionary, `StringComparer.OrdinalIgnoreCase`, keyed by channel name.
- `ChatChannelEndpointOptions`: `Provider` (enum `ChatProvider { Teams, Slack }`), `Url` (absolute, https), `DisplayName` (falls back to key), `TimeoutSeconds` (default 30, min 1, keep below 2-min step limit).
- `AdminBaseUrl`: optional absolute URL of the admin root, used for the contact link. Empty → no link.
- Webhook URLs are secrets (anyone with the URL can post). Document: user secrets / env vars / key vault, never committed appsettings.
- Teams: target is a **Workflows** webhook ("When a Teams webhook request is received" / "Post to a channel when a webhook request is received"). Office 365 connector incoming webhooks are retired – don't target the legacy MessageCard format. Verify current Microsoft docs (Microsoft Learn MCP or WebFetch) for the payload shape and success status code before coding; record the source URLs in the Usage Guide.
- Slack: incoming webhook (`https://hooks.slack.com/services/...`). Verify payload limits (text length, blocks count) in Slack docs.

## Properties (`PostChatMessageActionProperties : SkippableActionProperties`)

Categories: *General* (existing key) and *Result* (collapsible, existing key) – same as `SendWebhookActionProperties`.

| Property | Type | Component | Default | Notes |
| --- | --- | --- | --- | --- |
| `ChannelName` | string | `DropDownComponent`, `ChatChannelOptionsProvider` | `""` | Required. Options from config (text = DisplayName or key, value = key). Explanation says channels are configured by developers. |
| `Title` | string | `TextInputComponent` | `""` | Optional, max 150. Tokens allowed. |
| `Message` | string | `TextAreaComponent` | `""` | Required, max 2000. Tokens allowed (see below). Plain text; line breaks kept. |
| `IncludeContactDetails` | bool | `CheckBoxComponent` | false | Adds email, first/last name, company as facts/fields. Explanation warns: personal data leaves Xperience. |
| `IncludeAdminLink` | bool | `CheckBoxComponent` | true | Button/link to the contact in the admin. Ignored when `AdminBaseUrl` empty. |
| `RecordResult` | bool | `CheckBoxComponent` | false | Result category, same as Send webhook. |
| `ResultNumber` | string | `DropDownComponent`, `StepResultNumberOptionsProvider`, `VisibleIfTrue(RecordResult)` | default | Same as Send webhook. |

All texts localized in `AutomationExtensionsResources.resx`, `labs.automationextensions.postchatmessage.*`, macro form in attributes. Do not override `StepDisplayName`.

## Message tokens

Reuse the token engine of *Set contact field* (`Actions/SetContactField/ContactFieldValueTemplate` + its token values): `{processName}`, `{triggerIdentifier}`, `{contactId}`, `{contactGuid}`, `{contact.<Field>}`, `{triggerData.<Property>}`, `{now}`, `{today}`. If sharing requires moving it to `Core/` (e.g. `MessageTemplate`), do the move with no behavior change for #17 and keep its tests passing. Unresolved tokens → empty text + one warning log (same as #17). Don't invent a second token syntax.

## Payloads (pure, unit-tested builders)

`ChatMessage` (internal record): Title, Text, Facts (`IReadOnlyList<(string Name, string Value)>`), Link (`Uri?`, label).

- `SlackPayloadBuilder.Build(ChatMessage)` → `{ "text": <fallback plain text>, "blocks": [ header (title, if any), section mrkdwn (text), section fields (facts), actions/button or link (admin link) ] }`. Escape `&`, `<`, `>` in user text per Slack rules. Respect Slack limits (header 150, section text 3000, fields 10) – truncate with "…".
- `TeamsPayloadBuilder.Build(ChatMessage)` → `{ "type": "message", "attachments": [ { "contentType": "application/vnd.microsoft.card.adaptive", "content": { AdaptiveCard 1.4: TextBlock title (bolder, wrap), TextBlock text (wrap), FactSet, Action.OpenUrl } } ] }`. Confirm the exact envelope against current docs.
- Facts when `IncludeContactDetails`: Email, Name, Company (skip empty). Always: Process (process display name). Fact labels localized? **No** – payload labels are plain English constants (messages leave the admin; no culture context). Document.
- Admin link: `AdminBaseUrl` + contact detail route. **Verify the route** of the contact detail page in the installed Kentico.Xperience.Admin DLLs (UI page registrations / slugs) or report it as unverified and leave the route in one constant with a comment. Don't guess silently.

## Runtime behavior (`Execute`)

Same order as `SendWebhookAction`:

1. Disabled → `DisabledComponentLog.Write`, return.
2. `SkipStep` → `SkippableStep.LogSkipped`; record skipped result when `RecordResult` (same as Send webhook), return.
3. Load contact, trigger identifier, trigger data (as #17 does for tokens).
4. Resolve title/message tokens; build `ChatMessage`.
5. `IChatMessageSender.Send(channelName, message, ct)` → `ChatSendResult(Succeeded, Message, Exception?)`:
   - unknown channel / invalid URL → failure "Channel '<name>' not configured in 'AutomationExtensions:ChatChannels:Channels' or has an invalid URL".
   - POST JSON, UTF-8, timeout via linked CTS (copy `WebhookSender`), single attempt, no retry. 2xx = success (Teams Workflows typically 202, Slack 200 `ok`). Non-2xx → failure `HTTP <code>`; Slack error body (short text like `invalid_payload`) appended to the message, max 200 chars.
   - `HttpRequestException` → failure "Request failed"; timeout → failure "Timed out after N s"; real cancellation propagates.
   - Never log the webhook URL (secret).
6. Failure → `LogWarning` (process, channel name, contact ID, message). Success → `LogInformation` (process, channel, contact ID). No PII in logs.
7. `RecordResult` → `StepResultRecorder.Record` under new step result source `StepResultKeys.PostChatMessage` (register with `AddStepResultSource`, display name key `labs.automationextensions.postchatmessage.displayname`), so *Previous step succeeded* (#12) can branch on it.

DI: `IAutomationComponentStatus`, `TimeProvider`, `ILoggerFactory` (logger via `LogCategories.For(AutomationComponentCatalog.PostChatMessage)` – never `ILogger<T>`), `IChatMessageSender`, plus what the token engine needs. Register `AddHttpClient(ChatMessageSender.HTTP_CLIENT_NAME)` and `TryAddSingleton<IChatMessageSender, ChatMessageSender>()`.

Log events: next free block **41120–41129** (Admin uses ≤ 41101, Commerce 41110–41112). E.g. `ChatMessagePosted` 41120, `ChatMessageFailed` 41121, `ChatMessageTokensUnresolved` 41122, `ChatMessageResponse` (debug) 41123. Names must pass existing length tests.

## Documentation

- `docs/Usage-Guide.md`: component section (what/when), properties, config (`ChatChannels`, secrets), how to create a Teams Workflows webhook and a Slack incoming webhook (short steps + official doc links), tokens (link to #17 token section), PII note, admin link, no retries, recording result + *Previous step succeeded* example ("Post to Teams" → if failed, send internal email). Add to component table and enablement example. Do not touch README.
- `examples/DancingGoat/appsettings.Development.json`: enable `PostChatMessage`; add `ChatChannels` with `AdminBaseUrl` and example channels whose `Url` is empty (set via user secrets). Record in `.claude/skills/update-xperience/references/dancinggoat-overlay.md` only if the overlay doc already tracks appsettings changes for this library (check).

## Tests (existing NUnit projects, existing patterns, no new packages)

- Payload builders: Slack and Teams JSON shape (parse with `JsonDocument`), escaping, truncation, empty title/facts omitted, link present/absent.
- Sender: stub `HttpMessageHandler` (as in existing webhook sender tests) – success 200/202, non-2xx with Slack error body, timeout, request exception, unknown channel, invalid URL, cancellation propagates, URL never appears in result message.
- Admin link builder: base URL with/without trailing slash, empty base → null.
- Options provider: display name fallback, ordering.
- Token resolution for title/message (if engine moved: existing #17 tests unchanged and passing).
- Existing coverage tests: catalog vs. registrations, resx keys, skippable properties, log category / event name length, step result keys/sources, `StepRegistryPageTests` counts (actions +1).
- Run `dotnet test tests/Kentico.Xperience.Labs.AutomationExtensions.Admin.Tests --no-restore` and the Commerce test project; report counts.
- `dotnet build examples/DancingGoat/DancingGoat.csproj --no-restore -t:Compile` (the app may be running under Aspire and lock DLLs).
- `dotnet format --no-restore` (per `AGENTS.md`); keep formatter changes only in touched files. `git diff --check`.

## Work boundaries

- No commits, no dependency changes, do not edit `PLAN.md` or `STATUS.md` (caller updates them).
- No real webhook URLs anywhere in the repo.
- No changes to the DancingGoat database / CI repository; caller does running-app validation (real Teams/Slack channel).
- Stop and report unresolved platform questions (admin route, payload shape) instead of guessing.
- Report: changed files, commands + results, verified doc sources, limitations.

## Open for caller (running app)

- Real post to a Teams Workflows webhook and a Slack incoming webhook; card renders; admin link opens the contact.
- Failure path (bad URL) → warning log + recorded failed result → *Previous step succeeded* No branch.

## Addendum (2026-10-21): form submission link

User request after app test: when the process was started by the built-in **Form submission** trigger, also link to the
submission in the admin (example URL: `http://localhost:36259/admin/forms/list/2/submissions/2`).

- Verify against 31.9.3: (a) the members of `FormSubmissionTriggerData` (form ID / class, submission item ID); (b) the UI
  page route of a form submission (forms application → form → submissions → item?). In the example URL it's unclear
  whether the last segment is the submission ID or part of the listing route – resolve by reflecting UI page
  registrations (as done for `CONTACT_OVERVIEW_ROUTE`). If a direct submission page doesn't exist, link to the form's
  submissions listing and say so.
- Generalize the link builder: `ContactAdminLink` → admin link helper with route constants for contact overview and
  form submission (same base URL handling, same tests pattern).
- `ChatMessage.Link` becomes a list of links (label + URI), max 2 here. Slack: text links in one context/section line.
  Teams: one `Action.OpenUrl` per link. Labels: plain English constants ("Open contact", "Open form submission").
- New property `IncludeFormSubmissionLink` (bool, default true, `CheckBoxComponent`, after `IncludeAdminLink`,
  localized label + explanation "Only for processes started by a form submission"). Ignored when `AdminBaseUrl` empty,
  when not started by form submission (`context.TriggeredByFormSubmission` / trigger identifier), or when trigger data is
  missing.
- Tests: route building, link list in both payloads (0/1/2 links), property default, resx keys. Update Usage Guide.
- Follow-up candidate (not now): link for *Order completed* (#6) via a link hook in `.Commerce`.
- Automation process link (user request, 2026-10-21): `AutomationProcessContext.Process` (`AutomationProcessMetadata`)
  exposes only `DisplayName` (no ID/GUID, verified 31.9.3). User decision (option 1): look the process up by display
  name, link only when exactly one automation process matches. Implemented: `IAutomationProcessResolver` /
  `AutomationProcessResolver` (`WorkflowInfo` where `WorkflowType = Automation (3)` and `WorkflowDisplayName` = name,
  then ordinal filter – the admin requires display names but doesn't keep them unique; 0 or >1 → no link + debug
  `ChatMessageProcessLinkSkipped` 41124). Target: process **Contacts** tab, `automation/list/{0}/automation-contacts`
  (`AutomationProcessEditSection` `ObjectId` = WorkflowInfo ID → `AutomationProcessContactsTab`, verified). Property
  `IncludeProcessLink` (default true), label "Open process contacts". Lookup only when the property is on and
  `AdminBaseUrl` is valid. Route constants kept (no `IPageLinkGenerator`: its implementation is internal and needs the
  admin UI tree; constants work in background runs and are unit-testable).
