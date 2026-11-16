# Code conventions

These rules apply to all code in this repository.

## General

- Follow Microsoft's C# coding conventions and .NET design guidelines. `.editorconfig` and the analyzers enforce what they can, and warnings are errors. Do not suppress a warning without a comment giving the reason.
- Most of this document is not machine-checked: size limits, nesting depth, the comment rules and the organization rules rely on the author and the reviewer. Follow them as if the build did check.
- Line endings are LF in every text file, on every platform. `.gitattributes` and `.editorconfig` enforce it. Never commit CRLF.
- Types are `internal` and `sealed` unless they are public surface.
- Async end to end with `CancellationToken`. Never block on a task.
- All timing reads from an injected `TimeProvider`.
- No dead code and no commented-out code.
- Data types are immutable records: contract types, identifiers, options, results and the snapshots the simulator hands out. The simulator's domain entities that change over time, such as a job instance, are classes that guard their own state and are never exposed directly.
- JSON is `System.Text.Json` only, with one shared options definition per project.
- A class over about 300 lines or a method over about 40 needs a reason.
- Add `InternalsVisibleTo` only when a test project, or another project in this solution that the requirements say depends on it, first needs it.

## Organization

Organize by feature, never by type. Every feature lives in an area folder, and each namespace matches its folder: `<Area>/<Feature>/`.

- **Areas are required.** An area is a part of the Control-M API or a named concept of this project, such as the request journal. It is named for what it covers in the domain, not for a URL segment or a kind of class. `docs/requirements.md` lists the areas and what each means. The list is closed: a change that needs a new area adds it to that list in the same change.
- **The project root holds only the entry point.** Requirements section 2 names what that is for each project. Everything else is in an area.
- **A feature is self-contained within a project.** Its code sits together in one folder inside its area, so reading or changing it means opening one folder. A feature has a folder in each project it applies to, with the same area and feature names, and none where it does not apply.
- **A feature is one operation, one job type, or a family of operations that differ only by data.** A family is a single feature driven by a table, not a folder per member. Its folder is named for the family, and adding a member adds a row to its table and a public method beside the others. What must differ only by data is the HTTP side; behavior that differs per member, such as a state transition, belongs to the area's domain model.
- **An area owns its domain model.** In the simulator, the model and the test API that controls it live at the area root, and feature folders hold the HTTP side.
- **Areas depend on each other in one direction only.** Requirements section 2 gives the order for each project. An area may use the areas before it, never one after it.
- **A type used by several areas belongs to the earliest area whose concept it is.** Requirements section 2 places the shared identifiers.
- **Layout inside an area or feature is free.** Add subfolders where they help.
- **No type-based folders.** No `Models`, `Controllers`, `Services`, `Handlers`, `Dtos`, `Helpers` or `Utilities`. The default ASP.NET MVC layout is the counter-example: one feature scattered across a folder per kind of class.
- **The public API uses the same names.** An area the client exposes shares one name across its folder, namespace and public interface, and a single-operation feature's folder matches its public method without the `Async` suffix. An operation with no public method of its own takes its folder name from the operation table.
- **Area interfaces and their implementations are `partial`.** Each feature folder holds its own slice of both, so adding an operation edits no existing file apart from its one registration.
- **ASP.NET Core stays at the edge of the simulator.** Only types named `*Endpoint` and the hosting area may use it. A reflection test in `ControlM.Simulator.Tests` enforces this.
- **Shared code needs a real name.** Code used by several features lives in a folder named for the concept it implements, not in `Common/` or `Shared/`.
- **Tests mirror the area and feature folders** of the code they test.

An example, for one feature. The requirements define the real names; this only shows the shape.

```text
src/ControlM.Client/Jobs/
├── IControlMJobs.cs                  the interface declaration and its summary
├── ControlMJobs.cs                   constructor and plumbing shared by the area
└── GetOutput/
    ├── IControlMJobs.GetOutput.cs    the interface member and its XML documentation
    ├── ControlMJobs.GetOutput.cs     the method
    └── ...                           contract types, when the feature has any

src/ControlM.Simulator/Jobs/
├── JobInstance.cs                    domain model owned by the area
└── GetOutput/
    └── GetOutputEndpoint.cs          HTTP in, domain call, HTTP out

tests/ControlM.Client.Tests/Jobs/GetOutput/
tests/ControlM.Simulator.Tests/Jobs/GetOutput/
tests/ControlM.Conformance.Tests/Jobs/GetOutput/
```

## Comments

Code explains itself through names and small methods. A comment must earn its place: if deleting it costs a reader nothing, delete it. Before writing one, try to make it unnecessary by renaming or extracting.

