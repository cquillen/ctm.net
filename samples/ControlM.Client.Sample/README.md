# ControlM.Client.Sample

A console application that checks session login against a real Control-M server, using the client as an application would. It logs in, calls `GET /config/servers`, and logs out by disposing the client's service provider. Nothing runs it automatically: `eng/verify.sh` builds it and never runs it.

## Run it

```sh
dotnet run --project samples/ControlM.Client.Sample
```

It asks for three things:

- **Endpoint**: the Automation API address, including `/automation-api`, such as `https://host1:8443/automation-api`.
- **User name**.
- **Password**: typed without echo.

It reads nothing from environment variables or files. The server's certificate must be trusted by the machine's root certificates; the sample has no setting to bypass TLS checks.

## What it prints

One line per step, each starting with `PASS`, `FAIL` or `SKIP`:

```text
PASS  login
PASS  GET /config/servers: 3 servers
PASS  logout: the service provider was disposed
```

A failure gives the HTTP status and the first error message the server sent. The exit code is 0 when every step passes and 1 otherwise.

`logout` passes whenever the provider is disposed, because the client never reports a failed logout. It is skipped when no session was opened.

## What to paste back

Paste the lines from the first `PASS`, `FAIL` or `SKIP` on, as they are. Leave out the prompts above them: they show the endpoint and the user name you typed. The result lines hold no password, token, endpoint or server name.
