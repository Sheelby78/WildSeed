using WildSeed.Domain.Organisms;
using WildSeed.Simulation.Engine;

namespace WildSeed.Simulation.Spatial;

public sealed class SpatialGrid
{
    private readonly int _width;
    private readonly int _height;
    private readonly int _cellSize;
    private readonly int _cols;
    private readonly int _rows;
    private readonly int[] _head;
    private int[] _next;

    public SpatialGrid(int width, int height, int cellSize = 8, int initialCapacity = 10_000)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellSize);

        _width = width;
        _height = height;
        _cellSize = cellSize;
        _cols = Math.Max(1, (width + cellSize - 1) / cellSize);
        _rows = Math.Max(1, (height + cellSize - 1) / cellSize);
        _head = new int[_cols * _rows];
        Array.Fill(_head, -1);
        _next = new int[initialCapacity];
    }

    public void Clear()
    {
        Array.Fill(_head, -1);
    }

    public void Rebuild(IReadOnlyList<OrganismState> organisms)
    {
        Clear();
        if (_next.Length < organisms.Count)
        {
            _next = new int[Math.Max(organisms.Count * 2, 1024)];
        }

        for (int i = 0; i < organisms.Count; i++)
        {
            var organism = organisms[i];
            int cx = Math.Clamp((int)(organism.X / _cellSize), 0, _cols - 1);
            int cy = Math.Clamp((int)(organism.Y / _cellSize), 0, _rows - 1);
            int cell = cy * _cols + cx;
            _next[i] = _head[cell];
            _head[cell] = i;
        }
    }

    public OrganismState? FindNearest(float x, float y, float radius, Species targetSpecies, IReadOnlyList<OrganismState> organisms)
    {
        int minCx = Math.Clamp((int)((x - radius) / _cellSize), 0, _cols - 1);
        int maxCx = Math.Clamp((int)((x + radius) / _cellSize), 0, _cols - 1);
        int minCy = Math.Clamp((int)((y - radius) / _cellSize), 0, _rows - 1);
        int maxCy = Math.Clamp((int)((y + radius) / _cellSize), 0, _rows - 1);

        float radiusSq = radius * radius;
        float bestDistSq = float.PositiveInfinity;
        OrganismState? best = null;

        for (int cy = minCy; cy <= maxCy; cy++)
        {
            for (int cx = minCx; cx <= maxCx; cx++)
            {
                int cell = cy * _cols + cx;
                int current = _head[cell];
                while (current != -1)
                {
                    var organism = organisms[current];
                    if (organism.Species == targetSpecies)
                    {
                        float dx = organism.X - x;
                        float dy = organism.Y - y;
                        float distSq = dx * dx + dy * dy;
                        if (distSq <= radiusSq && (distSq < bestDistSq || (distSq == bestDistSq && (best is null || organism.Id.CompareTo(best.Id) < 0))))
                        {
                            bestDistSq = distSq;
                            best = organism;
                        }
                    }

                    current = _next[current];
                }
            }
        }

        return best;
    }

    public OrganismState? FindNearestEligibleMate(
        float x,
        float y,
        float radius,
        Species species,
        Guid selfId,
        IReadOnlyList<OrganismState> organisms)
    {
        int minCx = Math.Clamp((int)((x - radius) / _cellSize), 0, _cols - 1);
        int maxCx = Math.Clamp((int)((x + radius) / _cellSize), 0, _cols - 1);
        int minCy = Math.Clamp((int)((y - radius) / _cellSize), 0, _rows - 1);
        int maxCy = Math.Clamp((int)((y + radius) / _cellSize), 0, _rows - 1);

        float radiusSq = radius * radius;
        float bestDistSq = float.PositiveInfinity;
        OrganismState? best = null;

        for (int cy = minCy; cy <= maxCy; cy++)
        {
            for (int cx = minCx; cx <= maxCx; cx++)
            {
                int cell = cy * _cols + cx;
                int current = _head[cell];
                while (current != -1)
                {
                    var organism = organisms[current];
                    if (organism.Id != selfId &&
                        organism.Species == species &&
                        organism.AgeTicks >= Contracts.SurvivalRulesV4.MaturationAgeTicks &&
                        organism.ReproductionCooldownTicks <= 0 &&
                        organism.Needs.Energy >= Contracts.SurvivalRulesV4.MatingEnergyThreshold &&
                        organism.Needs.Hunger < Contracts.SurvivalRulesV4.CriticalNeed &&
                        organism.Needs.Thirst < Contracts.SurvivalRulesV4.CriticalNeed)
                    {
                        float dx = organism.X - x;
                        float dy = organism.Y - y;
                        float distSq = dx * dx + dy * dy;
                        if (distSq <= radiusSq && (distSq < bestDistSq || (distSq == bestDistSq && (best is null || organism.Id.CompareTo(best.Id) < 0))))
                        {
                            bestDistSq = distSq;
                            best = organism;
                        }
                    }

                    current = _next[current];
                }
            }
        }

        return best;
    }
}
