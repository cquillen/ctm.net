---
name: plan-slice
description: Plan one slice-planning issue from the slice map (#7) - claim it, grill it, land its doc changes, hand off to to-spec and to-tickets, then link and close it.
disable-model-invocation: true
argument-hint: "[#planning-issue]"
---

# Plan a slice

The **map** is issue #7, "Slice plan: Control-M client and simulator". Its body holds one table per section, and each row is a **planning issue**: the brief for one capability. Planning a row turns its brief into a **spec** issue. The maintainer writes the spec with to-spec and cuts it into **tickets** with to-tickets. Your job is everything around those two skills: claim the row, grill the brief, land the doc changes, hand off, and finish.

GitHub is the only record. Each run can start cold, after a `/clear` or in a new session, so work out the phase from the map and the issue's comments, never from memory. Commands, the map's table format and the comment templates are in [reference.md](reference.md). Read it before your first `gh` call.

Write every issue, comment and PR body to a file in the scratchpad and pass it with `--body-file`.

## 0. Find the phase

Read the map, then:

1. If the user named a planning issue, use it. Refuse an issue that is closed, blocked by an open planning issue, or `claimed` or `grilled` in a session the user has not asked you to resume.
2. Otherwise, if a row is `claimed` or `grilled` and assigned to the user, **resume** it. Post the resume comment with this session's link. Only one planning issue is in flight at a time.
3. Otherwise, **claim** the first row in map order that is `open`, unassigned, and has no open blocker among the planning issues.

Then go to the step its state calls for:

| Row state | Docs PR | Spec sub-issue of #7 not yet in any row | Step |
| --- | --- | --- | --- |
| `open` | | | 1. Claim |
| `claimed` | | | 2. Grill, rebuilding the decisions from the issue's comments |
| `grilled` | open | | Stop: the maintainer merges the docs PR |
| `grilled` | merged or `none` | no | 4. Hand off |
| `grilled` | merged or `none` | yes | 5. Finish |

## 1. Claim

Assign the issue to the user, set its row to `claimed`, and post the claim comment. Done when all three show on GitHub.

## 2. Grill

Load the `grilling` skill and run it against the brief. Before round 1, read `docs/requirements.md`, `docs/conventions.md`, `GLOSSARY.md`, every ADR in `docs/adr/`, and every comment on the issue, including the `From the planning of #N:` notes that other plans left. Load `domain-modeling` as soon as a decision touches the glossary or calls for an ADR.

**Control-M behavior needs a source.** It might be BMC's public documentation, the BSD-licensed Python client (`controlm/ctm-python-client`), or a probe of a real server. Look up public sources yourself, using a research subagent if the search is broad. When they do not settle the question, give the user exact `curl` probes with `<host>` and `<token>` placeholders, and ask them to remove every real name from what they paste back; this repository is public. Record what comes back under "Observed on a real server", with the API version the server reports. Anything a decision assumes without a source goes under "Assumed, not observed", and later into requirements section 4.8.

**After every answered round**, post a decision comment. Number decisions across comments, so the first decision in round 2 continues from the last in round 1. A reversal is a new decision that says "This replaces decision N".

**When a decision changes another planning issue's scope**, post a `From the planning of #N:` note on that issue in the same step.

**When the grilling shows the map itself is wrong** (a brief that should be two specs, two briefs that should be one, or a dependency that has no row), propose the exact edit to #7 as a question in the next round, and make it only once the user approves. One planning issue may produce several specs.

Done when the grilling's frontier is empty and the user has confirmed the shared understanding.

## 3. Settle

1. If the decisions change `docs/requirements.md`, `GLOSSARY.md` or the ADRs, switch to a new branch `docs/<slug>` from an up-to-date `main` and write the changes. Run `sh eng/verify.sh`. Commit with a `docs:` subject and `Refs #<planning issue>` in the body. Open a PR from `.github/pull_request_template.md`, filling in every section.
2. Post the settlement comment: the last round's decisions, plus the docs PR, or `none` with the reason no doc changed.
3. Set the row to `grilled` and put the docs PR (or `none`) in its row.

Then stop and ask the user to merge the docs PR. Never merge it yourself. Done when the PR is open, or `none` is recorded.

## 4. Hand off

Check that the docs PR is merged, then switch to `main` and pull. Print the hand-off block from reference.md, so the user can run to-spec and to-tickets with it. They often run both in this session; carry them out as those skills direct, holding to the shape the block gives.

Done when the spec and its tickets exist.

## 5. Finish

1. **Verify** the spec and its tickets with the verify query. Repair anything missing: the spec's parent (#7), `ready-for-agent` on the spec and every ticket, each ticket's parent (the spec), and the blocked-by chain the to-tickets breakdown named.
2. **Add cross-slice edges.** For every planning issue that blocked this one, read its closing comment for the tickets it produced. Add a blocked-by edge from each of this slice's tickets that needs that code to the upstream ticket that delivers it, usually the last ticket in that chain. Edges run ticket to ticket, never to a spec.
3. **Update the row**: `planned`, with the spec (or specs), docs PR and session.
4. **Retitle** the planning issue if the settled scope contradicts its title.
5. Post the closing comment, then close the issue and confirm it is closed.
6. **Clean up.** If the docs branch's PR is merged, delete the branch locally and on the remote. Leave the checkout on an up-to-date `main`.

Done when the row reads `planned`, the issue is closed, and the verify query shows every edge in place.
