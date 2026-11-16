# Control-M Client and Simulator: Requirements

This repository implements two things in .NET 10:

- **A typed client** for the BMC Control-M Automation API (target version 9.0.22.060; BMC publishes no documentation edition by that number, so the baseline is its 9.0.22.000 edition).
- **A stateful simulator** of that API, so an application that drives Control-M can be developed, tested and demonstrated without a real Control-M server.

The consuming application is an external orchestrator. For each run period it generates job definitions, deploys them, then orders and tracks individual jobs per tenant. The orchestrator decides what runs next; Control-M only executes jobs and reports status. Nothing here references or assumes anything about that application.

The simulator has two audiences: automated tests (in-process, deterministic, parallel) and UI development and demos (a standalone process running in real time).

## 1. Projects

| Project | Kind | Contains |
| --- | --- | --- |
| `ControlM.Client` | Library | Typed client, contract types, authentication, retry and capture handlers |
| `ControlM.Simulator` | Library | Simulator core, in-process host, test API, `/__sim/` control API |
| `ControlM.Simulator.Tool` | Executable | Thin `Program` that parses options and starts the standalone host; no behavior of its own |
| `ControlM.SpecCheck` | Library | Compares the client and simulator with an OpenAPI document |
| `ControlM.Client.Tests`, `ControlM.Simulator.Tests`, `ControlM.Conformance.Tests`, `ControlM.SpecCheck.Tests` | Tests | TUnit on Microsoft.Testing.Platform, Shouldly |

Third-party dependencies: TUnit and Shouldly, in the test projects only. Everything else comes from the .NET base libraries and Microsoft's own packages.

One further dependency is expected and not yet chosen. Validating live responses against a spec schema (section 6, `--live`) needs a JSON Schema validator, which the base libraries do not have. The change that implements `--live` names the package here, with its license and the reason.

Structural rules:

- Contract types live inside `ControlM.Client`; the simulator depends on the client. There is no separate contracts project.
- Inside `ControlM.Simulator`, the domain model and state machine do not depend on ASP.NET Core; only the host layer does.
- `ControlM.Simulator` depends on no test framework. It takes a `TimeProvider` and returns ordinary objects.
- Neither the client nor the simulator depends on an OpenAPI reader; only `ControlM.SpecCheck` does.
- The typed client interface, the types it exchanges, the operation table, the simulator test API and the control API are public surface. Everything else is `internal`. Where the simulator or `ControlM.SpecCheck` needs something internal from a project it depends on, that project grants it `InternalsVisibleTo`.
- Client and simulator are organized by area and, within each area, by operation. Adding an operation or a job type adds files and one registration, with no central switch to edit.

## 2. Areas and operations

The code, the namespaces and the public API are divided into five areas. The names follow Control-M's own vocabulary where it has a word.

| Area | Meaning | BMC API service |
| --- | --- | --- |
| `Authentication` | Proving who the caller is: session login and logout, and API keys. | Session |
| `Deployment` | Putting job definitions on a server. To *build* is to validate definitions without changing anything; to *deploy* is to validate them and install them. | Build, Deploy |
| `Orders` | Asking Control-M to run something. To *order* a job or folder is to place an instance of it into the active environment so that it runs. It does not mean putting jobs in sequence. An order returns a run ID that tracks what it started. | Run |
| `Jobs` | Job instances in the active environment: their status, the actions an operator can take on them, and their output and log. | Run |
| `Topology` | What exists to run jobs on: servers, agents and host groups. | Config |

The simulator has further areas of its own, which the client does not expose:

| Area | Meaning |
| --- | --- |
| `JobTypes` | One unit per supported job type, holding how its definition is parsed and how it behaves when run. |
| `Journal` | The record of every request the simulator received. |
| `Faults` | Injected API failures and stubbed responses. |
| `Scenarios` | Scenario files: the control calls written as data. |
| `VirtualFiles` | The per-host set of file paths that file watcher jobs observe. |
| `Hosting` | The in-process and standalone hosts, the `ControlMSimulator` entry point, and the `/__sim/` control API. |

