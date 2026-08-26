---
name: init
description: Initialize the /context directory in this project — scaffold context/{changes,archive,foundation}/ plus universal README.md files if absent.
---

# /init — Initialize /context Directory

Scaffold the `/context` directory skeleton (`changes/`, `archive/`, `foundation/`) plus a universal `README.md` in each, so the change-tracking and foundation-doc conventions have a place to land. Idempotent: each of the six artifacts (3 dirs + 3 READMEs) is independently create-if-absent; re-running on a project where everything is already present is a no-op.

This skill is the explicit entry point for users who want to scaffold the workflow conventions up-front. It is NOT a precondition for `/new`, `/archive`, or any consumer skill — `/new` will refuse if `context/changes/` is missing, and `/archive` lazily creates `context/archive/` on demand. `/init` exists for users who prefer to set up the skeleton first.

## Process

### Step 1: Scaffold `context/changes/` + `README.md`

If the directory exists, leave it untouched and note `present` for the directory in the summary. Otherwise create it with `mkdir -p` and note `created`.

If `context/changes/README.md` exists, leave it untouched and note `present`. Otherwise write it with this canonical content (embedded inline — no separate template file):

```
# Changes

In-flight changes. One folder per change at `context/changes/<change-id>/`, identified by a `change.md` identity file. Created via `/new`. Holds research, frame, plan, reviews, and other change-scoped artifacts.

When a change is complete, archive it with `/archive` to move it under `context/archive/`.
```

### Step 2: Scaffold `context/archive/` + `README.md`

If the directory exists, leave it untouched and note `present` for the directory. Otherwise create it with `mkdir -p` and note `created`.

If `context/archive/README.md` exists, leave it untouched and note `present`. Otherwise write it with this canonical content:

```
# Archive

Completed changes. Folders moved here from `context/changes/` when archived (see `/archive`). Read-only by convention; skills refuse to write here.
```

### Step 3: Scaffold `context/foundation/` + `README.md`

If the directory exists, leave it untouched and note `present` for the directory. Otherwise create it with `mkdir -p` and note `created`.

If `context/foundation/README.md` exists, leave it untouched and note `present`. Otherwise write it with this canonical content:

```
# Foundation Docs

Cross-change living documents that span multiple changes. Each project picks which foundation docs it needs (e.g. product requirements, tech-stack, roadmap, glossary, test-stack). Foundation docs are owned by the skills that read and write them; this README describes the conventions that apply to all of them.

## Update convention

**Edit-in-place.** Foundation docs evolve over the lifetime of the project. When something changes incrementally (a new dependency, a refined product goal, a shifted milestone), edit the existing file. Don't create dated copies.

## Archive convention

When a foundation doc is fully superseded — replaced by a new approach rather than refined — move it to `foundation/archive/YYYY-MM-DD-<doc>.md` and write the replacement at the original path. The archive folder is a historical record; nothing reads from it routinely.

## Anti-pattern

Do **not** put change-scoped docs here. Anything tied to a single change (its plan, its research, its review) belongs under `context/changes/<change-id>/`. Foundation is for what outlives any one change.
```

### Step 4: Print summary

Print a six-line status block:

```
context/changes/                [created|present]
context/changes/README.md       [created|present]
context/archive/                [created|present]
context/archive/README.md       [created|present]
context/foundation/             [created|present]
context/foundation/README.md    [created|present]
```

Then a one-paragraph guide on what each directory is for and where to look next:

- `context/changes/` holds in-flight changes. Run `/new` to create a new change folder with its `change.md` identity file.
- `context/archive/` holds completed changes. Run `/archive` when a change is done — it will move the folder out of `changes/` into `archive/`.
- `context/foundation/` holds cross-change living docs. There is no fixed list of files here; foundation docs are owned by the skills that write them (e.g. `/prd` writes `prd.md`, `/tech-stack-selector` writes `tech-stack.md`).

Stop. Do not chain into `/new` or any other skill; the user runs those when they have something to do.

## Notes

- **Idempotent.** Re-running `/init` on a project where all six artifacts already exist is a no-op (with a status print). It must never overwrite existing content.
- **No forced ordering.** All six artifacts are independent. If only some exist, create the missing ones and leave the existing ones alone.
- **Parent directories are created as needed.** `context/` may not exist in a fresh project — create it implicitly via `mkdir -p` semantics on each child directory.
- **Not a precondition.** Other skills self-bootstrap their own files. `/init` is for users who like to set up the `/context` skeleton up-front.
- **`lessons.md` is not scaffolded here.** This file is owned end-to-end by `/lesson` and `/impl-review`'s triage branches, which self-bootstrap it with their canonical headers on first use.