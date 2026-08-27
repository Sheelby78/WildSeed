namespace WildSeed.Domain.Organisms;

public readonly record struct OrganismNeeds
{
    public const int Minimum = 0;
    public const int Maximum = 1_000;

    public int Hunger { get; }
    public int Thirst { get; }
    public int Energy { get; }

    public OrganismNeeds() : this(200, 50, 800)
    {
    }

    public OrganismNeeds(int hunger = 200, int thirst = 50, int energy = 800)
    {
        Hunger = Math.Clamp(hunger, Minimum, Maximum);
        Thirst = Math.Clamp(thirst, Minimum, Maximum);
        Energy = Math.Clamp(energy, Minimum, Maximum);
    }

    public OrganismNeeds Metabolize(int hunger, int thirst, int energy) => new(Hunger + hunger, Thirst + thirst, Energy - energy);

    public OrganismNeeds Eat(int units) => new(Hunger - units, Thirst, Energy);

    public OrganismNeeds Feed(int hungerUnits, int energyUnits) => new(Hunger - hungerUnits, Thirst, Energy + energyUnits);

    public OrganismNeeds Drink(int units) => new(Hunger, Thirst - units, Energy);

    public OrganismNeeds Rest(int units) => new(Hunger, Thirst, Energy + units);
}
