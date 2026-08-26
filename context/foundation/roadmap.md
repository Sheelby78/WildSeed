---
project: "Wild Seed"
version: 1
status: draft
created: 2026-08-26
updated: 2026-08-26
prd_version: 1
main_goal: learn
top_blocker: skills
---

# Roadmap: Wild Seed

> Derived from `context/foundation/prd.md` (v1) + auto-researched codebase baseline.
> Edit in place; archive when superseded.
> Slices below are listed in dependency order. The "At a glance" table is the index.

## Vision recap

Wild Seed is an interactive 2D ecosystem simulation intended to feel like a living, autonomous world. Its defining experience is observing behavior and evolutionary direction emerge from interactions between simple rules rather than scripted scenarios. It is both a private experiment in simulation design and a polished demonstration of programming ability.

## North star

**S-02: Visitor can run and observe a deterministic survival loop** — it exercises the unfamiliar decision mechanics early and turns them into a cohesive visual experience before predator-prey behavior or inheritance increases the number of interactions.

> Here, the north star means the smallest end-to-end capability that demonstrates the product's central idea. It is placed as early as its prerequisites allow because later ecosystem depth only matters if autonomous survival behavior is convincing and repeatable.

## At a glance

| ID | Change ID | Outcome (user can …) | Prerequisites | PRD refs | Status |
|---|---|---|---|---|---|
| F-01 | determinism-performance-contract | (foundation) deterministic result equivalence and headless throughput can be measured before simulation behavior expands | — | PRD §Non-Functional Requirements | done |
| S-01 | configurable-procedural-world | visitor can configure world parameters and see a cohesive rendered procedural world | F-01 | US-01, FR-001, FR-002, FR-009 | proposed |
| S-02 | deterministic-survival-loop | visitor can run, pause, accelerate, and observe a deterministic needs-driven survival loop | S-01 | US-01, FR-003, FR-004, FR-005, FR-008, FR-009 | proposed |
| S-03 | predator-prey-dynamics | visitor can observe herbivores feeding and predators hunting while prey flee | S-02 | US-01, FR-006, FR-009 | proposed |
| S-04 | inherited-evolution | visitor can observe reproduction, inherited genomes, mutation, and trait trade-offs | S-02 | US-01, FR-007, FR-009 | proposed |
| S-05 | ecosystem-statistics | visitor can follow population, birth, death, lifespan, and average genome-trait statistics over time | S-03, S-04 | US-01, FR-010 | proposed |
| S-06 | organism-inspection | visitor can select an organism and inspect its state, action, genome, parents, and children | S-04 | US-01, FR-011 | proposed |
| S-07 | limited-god-mode | visitor can perform limited interventions and observe the ecosystem's response | S-03, S-04 | US-01, FR-012 | proposed |

## Baseline

What's already in place in the codebase as of `2026-08-26` (auto-researched + user-confirmed). Foundations below assume present capabilities are available and do not re-scaffold them.

- **Frontend:** present per `tech-stack.md` — React and TypeScript with Vite and PixiJS are the declared UI and rendering stack.
- **Backend / API:** present per `tech-stack.md` — ASP.NET Core is the declared API foundation, with SignalR planned as the sampled-state adapter.
- **Data:** absent — no database driver, object-relational mapper, schema, migrations, or seeded data; persistence is outside MVP scope.
- **Auth:** absent per `tech-stack.md` — intentional because the PRD specifies a public demo without accounts or roles.
- **Deploy / infra:** present per `tech-stack.md` — Azure hosting targets and a GitHub Actions deployment flow are declared.
- **Observability:** partial — built-in backend log-level configuration exists, but metrics, tracing, error tracking, dashboards, health checks, and frontend instrumentation are absent.

## Foundations

### F-01: Determinism and performance verification contract

- **Outcome:** (foundation) deterministic result equivalence and headless throughput can be measured before simulation behavior expands.
- **Change ID:** determinism-performance-contract
- **PRD refs:** PRD §Non-Functional Requirements
- **Unlocks:** S-01; deterministic verification for S-02, S-03, and S-04; headless throughput verification for S-02
- **Prerequisites:** —
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Without a minimal repeatability and throughput measurement contract, learning from later mechanics could be invalidated by silent nondeterminism or rendering-coupled performance.
- **Status:** done

## Slices

### S-01: Configure and generate a procedural world

- **Outcome:** visitor can configure world parameters and see a cohesive rendered procedural world.
- **Change ID:** configurable-procedural-world
- **PRD refs:** US-01, FR-001, FR-002, FR-009
- **Prerequisites:** F-01
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Establishing a reproducible initial world before live behavior isolates generation defects and gives later mechanics a stable visual stage.
- **Status:** proposed

### S-02: Run and observe a deterministic survival loop

