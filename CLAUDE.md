# Working in this repository

A typed .NET client for the BMC Control-M Automation API and a simulator of that API. `docs/requirements.md` says what is being built. This repository is public: write everything as if anyone will read it.

## Commands

```sh
sh eng/verify.sh     # the gate: run before every push; CI runs exactly this
dotnet build
dotnet test
dotnet format        # fix formatting; verify.sh fails on unformatted code
```

`eng/verify.sh` must pass before a push. CI is a backstop, not the gate.

## Worktrees

Every change happens in a git worktree of its own, on its own branch. The default checkout is shared by the human and by every session, so treat it as read-only: no checkout, pull, merge, commit or edit there. Create the worktree under `.claude/worktrees/`, which is ignored (`git worktree add .claude/worktrees/<name> -b <branch> origin/main`), run every build, test and git command inside it, and remove it once its branch is merged. To move a branch without a checkout, push to it (`git push origin <commit>:refs/heads/<branch>`) or fetch into it (`git fetch origin main:main`).

## Projects

- `ControlM.Client` references nothing else in the solution.
- `ControlM.Simulator` references `ControlM.Client`. Its domain model and state machine must not depend on ASP.NET Core; only the host layer does. It depends on no test framework.
- `ControlM.SpecCheck` is the only project that may reference an OpenAPI reader.
- `ControlM.Simulator.Tool` is a thin `Program`. It adds no behavior.

## Conventions

The rules for writing code here are in `docs/conventions.md`, imported in full below.

@docs/conventions.md

## Not yet in place

- **Zero-tests setting.** `ControlM.SpecCheck.Tests` has no tests yet, so its project file passes `--ignore-exit-code 8` to tolerate a run that finds none. Remove the line in the change that adds its first test.

## Slices

- **A slice is one feature, or one behavior of a feature, taken through every project it applies to**: client, simulator, and the tests in each. It is one branch and one pull request.
- **Done means**: `eng/verify.sh` passes; the feature has its folder and tests in each project it applies to; every implemented operation has its fixture test; and anything decided along the way that the requirements did not settle is recorded in `docs/requirements.md` in the same pull request, with assumed Control-M behavior going in section 4.8.
- **Commit subjects** are `<type>: <summary>` in the imperative, with type one of `feat`, `fix`, `test`, `refactor`, `docs`, `build`, `ci`.
- **When a rule cannot be followed, or two rules conflict, stop and say so.** Do not pick a reading silently.

## Pull requests

Use `.github/pull_request_template.md` and fill in every section. "New dependencies" comes first so an added package cannot be missed.

## What may enter the repository

- **BMC's API spec file never enters the repository**, its history or CI. Do not download it and do not generate code from it. Contract types are written by hand from BMC's public documentation and its BSD-licensed Python client (`controlm/ctm-python-client`); note the source beside each type. `eng/check-no-spec.sh` fails the build if an OpenAPI or Swagger document appears outside `tests/ControlM.SpecCheck.Tests/TestData/`, which holds only small documents written for this project's own tests.
- **Do not copy from `controlm/automation-api-quickstart`.** Its license is proprietary. Read it for reference only.
- **No real captures.** Fixtures are synthetic: real shapes, invented values.
- **Neutral names only.** No company, host, server, folder, job or user names from a real environment. Use `SERVER1`, `HOST1`, `FOLDER1`, `TENANT17` and similar.
- **No credentials or real endpoints.**
- **Dependencies must be permissively licensed** (MIT, Apache-2.0, BSD). Keep the client's runtime dependencies to a minimum.
- **If code is ported or adapted from BMC's Python client**, add a `THIRD-PARTY-NOTICES` file with the full BSD 3-Clause notice and BMC's copyright line, and mark the affected files. Prefer writing the C# fresh so this is not needed.

## Agent skills

### Issue tracker

Issues and specs live in this repository's GitHub Issues, through the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

The five default triage labels, each named for its role: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

### Slice planning

The slice map is issue #7, a table of planning issues. Run `/plan-slice` to plan the next one, from claiming it through closing it (`.claude/skills/plan-slice/`).

### Domain docs

Single-context: one `GLOSSARY.md` at the repository root and ADRs in `docs/adr/`, both created when first needed. See `docs/agents/domain.md`.
