# Agent instructions

## Formatting

- Format with: `dotnet format Kentico.Xperience.Labs.AutomationExtensions.slnx --no-restore --exclude ./examples/**`
- Never run a bare `dotnet format` on the solution.
- `examples/` is excluded by `examples/.editorconfig` (`generated_code = true`); don't reformat it.
- Don't commit formatter-only changes to files unrelated to the task.
