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
| `ControlM.Client.Sample` | Executable | A console application, in `samples/`, that logs in to a real server with credentials typed at a prompt; run by hand, never by the gate |
| `ControlM.Client.Tests`, `ControlM.Simulator.Tests`, `ControlM.Conformance.Tests`, `ControlM.SpecCheck.Tests` | Tests | TUnit on Microsoft.Testing.Platform, Shouldly |

Third-party dependencies: TUnit and Shouldly, in the test projects only. Everything else comes from the .NET base libraries and Microsoft's own packages.

`Microsoft.Extensions.TimeProvider.Testing` (MIT), in `ControlM.Simulator.Tests` only, supplies the `FakeTimeProvider` through which those tests move the simulator's clock.

One further dependency is expected and not yet chosen. Validating live responses against a spec schema (section 6, `--live`) needs a JSON Schema validator, which the base libraries do not have. The change that implements `--live` names the package here, with its license and the reason.

Structural rules:

- Contract types live inside `ControlM.Client`; the simulator depends on the client. There is no separate contracts project.
- Inside `ControlM.Simulator`, the domain model and state machine do not depend on ASP.NET Core; only the host layer does.
- `ControlM.Simulator` depends on no test framework. It takes a `TimeProvider` and returns ordinary objects.
- Neither the client nor the simulator depends on an OpenAPI reader; only `ControlM.SpecCheck` does.
- `ControlM.Client.Sample` references only `ControlM.Client`, and uses only its public API, as a consuming application would.
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
| `Operations` | Client | The operation table: the name, method and path of every implemented operation, and how a path relative to the endpoint becomes a request URL. |
| `Operations` | Simulator | The known operations the simulator does not implement and their 501 responses, the 404 for an unknown operation, and the 405 for a wrong method on a real path. |
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

The project root holds only the entry point: in the client, `IControlMClient`, its implementation, the composition of the handler pipeline, and `AddControlMClient` with its options; in `ControlM.SpecCheck`, the `spec-check` command that the tool calls; in `ControlM.Client.Sample`, its `Program`, and it has no areas. The simulator's root is empty.

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

An authentication scheme is required, because every operation needs a credential. `UseApiKey(key)` and `UseSessionLogin(username, password)` are methods of `ControlMClientOptions`. A client fails when it is first resolved if it has no scheme, both schemes, an empty or whitespace API key, or an empty or whitespace user name or password.

| Area | Members |
| --- | --- |
| `Orders` | `OrderJobAsync(JobRef, variables, options)` (the primary path), `OrderAsync(OrderRequest)` (whole folder or jobs filter), `GetRunStatusAsync` |
| `Jobs` | `GetJobAsync`, `QueryJobsAsync`, `RerunAsync`, `HoldAsync`, `FreeAsync`, `KillAsync`, `SetToOkAsync`, `ConfirmAsync`, `RunNowAsync`, `GetOutputAsync`, `GetLogAsync`, `GetWaitingInfoAsync` |
| `Deployment` | `BuildAsync`, `DeployAsync`, `GetJobsAsync`, `GetFoldersAsync`, `DeleteFolderAsync` |
| `Topology` | `GetServersAsync`, `GetAgentsAsync`, `GetHostGroupsAsync` |

Each member is one feature, in a folder named for the method without `Async`. The exception is the seven job actions (`RerunAsync` to `RunNowAsync`), which differ only by path segment and form one family, `Jobs/Actions`. An operation with no member of its own (`GET /deploy/poll` and the two session calls, `Login` and `Logout`) takes its folder name from the operation table. The session login scheme itself is the feature `Authentication/SessionLogin`, named for `UseSessionLogin`, as `Authentication/ApiKey` is for `UseApiKey`.

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
- A failed login is mapped by its status like any other failure, with the operation `Login` and the path `/session/login`, so a caller can tell rejected credentials from a token refused after a fresh login. A real server answers 401 for an unknown user and 500 for a known user's wrong password (section 4.2).
- An API key the server cannot decode is answered with 400 (section 4.2), so it throws `ControlMApiException`, not `ControlMAuthenticationException`.
- An error entry carries the server's `message` and, where the server sends them, `id`, `item`, `file`, `line`, `col`, `code` and `uri`. The first six come from the Python client; `code` and `uri` were observed on a real server.
- The path the exception carries is the one the caller gave, relative to the endpoint and with its query string. Its message names the status code, the method, the path and the first error message.
- A failure whose body is not an `ErrorList` still throws `ControlMApiException`, with no entries and the text of the body. A body counts as an `ErrorList` only when it parses as one and every entry has a message.
- Build and deploy validation problems are returned as a result (`BuildResult`, `DeployResult`), not thrown.
- A job that ended badly is data, not an exception.

