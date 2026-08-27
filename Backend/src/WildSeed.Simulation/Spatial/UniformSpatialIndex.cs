using WildSeed.Simulation.Engine;

namespace WildSeed.Simulation.Spatial;

public sealed class UniformSpatialIndex
{
    private readonly Dictionary<(int X, int Y), List<OrganismState>> _cells = [];

    public void Rebuild(IEnumerable<OrganismState> organisms)
    {
        _cells.Clear();
        foreach (var organism in organisms.OrderBy(item => item.Id))
        {
            var key = ((int)MathF.Floor(organism.X), (int)MathF.Floor(organism.Y));
            if (!_cells.TryGetValue(key, out var cell))
            {
                cell = [];
                _cells.Add(key, cell);
            }
            cell.Add(organism);
        }
    }

    public IReadOnlyList<OrganismState> Query(float x, float y, int radius) => _cells
        .Where(pair => Math.Abs(pair.Key.X - x) <= radius && Math.Abs(pair.Key.Y - y) <= radius)
        .OrderBy(pair => pair.Key.Y).ThenBy(pair => pair.Key.X)
        .SelectMany(pair => pair.Value).OrderBy(item => item.Id).ToArray();
}
