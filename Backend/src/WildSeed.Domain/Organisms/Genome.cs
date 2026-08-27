namespace WildSeed.Domain.Organisms;

public readonly record struct Genome
{
    public float Speed { get; }

    public Genome(float speed)
    {
        Speed = Math.Clamp(speed, 0.1f, 10.0f);
    }
}