- **Outcome:** visitor can run, pause, accelerate, and observe a deterministic needs-driven survival loop.
- **Change ID:** deterministic-survival-loop
- **PRD refs:** US-01, FR-003, FR-004, FR-005, FR-008, FR-009
- **Prerequisites:** S-01
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:** —
- **Risk:** This deliberately exercises the least familiar decision and movement mechanics before predation and inheritance make failures harder to interpret.
- **Status:** proposed

### S-03: Observe predator-prey dynamics

- **Outcome:** visitor can observe herbivores feeding and predators hunting while prey flee.
- **Change ID:** predator-prey-dynamics
- **PRD refs:** US-01, FR-006, FR-009
- **Prerequisites:** S-02
- **Parallel with:** S-04
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Adding pursuit only after the survival loop works keeps combat behavior attributable while delivering a strong visual demonstration.
- **Status:** proposed

### S-04: Observe inherited evolution

- **Outcome:** visitor can observe reproduction, inherited genomes, mutation, and trait trade-offs.
- **Change ID:** inherited-evolution
- **PRD refs:** US-01, FR-007, FR-009
- **Prerequisites:** S-02
- **Parallel with:** S-03
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Sequencing inheritance after stable survival behavior makes population change meaningful without coupling its discovery to predator implementation.
- **Status:** proposed

### S-05: Follow ecosystem statistics

- **Outcome:** visitor can follow population, birth, death, lifespan, and average genome-trait statistics over time.
- **Change ID:** ecosystem-statistics
- **PRD refs:** US-01, FR-010
- **Prerequisites:** S-03, S-04
- **Parallel with:** S-06, S-07
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Building the complete statistics view after predation and inheritance exist ensures polished charts describe real ecosystem events rather than provisional signals.
- **Status:** proposed

### S-06: Inspect an organism and its family

- **Outcome:** visitor can select an organism and inspect its state, action, genome, parents, and children.
- **Change ID:** organism-inspection
- **PRD refs:** US-01, FR-011
- **Prerequisites:** S-04
- **Parallel with:** S-03, S-05, S-07
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Waiting until inherited relationships exist avoids a temporary inspector contract while still exposing behavior details before optional interventions.
- **Status:** proposed

### S-07: Intervene through limited God Mode

- **Outcome:** visitor can perform limited interventions and observe the ecosystem's response.
- **Change ID:** limited-god-mode
- **PRD refs:** US-01, FR-012
- **Prerequisites:** S-03, S-04
- **Parallel with:** S-05, S-06
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Keeping optional intervention behind autonomous ecosystem behavior prevents experiment controls from masking whether emergence works on its own.
- **Status:** proposed

## Backlog Handoff

| Roadmap ID | Change ID | Suggested issue title | Ready for `/plan` | Notes |
|---|---|---|---|---|
| F-01 | determinism-performance-contract | Establish deterministic and performance verification contract | yes | Run `/plan determinism-performance-contract` |
| S-01 | configurable-procedural-world | Let visitors configure and generate a procedural world | no | Requires F-01 |
| S-02 | deterministic-survival-loop | Let visitors run and observe a deterministic survival loop | no | Requires S-01 |
| S-03 | predator-prey-dynamics | Let visitors observe predator-prey dynamics | no | Requires S-02 |
| S-04 | inherited-evolution | Let visitors observe inherited evolution | no | Requires S-02 |
| S-05 | ecosystem-statistics | Let visitors follow ecosystem statistics | no | Requires S-03 and S-04 |
| S-06 | organism-inspection | Let visitors inspect organisms and family relationships | no | Requires S-04 |
| S-07 | limited-god-mode | Let visitors intervene through limited God Mode | no | Requires S-03 and S-04 |

## Open Roadmap Questions

—

## Parked

- **Weather, seasons, and diseases** — Why parked: PRD §Non-Goals defers environmental expansions beyond the core ecosystem proof.
- **Herd and territory behavior** — Why parked: PRD §Non-Goals defers social behavior.
- **Automatic speciation, color genetics, and an evolution tree** — Why parked: PRD §Non-Goals defers advanced evolutionary visualization and classification.
- **Saved worlds, replay, and a time machine** — Why parked: PRD §Non-Goals excludes persistence and historical state navigation.
- **Accounts and multiplayer** — Why parked: PRD §Non-Goals keeps the MVP as independent public simulations.
- **Complex combat** — Why parked: PRD §Non-Goals limits combat to the simple stated rules.
- **LLM-based or separately scripted AI** — Why parked: PRD §Non-Goals requires behavior to emerge from deterministic simulation rules.
- **Treating limited God Mode as a core completion requirement** — Why parked: PRD §Non-Goals keeps the limited intervention slice optional.

## Done
 
- **F-01: (foundation) deterministic result equivalence and headless throughput can be measured before simulation behavior expands** — Archived 2026-08-26 → `context/archive/2026-08-26-determinism-performance-contract/`. Lesson: —.