Some areas hold what every operation shares:

| Area | Project | Meaning |
| --- | --- | --- |
| `Serialization` | Client | The one set of JSON options for every request and response body. |
| `Serialization` | Simulator | The one set of JSON options for every response body the simulator writes. |
| `Operations` | Client | The operation table: the name, method and path of every implemented operation. |
| `Operations` | Simulator | The known operations the simulator does not implement and their 501 responses, and the 404 for an unknown operation. |
| `Errors` | Client | The `ErrorList` envelope and the exceptions it maps to. |
| `Errors` | Simulator | Building the `ErrorList` envelope as data, with the client's types. The endpoint that answers writes it. |
| `Tls` | Client | Per-environment certificate trust. |
| `Retry` | Client | Backoff for idempotent calls. |
| `Capture` | Client | Writing sanitized requests and responses to disk. |

`ControlM.SpecCheck` has areas of its own:

| Area | Meaning |
| --- | --- |
| `SpecReading` | Loading an OpenAPI or Swagger document. The only code that touches the OpenAPI reader. |
| `Operations`, `Parameters`, `Bodies`, `StatusCodes`, `ContentTypes`, `Authentication`, `LiveResponses` | One per row of the table in section 6: the comparison that produces those findings. |
| `Reporting` | Findings, severities, and the Markdown and JSON reports. |

The project root holds only the entry point: in the client, `IControlMClient`, its implementation, the composition of the handler pipeline, and `AddControlMClient` with its options; in `ControlM.SpecCheck`, the `spec-check` command that the tool calls. The simulator's root is empty.

An area may use the areas before it in these lists, never one after it:

- **Client:** `Serialization`, `Operations`, `Errors`, `Tls`, `Retry`, `Capture`, `Authentication`, `Topology`, `Deployment`, `Jobs`, `Orders`.
- **Simulator:** `Serialization`, `Errors`, `Operations`, `Journal`, `Faults`, `Authentication`, `Topology`, `VirtualFiles`, `JobTypes`, `Deployment`, `Jobs`, `Orders`, `Scenarios`, `Hosting`.
- **SpecCheck:** `SpecReading`, `Reporting`, then the comparison areas, which do not use each other.

Shared identifiers belong to the earliest area whose concept they are: `ServerName` to `Topology`; `JobRef` and `DefinitionDocument` to `Deployment`; `JobId`, `JobStatus` and `JobSnapshot` to `Jobs`; `RunId` to `Orders`.

Contract types, client methods and simulator behavior exist only for the operations below. Full API coverage is not a goal.

| Area | Operations |
| --- | --- |
| `Authentication` | `POST /session/login`, `POST /session/logout` |
| `Deployment` | `POST /build`, `POST /deploy`, `GET /deploy/poll`, `GET /deploy/jobs`, `GET /deploy/folders`, `DELETE /deploy/folder/{server}/{folderName}` |
| `Orders` | `POST /run/order`, `GET /run/status/{runId}` |
| `Jobs` | Status: `GET /run/jobs/status`, `GET /run/job/{jobId}/status`<br>Actions: `POST /run/job/{jobId}/` + `rerun`, `hold`, `free`, `kill`, `setToOk`, `confirm`, `runNow`<br>Diagnostics: `GET /run/job/{jobId}/output`, `GET /run/job/{jobId}/log`, `GET /run/job/{jobId}/waitingInfo` |
| `Topology` | `GET /config/servers`, `GET /config/server/{server}/agents`, `GET /config/server/{server}/hostgroups` |

The operation table is the set of static properties of the public `Operation` record, one per implemented operation, such as `Operation.GetServers`. Its name is the client method's without `Async`, or the name above for an operation with no method. Its path is relative to the endpoint, in route-template syntax. Adding the property is the operation's one registration in the client. In the simulator, the operation's endpoint maps itself from that property, and the one registration is the call to it in `Hosting`.

