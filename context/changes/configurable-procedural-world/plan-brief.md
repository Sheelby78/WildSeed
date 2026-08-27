# Configurable Procedural World — Plan Brief

> Full plan: `context/changes/configurable-procedural-world/plan.md`

## What & Why

Implement S-01, the first full-stack vertical slice of Wild Seed. A visitor can configure world generation parameters (seed, map dimensions, initial populations, vegetation/water density, mutation parameters) through presets or an advanced toggle, generate a deterministic procedural terrain with renewable vegetation and initial organism placement, and observe the rendered world in PixiJS with pan/zoom and regeneration.

## Starting Point

Domain contains only assembly markers. Simulation contains contract v1 determinism and fingerprint infrastructure (`CanonicalStateWriter`, `StateFingerprint`), but no production engine, world model, or RNG. API and Frontend are default scaffold templates.

## Desired End State

Visitors have a polished, interactive web interface showing a procedural 2D tile map rendered via PixiJS. The frontend communicates with a REST endpoint (`POST /api/world/generate`) that invokes a deterministic FastNoiseLite-powered generator in C#. Worlds generated with identical seeds and parameters produce bit-for-bit identical state fingerprints.

## Key Decisions Made

| Decision | Choice | Why | Source |
|---|---|---|---|
| Terrain model | Discrete tile grid (128x128 to 256x256) | Simple spatial queries, natural for grid perception in S-02, deterministic by construction. | Plan |
| Generation algorithm | FastNoiseLite (OpenSimplex2) with elevation thresholds | Single C# file, zero external dependencies, deterministic from seed, natural terrain shapes. | Plan |
| Vegetation model | Per-tile density (0.0 to 1.0) | Integrates smoothly into tile grid, simple color tinting in PixiJS, low per-tick overhead. | Plan |
| World rendering | PixiJS 8 `ParticleContainer` + custom camera | High performance for 16K-65K tiles using white texture tinting; native GPU container pan/zoom. | Plan |
| Transport | REST endpoint (`POST /api/world/generate`) | Simplest request-response pattern for one-shot world generation; SignalR deferred to S-02 tick loop. | Plan |
| Configuration UX | Presets (Island, Continental, Arid, Lush) + Advanced toggle | Friendly initial experience with full parameter exposure for experimenters. | Plan |
| Determinism check | SHA-256 fingerprint via `CanonicalStateWriter` | Leverages verified F-01 hashing contract to prove repeatability across runs. | Plan |

## Scope

**In scope:**
- Domain types: `TerrainType`, `Tile`, `WorldConfiguration`, `WorldMap`, `Organism` (stub), `Genome` (stub), `Species`.
- Simulation generator: `FastNoiseLite`, `WorldGenerator`, `SimulationRandom`, and `WorldFingerprint`.
- API endpoint: `POST /api/world/generate` with DTOs and validation.
- Frontend: Feature-driven architecture, PixiJS 8 tilemap renderer, camera pan/zoom, preset/custom configuration sidebar.
- Determinism and domain unit/integration tests.

**Out of scope:**
- Simulation tick loop, organism movement, metabolism/needs, autonomous behavior (S-02).
- Realtime SignalR streaming (S-02).
- Mutation execution and reproduction mechanics (S-04).
- Frontend automated testing (Vitest deferred).

## Architecture / Approach

```
[ Frontend: React 19 + PixiJS 8 ]
         │
         │ POST /api/world/generate (JSON config)
         ▼
[ API: WildSeed.Api ]
         │
         │ WorldConfiguration DTO -> Domain
         ▼
[ Simulation: WorldGenerator ] ──► [ FastNoiseLite & SimulationRandom ]
         │
         │ Produces WorldMap & Canonical State Fingerprint
         ▼
[ Domain: WildSeed.Domain (WorldMap, Tiles, Organisms) ]
```

## Phases at a Glance

| Phase | What it delivers | Key risk |
|---|---|---|
| 1. Domain World Model | Core value objects and entities for terrain, tiles, map, organisms | Introducing unnecessary framework dependencies into Domain. |
| 2. Simulation Procedural Generation | `FastNoiseLite`, `WorldGenerator`, and `WorldFingerprint` | Nondeterministic float math or coordinate indexing. |
| 3. Backend Tests | Unit tests for configuration validation, RNG, and generation determinism | Incomplete assertion coverage of parameter edge cases. |
| 4. API World Generation Endpoint | `POST /api/world/generate` minimal API endpoint and DTOs | DTO serialization mismatches or missing CORS headers. |
| 5. Frontend Setup & PixiJS Rendering | Directory structure, PixiJS `ParticleContainer` renderer, pan/zoom camera | PixiJS 8 API breaking changes vs legacy v7 patterns. |
| 6. Frontend Configuration UI | Presets, advanced sliders, and API wiring for regeneration | State synchronization between presets and custom inputs. |
| 7. API Integration Test | In-memory integration test via `WebApplicationFactory` | Test hosting configuration differences. |
| 8. Integration & Final Verification | End-to-end build, test execution, and manual verification | Layer dependency violations caught by architecture tests. |

**Prerequisites:** Foundation F-01 complete.
**Estimated effort:** ~1-2 focused sessions across 8 phases.

## Open Risks & Assumptions

- Assumes WebGL is available in the user's browser for PixiJS rendering (Canvas fallback is secondary).
- JSON payload for 256x256 tiles is estimated at ~500KB-1MB, which is lightweight for a modern local/web connection.

## Success Criteria (Summary)

- Independent world generations with the same seed and parameters produce identical SHA-256 state fingerprints.
- The visitor can select presets or adjust sliders, click "Generate World", and see the terrain rendered smoothly in PixiJS.
- The viewport supports fluid panning and zooming across the generated map.
- All backend unit, integration, and architecture tests pass cleanly.