### 3.4 Cross-cutting handlers

| Concern | Requirement |
| --- | --- |
| Authentication | One abstraction, two implementations, held by a singleton so that its state outlives the handlers `IHttpClientFactory` rotates. API key: adds `x-api-key`. Session: see below. |
| Retry | Backoff on 503 and transient network failures, for idempotent calls only. An order or a job action is never retried automatically. |
| Capture | When enabled, writes each request and response to disk after sanitizing. Passwords, tokens and API keys are always removed. Host, server and folder names are replaced with stable stand-ins through a configurable map. |
| TLS | Certificate trust is configurable per environment, to allow for self-signed server certificates. Validation is never disabled globally. |

The handlers run outermost first in the order Retry, Authentication, Capture, then the primary handler, so that capture records each attempt as it was sent. They are composed in one place in the client's root. Error mapping is not a handler: it runs in the client's one send path, after retry and re-login have finished.

Session login:

- The client logs in on the first request that needs a token, not when it is resolved, and sends the token as `Authorization: Bearer`.
- It keeps no clock for the token. It logs in again only when a request is answered 401, and then resends that request once; a second 401 reaches the send path. Request content is buffered before the first send so that it can be sent again.
- Calls that are answered 401 at the same time share one login. A call whose 401 came from a token that has already been replaced resends with the new token without logging in.
- A failed login, first or repeated, is not retried.
- Login and logout are sent through the client's own named `HttpClient`, created from `IHttpClientFactory`, so they get the same primary handler and Capture as every other request, including a primary handler the consumer configured. They carry a request option that the Authentication handler passes through without applying a credential, and Retry never retries them, because both are `POST`s.
- Disposing the client's service provider logs out if a token is held. Logout is best effort: any failure is swallowed, nothing is retried, and disposal never throws.

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
- A path of an implemented operation called with a method it does not serve returns 405 as a real server does, whatever the credential: an `Allow` header naming the methods it serves, and one error entry with the message `Method Not Allowed`, the code `405`, and a `uri` formed as for the 404. The fallback route that answers the 404 matches every method, so ASP.NET Core routing never produces its own 405; the simulator works out the 405 and its `Allow` value from the endpoints registered for the path.
- An unexpected call never succeeds silently, and the journal marks a request for an unknown operation, whether it was answered 404 or 405.
- A real server sends its error bodies with no `Content-Type`, with CRLF line endings and a leading blank line. The simulator sends them as `application/json` and does not reproduce the formatting: a caller reads the JSON, and tests compare it by value.

### 4.2 Authentication

- Users are seeded with a user name and password, and log in with `POST /session/login`. API keys are seeded and sent as `x-api-key`.
- Session login issues a token, sent as `Authorization: Bearer`. Tokens come from a per-instance sequence: `SIMTOKEN1`, `SIMTOKEN2` and so on.
- A session is an idle timeout, as BMC's administration guide documents: it expires `ControlMSimulatorOptions.SessionLifetime` (default 30 minutes) after its last authenticated request, read from the `TimeProvider`. Login starts that time, and only a request that passes the credential check with the token restarts it, so an unknown path or a wrong method does not. A real server may expire a token up to 10 minutes late, and caps the timeout at 3 hours; neither is simulated. A session can be expired on demand (section 5.1).
- Every implemented operation checks the credential, except the two session operations below. An unknown path and a wrong method do not: they answer 404 and 405 whatever the request carries.
- The credential check looks first at which headers are present, then at their values. A failure returns one error entry with no `code` or `uri`:

  | Request carries | Status | Message |
  | --- | --- | --- |
  | No `x-api-key` with a value, and no `Authorization` header | 401 | `Unauthenticated request: No authentication header was provided` |
  | Both an `x-api-key` with a value and an `Authorization` header, whatever their values | 401 | `Unauthenticated request: Request cannot contain both x-api-key and authorization headers` |
  | A bearer token that is invalid or expired | 401 | `User not found. Session token is invalid or expired.` |
  | An API key that was never seeded | 400 | `Cannot validate token value, decode failed` |

  All four were observed on a real server: the second with an unknown key beside a valid token, the last with a value the server could not decode. The simulator's keys are of that kind. What a real server answers for a well-formed key that is unknown or revoked is not known and not simulated.