Supported job types: `Job:Command`, `Job:Script`, `Job:EmbeddedScript`, `Job:FileWatcher`.

Both authentication schemes are supported: session login with a bearer token, and an API key in `x-api-key`. BMC calls an API key an API token.

## 3. Client

### 3.1 Contract types

- Hand-written request and response types for the supported operations only.
- Unknown JSON properties are ignored, so a newer server does not break deserialization.

### 3.2 Typed interface

One root interface, `IControlMClient`, exposes four of the areas as properties, plus the two low-level `SendAsync` members described below. Authentication is configured at registration (`services.AddControlMClient(o => ...)` with `UseSessionLogin` or `UseApiKey`) and is not part of the interface. The interface can be faked in the consumer's unit tests.

`ControlMClientOptions` follows the standard .NET options pattern: the registration callback sets its properties, `services.AddControlMClient(o => o.Endpoint = endpoint)`, as `ControlMSimulator.StartAsync` does in section 5.2. `Endpoint` includes the `/automation-api` prefix, and a missing one fails when the client is first resolved.

An authentication scheme is required, because every operation needs a credential. `UseApiKey(key)` and `UseSessionLogin(...)` are methods of `ControlMClientOptions`. A client with no scheme, or with an empty or whitespace API key, fails when it is first resolved.

| Area | Members |
| --- | --- |
| `Orders` | `OrderJobAsync(JobRef, variables, options)` (the primary path), `OrderAsync(OrderRequest)` (whole folder or jobs filter), `GetRunStatusAsync` |
| `Jobs` | `GetJobAsync`, `QueryJobsAsync`, `RerunAsync`, `HoldAsync`, `FreeAsync`, `KillAsync`, `SetToOkAsync`, `ConfirmAsync`, `RunNowAsync`, `GetOutputAsync`, `GetLogAsync`, `GetWaitingInfoAsync` |
| `Deployment` | `BuildAsync`, `DeployAsync`, `GetJobsAsync`, `GetFoldersAsync`, `DeleteFolderAsync` |
| `Topology` | `GetServersAsync`, `GetAgentsAsync`, `GetHostGroupsAsync` |

Each member is one feature, in a folder named for the method without `Async`. The exception is the seven job actions (`RerunAsync` to `RunNowAsync`), which differ only by path segment and form one family, `Jobs/Actions`. An operation with no member of its own (`GET /deploy/poll` and the two session calls) takes its folder name from the operation table.

The interface adds the typing the API lacks:

- **Identifiers.** Value types `ServerName`, `RunId` and `JobId` (`server:orderId`, with `Parse`). `JobRef` (server, folder, optional sub-folder, job name) addresses a job in the deployed definitions; the client turns it into the order call.
- **Status.** A `JobStatus` enum (`WaitCondition`, `WaitResource`, `WaitHost`, `WaitUser`, `WaitWorkload`, `Executing`, `EndedOk`, `EndedNotOk`, `StatusUnknown`, `Unrecognized`) plus the server's exact text in `RawStatus`. `StatusUnknown` is Control-M's own "Status Unknown". Text the client does not know maps to `Unrecognized` and never throws.
- **Agent status.** An enum of the server's four values (`Available`, `Unavailable`, `Disabled`, `Discovering`) plus `Unrecognized`, with the exact text kept beside it, as for job status.
- **Dates.** Start and end times are `DateTimeOffset`. The order date stays the server's string.
- **Job definitions.** An opaque JSON document (`DefinitionDocument`). The application owns the content.
- **Ordering.** `OrderOptions.IgnoreCriteria` defaults to `true`, because jobs run on demand and have no schedule.
- **Arguments.** Required arguments are checked before a request is sent.

Behavior:

