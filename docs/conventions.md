# Code conventions

These rules apply to all code in this repository.

## General

- Follow Microsoft's C# coding conventions and .NET design guidelines. `.editorconfig` and the analyzers enforce them, and warnings are errors. Do not suppress a warning without a comment giving the reason.
- Line endings are LF in every text file, on every platform. `.gitattributes` and `.editorconfig` enforce it. Never commit CRLF.
- Types are `internal` and `sealed` unless they are public surface.
- Async end to end with `CancellationToken`. Never block on a task.
- All timing reads from an injected `TimeProvider`.
- No dead code and no commented-out code.
- Package versions live in `Directory.Packages.props`. Add a package only when code first uses it.
- Add `InternalsVisibleTo` only when a test first needs it.

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
- **Open/closed.** Code is grouped by operation within each area (session, deploy, run, config). Adding an operation or a job type adds files and one registration. It does not edit existing handlers or a central switch.
- **Liskov.** Every implementation of an interface honors the same contract.
- **Interface segregation.** Small interfaces per area. No consumer depends on members it does not use.
- **Dependency inversion.** Depend on abstractions at true boundaries: time, HTTP, authentication, simulator state. Constructor injection only; no service locator, no mutable static state.

**An abstraction must carry its weight.** Introduce an interface, base class or generic only for a real boundary, a test seam, or a second implementation that exists today. No layers that only forward calls, no patterns for their own sake, no framework inside the project. When the principles and simplicity pull apart, choose the simpler design.

## Testing

All behavior is built test first, one behavior per cycle.

1. **Red.** Write one test for one behavior that does not exist yet. Run it and confirm it fails on its assertion, for the expected reason. A compile error is not a valid red.
2. **Green.** Write the simplest code that passes. Run the whole suite.
3. **Refactor.** Improve the structure with the suite green.

- Commit the failing test separately from the code that passes it where that is natural. Give the test name and the observed failure message in the commit body.
- Never weaken, skip or delete a failing test to get to green. If the test was wrong, say why in the commit that changes it.
- Tests assert on observable behavior: through the typed client against an in-process simulator, and through raw HTTP for response shapes. Compare raw JSON with fixtures, not round-tripped types.
- Substitute only at true boundaries: time, the network and the file system. Do not mock this project's own classes.
- Tests never sleep or read the wall clock.
- Scaffolding, build configuration, CI and documentation do not need a test first.
