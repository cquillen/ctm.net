# plan-slice reference

General `gh` usage is in `docs/agents/issue-tracker.md`. This file holds only what the plan-slice skill adds to it.

## The map's table

Each section of #7 keeps its heading and holds one table:

```markdown
### Foundation

| # | Capability | Planning | State | Spec | Docs PR | Session |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Walking skeleton | #8 | planned | #40 | #39 | [session](https://claude.ai/code/session_...) |
| 2 | API key authentication | #9 | open | | | |
```

- `#` gives the order across the whole map, not within a section.
- `State` is one of `open`, `claimed`, `grilled` or `planned`.
- `Spec` lists every spec the row produced, separated by commas. `Docs PR` is a PR number or `none`.
- `Session` links the session that claimed the row. A resumed or finishing session replaces the link.

If the body has no such table, stop and tell the user. Do not rebuild it.

### Editing the map

Edit only the row you own:

1. `gh issue view 7 --json body --jq .body > map.before.md`
2. Copy it to `map.md` and change the row with the Edit tool.
3. Read the body again, and compare it with `map.before.md`. If anything changed, start again from step 1.
4. `gh issue edit 7 --body-file map.md`

## Finding the next row

- Map order: the `#` column.
- Assignee and state: `gh issue view <n> --json state,assignees`.
- Open blockers: `gh issue view <n> --json blockedBy --jq '[.blockedBy.nodes[] | select(.state == "OPEN") | .number]'`. Only blockers that are planning issues (rows of the map) count. Implementation tickets do not block planning.

## The session link

Use the URL on the `Claude-Session:` line of the attribution in the system context. If there is none, use the session ID from the scratchpad path (the folder above `scratchpad`) as `session <id>`.

## Claiming

```sh
gh issue edit <n> --add-assignee @me
gh issue comment <n> --body-file claim.md
```

## Docs pull request

```sh
git fetch origin
git worktree add .claude/worktrees/<slug> -b docs/<slug> origin/main
cd .claude/worktrees/<slug>
sh eng/verify.sh
git commit -F commit.md
git push -u origin docs/<slug>
gh pr create --title "docs: <summary>" --body-file pr.md
```

Merged? `gh pr view <pr> --json state,mergedAt`.

Once it is merged, from the repository root:

```sh
git worktree remove .claude/worktrees/<slug>
git branch -D docs/<slug>
git push origin --delete docs/<slug>
```

## Verify query

Run it for each spec:

```sh
gh api graphql -F n=<spec> -f query='
query($n: Int!) {
  repository(owner: "cquillen", name: "ctm.net") {
    issue(number: $n) {
      parent { number }
      labels(first: 10) { nodes { name } }
      subIssues(first: 50) {
        nodes {
          number title state
          labels(first: 10) { nodes { name } }
          blockedBy(first: 20) { nodes { number state } }
        }
      }
    }
  }
}'
```

Repairs:

- `gh issue edit 7 --add-sub-issue <spec>`
- `gh issue edit <spec> --add-sub-issue <ticket>`
- `gh issue edit <ticket> --add-blocked-by <blocker>`
- `gh issue edit <n> --add-label ready-for-agent`

To find a new spec when the user has not named it, list #7's sub-issues and pick the ones absent from every row's `Spec` column.

## Comment templates

Head every comment with its bold title, then `(session <link>)`.

**Claim**: `**Claimed for slice planning.**`

- Session: <link>
- Started: <date>, from `main` at `<short sha>`
- Why this one: <first open, unassigned, unblocked row in #7 | named by the maintainer>
- Process: grill the brief against the requirements, conventions, glossary and ADRs, recording decisions here after each round; land any doc changes in their own PR; the spec (to-spec) and its tickets (to-tickets) follow; then the spec is linked to #7 and this issue is closed.

**Resume**: `**Resumed.**`, followed by the session link and the last decision number already recorded.

**Round**: `**Grilling record, round <k>**`

- `### Observed on a real server`, if anything was probed: the API version from requirements section 7, each request, and its literal response. List what was not probed.
- `### Decisions`, numbered from where the last comment stopped. Each decision is a bold title and then the decision itself.
- `### Assumed, not observed`, if any decision rests on an assumption.

**Settlement**: `**Grilling record, round <k>, and settlement**`, laid out as a round comment, then the docs PR (or `none` and why), then `Next: the spec (to-spec) and its tickets (to-tickets).`

**Note on another issue**: `From the planning of #<n>: <what changed for this issue, and the decision number>.`

**Closing**: `**Planning complete.**`

- **Spec:** #<spec>, a sub-issue of #7.
- **Tickets:** each a sub-issue of the spec, in the order of their blocked-by chain.
- **Cross-slice edges:** each one added, as `#<ticket> blocked by #<upstream ticket>`, or `none`.
- **Docs:** #<pr>, or `none`.
- **Notes left on:** each issue, and what the note says.
- The old title, if the issue was retitled.
- What closing this unblocks in the map.

## Hand-off block

Print this for the user, filled in:

```text
Planning issue: #<n>, <title>
Decisions: the round comments on #<n>, decisions 1 to <last>
Docs: #<pr> merged | none

to-spec:    one spec per settled capability, titled "Spec: <capability>",
            labelled ready-for-agent, created with --parent 7
to-tickets: each ticket labelled ready-for-agent, created with --parent <spec>,
            with --blocked-by chaining each ticket to the one it builds on
```
