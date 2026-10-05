# Contributing Setup

## Required Software

The requirements to setup, develop, and build this project are listed below.

### .NET Runtime

.NET SDK 10.0 or newer

- <https://dotnet.microsoft.com/en-us/download/dotnet/10.0>
- See `global.json` file for specific SDK requirements

### Node.js Runtime

- [Node.js](https://nodejs.org/en/download) 24 or newer
- [NVM for Windows](https://github.com/coreybutler/nvm-windows) or [NVM for macOS](https://github.com/nvm-sh/nvm) to manage multiple installed versions of Node.js
- See `engines` in `src/Kentico.Xperience.Labs.AutomationExtensions.Admin/Client/package.json` for specific version requirements

### C# Editor

- VS Code/VS
- Cursor
- Rider

### Database

SQL Server 2019 or newer compatible database

- [SQL Server Linux](https://learn.microsoft.com/en-us/sql/linux/install-upgrade/setup?view=sql-server-ver15)

### SQL Editor

- VS Code with official [MSSQL extension](https://marketplace.visualstudio.com/items?itemName=ms-mssql.mssql)
- MS SQL Server Management Studio

## Sample Project

### Database Setup

Running the sample project requires creating a new Xperience by Kentico database using the included template.

Change directory in your console to `./examples/DancingGoat` and follow the instructions in the Xperience
documentation on [creating a new database](https://docs.kentico.com/documentation/developers-and-admins/installation#create-the-project-database).

### Admin Customization

The library project is `src/Kentico.Xperience.Labs.AutomationExtensions.Admin`.
The repository and solutions keep the Automation Extensions name so additional
projects can be added later. Existing C# namespaces are unchanged.

The sample's `appsettings.Development.json` already enables the HTTPS client proxy:

```json
"CMSAdminClientModuleSettings": {
  "kentico-xperience-admin-labs-automation-extensions": {
    "Mode": "Proxy",
    "Port": 3009,
    "UseSSL": true
  }
}
```

Run these commands from the repository root:

```powershell
Set-Location src\Kentico.Xperience.Labs.AutomationExtensions.Admin\Client
npm ci
npm start
```

In another terminal, run `dotnet run --project examples\DancingGoat`.
The client dev server exports a .NET HTTPS development certificate automatically;
trust it with `dotnet dev-certs https --trust` if needed. The proxy and webpack
must use the same module name and port.

For development without the proxy, override the module's `Mode` to `Embedded`
in user secrets and run `npm run build:dev` before building .NET in Debug.
Debug builds embed existing `Client/dist` assets but do not run npm.
Release builds run `npm ci` and `npm run build` automatically and embed the output.
Run `npm run typecheck` to check TypeScript.

### Adding client extensions

`Client/src/form-components/TimeInputFormComponent.tsx` adapts the admin design
reference's React Aria time field and dropdown. It preserves `HH:mm` values and
uses scoped CSS with admin design tokens.
Add React page templates or form components there and export them from `entry.tsx`.
Register the corresponding C# extension using client module
`@kentico/xperience-admin-labs-automation-extensions` and its exported component name.
`AutomationExtensionsAdminModule` registers the module; webpack and the project's
`AdminClientPath` package its assets under the same organization and project name.

## Development Workflow

1. Create a new branch with one of the following prefixes
   - `feat/` - for new functionality
   - `refactor/` - for restructuring of existing features
   - `fix/` - for bugfixes

1. Run `dotnet format Kentico.Xperience.Labs.AutomationExtensions.slnx --no-restore --exclude ./examples/**`

   > use `dotnet: format` VS Code task.

1. Commit changes, with a commit message preferably following the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/#summary) convention.

1. Once ready, create a PR on GitHub. The PR will need to have all comments resolved and all tests passing before it will be merged.
   - The PR should have a helpful description of the scope of changes being contributed.
   - Include screenshots or video to reflect UX or UI updates
   - Indicate if new settings need to be applied when the changes are merged - locally or in other environments
   - Use the `populate-pr-template` agent skill (`.claude/skills/populate-pr-template/SKILL.md`, works with
     both Copilot and Claude) to fill in `.github/PULL_REQUEST_TEMPLATE.md` from git diff and CI evidence
     instead of writing the description by hand.

1. This repository uses `lf` line endings for text files. EditorConfig and Git enforce this on all platforms; no Git line-ending override is needed.
