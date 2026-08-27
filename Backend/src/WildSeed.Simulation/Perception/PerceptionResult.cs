namespace WildSeed.Simulation.Perception;

public readonly record struct PerceptionResult((int X, int Y)? FoodTile, (int X, int Y)? WaterTile);
