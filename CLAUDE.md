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

The rules for writing code here are in `docs/conventions.md`, imported in full below.

@docs/conventions.md

## Empty test projects

The test projects start empty, so `tests/Directory.Build.props` passes `--ignore-exit-code 8` to tolerate a run that finds no tests. **Remove that line in the change that adds the first real test.**

## What may enter the repository

- **BMC's API spec file never enters the repository**, its history or CI. Do not download it and do not generate code from it. Contract types are written by hand from BMC's public documentation and its BSD-licensed Python client (`controlm/ctm-python-client`); note the source beside each type. `eng/check-no-spec.sh` fails the build if an OpenAPI or Swagger document appears outside `tests/ControlM.SpecCheck.Tests/TestData/`, which holds only small documents written for this project's own tests.
- **Do not copy from `controlm/automation-api-quickstart`.** Its license is proprietary. Read it for reference only.
- **No real captures.** Fixtures are synthetic: real shapes, invented values.
- **Neutral names only.** No company, host, server, folder, job or user names from a real environment. Use `SERVER1`, `HOST1`, `FOLDER1`, `TENANT17` and similar.
- **No credentials or real endpoints.**
- **Dependencies must be permissively licensed** (MIT, Apache-2.0, BSD). Keep the client's runtime dependencies to a minimum.
- **If code is ported or adapted from BMC's Python client**, add a `THIRD-PARTY-NOTICES` file with the full BSD 3-Clause notice and BMC's copyright line, and mark the affected files. Prefer writing the C# fresh so this is not needed.
