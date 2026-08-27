namespace WildSeed.Simulation.Contracts;

public static class SurvivalRulesV2
{
    public const int NeedScale = 1_000;
    public const int MetabolismHunger = 1;
    public const int MetabolismThirst = 1;
    public const int ThirstMetabolismCadenceTicks = 4;
    public const int MovementEnergyCost = 3;
    public const int RestEnergyGain = 18;
    public const int DrinkThirstGain = 400;
    public const int FoodNeedPerVegetationUnit = 8;
    public const int VegetationCapacityPerDensity = 500;
    public const int VegetationRegrowthPerTick = 2;
    public const int PerceptionRadius = 8;
    public const int CriticalNeed = 750;
    public const int ActionNeedThreshold = 150;
    public const int MaximumAgeTicks = 20_000;
    public const int ExplorationCadenceTicks = 20;
}
