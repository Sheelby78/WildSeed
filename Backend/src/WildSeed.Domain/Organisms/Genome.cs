namespace WildSeed.Domain.Organisms;

public readonly record struct Genome
{
    public float Speed { get; }
    public float Size { get; }
    public float Vision { get; }

    public Genome(float speed = 1.0f, float size = 1.0f, float vision = 8.0f)
    {
        Speed = Math.Clamp(speed, 0.1f, 10.0f);
        Size = Math.Clamp(size, 0.1f, 10.0f);
        Vision = Math.Clamp(vision, 1.0f, 50.0f);
    }
}
