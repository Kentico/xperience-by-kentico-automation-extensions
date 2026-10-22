# Xperience by Kentico Labs: Automation Extensions

[![Kentico Labs](https://img.shields.io/badge/Kentico_Labs-grey?labelColor=orange&logo=data:image/svg+xml;base64,PHN2ZyBjbGFzcz0ic3ZnLWljb24iIHN0eWxlPSJ3aWR0aDogMWVtOyBoZWlnaHQ6IDFlbTt2ZXJ0aWNhbC1hbGlnbjogbWlkZGxlO2ZpbGw6IGN1cnJlbnRDb2xvcjtvdmVyZmxvdzogaGlkZGVuOyIgdmlld0JveD0iMCAwIDEwMjQgMTAyNCIgdmVyc2lvbj0iMS4xIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciPjxwYXRoIGQ9Ik05NTYuMjg4IDgwNC40OEw2NDAgMjc3LjQ0VjY0aDMyYzE3LjYgMCAzMi0xNC40IDMyLTMycy0xNC40LTMyLTMyLTMyaC0zMjBjLTE3LjYgMC0zMiAxNC40LTMyIDMyczE0LjQgMzIgMzIgMzJIMzg0djIxMy40NEw2Ny43MTIgODA0LjQ4Qy00LjczNiA5MjUuMTg0IDUxLjIgMTAyNCAxOTIgMTAyNGg2NDBjMTQwLjggMCAxOTYuNzM2LTk4Ljc1MiAxMjQuMjg4LTIxOS41MnpNMjQxLjAyNCA2NDBMNDQ4IDI5NS4wNFY2NGgxMjh2MjMxLjA0TDc4Mi45NzYgNjQwSDI0MS4wMjR6IiAgLz48L3N2Zz4=)](https://github.com/Kentico/.github/blob/main/SUPPORT.md#labs-limited-support) [![CI: Build and Test](https://github.com/Kentico/xperience-by-kentico-automation-extensions/actions/workflows/ci.yml/badge.svg)](https://github.com/Kentico/xperience-by-kentico-automation-extensions/actions/workflows/ci.yml)

## Description

Extra triggers, conditions, and actions for the Xperience by Kentico **Automation Builder**, plus a *Step registry* admin page that shows which components are installed and enabled.

### Triggers

- **Contact date anniversary** – yearly on (or N days before) a contact date field, such as a birthday.
- **Contact in contact group (daily)** – daily for members of a contact group (nurture series).
- **Contact in recipient list (daily)** – daily for subscribed contacts of a recipient list (newsletter welcome series).
- **Contact inactive for N days** – when a contact's last activity was N days ago (win-back).
- **Order completed** (`.Commerce` package) – when a visitor completes an order, with minimum total, currency, and first-order filters.

### Conditions

- **Activity frequency** – how often a contact performed an activity in the last N days (e.g. 3+ pricing page visits in 7 days).
- **Compare contact number field** – compare an integer contact field, such as a lead score.
- **Previous step succeeded** – branch on the saved result of an earlier step (e.g. a webhook).
- **Random split (A/B)** – stable percentage split per contact.
- **Started by trigger** – branch on the trigger that started the process and its trigger data.
- **Within time window** – business hours / day-of-week check.

### Actions

- **Adjust contact score** – add, subtract, or set an integer contact field (lightweight lead scoring).
- **Post to Teams / Slack** – message about the contact to a Teams (Workflows) or Slack webhook, with links to the contact, form submission, and process.
- **Send webhook** – signed JSON payload to a CRM, CDP, Zapier, Power Automate, …
- **Set contact field (dynamic value)** – set a field from placeholders (`{contact.X}`, `{triggerData.X}`, `{today}`, …) or calculate from its current value.
- **Write to log** – log a message for each contact that reaches the step.

Most steps can be skipped per step, and actions can record their result for *Previous step succeeded*.

### Limitations

- All components are **disabled by default** and are turned on in `appsettings.json`.
- *Contact date anniversary*, *Contact in contact group*, *Contact in recipient list*, and *Contact inactive* need scheduled task configuration.
- *Order completed* needs a call from your checkout code; Xperience doesn't fire it on its own.
- *Send webhook* and *Post to Teams / Slack* send once, without retries. *Post to Teams / Slack* hasn't been tested with a real Teams channel yet.
- The *Post to Teams / Slack* process link is added only when exactly one automation process has the process's display name.
- Trigger names are plain English (not localized) because of a platform issue with localized trigger names.

## Requirements

### Library Version Matrix

| Xperience Version | Library Version |
| ----------------- | --------------- |
| >= 31.9.3         | 1.0.0           |

### Dependencies

- [ASP.NET Core 10.0](https://dotnet.microsoft.com/en-us/download)
- [Xperience by Kentico](https://docs.kentico.com)

## Package Installation

Add the package to your application using the .NET CLI:

```powershell
dotnet add package Kentico.Xperience.Labs.AutomationExtensions.Admin
```

For the *Order completed* trigger, also add the commerce package:

```powershell
dotnet add package Kentico.Xperience.Labs.AutomationExtensions.Commerce
```

The `.Admin` package contains the automation components and the admin UI client module. Client assets are embedded, so your application doesn't need Node.js.

## Quick Start

1. Register the services in `Program.cs` (after `AddKentico`):

   ```csharp
   builder.Services
       .AddAutomationExtensions(builder.Configuration)
       .AddAutomationExtensionsCommerce(); // only with the .Commerce package
   ```

2. Enable the components you want in `appsettings.json`:

   ```json
   {
     "AutomationExtensions": {
       "Components": {
         "RandomSplit": true,
         "WriteToLog": true
       }
     }
   }
   ```

3. Restart the application, open **Automation**, and add the steps to a process. Check **Step registry** to see which components are enabled.

## Full Instructions

View the [Usage Guide](./docs/Usage-Guide.md) for configuration of each component, scheduled tasks, webhooks and chat channels, logging, and extension points.

## Contributing

To see the guidelines for Contributing to Kentico open source software, please see [Kentico's `CONTRIBUTING.md`](https://github.com/Kentico/.github/blob/main/CONTRIBUTING.md) for more information and follow the [Kentico's `CODE_OF_CONDUCT`](https://github.com/Kentico/.github/blob/main/CODE_OF_CONDUCT.md).

Instructions and technical details for contributing to **this** project can be found in [Contributing Setup](./docs/Contributing-Setup.md).

## License

Distributed under the MIT License. See [`LICENSE.md`](./LICENSE.md) for more information.

## Support

[![Kentico Labs](https://img.shields.io/badge/Kentico_Labs-grey?labelColor=orange&logo=data:image/svg+xml;base64,PHN2ZyBjbGFzcz0ic3ZnLWljb24iIHN0eWxlPSJ3aWR0aDogMWVtOyBoZWlnaHQ6IDFlbTt2ZXJ0aWNhbC1hbGlnbjogbWlkZGxlO2ZpbGw6IGN1cnJlbnRDb2xvcjtvdmVyZmxvdzogaGlkZGVuOyIgdmlld0JveD0iMCAwIDEwMjQgMTAyNCIgdmVyc2lvbj0iMS4xIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciPjxwYXRoIGQ9Ik05NTYuMjg4IDgwNC40OEw2NDAgMjc3LjQ0VjY0aDMyYzE3LjYgMCAzMi0xNC40IDMyLTMycy0xNC40LTMyLTMyLTMyaC0zMjBjLTE3LjYgMC0zMiAxNC40LTMyIDMyczE0LjQgMzIgMzIgMzJIMzg0djIxMy40NEw2Ny43MTIgODA0LjQ4Qy00LjczNiA5MjUuMTg0IDUxLjIgMTAyNCAxOTIgMTAyNGg2NDBjMTQwLjggMCAxOTYuNzM2LTk4Ljc1MiAxMjQuMjg4LTIxOS41MnpNMjQxLjAyNCA2NDBMNDQ4IDI5NS4wNFY2NGgxMjh2MjMxLjA0TDc4Mi45NzYgNjQwSDI0MS4wMjR6IiAgLz48L3N2Zz4=)](https://github.com/Kentico/.github/blob/main/SUPPORT.md#labs-limited-support)

This project has **Kentico Labs limited support**.

See [`SUPPORT.md`](https://github.com/Kentico/.github/blob/main/SUPPORT.md#labs-limited-support) for more information.

For any security issues see [`SECURITY.md`](https://github.com/Kentico/.github/blob/main/SECURITY.md).