- `POST /session/login` checks no credential. Its body is `{"username","password"}`, and it answers:

  | Case | Status | Body |
  | --- | --- | --- |
  | A seeded user and the right password | 200 | `{"username","token","version"}`, with the user name as sent and `version` `9.22.60` |
  | An unknown user, or a body without `username` or `password` | 401 | `Failed to login: Incorrect username or password` |
  | A seeded user and the wrong password | 500 | `Failed to login: Login failed.` |
  | A body that is not JSON | 400 | (assumed, section 4.8) |

  The first three were observed on a real server. The 500 was seen for one user, and its cause, such as how that user authenticates, is not known.
- `POST /session/logout` answers the two header-level 401s above, but does not check the token's value. Any bearer token gets 200 with `{"message":"Successfully logged out from session <token>"}`, and the session ends if it was live. A real server answered the same for a token that had already logged out.

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
| Session lifetime | An idle timeout, as BMC's administration guide documents: a token expires 30 minutes after its last authenticated request. Not probed |
| Order ID across reruns | A rerun keeps the job instance's order ID |
| Agents and servers | An agent belongs to the server it was added to; the same name on two servers is two agents |
| Unknown path with a method other than `GET` | The same 404 as the `GET` that was observed |
| Unknown path outside `/automation-api` | The same 404, with the whole path in `uri` |
| The bare path `/automation-api`, with nothing after it | Outside the prefix: the same 404, with `/automation-api` in `uri` |
| Wrong method on a real path | The same 405 as the observed `PATCH /run/jobs/status`, for every method, path and credential |
| `Allow` for a path served by more than one method | The methods joined by `, `; only a path served by one method was observed |
| A seeded API key beside an `Authorization` header | The same 401 as the observed unknown key beside a valid token: the check is on which headers are present |
| An `Authorization` header that is not a live bearer token | The invalid or expired token message, whatever the header's scheme |
| `POST /session/login` with credential headers | They are ignored, even both at once |
| A login body that is not a JSON object | 400, with one error entry, `Failed to login: Invalid request body`. A JSON array counts as not an object; the JSON literal `null` counts as a body without `username` or `password`. The `Content-Type` of a login body is not checked |
| The user name in a login response | Echoed exactly as sent |
| `POST /session/logout` with only an API key | 200, with the key in the message where a token would be; nothing ends |
| `POST /session/logout` with an `Authorization` header that is not a bearer token | 200, with the whole header value in the message where a token would be; nothing ends |
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
| Credentials | `sim.Credentials` | `Authentication` | Seed users (`AddUser(username, password)`) and API keys; expire every live session (`ExpireSessions()`) or one (`ExpireSession(token)`). An expired and a revoked token look the same on the wire, so there is no separate revoke |
| Virtual files | `sim.Files` | `VirtualFiles` | Create or remove paths on a host |
| Time | `TimeProvider`, `sim.TriggerNewDay()` | `Jobs` | Advance the injected provider; trigger New Day |
| Journal | `sim.Journal` | `Journal` | Every request received: operation, method, path, query string, headers, body, time and status code. A request for an unknown operation has no operation |
| State | `sim.State` | `Jobs`, `Orders` | Current state of any folder or job instance; every order received with its variables |
| Reset | `sim.Reset()` | `Hosting` | Return to empty state |

The test API is named for what a test controls, so its property names need not match the area that implements them. The "Owning area" column says where each lives.

A job pattern is a glob over `folder/subfolder/job`. The most specific matching rule wins. A count in a rule, such as failing N times, is kept per job instance: every instance the rule matches fails its first N runs.

The journal holds every request the simulator receives, including one refused for its credential and one answered by a stub, a fault, a 501, the unknown-operation 404 or the wrong-method 405. That includes a path outside `/automation-api`, which is answered with the same 404. Requests under `/__sim/`, typed controls and operator actions are not journaled.

