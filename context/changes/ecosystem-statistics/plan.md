# Ecosystem Statistics Implementation Plan

## Overview

Implement roadmap slice S-05: ecosystem statistics. The simulation will deterministically aggregate time-series population dynamics, birth and death rates, cause-specific mortality breakdowns, rolling lifespans, and average genome traits (`Speed`, `Size`, `Vision`) split by species into a fixed-capacity ring buffer. A collapsible, multi-tabbed Analytics panel built with Recharts will visualize these trends in real time without impacting rendering performance.

## Current State Analysis

In S-04 (`inherited-evolution`), organisms evolve across generations through multi-trait genomes with sexual reproduction, trait crossover, and mutation deltas.
Currently:
- `SimulationSession` only maintains simple instantaneous counts (`Population`, `Herbivores`, `Carnivores`), an active actions dictionary, and a cumulative dictionary of death causes (`_deathCounts`).
- `SimulationEngine` emits `OrganismBorn` and `OrganismDied` events during `AdvanceTick()`, but does not aggregate historical time series, birth rates, mortality rates, or average lifespan data.
- Average genome traits are calculated ad-hoc on the frontend for `Avg Gen`, but historical trait drift (`Speed`, `Size`, `Vision`) is neither sampled nor charted over time.
- The frontend UI displays a minimal HUD overlay with raw numbers and text lines (`survival-telemetry`), but lacks graphical time-series charts, tabs, or deep demographic insights as required by FR-010.

## Desired End State

After this plan is complete:
1. **Deterministic Metric Tracking**: `EcosystemStatisticsTracker` in `WildSeed.Simulation` records:
   - Population counts (total, herbivores, carnivores).
   - Trait averages (speed, size, vision) per species and overall.
   - Cumulative and windowed births and deaths with cause breakdowns (`Starvation`, `Dehydration`, `OldAge`, `Predation`, `Combat`).
   - Rolling and lifetime average organism lifespan in ticks (overall, herbivores, carnivores).
2. **Fixed-Size Ring Buffer History**: Simulation stores a fixed-capacity history buffer (e.g. 500 samples, sampled every 10 ticks = 5,000 ticks of history) ensuring zero unbounded memory growth even in long simulations or `MAX` speed mode.
3. **SignalR API Streaming**: API DTOs include `EcosystemStatisticsSummary` in fast snapshots and provide batched time-series history points (`SimulationHistoryPoint`) over SignalR.
4. **Interactive Analytics Panel**: A collapsible drawer on the right side of the canvas features Recharts-powered interactive charts:
   - **Overview Tab**: Key performance indicators, current trophic balance, birth/death rates, and average lifespan.
   - **Population Dynamics Tab**: Multi-line area chart tracking herbivore, carnivore, and total population curves.
   - **Genetics & Trait Drift Tab**: Line charts tracking evolutionary drift for `Speed`, `Size`, and `Vision` per species.
   - **Mortality & Demographics Tab**: Breakdown of mortality causes and lifespan distributions.
5. **Aesthetic Sci-Fi UI**: Matches Wild Seed dark neon visual theme, responsive to screen resize, with tooltips and smooth transitions.

### Key Discoveries

- `Backend/src/WildSeed.Simulation/Engine/SimulationEngine.cs:116-125`: `SimulationEngine` produces `births` (`OrganismBorn`) and `environmentalDeaths`/`predationDeaths` (`OrganismDied`) in every tick. Passing these to a tracker provides zero-overhead event capture.
- `Backend/src/WildSeed.Simulation/Events/OrganismDied.cs:5`: `OrganismDied` event already contains `AgeTicks` and `Species`, which directly enables calculating precise lifespan metrics on death without polling living organisms.
- `Backend/src/WildSeed.Api/SimulationHosting/SimulationSession.cs:29-56`: `SimulationSession.CreateResponse()` can attach current statistics and synchronize history buffer efficiently to SignalR clients.
- `Frontend/src/app/App.tsx:112-202`: The right side of the canvas viewport is currently open, providing the natural placement for a collapsible `<StatisticsPanel />`.

## What We're NOT Doing

- No database persistence or exporting CSV/JSON historical logs to disk (MVP persists in-memory per session).
- No genealogical tree graph / ancestor network visualization (belongs to S-06: `organism-inspection`).
- No interactive God Mode controls or environment tweaks from the stats panel (belongs to S-07: `limited-god-mode`).
- No complex statistical regression or machine-learning population forecasting models.