- `QueryJobsAsync` hides paging behind `IAsyncEnumerable<JobSnapshot>`. A `JobSnapshot` is a job instance's state at the time of the call. A 206 from `GET /run/jobs/status` is a partial result and is surfaced to the caller.
- `BuildAsync` and `DeployAsync` build the multipart upload from an in-memory definition. `DeployAsync` follows a `pollId` through `GET /deploy/poll` to completion.
- The server parameter is always sent as `server`, never `ctm`. A response that names it `ctm` is read.
- A low-level `SendAsync<T>(method, path, body?)` calls any operation the typed interface does not cover and returns the response body as `T`. `SendAsync(method, path, body?)`, without `<T>`, does the same and reads no response body, for a call whose answer the caller does not need. Both go through the same authentication, retry, error mapping and capture pipeline. The path is relative to the endpoint, may carry a query string, and gives the same URL with or without a leading slash, as the endpoint does with or without a trailing one.

### 3.3 Errors

- An API failure throws `ControlMApiException`, carrying the status code, every entry of the `ErrorList` body, the method and path, and the operation. A call made through either `SendAsync` has no operation. Subtypes: `ControlMAuthenticationException` (a 401: for session login, one that remains after one re-login) and `ControlMNotFoundException` (404).
- An error entry carries the server's `message` and, where the server sends them, `id`, `item`, `file`, `line`, `col`, `code` and `uri`. The first six come from the Python client; `code` and `uri` were observed on a real server.
- The path the exception carries is the one the caller gave, relative to the endpoint and with its query string. Its message names the status code, the method, the path and the first error message.
- A failure whose body is not an `ErrorList` still throws `ControlMApiException`, with no entries and the text of the body. A body counts as an `ErrorList` only when it parses as one and every entry has a message.
- Build and deploy validation problems are returned as a result (`BuildResult`, `DeployResult`), not thrown.
- A job that ended badly is data, not an exception.

### 3.4 Cross-cutting handlers

| Concern | Requirement |
| --- | --- |
| Authentication | One abstraction, two implementations, held by a singleton so that its state outlives the handlers `IHttpClientFactory` rotates. Session: login, re-login once on 401, logout on disposal. API key: adds `x-api-key`. |
| Retry | Backoff on 503 and transient network failures, for idempotent calls only. An order or a job action is never retried automatically. |
| Capture | When enabled, writes each request and response to disk after sanitizing. Passwords, tokens and API keys are always removed. Host, server and folder names are replaced with stable stand-ins through a configurable map. |
| TLS | Certificate trust is configurable per environment, to allow for self-signed server certificates. Validation is never disabled globally. |

The handlers run outermost first in the order Retry, Authentication, Capture, then the primary handler, so that capture records each attempt as it was sent. They are composed in one place in the client's root. Error mapping is not a handler: it runs in the client's one send path, after retry and re-login have finished.

## 4. Simulator

The simulator reproduces the HTTP surface and observable behavior of the supported operations, holds all state in memory, and never executes a job.

### 4.1 HTTP surface

- The Control-M API is served under `/automation-api`, as on a real server.
- Response bodies, status codes and the error envelope (an `errors` array whose entries carry a `message` and optional location details) match the real API.
- Malformed requests get a 400.
- Both `ctm` and `server` are accepted wherever the real API accepts both.
- Endpoints are registered from the client's operation table, one handler per operation, so each path is written once.
- The simulator also lists the rest of the API's operations (name, method, path), written by hand from the public documentation and the Python client.
- A known but unimplemented operation returns 501 with an `ErrorList` body naming the operation.
- A path matching no known operation returns 404 as a real server does, whether or not the request carries a credential: one error entry with the message `Not Found`, the code `404`, and a `uri` holding the path without the `/automation-api` prefix or the query string. [ADR 0001](adr/0001-unknown-path-answers-404.md) gives the reason.
- A path of an implemented operation called with a method it does not serve returns 405 as a real server does, whatever the credential: an `Allow` header naming the methods it serves, and one error entry with the message `Method Not Allowed`, the code `405`, and a `uri` formed as for the 404.
- An unexpected call never succeeds silently, and the journal marks a request that matched no operation.