- `sim.Journal` is a `RequestJournal` with three members: `Entries`, a snapshot of every `JournalEntry` in the order the requests arrived; `Count(Operation)`, the number of requests routed to that operation, whatever they were answered; and `Unmatched`, the entries for unknown operations. A test filters `Entries` for anything else.
- A `JournalEntry` is immutable. Its `Operation` is the client's `Operation`, or `null` for an unknown operation. `Path` is the whole path as received, with the `/automation-api` prefix; `Query` is the query string, empty when there is none. `Headers` is case-insensitive, with one string per name and repeated values joined by `, `. `Body` is the request body as UTF-8 text, empty when there is none. `Time` is when the request arrived, read from the `TimeProvider`. `Status` is the status code it was answered with, or `null` when no response was sent.
- Headers and body are kept as received, credentials and passwords included. Every credential the simulator accepts is one a test seeded, and a test may assert which one was sent. Capture, which writes to disk, is what sanitizes.
- The journal is unbounded; `sim.Reset()` empties it.
- The journal model is in the `Journal` area and does not depend on ASP.NET Core. `Hosting` feeds it from a middleware that runs after routing and around every endpoint and filter. Each endpoint carries its `Operation` as endpoint metadata.
- How a request answered by the named 501 is shown is decided with known operations.

### 5.2 In-process use

- `ControlMSimulator.StartAsync(o => o.TimeProvider = time)` starts an instance in milliseconds. `sim.CreateClient()` returns a real client wired to it, after seeding a fresh API key and configuring the client with it. `sim.CreateClient(o => ...)` lets the test choose the credential instead, such as an unknown key: it seeds no key, and runs the callback after setting the endpoint. A session client is a seeded user and `sim.CreateClient(o => o.UseSessionLogin("USER1", "PASSWORD1"))`. `sim.CreateHttpClient()` returns a raw `HttpClient`, which sends no credential of its own.
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
- Identifiers the simulator generates (order IDs, run IDs, poll IDs, session tokens such as `SIMTOKEN1`, and the API keys `CreateClient()` seeds, such as `SIMKEY1`) come from per-instance sequences, so the same calls on a new instance produce the same values and a fixture can hold them literally. Each kind of identifier has its own sequence, held by the area that issues it.
- Every observation of a real server in this project comes from one that reports API version 9.22.60, the target version; a probe need not ask again. The maintainer can probe it with session login only and holds no API token, so API key behavior is taken from BMC's documentation and Python client, and what they leave open is recorded in section 4.8. The exceptions were probed with a session token: a value the server cannot decode sent as an API key, and both headers in one request.
- The conformance suite is written against the client interface and runs against either the simulator or a real server.
- A real server is opt-in and never used by CI. Its endpoint, credential, and the server, host and folder names to use come from environment variables prefixed `CTM_CONFORMANCE_`. When they are absent, the suite runs against the simulator.
- A conformance test that arranges state through the simulator's test API is marked simulator-only and is skipped against a real server. The rest use only the client interface. A simulator-only test carries `[Category("SimulatorOnly")]`, as does a test of behavior not yet verifiable against a real server, such as API key authentication while no API token is available. Session login tests that use only the client interface are not simulator-only; a session expired through `sim.Credentials` is.
- `ControlM.Client.Sample` checks session login against a real server by hand, with the client as a consumer uses it. It prompts for the endpoint, user name and password, reading the password without echo, and takes nothing from the environment or files. It logs in, calls `GET /config/servers`, and logs out by disposing the service provider, printing one pass or fail line per step with the status and first error message of a failure, and exits non-zero on any failure. It prints no password, token or server name, so its output can be pasted into an issue as it is. The gate builds it and never runs it, and it has no tests. It does not replace a conformance run against a real server.
  - Its steps are `login`, `GET /config/servers` and `logout`, each reported `PASS`, `FAIL` or `SKIP`. Login is not a call of its own, so it passes when the first call gets past it and fails when that call throws with the operation `Login`; `GET /config/servers` is then skipped.
  - `logout` passes once the service provider is disposed, since the client swallows every failure of that logout and the sample has nothing to observe. It is skipped when no session was opened: a login that failed or got no response, or options that fail when the client is resolved, which is reported as a failed `configure` step. The sample learns whether a session opened from a handler it adds that watches for a successful login response.
  - A request that gets no response cannot be placed on login or the call, so it gives one failed `login or GET /config/servers` line naming only the `HttpRequestError`, or a timeout. The exception's message is not printed, because it can name the host.
  - An endpoint that is not an absolute `http` or `https` address fails at once, before the user name and password are asked for. The exit code of any failure is 1.
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
