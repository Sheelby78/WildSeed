namespace WildSeed.Api.Contracts;

public sealed record OrganismBornDto(
    long Tick,
    string OrganismId,
    string Species,
    float X,
    float Y,
    string? MotherId,
    string? FatherId,
    int Generation,
    GenomeDto Genome);