### 4.2 Authentication

- Session login issues a token, sent as `Authorization: Bearer`. API keys are seeded and sent as `x-api-key`.
- Session lifetime is configurable (default: 30 minutes from login, whatever the activity). A token can be expired or revoked on demand.
- Every implemented operation checks the credential. An unknown path and a wrong method do not: they answer 404 and 405 whatever the request carries.
- A missing, invalid or expired credential returns 401 with one error entry and no `code` or `uri`. The message depends on what the request carried:

  | Request carries | Message |
  | --- | --- |
  | No `x-api-key` with a value, and no `Authorization` header | `Unauthenticated request: No authentication header was provided` |
  | A bearer token that is invalid or expired | `User not found. Session token is invalid or expired.` |
  | An API key that was never seeded | `Invalid API token.` (assumed, section 4.8) |

  The first two were observed on a real server.
- A request with a seeded API key passes, whatever other headers it carries.

### 4.3 Domain model

- **Topology:** servers, agents with status, and host groups.
- **Deployed definitions:** folders, sub-folders and jobs. Each job has its type, host, run-as user and type-specific fields.
- **Active environment:** folder and job instances with order ID, status, run count, held flag, start and end times, order date, output and log text, and the variables passed at order time.
- **Virtual files:** a per-host set of paths (no content), used by file watcher jobs.

### 4.4 Build and deploy

- The definition JSON is parsed for folders, sub-folders and the four supported job types. Anything else is rejected with a build-style error naming the unsupported element; nothing is accepted and ignored.
- A job whose host is blank, or is not a known agent or host group, is rejected.
- `POST /build` validates and changes nothing. `POST /deploy` validates, then replaces the named folders.
- A deploy can be scripted to be an asynchronous deployment, returning a `pollId` that `GET /deploy/poll` resolves.
- `GET /deploy/jobs` returns what was deployed, in the real server's format.

### 4.5 Ordering and job lifecycle

- `POST /run/order` orders a whole folder, or individual jobs through the `jobs` filter. Individual-job ordering is the primary path.
- A whole-folder order starts every job. There is no dependency evaluation.
- Ordering something that was never deployed fails as the real server does.
- Each job instance moves through the real status values (for example Wait Host, Executing, Ended OK, Ended Not OK).
- A job whose agent is in any status other than available waits and does not run.
- Job actions apply the correct transitions and reject invalid ones:
  - `rerun` increments the run count on the same instance.
  - `hold` prevents start; `free` releases it.
  - `kill` ends an executing job as failed.
  - `setToOk` forces success.
  - `confirm` releases a job that is waiting for confirmation.
  - `runNow` starts a waiting job at once, unless it is held.
- `waitingInfo` lists why a job is not running: held, awaiting confirmation, or its agent unavailable. For any other job the list is empty.
- Status queries page correctly and honor at least these filters: folder, job name, status, host, server, order date range, job ID.
- Statuses include an `outputURI` and `logURI` that resolve against the simulator.

### 4.6 Job type behavior

| Type | Simulated behavior |
| --- | --- |
| `Job:Command`, `Job:Script`, `Job:EmbeddedScript` | Runs for a scripted or default duration, then ends with the scripted or default outcome. May add or remove paths in the host's virtual file set. Produces output text. |
| `Job:FileWatcher` | Stays executing until the watched path exists on its host, then succeeds. Fails when its own timeout passes. |

An unscripted job succeeds after a default duration. Default durations, output and log text are plausible enough for a monitoring UI.

### 4.7 Time and New Day

