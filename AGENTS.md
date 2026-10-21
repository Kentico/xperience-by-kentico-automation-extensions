# Agent instructions

## Development

- Before a commit, format with: `dotnet format --no-restore`
- `examples/` is excluded from formatting by `examples/.editorconfig` (`generated_code = true`)
- The Dancing Goat connection string and management API secret can be committed to source control because they are for local development only.
- Package versions live only in `Directory.Packages.props`; lock files are committed, so restore with `--force-evaluate` after changing versions or project references.

## Planning docs (`.agent-resources/<feature>/`)

Each feature gets its own folder, `.agent-resources/<feature>/` (kebab-case name). Write all agent plans, research, and implementation specs for that feature there, not elsewhere in the repo. Before working on an existing feature, read its `STATUS.md`.

- `STATUS.md` – single source for the feature's current state (components, open checks, pending decisions). Update after each slice or when an item closes.
- `PLAN.md` – the feature's research, design rules, component catalog, decisions, append-only slice log. Don't track status there.
- `implementation-spec-<n>.md` – scope of slice `<n>` of the feature.

Features:

- `automation-extensions` – the automation triggers, conditions, and actions library (`.Admin`, `.Commerce`). Its specs are named by component (`implementation-spec-<component>.md`) rather than by slice number.
