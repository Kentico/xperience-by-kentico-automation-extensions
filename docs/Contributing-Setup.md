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

Running the sample project requires an Xperience by Kentico database. AI agents can do the whole local setup (SQL Server, database, dependencies, verification) with the `setup-local` skill. To create the database by hand, run from the repository root:

```powershell
./scripts/Initialize-Database.ps1
```

- Server, database (`xperience-by-kentico-automation-extensions`), and SQL login come from `ConnectionStrings` in `examples/DancingGoat/appsettings.json`. The default targets a Docker SQL Server on `localhost,1433` with the `sa` login; for a locally installed SQL Server pass `-ServerName` (and `-Username ""` for Windows authentication).
- The `administrator` account password is `Pass@12345`, or the `XPERIENCE_ADMIN_PASSWORD` environment variable.
- Pass `-LicenseFile <path>` (or set `XPERIENCE_LICENSE_FILE`) to install your Xperience license. Without it, add the license when you first sign in to the administration.
- `-Recreate` drops and recreates an existing database.
- The script runs [`kentico-xperience-dbmanager`](https://docs.kentico.com/documentation/developers-and-admins/installation#create-the-project-database), which writes the connection string and `CMSHashStringSalt` to `examples/DancingGoat/appsettings.json`. The script moves them into DancingGoat user secrets and restores `appsettings.json` (`-SaveTo` changes this). It reuses the salt already in user secrets, or else the one committed in `appsettings.json`.

### Run with Aspire

[Aspire](https://aspire.dev) starts everything together: DancingGoat, the admin client dev server (Proxy mode), and [Mailpit](https://mailpit.axllent.org) for emails. Install the [Aspire CLI](https://aspire.dev/get-started/install-cli/), then from the repository root:

```powershell
aspire run
```

The dashboard links the live site and the administration. By default DancingGoat uses the database from `scripts/Initialize-Database.ps1`. To use a SQL Server container instead (Docker required; the database is created on first run), set `Sql:UseExistingServer` to `false` in `examples/AppHost` user secrets:

```powershell
dotnet user-secrets set "Sql:UseExistingServer" false --project examples/AppHost
```

Set `XPERIENCE_LICENSE_FILE` before `aspire run` to install the license into a new container database.

The root `.mcp.json` registers the Aspire MCP server (`aspire agent mcp`), so AI agents can list resources, read logs, and restart them. `aspire agent init` can also install Aspire's agent skills; note that it adds Aspire telemetry hooks to your global agent settings unless `ASPIRE_CLI_TELEMETRY_OPTOUT=true` is set.

### End-to-end tests

Playwright tests for the live site and the administration (including the automation process **Step registry** tab) live in `tests/e2e`; they need a licensed database. See [`tests/e2e/README.md`](../tests/e2e/README.md).

### Admin Customization

The library project is `src/Kentico.Xperience.Labs.AutomationExtensions.Admin`.
The repository and solutions keep the Automation Extensions name so additional
projects can be added later. Existing C# namespaces are unchanged.

The sample's `appsettings.Development.json` already enables the client proxy (plain HTTP, like DancingGoat in Development):

```json
"CMSAdminClientModuleSettings": {
  "kentico-xperience-labs-automation-extensions-admin": {
    "Mode": "Proxy",
    "Port": 3009
  }
}
```

`aspire run` starts the dev server for you. Without Aspire, run these commands from the repository root:

```powershell
Set-Location src\Kentico.Xperience.Labs.AutomationExtensions.Admin\Client
npm ci
npm start
```

In another terminal, run `dotnet run --project examples\DancingGoat`.
The proxy and webpack must use the same module name and port.

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
`@kentico/xperience-labs-automation-extensions-admin` and its exported component name.
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
