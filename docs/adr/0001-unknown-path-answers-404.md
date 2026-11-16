# 1. An unknown path answers 404, as a real server does

## Context

The simulator has to answer a request whose method and path match no operation it knows. The requirements first said 501, naming the method and path, so that an unexpected call would fail in a way no caller could mistake for an answer from Control-M.

A real server was then probed (its API reports version 9.22.60). A `GET` to an unknown path under `/automation-api` answers the same with and without a credential:

```text
404
{"errors":[{"message":"Not Found","code":"404","uri":"/no/such/path"}]}
```

The `uri` holds the path without the `/automation-api` prefix and without the query string. `code` and `uri` are not among the error fields in BMC's Python client.

## Decision

The simulator answers an unknown path with 404 and that body. A known but unimplemented operation still answers 501 naming the operation, because a real server would have served it and the simulator is what falls short.

## Consequences

- An application sees the same response from the simulator as from a real server, and a conformance test of it can run against both.
- The client maps this to `ControlMNotFoundException`, the same exception as a job or folder that does not exist. A caller that catches it can hide a mistyped path. The request journal marks a request that matched no operation, so a test can assert there were none.
- A real operation that is missing from the simulator's hand-written list of known operations answers 404, where a real server would have served it. Keeping that list complete matters more than it did.
- Methods other than `GET`, and paths outside `/automation-api`, were not probed. Requirements section 4.8 records what is assumed for them.
