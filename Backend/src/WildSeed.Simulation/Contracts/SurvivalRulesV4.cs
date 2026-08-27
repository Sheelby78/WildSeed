namespace WildSeed.Simulation.Contracts;

public static class SurvivalRulesV4
{
    public const int NeedScale = 1_000;
    public const int MetabolismHunger = 1;
    public const int MetabolismThirst = 1;
    public const int ThirstMetabolismCadenceTicks = 3;
    public const int HungerMetabolismCadenceTicks = 2;
    public const int MovementEnergyCost = 3;
    public const int SprintEnergyCost = 6;
    public const int RestEnergyGain = 18;
    public const int DrinkThirstGain = 400;
    public const int PredationHungerGain = 500;
    public const int PredationEnergyGain = 200;
    public const int FoodNeedPerVegetationUnit = 8;
    public const int VegetationEnergyGainPerUnit = 4;
    public const int VegetationCapacityPerDensity = 500;
    public const int VegetationRegrowthPerTick = 2;
    public const int PerceptionRadius = 8;
    public const int DangerPerceptionRadius = 8;
    public const int HuntPerceptionRadius = 10;
    public const float AttackRangeSquared = 1.0f;
    public const float SprintSpeedMultiplier = 1.5f;
    public const int CriticalNeed = 750;
    public const int ActionNeedThreshold = 150;
    public const int MaximumAgeTicks = 20_000;
    public const int ExplorationCadenceTicks = 20;

    public const int MaturationAgeTicks = 200;
    public const int MatingEnergyThreshold = 600;
    public const int MatingCooldownTicks = 200;
    public const int MatingEnergyCost = 200;
    public const float MatingRangeSquared = 4.0f;

    public const float MinSpeed = 0.5f;
    public const float MaxSpeed = 3.0f;
    public const float MinSize = 0.5f;
    public const float MaxSize = 2.5f;
    public const float MinVision = 4.0f;
    public const float MaxVision = 16.0f;

    public const int MaxPopulationCap = 5_000;
}
