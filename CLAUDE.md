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

## Projects

- `ControlM.Client` references nothing else in the solution.
- `ControlM.Simulator` references `ControlM.Client`. Its domain model and state machine must not depend on ASP.NET Core; only the host layer does. It depends on no test framework.
- `ControlM.SpecCheck` is the only project that may reference an OpenAPI reader.
- `ControlM.Simulator.Tool` is a thin `Program`. It adds no behavior.

## Conventions

- Microsoft's C# coding conventions and .NET design guidelines. `.editorconfig` and the analyzers enforce them, and warnings are errors. Do not suppress a warning without a comment giving the reason.
- Line endings are LF in every text file, on every platform. `.gitattributes` and `.editorconfig` enforce it. Never commit CRLF.
- Types are `internal` and `sealed` unless they are public surface. Public types and members need XML documentation; the build fails without it.
- Constructor injection only. No service locator and no mutable static state.
- Async end to end with `CancellationToken`. Never block on a task.
- All timing reads from an injected `TimeProvider`. Tests never sleep or read the wall clock.
- Organize code by operation within each area (session, deploy, run, config). Adding an operation or a job type adds files and one registration; it does not edit a central switch.
- Define each concept once: status-string mapping, route paths, JSON options, the error envelope, the operation table.
- No speculative abstraction, no dead code, no commented-out code, no leftover TODOs.
- Package versions live in `Directory.Packages.props`. Add a package only when code first uses it.
- Add `InternalsVisibleTo` only when a test first needs it.

## Test-first

All behavior is built test first, one behavior per cycle.

1. **Red.** Write one test for one behavior that does not exist yet. Run it and confirm it fails on its assertion, for the expected reason. A compile error is not a valid red.
2. **Green.** Write the simplest code that passes. Run the whole suite.
3. **Refactor.** Improve the structure with the suite green.

- Commit the failing test separately from the code that passes it where that is natural. Give the test name and the observed failure message in the commit body.
- Never weaken, skip or delete a failing test to get to green. If the test was wrong, say why in the commit that changes it.
- Tests assert on observable behavior: through the typed client against an in-process simulator, and through raw HTTP for response shapes. Compare raw JSON with fixtures, not round-tripped types.
- Substitute only at true boundaries: time, the network and the file system. Do not mock this project's own classes.
- Scaffolding, build configuration, CI and documentation do not need a test first.

The test projects start empty, so `tests/Directory.Build.props` passes `--ignore-exit-code 8` to tolerate a run that finds no tests. **Remove that line in the change that adds the first real test.**

## What may enter the repository

- **BMC's API spec file never enters the repository**, its history or CI. Do not download it and do not generate code from it. Contract types are written by hand from BMC's public documentation and its BSD-licensed Python client (`controlm/ctm-python-client`); note the source beside each type. `eng/check-no-spec.sh` fails the build if an OpenAPI or Swagger document appears outside `tests/ControlM.SpecCheck.Tests/TestData/`, which holds only small documents written for this project's own tests.
- **Do not copy from `controlm/automation-api-quickstart`.** Its license is proprietary. Read it for reference only.
- **No real captures.** Fixtures are synthetic: real shapes, invented values.
- **Neutral names only.** No company, host, server, folder, job or user names from a real environment. Use `SERVER1`, `HOST1`, `FOLDER1`, `TENANT17` and similar.
- **No credentials or real endpoints.**
- **Dependencies must be permissively licensed** (MIT, Apache-2.0, BSD). Keep the client's runtime dependencies to a minimum.
- **If code is ported or adapted from BMC's Python client**, add a `THIRD-PARTY-NOTICES` file with the full BSD 3-Clause notice and BMC's copyright line, and mark the affected files. Prefer writing the C# fresh so this is not needed.