## Implementation Approach

1. **Domain & Simulation Analytics Engine**:
   - Create value objects in `WildSeed.Simulation.Statistics`: `TraitStatistics`, `MortalityStatistics`, `SimulationHistoryPoint`, `EcosystemStatisticsSummary`.
   - Implement `EcosystemStatisticsTracker` with a circular ring buffer (`HistoryRingBuffer<SimulationHistoryPoint>`), recording periodic samples every 10 ticks up to 500 entries.
   - Calculate rolling lifespan averages upon each `OrganismDied` event.
2. **Engine & API Integration**:
   - Integrate `EcosystemStatisticsTracker` into `SimulationEngine.AdvanceTick()`.
   - Expose statistics through `SimulationSnapshotResponse` and `SimulationSession`.
   - Ensure determinism: statistical tracking reads state without modifying simulation RNG or entity state.
3. **Frontend Dependency & Contract Layer**:
   - Add `recharts` to `Frontend/package.json`.
   - Define TypeScript interfaces in `Frontend/src/transport/types.ts` for history points, trait statistics, and mortality stats.
   - Update `SimulationConnection` to receive and store statistics updates.
4. **Frontend Analytics Component**:
   - Create `StatisticsPanel` in `Frontend/src/features/statistics/StatisticsPanel.tsx`.
   - Create sub-tabs: Overview, Population, Genetics, Mortality.
   - Style with custom CSS (`StatisticsPanel.css`) adhering to the dark cyberpunk design system with glowing accents, custom tooltip styling, and responsive layout.
5. **Testing & Benchmark Verification**:
   - Unit tests for `EcosystemStatisticsTracker` (ring buffer bounds, lifespan calculation with 0 deaths, trait calculations with 0 organisms).
   - Determinism test ensuring running with/without stats aggregation produces bit-for-bit identical state fingerprints.
   - Frontend build and lint verification.

## Critical Implementation Details

- **Zero-Division Safeguards**: When population of a species drops to 0 (e.g. carnivore extinction), trait averages must gracefully yield 0.0 or baseline without throwing `DivideByZeroException` or producing `NaN` in JSON/charts.
- **Ring Buffer Head/Tail Sequencing**: The ring buffer must return chronological history points (`[oldest -> newest]`) so Recharts renders line charts left-to-right without discontinuity.
- **Render Throttling**: Chart components should memoize datasets and only re-render when new history points or tab selections change to avoid triggering React re-renders on every 15Hz PixiJS canvas update.

---

## Phase 1: Statistics Domain Models & Metric Aggregation

### Overview

Create domain and simulation models for tracking population counts, species-specific trait averages (`Speed`, `Size`, `Vision`), mortality causes, and rolling organism lifespans, along with a deterministic `HistoryRingBuffer` and `EcosystemStatisticsTracker`.

### Changes Required

#### 1. Statistical Data Models

**File**: `Backend/src/WildSeed.Simulation/Statistics/TraitStatistics.cs`
**Intent**: Immutable record holding `AverageSpeed`, `AverageSize`, and `AverageVision` for a group of organisms.
**Contract**: `public sealed record TraitStatistics(float AverageSpeed, float AverageSize, float AverageVision)`.

**File**: `Backend/src/WildSeed.Simulation/Statistics/MortalityStatistics.cs`
**Intent**: Immutable record holding total deaths, cause counts (`Starvation`, `Dehydration`, `OldAge`, `Predation`, `Combat`), and lifespan statistics (`AverageLifespanTicks`, `MaxLifespanTicks`, `HerbivoreAverageLifespanTicks`, `CarnivoreAverageLifespanTicks`).
**Contract**: `public sealed record MortalityStatistics(...)`.

**File**: `Backend/src/WildSeed.Simulation/Statistics/SimulationHistoryPoint.cs`
**Intent**: Single time-series sample holding `long Tick`, `int TotalPopulation`, `int HerbivoreCount`, `int CarnivoreCount`, `int BirthsThisWindow`, `int DeathsThisWindow`, `TraitStatistics HerbivoreTraits`, `TraitStatistics CarnivoreTraits`.
**Contract**: `public sealed record SimulationHistoryPoint(...)`.

