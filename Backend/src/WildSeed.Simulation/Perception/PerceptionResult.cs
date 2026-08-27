namespace WildSeed.Simulation.Perception;

public readonly record struct PerceptionResult(
    (int X, int Y)? FoodTile,
    (int X, int Y)? WaterTile,
    (float X, float Y)? NearestThreat = null,
    (float X, float Y)? NearestPrey = null,
    Guid? PreyId = null,
    (float X, float Y)? NearestMate = null,
    Guid? MateId = null);