- **XML documentation** goes on the public API only, and is required there. Say what a caller needs that the signature does not show. Never restate the name.
- **Inline comments** are only for what would surprise a reader: why a non-obvious approach was taken, a Control-M or dependency quirk, where a contract type came from, or why a warning is suppressed.
- **Never** describe what the code does, restate a requirement recorded elsewhere (link to the document or ADR instead), narrate history, label sections of a method, or leave `TODO`-style notes.

## Control flow

- **Guard clauses first.** A method validates its arguments and state at the top and exits immediately on failure, by returning or throwing. The happy path is the unindented code at the bottom, never inside an `else`.
- **Return early.** Return as soon as the result is known. Do not hold a result in a variable to reach a single return at the end.
- **Keep nesting shallow.** More than two levels of nesting inside a method is a signal to restructure: invert the condition and exit, `continue` past the uninteresting case in a loop, or extract the inner block into a named method.

## Design

Follow DRY and SOLID, and make every abstraction earn its place.

- **DRY.** Every piece of logic and every concept has one definition: status-string mapping, route paths, JSON options, the error envelope, the operation table. A second copy of the same logic is a defect; extract it. The one exception is code that looks alike but represents different concepts and would change for different reasons.
- **Single responsibility.** A class has one reason to change. No class mixes HTTP, domain rules and serialization.
- **Open/closed.** Adding an operation or a job type adds a feature folder and one registration. It does not edit existing handlers or a central switch.
- **Liskov.** Every implementation of an interface honors the same contract.
- **Interface segregation.** Small interfaces per area. No consumer depends on members it does not use.
- **Dependency inversion.** Depend on abstractions at true boundaries: time, HTTP, authentication, simulator state. Constructor injection only; no service locator, no mutable static state.

**An abstraction must carry its weight.** Introduce an interface, base class or generic only for a real boundary, a test seam, or a second implementation that exists today. A behavior that may change, such as an assumption about Control-M, is isolated in one named method or type, not behind an interface. No layers that only forward calls, no patterns for their own sake, no framework inside the project. When the principles and simplicity pull apart, choose the simpler design.

## Dependencies

- **Prefer the base framework.** Reach for the .NET base libraries and Microsoft's own packages first.
- **A third-party package is a decision, not a convenience.** Adding one requires a line in `docs/requirements.md` naming the package and why it is needed, in the same change that references it. This covers packages the repository references directly, not what they bring with them.
- **Do not hand-write what a well-known package does well** when the alternative is a large or risky amount of code. Say so and propose the package.
- **Make it visible.** A pull request that adds a package lists it under "New dependencies" at the top of its description, with its license and the reason.
- Package versions live in `Directory.Packages.props`. Add a package only when code first uses it.

## Testing

All behavior is built test first, one behavior per cycle.

1. **Red.** Write one test for one behavior that does not exist yet. Run it and confirm it fails on its assertion, for the expected reason. A compile error is not a valid red: first add the smallest skeleton that lets the test compile, a signature whose body returns a default value or throws `NotImplementedException`. The test then fails on its assertion or on that exception. The skeleton is part of the red commit and is replaced in green.
2. **Green.** Write the simplest code that passes. Run the whole suite.
3. **Refactor.** Improve the structure with the suite green.

- Commit the failing test separately from the code that passes it where that is natural. Give the test name and the observed failure message in the commit body.
- Never weaken, skip or delete a failing test to get to green. If the test was wrong, say why in the commit that changes it.
- Tests assert on observable behavior: through the typed client against an in-process simulator, and through raw HTTP for response shapes. Compare raw JSON with fixtures, not round-tripped types.
- A fixture has one copy, in `tests/Fixtures/<Area>/<Feature>/`, named `<Operation>.<case>.json`. The test projects that need it link to it; none holds its own copy.
- JSON is compared by value: property order and whitespace are ignored, nothing else is. A fixture holds literal values, never placeholders, which the simulator's deterministic identifiers make possible (requirements section 7).
- A test that guards a structural rule, such as the ASP.NET Core boundary test, passes as soon as it is written. Prove it can fail: introduce a violation locally, watch the test fail for the expected reason, remove the violation, and give the observed failure in the commit body.
- Substitute only at true boundaries: time, the network and the file system. Do not mock this project's own classes.
- Tests never sleep or read the wall clock.
- Scaffolding, build configuration, CI and documentation do not need a test first.

Each test project is defined by what is on the other end of the wire:

| Project | Tests | Other end |
| --- | --- | --- |
| `ControlM.Client.Tests` | The client alone: fixture deserialization, error mapping, retry, authentication, paging | A stub HTTP handler returning fixtures |
| `ControlM.Simulator.Tests` | The simulator alone: raw HTTP responses compared with fixtures, and domain rules directly | A raw `HttpClient`, or no HTTP at all |
| `ControlM.Conformance.Tests` | The typed client against a server | The in-process simulator, or a real server on demand |
| `ControlM.SpecCheck.Tests` | The spec comparison | Small OpenAPI documents written for this project |