- All timing reads from an injected `TimeProvider`. The simulator defines no clock abstraction of its own.
- In tests, nothing progresses until the test advances a `FakeTimeProvider`. The standalone host uses the system provider, with a scaled provider when a speed multiplier is set.
- The daily New Day cleanup, which removes job instances that ended OK from the active environment and leaves the rest, is simulated and can be triggered on demand.

### 4.8 Assumed Control-M behavior

These behaviors are not yet verified against a real server. Each is implemented in one named method or type, so it can change in one place. That does not mean an interface.

When the public documentation and the Python client do not settle a detail, choose the most plausible behavior and add a row here in the same change.

| Item | Working assumption |
| --- | --- |
| Addressing a sub-folder job when ordering | The `jobs` filter takes the job name; the sub-folder is identified by path in the folder or jobs value. Deploy queries separate folder and sub-folder with a colon (`FOLDER1:TENANT17`); status names use a slash |
| What a single-job order creates | A new instance of the parent folder containing only that job, governed by `orderIntoFolder` and `createDuplicate` |
| Jobs with no schedule | Can be ordered on demand; `ignoreCriteria` may be required |
| `runId` for a single-job order | Tracks just the ordered job through `GET /run/status/{runId}` |
| New Day cleanup | Jobs that ended OK leave the active environment at the next New Day |
| Not-found on status and deploy queries | Follows the public documentation (404 or an empty list) |
| Embedded script size | No limit enforced |
| Job status strings | As given in the public documentation |
| Which jobs wait for confirmation | A job whose definition sets `Confirm` to true waits in Wait User until `confirm` |
| What `runNow` bypasses | Agent availability and confirmation, but not a hold |
| Shape of `waitingInfo` | A list of reason strings |
| `server` in the order body | Accepted in place of `ctm`, which the documented example still sends |
| Path of the delete-folder operation | `DELETE /deploy/folder/{server}/{folderName}`, as in the Python client. The documentation's example shows `DELETE /deploy/folder/{folder}?server={server}` |
| Session lifetime | A token expires 30 minutes after login; activity does not extend it |
| Order ID across reruns | A rerun keeps the job instance's order ID |
| Agents and servers | An agent belongs to the server it was added to; the same name on two servers is two agents |
| Unknown path with a method other than `GET` | The same 404 as the `GET` that was observed |
| Unknown path outside `/automation-api` | The same 404, with the whole path in `uri` |
| The bare path `/automation-api`, with nothing after it | Outside the prefix: the same 404, with `/automation-api` in `uri` |
| Wrong method on a real path | The same 405 as the observed `PATCH /run/jobs/status`, for every method, path and credential |
| 401 for an API key that was never seeded | The message `Invalid API token.`; no API token was available to probe |
| A seeded API key beside other credential headers | The key is enough; the other headers are ignored |
| A request with no credential and an invalid body | 401, not 400: the credential is checked before the body |

## 5. Simulator control

One set of control capabilities, exposed three ways over a single implementation: typed C# calls in-process, HTTP under `/__sim/`, and scenario files.

### 5.1 Capabilities

| Control | Test API | Owning area | Capability |
| --- | --- | --- | --- |
| Topology | `sim.Topology` | `Topology` | Add servers, agents and host groups; mark an agent unavailable and available again |
| Definitions | `sim.Definitions` | `Deployment` | Seed folders and jobs with a fluent builder, as an alternative to deploying through the real endpoint |
| Job outcomes | `sim.Jobs` | `Jobs` | By job pattern: succeed, fail N times then succeed, hang, set duration, set output and log text, create or remove virtual files |
| API faults | `sim.Api` | `Faults` | By operation: status code, added latency, dropped connection, malformed body; once, N times or always |
| Stubs | `sim.Api.Stub` | `Faults` | Canned response for any method and path. Stateless, takes precedence over the named 501 and the unknown-operation 404, recorded in the journal |
| Operator actions | `sim.Operator` | `Jobs` | Rerun, hold, free, kill, set-to-OK, confirm or run-now a job out of band, as a Control-M operator would |
| Credentials | `sim.Credentials` | `Authentication` | Seed users and API keys; expire or revoke sessions |
| Virtual files | `sim.Files` | `VirtualFiles` | Create or remove paths on a host |
| Time | `TimeProvider`, `sim.TriggerNewDay()` | `Jobs` | Advance the injected provider; trigger New Day |
| Journal | `sim.Journal` | `Journal` | Every request received: operation, method, path, headers, body, time. A request that matched no operation is marked as such |
| State | `sim.State` | `Jobs`, `Orders` | Current state of any folder or job instance; every order received with its variables |
| Reset | `sim.Reset()` | `Hosting` | Return to empty state |

