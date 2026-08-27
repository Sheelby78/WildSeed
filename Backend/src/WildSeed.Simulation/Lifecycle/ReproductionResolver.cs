using System.Buffers.Binary;
using WildSeed.Domain.Organisms;
using WildSeed.Domain.Terrain;
using WildSeed.Simulation.Behavior;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Events;
using WildSeed.Simulation.Random;

namespace WildSeed.Simulation.Lifecycle;

public static class ReproductionResolver
{
    private static readonly (int Dx, int Dy)[] PlacementOffsets =
    [
        (0, 1), (1, 0), (0, -1), (-1, 0),
        (1, 1), (-1, 1), (1, -1), (-1, -1),
        (0, 0)
    ];

    public static IReadOnlyList<OrganismBorn> Resolve(
        SimulationState state,
        ReadOnlySpan<(OrganismState Organism, ActionIntent Intent)> scored)
    {
        var matedSet = new HashSet<Guid>();
        var births = new List<OrganismBorn>();

        var organismMap = new Dictionary<Guid, OrganismState>(state.Organisms.Count);
        foreach (var org in state.Organisms)
        {
            organismMap[org.Id] = org;
        }

        ulong seed = (ulong)(uint)state.World.Configuration.Seed;
        long epoch = state.Tick;
        Span<byte> guidBytes = stackalloc byte[16];

        foreach (var (parentA, intent) in scored)
        {
            if (intent.Action != OrganismAction.Mate || intent.TargetOrganismId is not { } targetId)
            {
                continue;
            }

            if (matedSet.Contains(parentA.Id) || matedSet.Contains(targetId))
            {
                continue;
            }

            if (!organismMap.TryGetValue(targetId, out var parentB))
            {
                continue;
            }

            if (parentB.Species != parentA.Species || parentA.Id == parentB.Id)
            {
                continue;
            }

            if (parentA.AgeTicks < SurvivalRulesV4.MaturationAgeTicks ||
                parentB.AgeTicks < SurvivalRulesV4.MaturationAgeTicks ||
                parentA.ReproductionCooldownTicks > 0 ||
                parentB.ReproductionCooldownTicks > 0 ||
                parentA.Needs.Energy < SurvivalRulesV4.MatingEnergyThreshold ||
                parentB.Needs.Energy < SurvivalRulesV4.MatingEnergyThreshold)
            {
                continue;
            }

            float dx = parentA.X - parentB.X;
            float dy = parentA.Y - parentB.Y;
            float distSq = dx * dx + dy * dy;

            if (distSq > SurvivalRulesV4.MatingRangeSquared)
            {
                continue;
            }

            matedSet.Add(parentA.Id);
            matedSet.Add(parentB.Id);

            parentA.Needs = parentA.Needs.Metabolize(0, 0, SurvivalRulesV4.MatingEnergyCost);
            parentB.Needs = parentB.Needs.Metabolize(0, 0, SurvivalRulesV4.MatingEnergyCost);
            parentA.ReproductionCooldownTicks = SurvivalRulesV4.MatingCooldownTicks;
            parentB.ReproductionCooldownTicks = SurvivalRulesV4.MatingCooldownTicks;

            if (state.Organisms.Count >= SurvivalRulesV4.MaxPopulationCap)
            {
                continue;
            }

            Guid pairId = parentA.Id.CompareTo(parentB.Id) < 0 ? parentA.Id : parentB.Id;

            float baseSpeed = (parentA.Genome.Speed + parentB.Genome.Speed) / 2.0f;
            float baseSize = (parentA.Genome.Size + parentB.Genome.Size) / 2.0f;
            float baseVision = (parentA.Genome.Vision + parentB.Genome.Vision) / 2.0f;

            float speed = baseSpeed;
            if (DeterministicRandom.NextSingle(seed, epoch, pairId, RandomChannel.ReproductionMutationSpeedChance) < state.World.Configuration.MutationProbability)
            {
                float r = DeterministicRandom.NextSingle(seed, epoch, pairId, RandomChannel.ReproductionMutationSpeed);
                float delta = (r * 2.0f - 1.0f) * state.World.Configuration.MutationStrength;
                speed = Math.Clamp(baseSpeed * (1.0f + delta), SurvivalRulesV4.MinSpeed, SurvivalRulesV4.MaxSpeed);
            }

            float size = baseSize;
            if (DeterministicRandom.NextSingle(seed, epoch, pairId, RandomChannel.ReproductionMutationSizeChance) < state.World.Configuration.MutationProbability)
            {
                float r = DeterministicRandom.NextSingle(seed, epoch, pairId, RandomChannel.ReproductionMutationSize);
                float delta = (r * 2.0f - 1.0f) * state.World.Configuration.MutationStrength;
                size = Math.Clamp(baseSize * (1.0f + delta), SurvivalRulesV4.MinSize, SurvivalRulesV4.MaxSize);
            }

            float vision = baseVision;
            if (DeterministicRandom.NextSingle(seed, epoch, pairId, RandomChannel.ReproductionMutationVisionChance) < state.World.Configuration.MutationProbability)
            {
                float r = DeterministicRandom.NextSingle(seed, epoch, pairId, RandomChannel.ReproductionMutationVision);
                float delta = (r * 2.0f - 1.0f) * state.World.Configuration.MutationStrength;
                vision = Math.Clamp(baseVision * (1.0f + delta), SurvivalRulesV4.MinVision, SurvivalRulesV4.MaxVision);
            }

            int parentTileX = (int)MathF.Floor(parentA.X);
            int parentTileY = (int)MathF.Floor(parentA.Y);
            float spawnX = parentA.X;
            float spawnY = parentA.Y;

            foreach (var (ox, oy) in PlacementOffsets)
            {
                int tx = parentTileX + ox;
                int ty = parentTileY + oy;
                if (tx >= 0 && tx < state.World.Width && ty >= 0 && ty < state.World.Height &&
                    state.World.Tiles[tx, ty].Terrain is not (TerrainType.DeepWater or TerrainType.ShallowWater))
                {
                    spawnX = tx + 0.5f;
                    spawnY = ty + 0.5f;
                    break;
                }
            }

            ulong h1 = DeterministicRandom.NextUInt64(seed, epoch, pairId, RandomChannel.ReproductionGuid);
            ulong h2 = DeterministicRandom.NextUInt64(seed ^ 0x517cc1b727220a95UL, epoch, pairId, RandomChannel.ReproductionGuid);
            BinaryPrimitives.WriteUInt64LittleEndian(guidBytes[..8], h1);
            BinaryPrimitives.WriteUInt64LittleEndian(guidBytes[8..], h2);
            var offspringId = new Guid(guidBytes);

            int generation = Math.Max(parentA.Generation, parentB.Generation) + 1;
            var offspringGenome = new Genome(speed, size, vision);
            var offspring = new OrganismState(
                offspringId,
                parentA.Species,
                offspringGenome,
                spawnX,
                spawnY,
                motherId: parentA.Id,
                fatherId: parentB.Id,
                generation: generation,
                reproductionCooldownTicks: SurvivalRulesV4.MatingCooldownTicks / 2);

            state.Organisms.Add(offspring);
            births.Add(new OrganismBorn(state.Tick, offspringId, parentA.Species, spawnX, spawnY, parentA.Id, parentB.Id, generation, offspringGenome));
        }

        return births;
    }
}
