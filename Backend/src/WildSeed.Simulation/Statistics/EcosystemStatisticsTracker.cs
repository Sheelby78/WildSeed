using WildSeed.Domain.Organisms;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Events;

namespace WildSeed.Simulation.Statistics;

public sealed class EcosystemStatisticsTracker
{
    private readonly HistoryRingBuffer<SimulationHistoryPoint> _history;
    private readonly Dictionary<string, int> _deathsByCause = new();
    private int _totalBirths;
    private int _totalDeaths;
    private int _windowBirths;
    private int _windowDeaths;

    private long _totalLifespanTicksSum;
    private int _totalLifespanCount;
    private long _herbivoreLifespanTicksSum;
    private int _herbivoreLifespanCount;
    private long _carnivoreLifespanTicksSum;
    private int _carnivoreLifespanCount;
    private float _maxLifespanTicks;

    public EcosystemStatisticsTracker(int capacity = 500, int sampleCadenceTicks = 10)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleCadenceTicks);

        SampleCadenceTicks = sampleCadenceTicks;
        _history = new HistoryRingBuffer<SimulationHistoryPoint>(capacity);
    }

    public int SampleCadenceTicks { get; }
    public int TotalBirths => _totalBirths;
    public int TotalDeaths => _totalDeaths;
    public int WindowedBirths => _windowBirths;
    public int WindowedDeaths => _windowDeaths;
    public HistoryRingBuffer<SimulationHistoryPoint> History => _history;

    public void RecordBirths(IReadOnlyList<OrganismBorn> births)
    {
        _totalBirths += births.Count;
        _windowBirths += births.Count;
    }

    public void RecordDeaths(IReadOnlyList<OrganismDied> deaths)
    {
        _totalDeaths += deaths.Count;
        _windowDeaths += deaths.Count;

        for (int i = 0; i < deaths.Count; i++)
        {
            var death = deaths[i];
            string cause = death.Cause.ToString();
            _deathsByCause[cause] = _deathsByCause.GetValueOrDefault(cause) + 1;

            if (death.AgeTicks >= 0)
            {
                _totalLifespanTicksSum += death.AgeTicks;
                _totalLifespanCount++;

                if (death.AgeTicks > _maxLifespanTicks)
                {
                    _maxLifespanTicks = death.AgeTicks;
                }

                if (death.Species == Species.Herbivore)
                {
                    _herbivoreLifespanTicksSum += death.AgeTicks;
                    _herbivoreLifespanCount++;
                }
                else if (death.Species == Species.Carnivore)
                {
                    _carnivoreLifespanTicksSum += death.AgeTicks;
                    _carnivoreLifespanCount++;
                }
            }
        }
    }

    public void SampleTick(SimulationState state)
    {
        if (state.Tick % SampleCadenceTicks == 0)
        {
            int herbivoreCount = 0;
            int carnivoreCount = 0;
            for (int i = 0; i < state.Organisms.Count; i++)
            {
                if (state.Organisms[i].Species == Species.Herbivore)
                {
                    herbivoreCount++;
                }
                else if (state.Organisms[i].Species == Species.Carnivore)
                {
                    carnivoreCount++;
                }
            }

            var herbTraits = ComputeTraitStatistics(state.Organisms, Species.Herbivore);
            var carnTraits = ComputeComputeTraitStatistics(state.Organisms, Species.Carnivore);

            _history.Add(new SimulationHistoryPoint(
                state.Tick,
                state.Organisms.Count,
                herbivoreCount,
                carnivoreCount,
                _windowBirths,
                _windowDeaths,
                herbTraits,
                carnTraits));

            _windowBirths = 0;
            _windowDeaths = 0;
        }
    }

    public TraitStatistics ComputeTraitStatistics(IReadOnlyList<OrganismState> organisms, Species? species = null)
    {
        float sumSpeed = 0;
        float sumSize = 0;
        float sumVision = 0;
        int count = 0;

        for (int i = 0; i < organisms.Count; i++)
        {
            var org = organisms[i];
            if (species is null || org.Species == species.Value)
            {
                sumSpeed += org.Genome.Speed;
                sumSize += org.Genome.Size;
                sumVision += org.Genome.Vision;
                count++;
            }
        }

        return count > 0
            ? new TraitStatistics(sumSpeed / count, sumSize / count, sumVision / count)
            : new TraitStatistics(0f, 0f, 0f);
    }

    private TraitStatistics ComputeComputeTraitStatistics(IReadOnlyList<OrganismState> organisms, Species species) =>
        ComputeTraitStatistics(organisms, species);

    public MortalityStatistics GetMortalityStatistics()
    {
        float avgLifespan = _totalLifespanCount > 0 ? (float)_totalLifespanTicksSum / _totalLifespanCount : 0f;
        float herbAvgLifespan = _herbivoreLifespanCount > 0 ? (float)_herbivoreLifespanTicksSum / _herbivoreLifespanCount : 0f;
        float carnAvgLifespan = _carnivoreLifespanCount > 0 ? (float)_carnivoreLifespanTicksSum / _carnivoreLifespanCount : 0f;

        return new MortalityStatistics(
            _totalDeaths,
            new Dictionary<string, int>(_deathsByCause),
            avgLifespan,
            _maxLifespanTicks,
            herbAvgLifespan,
            carnAvgLifespan);
    }

    public EcosystemStatisticsSummary GetSummary(SimulationState state)
    {
        int herbivoreCount = 0;
        int carnivoreCount = 0;
        for (int i = 0; i < state.Organisms.Count; i++)
        {
            if (state.Organisms[i].Species == Species.Herbivore)
            {
                herbivoreCount++;
            }
            else if (state.Organisms[i].Species == Species.Carnivore)
            {
                carnivoreCount++;
            }
        }

        var overallTraits = ComputeTraitStatistics(state.Organisms);
        var herbTraits = ComputeTraitStatistics(state.Organisms, Species.Herbivore);
        var carnTraits = ComputeTraitStatistics(state.Organisms, Species.Carnivore);
        var mortality = GetMortalityStatistics();

        return new EcosystemStatisticsSummary(
            state.Organisms.Count,
            herbivoreCount,
            carnivoreCount,
            overallTraits,
            herbTraits,
            carnTraits,
            mortality,
            _totalBirths,
            _totalDeaths,
            _windowBirths,
            _windowDeaths);
    }
}