#### 2. Ring Buffer & Tracker

**File**: `Backend/src/WildSeed.Simulation/Statistics/HistoryRingBuffer.cs`
**Intent**: Fixed-capacity generic circular buffer that overwrites oldest entries when full and provides `IReadOnlyList<T>` in chronological order.
**Contract**: `public sealed class HistoryRingBuffer<T>(int capacity) : IReadOnlyList<T>`.

**File**: `Backend/src/WildSeed.Simulation/Statistics/EcosystemStatisticsTracker.cs`
**Intent**:
- Maintain cumulative counts (total births, total deaths, cause counts, cumulative lifespan sum/count overall and by species).
- Maintain rolling window counters for recent births/deaths.
- Periodically (every `sampleCadenceTicks`, default 10) compute current trait averages and push `SimulationHistoryPoint` to ring buffer.
**Contract**: `public sealed class EcosystemStatisticsTracker(int capacity = 500, int sampleCadenceTicks = 10)`.

### Success Criteria

#### Automated Verification

- `dotnet build Backend/WildSeed.slnx --no-restore` compiles cleanly.
- Unit tests verify `HistoryRingBuffer` chronological order, capacity limits, and overwrite behavior.
- Unit tests verify `EcosystemStatisticsTracker` handles zero population and zero deaths without exceptions.

#### Manual Verification

- Verify statistics tracker produces consistent summaries for mock tick event streams.

---

## Phase 2: Simulation Engine Integration & API Transport

### Overview

Integrate `EcosystemStatisticsTracker` into `SimulationEngine` and `SimulationSession`. Expose statistics and history data through SignalR snapshot DTOs and API endpoints.

### Changes Required

#### 1. Simulation Engine Wiring

**File**: `Backend/src/WildSeed.Simulation/Engine/SimulationEngine.cs`
**Intent**:
- Instantiate `EcosystemStatisticsTracker`.
- In `AdvanceTick()`, feed births and deaths into the tracker and advance tracker tick.
- Expose `StatisticsTracker` on `SimulationEngine`.
**Contract**: `public EcosystemStatisticsTracker Statistics { get; }`.

#### 2. API DTOs & SignalR Adapter

**File**: `Backend/src/WildSeed.Api/Contracts/EcosystemStatisticsDto.cs`
**Intent**: Define `EcosystemStatisticsSummaryDto`, `TraitStatisticsDto`, `MortalityStatisticsDto`, and `SimulationHistoryPointDto`.
**Contract**: Clean serializable DTO records.

**File**: `Backend/src/WildSeed.Api/Contracts/SimulationSnapshotResponse.cs`
**Intent**: Add `EcosystemStatisticsSummaryDto Statistics` and optional `IReadOnlyList<SimulationHistoryPointDto>? History` to `SimulationSnapshotResponse`.
**Contract**: Snapshot response includes rich statistics summary and history.

**File**: `Backend/src/WildSeed.Api/SimulationHosting/SimulationSession.cs`
**Intent**: Map simulation statistics tracker data to response DTOs and provide history points to connected SignalR clients.
**Contract**: Session publishes statistics payload in snapshot responses.

### Success Criteria

#### Automated Verification

- `dotnet test Backend/tests/WildSeed.Api.Tests/ --no-build` passes cleanly.
- Architecture tests confirm no domain/api layer violations.

#### Manual Verification

- Verify SignalR hub streams snapshots containing `Statistics` summaries and populated `History` series.

---

## Phase 3: Frontend Dependencies & Data Contracts

### Overview

Install `recharts` in the frontend application, define TypeScript data models for all statistics DTOs, and connect statistics state to `SimulationConnection`.

### Changes Required

#### 1. Install Dependencies

**File**: `Frontend/package.json`
**Intent**: Add `recharts` dependency.
**Command**: `npm --prefix Frontend install recharts`.

#### 2. TypeScript Data Contracts

**File**: `Frontend/src/transport/types.ts`
**Intent**:
- Add `TraitStatistics` (`averageSpeed`, `averageSize`, `averageVision`).
- Add `MortalityStatistics` (`totalDeaths`, `deathsByCause`, `averageLifespanTicks`, `maxLifespanTicks`, `herbivoreAverageLifespanTicks`, `carnivoreAverageLifespanTicks`).
- Add `SimulationHistoryPoint` (`tick`, `totalPopulation`, `herbivoreCount`, `carnivoreCount`, `birthsThisWindow`, `deathsThisWindow`, `herbivoreTraits`, `carnivoreTraits`).
- Add `EcosystemStatisticsSummary` to `SimulationSnapshot`.
**Contract**: Type definitions match backend DTOs.

