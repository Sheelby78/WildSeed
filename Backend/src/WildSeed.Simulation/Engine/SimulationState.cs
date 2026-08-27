using WildSeed.Domain.Terrain;
using WildSeed.Domain.World;

namespace WildSeed.Simulation.Engine;

public sealed class SimulationState
{
    private readonly bool[] _drinkingTiles;

    internal SimulationState(WorldMap world, VegetationResource[] vegetation, List<OrganismState> organisms, bool[] drinkingTiles)
    {
        World = world;
        Vegetation = vegetation;
        Organisms = organisms;
        _drinkingTiles = drinkingTiles;
    }

    public WorldMap World { get; }
    public long Tick { get; internal set; }
    internal VegetationResource[] Vegetation { get; }
    internal List<OrganismState> Organisms { get; }
    public int LivingPopulation => Organisms.Count;
    public VegetationResource GetVegetation(int x, int y) => Vegetation[y * World.Width + x];
    public bool IsDrinkingTile(int x, int y) => _drinkingTiles[y * World.Width + x];
}