The test API is named for what a test controls, so its property names need not match the area that implements them. The "Owning area" column says where each lives.

A job pattern is a glob over `folder/subfolder/job`. The most specific matching rule wins. A count in a rule, such as failing N times, is kept per job instance: every instance the rule matches fails its first N runs.

The journal holds every request to the Control-M API surface, including one answered by a stub, a fault, a 501 or the unknown-operation 404. Control API calls, typed controls and operator actions are not journaled.

### 5.2 In-process use

- `ControlMSimulator.StartAsync(o => o.TimeProvider = time)` starts an instance in milliseconds. `sim.CreateClient()` returns a real client wired to it, after seeding a fresh API key and configuring the client with it. `sim.CreateClient(o => ...)` lets the test choose the credential instead, such as an unknown key. `sim.CreateHttpClient()` returns a raw `HttpClient`, which sends no credential of its own.
- Each test starts its own instance. Instances share no state, so tests run in parallel.
- The in-process host uses the ASP.NET Core test server and opens no socket. The standalone host uses Kestrel. A dropped-connection fault reaches the client as a transient network failure under both.
- Typed control calls act on the simulator core directly, with no HTTP round trip.
- Nothing depends on wall-clock time or sleeps.
- A scenario file can be loaded at start (`o.LoadScenario(...)`) and then adjusted in code.

```csharp
var time = new FakeTimeProvider();
await using var sim = await ControlMSimulator.StartAsync(o => o.TimeProvider = time);
sim.Topology.AddServer("SERVER1").AddAgent("HOST1");
sim.Definitions.Folder("SERVER1", "FOLDER1")
    .SubFolder("TENANT17", sub => sub
        .Command("Validate", host: "HOST1", command: "validate.sh"));
sim.Jobs.Matching("FOLDER1/TENANT17/Validate").FailTimes(1).ThenSucceed();

IControlMClient client = sim.CreateClient();
// ... order the job, advance `time`, rerun on failure ...

sim.Journal.Count(Operation.RerunJob).ShouldBe(1);
sim.State.Job("FOLDER1/TENANT17/Validate").Status.ShouldBe(JobStatus.EndedOk);
```

### 5.3 Standalone use

- Runs as a command with configurable port, TLS and time multiplier.
- Loads a named scenario file at startup or through the control API.
- A failure can be triggered through the control API while a run is in progress, without a restart.
- The control API is unauthenticated and test-only. The host binds to localhost by default.
- A single long-lived simulator with `Reset` is for demos and manual work, not automated tests.

### 5.4 Scenario files

JSON with comments allowed. A scenario is the same calls as the test API written as data: name, clock speed, topology, credentials, definition files to deploy, job outcome rules and API faults.

Shipped scenarios: clean run, several tenants failing at one step, one agent down, slow drop file, expired session mid-run.

## 6. Spec comparison

`ControlM.SpecCheck` reports every difference between an OpenAPI document and this project. It reads the document with Microsoft.OpenApi. The command, its options and its exit code are implemented in `ControlM.SpecCheck`; the tool only passes its arguments on. It is run through the tool:

```
dotnet run --project src/ControlM.Simulator.Tool -- spec-check --spec <file> [--out report.md] [--json report.json] [--live] [--all]
```

- Accepts Swagger 2.0 and OpenAPI 3.0 and 3.1, in YAML or JSON. The file path is supplied at run time.
- Produces Markdown and JSON reports with the same content, led by a summary: counts by area and severity, the spec's title and version, and this project's target version.
- Every finding names the operation, the location, what the spec says and what this project has.
- Severities: **error** (the client would send something invalid or misread a response), **warning** (data is ignored or a documented case is unhandled), **info** (outside the implemented surface).
- By default only implemented operations are detailed, with a single count for the rest; `--all` lists everything.
- The exit code is non-zero when any error-level finding exists.

| Area | Reported differences |
| --- | --- |
| Operations | Missing from the operation table or from the spec; method or path differences; which spec operations are implemented, named-501 or unknown |
| Parameters | Query, path, header and body parameters present on one side only; required-flag and type mismatches |
| Bodies | Per contract type, recursively: missing and extra properties; type, format, required, nullable and enum mismatches |
| Status codes | Codes the spec declares that are never returned or handled; codes the simulator returns that the spec lacks |
| Content types | Media type differences, including multipart field names for build and deploy |
| Authentication | Security schemes in the spec compared with the two supported |
| Live responses (`--live`) | Runs every implemented operation through the real client against an in-process simulator and validates each response body against the spec schema |

## 7. Fidelity and tests

- Simulator tests compare raw response JSON with fixtures or documented examples, not DTO round-trips, so the client and simulator cannot agree while both are wrong.
- Client tests deserialize the raw fixtures directly, with no simulator involved.
- Fixtures are written by hand from the public documentation and the Python client: real shapes, invented values. Each has one copy, shared by the test projects that use it.
- A test project brings in the fixtures, and `JsonFixture`, which loads one and compares JSON by value, by importing `tests/Fixtures/Fixtures.props`. There is no shared test project.
- Identifiers the simulator generates (order IDs, run IDs, poll IDs, session tokens, and the API keys `CreateClient()` seeds, such as `SIMKEY1`) come from per-instance sequences, so the same calls on a new instance produce the same values and a fixture can hold them literally. Each kind of identifier has its own sequence, held by the area that issues it.
- Every observation of a real server in this project comes from one that reports API version 9.22.60, the target version; a probe need not ask again. The maintainer can probe it with session login only and holds no API token, so API key behavior is taken from BMC's documentation and Python client, and what they leave open is recorded in section 4.8.
- The conformance suite is written against the client interface and runs against either the simulator or a real server.
- A real server is opt-in and never used by CI. Its endpoint, credential, and the server, host and folder names to use come from environment variables prefixed `CTM_CONFORMANCE_`. When they are absent, the suite runs against the simulator.
- A conformance test that arranges state through the simulator's test API is marked simulator-only and is skipped against a real server. The rest use only the client interface.
- Every implemented operation has a test comparing its response with a fixture or a documented example.
- The whole test suite runs in parallel and completes in under one minute.
- Scale target: an in-process test can deploy generated definitions, order individual jobs in at least 100 sub-folders across several agents, fail a chosen job once, observe the rerun, and assert on the request journal, with no wall-clock waits.

## 8. Out of scope

- Executing commands or scripts.
- Scheduling, calendars and order-date logic beyond recording the order date.
- Dependencies between jobs: events, conditions, flows and resources.
- Other job types, including file transfer, database and cloud types.
- Agentless hosts, and jobs with a blank host that run on the server itself. A host group contains agents only.
- Full validation of job definitions against Control-M's rules.
- All other API operations, including `/config` administration, provisioning, reporting, archive, workload policies and API token management.
- Persistence across restarts.
- Any Control-M user interface.
- The orchestrator, the definition generator and the consuming application's UI.
- NuGet packaging, a packaged .NET tool and a container image.
