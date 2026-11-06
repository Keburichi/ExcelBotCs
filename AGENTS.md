# AGENTS

Notes for agents working in this repo. Keep this file short and specific to ExcelBotCs; it is read at the
start of every session.

## Agent skills

### Issue tracker

Issues and specs live as GitHub issues in `Keburichi/ExcelBotCs` (the fork — upstream `CarimA/ExcelBotCs` is read-only). See `docs/agents/issue-tracker.md`.

### Triage labels

Five canonical triage roles, labels named after them. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: `GLOSSARY.md` and `docs/adr/` at the repo root, created lazily. See `docs/agents/domain.md`.

## Planning for parallel worktrees

Work is moving to one story per git worktree, several sessions at a time. The plan and its progress live in
**issue #78** (the map, labelled `wayfinder:map`); its open sub-issues are the tickets, and native issue
dependencies mark what is blocked. Before touching the dev environment, ports, MongoDB configuration, the
Discord bot, or `AGENTS.md`/build scripts, read that map and the two evidence comments on it.

Until #91 lands, this file does not yet document the per-worktree run commands, the working test command,
or the rules for what must not run in a worktree. Do not improvise them from the map alone: resolve #91 or
ask.

## Known environment traps (until fixed)

- `dotnet test` on this machine silently builds and runs **nothing** (exit 0, no results) with SDK 9.0.123
  and `Microsoft.NET.Test.Sdk` 18.5.1. Run the suites explicitly:
  `dotnet vstest ExcelBotCs.Tests/bin/Debug/net9.0/ExcelBotCs.Tests.dll`.
- Configuration precedence is surprising: `LoadSettings` re-adds the JSON providers *after* the command
  line, so `--Key=Value` overrides are silently discarded while environment variables win.
- Nested, dictionary-shaped config needs the JSON's exact casing in an environment variable
  (`OAuthProviders__Providers__Discord__ClientSecret`); an all-uppercase name creates a second entry and
  breaks lookups.