#### 3. State Management

**File**: `Frontend/src/transport/WorldApi.ts` & `Frontend/src/transport/SimulationConnection.ts`
**Intent**: Ensure incoming statistics and historical time series are parsed and propagated to React state.
**Contract**: SignalR connection callback exposes statistics payload.

### Success Criteria

#### Automated Verification

- `npm --prefix Frontend run build` compiles with zero TypeScript errors.
- `npm --prefix Frontend run lint` passes cleanly.

#### Manual Verification

- Verify frontend logs receive valid history arrays and trait averages on world generation and tick streaming.

---

## Phase 4: Interactive Statistics Panel & Charts

### Overview

Build a collapsible, tabbed `StatisticsPanel` component with customized Recharts visualizations for Population Dynamics, Genetics/Trait Drift, and Mortality/Lifespan.

### Changes Required

#### 1. Statistics Panel Component & Sub-views

**File**: `Frontend/src/features/statistics/StatisticsPanel.tsx`
**Intent**:
- Floating collapsible panel on the right edge of the canvas.
- Tab bar with icons/labels: `Overview`, `Population`, `Genetics`, `Mortality`.
- **Overview Tab**: KPI cards showing current population, net growth rate (births vs deaths), average lifespan, and trait radar/bar comparison.
- **Population Tab**: Recharts `ResponsiveContainer` + `AreaChart`/`LineChart` displaying Total, Herbivore, and Carnivore curves over ticks.
- **Genetics Tab**: Recharts `LineChart` showing average `Speed`, `Size`, and `Vision` trends for Herbivores (yellow/green) vs Carnivores (red/purple).
- **Mortality Tab**: Recharts `BarChart` or `PieChart` showing cause of death breakdown, plus rolling average lifespan curve.
**Contract**: `export function StatisticsPanel({ statistics, history, isVisible, onToggle }: StatisticsPanelProps)`.

#### 2. Styling & Theme Integration

**File**: `Frontend/src/features/statistics/StatisticsPanel.css`
**Intent**: Dark glassmorphic styling, neon accent colors (`#facc15` for herbivores, `#ef4444` for carnivores, `#38bdf8` for total, `#a855f7` for vision/traits), custom styled Recharts tooltip and legends matching Wild Seed UI.
**Contract**: Bespoke stylesheet scoped to `.statistics-panel`.

#### 3. Viewport Integration

**File**: `Frontend/src/app/App.tsx`
**Intent**:
- Add toggle button in header / HUD to show/hide the Analytics panel.
- Mount `<StatisticsPanel />` over canvas viewport.
- Maintain responsive layout on small screens.
**Contract**: UI includes seamless statistics toggle and rendering.

### Success Criteria

#### Automated Verification

- `npm --prefix Frontend run build` and `npm --prefix Frontend run lint` succeed without warnings.

#### Manual Verification

- Open public demo in browser, toggle Analytics panel, switch tabs, and verify live chart streaming as the simulation runs across speeds (1x, 5x, 20x, MAX).

---

## Phase 5: Automated Testing, Determinism, & Verification

### Overview

Implement comprehensive unit tests for `HistoryRingBuffer` and `EcosystemStatisticsTracker`, verify determinism integrity, and validate frontend build and performance.

### Changes Required

#### 1. Unit Tests

**File**: `Backend/tests/WildSeed.Simulation.Tests/Statistics/HistoryRingBufferTests.cs`
**Intent**: Test FIFO ring buffer capacity clamping, wrapping, index enumeration, and zero capacity edge cases.
**Contract**: Xunit test suite.

**File**: `Backend/tests/WildSeed.Simulation.Tests/Statistics/EcosystemStatisticsTrackerTests.cs`
**Intent**: Test rolling lifespan calculations, cause of death increments, zero population trait defaults, and sample cadence intervals.
**Contract**: Xunit test suite.

#### 2. Determinism Verification

