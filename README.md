# ctm.net

A typed .NET client for the BMC Control-M Automation API, and a stateful simulator of that API.

The simulator lets an application that drives Control-M be developed, tested and demonstrated without a real Control-M server. It runs in-process for fast, deterministic tests, or as a standalone process for UI work and demos.

**Status:** scaffolding only. The projects build but contain no behavior yet. [docs/requirements.md](docs/requirements.md) describes what is being built.

## Build and test

You need the .NET 10 SDK (the exact version is pinned in `global.json`) and a POSIX shell. On Windows, Git Bash works.

```sh
sh eng/verify.sh
```

That one script is the gate for the repository, and CI runs exactly the same thing. It checks that no OpenAPI document has entered the tree, then restores, builds, runs the tests and checks formatting.

The individual steps also work on their own:

```sh
dotnet build
dotnet test
dotnet format
```

## Layout

| Path | Contents |
| --- | --- |
| `src/ControlM.Client` | Typed client, contract types, authentication, retry and capture handlers |
| `src/ControlM.Simulator` | Simulator core, in-process host, test API and control API |
| `src/ControlM.Simulator.Tool` | Thin executable that starts the standalone simulator host |
| `src/ControlM.SpecCheck` | Compares the client and simulator with an OpenAPI document |
| `samples/ControlM.Client.Sample` | Console sample that checks session login against a real server by hand; see its [README](samples/ControlM.Client.Sample/README.md) |
| `tests/ControlM.Client.Tests` | Client tests |
| `tests/ControlM.Simulator.Tests` | Simulator tests |
| `tests/ControlM.Conformance.Tests` | One suite written against the client interface, runnable against the simulator or a real server |
| `tests/ControlM.SpecCheck.Tests` | Spec comparison tests |
| `eng/` | Build and verification scripts |
| `docs/` | [Requirements](docs/requirements.md) and [code conventions](docs/conventions.md) |

Tests use [TUnit](https://tunit.dev) on Microsoft.Testing.Platform, with [Shouldly](https://docs.shouldly.org) for assertions.

## Relationship to BMC

This is an independent project. It is not affiliated with, sponsored by or endorsed by BMC Software. Control-M is a trademark of BMC Software, Inc.

The client and simulator are written from BMC's public documentation and its BSD-licensed Python client. BMC's API spec file is confidential and is not part of this repository.

## License

BSD 3-Clause. See [LICENSE](LICENSE).
