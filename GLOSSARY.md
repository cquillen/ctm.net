# Control-M client and simulator

The language of a typed client for the BMC Control-M Automation API and of a simulator of that API. Terms follow BMC's documentation where it has a word. Where this project coins a term, or BMC's name misleads, the entry says so.

## Language

### Topology

**Server**:
A Control-M/Server: the scheduler that holds deployed definitions and directs agents to run jobs. Never a machine that runs jobs.
_Avoid_: ctm (BMC's deprecated name for the same thing), host

**Agent**:
The Control-M component installed on a machine so that a server can run jobs there, known to the server by a logical name. A job runs on an agent only while the agent is available.
_Avoid_: node

**Host group**:
A named collection of agents on one server. A job sent to a host group runs on one of its members.
_Avoid_: node group

**Host**:
The name in a job definition that says where the job runs: an agent or a host group.

### Definitions

**Job definition**:
A job as deployed to a server inside a folder: its type, its host and the properties of its type.
_Avoid_: bare "job" where it could also mean a job instance

**Folder**:
A container of job definitions and sub-folders, deployed to a server.

**Sub-folder**:
A folder contained within another folder or sub-folder.

**Job type**:
The kind of a job definition, written as Control-M writes it, such as `Job:Command`.

**Definition document**:
The JSON a caller builds or deploys, holding folders and their job definitions.

**Build**:
To validate definitions without changing anything on the server.

**Deploy**:
To validate definitions and install them on the server.

**Asynchronous deployment**:
A deploy that answers with a poll ID instead of its result, because the server has not finished.

**Poll ID**:
The identifier that fetches the result of one asynchronous deployment.

### Ordering and running

**Order**:
To place an instance of a deployed folder or job into the active environment so that it runs; also the request that does so. It never means putting jobs in sequence.

**Active environment**:
The folder and job instances a server currently holds. BMC's documentation calls it the Active Jobs database.
_Avoid_: AJF

**Job instance**:
One ordered copy of a job definition in the active environment. This project's term; BMC says "job" or "job order".

**Folder instance**:
One ordered copy of a folder in the active environment, holding the job instances ordered with it. This project's term; BMC says "folder".

**Run**:
One execution of a job instance. Rerunning adds a run to the same instance. BMC also says "job run".
_Avoid_: execution, attempt

**Run ID**:
The identifier an order returns, which tracks everything that order started. Despite BMC's name, it identifies an order, not a run.

**Order ID**:
The identifier a server assigns to a job instance or folder instance. Despite BMC's name, it identifies an instance, not an order.

**Job ID**:
A server name and an order ID together, written `server:orderId`. It identifies one job instance across servers.

**Job status**:
The state of one job instance, as one of Control-M's status values such as Wait Host, Executing or Ended OK.

**Run status**:
Whether everything an order started has finished, with the job status of each instance. Despite BMC's name, it describes an order, not a run.

**Held**:
A flag on a job instance, beside its job status, that stops it from starting until it is freed. Not a job status.

**Awaiting confirmation**:
The condition of a job instance whose definition requires confirmation and that has not yet been confirmed. Its job status is Wait User.

**New Day**:
The server's daily cleanup, which removes job instances that ended OK from the active environment.

### Authentication

**User**:
An identity that logs in to the API.

**Run-as user**:
The account, named in a job definition, under which the job runs on its agent. Unrelated to the user.

**Session**:
The period during which the token from one login is accepted: until logout, or until it has gone unused for the session lifetime.

**Session token**:
The token a login returns, sent as `Authorization: Bearer`.

**API key**:
A long-lived credential, sent in `x-api-key`, that a user creates ahead of time instead of logging in. BMC calls it an API token.
_Avoid_: API token, except when quoting BMC

**Credential**:
What a request presents to prove who the caller is: a session token or an API key.

### Simulator

**Control**:
One capability for arranging or inspecting the simulator's state.

**Test API**:
The controls as typed, in-process calls.

**Control API**:
The controls over HTTP.
_Avoid_: test API, for the HTTP form

**Scenario**:
A set of controls written as data and applied together.

**Job pattern**:
A glob over `folder/subfolder/job` that selects job definitions.

**Outcome rule**:
A job pattern together with what the runs of matching job instances do.

**Fault**:
An injected failure of an implemented operation.

**Stub**:
A canned response for any method and path.

**Operator action**:
A job action applied directly to the simulator, as an operator at a Control-M console would, rather than through the API.

**Virtual file**:
A path that exists on an agent, with no content.

**Journal**:
The record of every request the simulator received, other than those to its control API.

**Journal entry**:
One request in the journal, with how it was answered: its operation, or none for an unknown operation, and the status code sent back.

### Spec comparison

**Spec**:
An OpenAPI or Swagger document describing the Control-M Automation API, supplied when a comparison is run.

**Finding**:
One difference between the spec and this project.

**Severity**:
How much a finding matters: error, warning or info.

### Operations

**Operation**:
One method and path of the Control-M Automation API.

**Implemented operation**:
An operation the client can call and the simulator answers.

**Known operation**:
An operation of the real API that this project lists but does not implement.

**Unknown operation**:
A method and path that matches no implemented or known operation.