**File**: `Backend/tests/WildSeed.Simulation.Tests/Determinism/StatisticsDeterminismTests.cs`
**Intent**: Verify that running simulation with statistics tracking enabled produces exact golden fingerprints identical to uninstrumented baseline runs.
**Contract**: Fingerprint equality assertions across seeds.

### Success Criteria

#### Automated Verification

- `dotnet test Backend/WildSeed.slnx --no-build` passes 100% of all unit, integration, and architecture tests.
- `npm --prefix Frontend run build` and `npm --prefix Frontend run lint` pass cleanly.

#### Manual Verification

- Confirm zero FPS drop or UI stutter when Analytics panel is open during fast simulation ticks.

---

## Testing Strategy

### Unit Tests

- `HistoryRingBufferTests`: Ring buffer wrapping, capacity boundaries, enumerator ordering.
- `EcosystemStatisticsTrackerTests`: Trait averaging across species, lifespan updates on death events, birth/death window accumulation.
- `StatisticsDeterminismTests`: Verification that statistical sampling has zero side-effects on simulation RNG or state fingerprint.

### Integration Tests

- Multi-generation simulation test verifying history points populate accurately over 500+ ticks.
- SignalR hub integration test verifying serialized `SimulationSnapshotResponse` contains populated statistics.

### Manual Testing Steps

1. Start API (`dotnet run --project Backend/src/WildSeed.Api`) and Frontend (`npm --prefix Frontend run dev`).
2. Generate a world with standard parameters.
3. Click the **Analytics** toggle button in the viewport header to expand the right statistics panel.
4. Accelerate simulation to 5x or 20x.
5. Switch between **Overview**, **Population**, **Genetics**, and **Mortality** tabs and observe live chart updates.
6. Verify tooltips on hover show exact tick numbers and metric values.
7. Collapse the panel and verify smooth canvas rendering continues unobstructed.

---

## Performance Considerations

- **Fixed Memory Footprint**: Ring buffer capped at 500 history points prevents memory leaks regardless of how long the simulation runs.
- **Zero Simulation Overhead**: Statistical sampling runs at a low tick cadence (every 10 ticks) and reuses existing tick events without redundant spatial traversals.
- **Client React Optimization**: Recharts components are wrapped in memoized containers, avoiding expensive DOM re-renders on high-frequency organism position updates.

---

## References

- PRD requirements: `context/foundation/prd.md` (FR-010, US-01)
- Roadmap definition: `context/foundation/roadmap.md` (S-05)
- Prior evolution plan: `context/changes/inherited-evolution/plan.md`

---

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Statistics Domain Models & Metric Aggregation

#### Automated

- [x] 1.1 Compile backend with TraitStatistics, MortalityStatistics, SimulationHistoryPoint, and HistoryRingBuffer
- [x] 1.2 Unit tests verify HistoryRingBuffer FIFO bounds and EcosystemStatisticsTracker calculations

#### Manual

- [x] 1.3 Verify tracker handles edge cases (zero population, zero deaths) without exceptions

### Phase 2: Simulation Engine Integration & API Transport

#### Automated

- [x] 2.1 Integrate EcosystemStatisticsTracker into SimulationEngine tick loop
- [x] 2.2 API DTOs and SignalR snapshot responses include statistics and history points

#### Manual

- [x] 2.3 Verify SignalR hub streams statistics payloads without performance degradation

### Phase 3: Frontend Dependencies & Data Contracts

#### Automated

- [x] 3.1 Install recharts and add TypeScript data contracts in Frontend
- [x] 3.2 Frontend builds cleanly with new statistics types

#### Manual

- [x] 3.3 Verify SignalR connection updates frontend statistics state

### Phase 4: Interactive Statistics Panel & Charts

#### Automated

- [x] 4.1 Frontend builds cleanly and passes oxlint with StatisticsPanel component

#### Manual

- [x] 4.2 Visual verification of collapsible Analytics panel, tab switching, and live Recharts rendering

### Phase 5: Automated Testing, Determinism, & Verification

#### Automated

- [x] 5.1 Unit tests for statistics tracker and ring buffer pass
- [x] 5.2 Determinism tests confirm statistics tracking does not alter simulation fingerprints
- [x] 5.3 Complete backend test suite passes and frontend lint passes

#### Manual

- [x] 5.4 Verify smooth simulation execution at high speeds (20x/MAX) with Analytics panel open
