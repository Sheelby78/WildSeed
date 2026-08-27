using Microsoft.AspNetCore.Http.HttpResults;
using WildSeed.Api.Contracts;
using WildSeed.Domain.World;
using WildSeed.Simulation.WorldGeneration;

namespace WildSeed.Api.Endpoints;

public static class WorldEndpoints
{
    public static IEndpointRouteBuilder MapWorldEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/world/generate", GenerateWorld)
            .WithName("GenerateWorld");

        return app;
    }

    private static Results<Ok<WorldSnapshotResponse>, ProblemHttpResult> GenerateWorld(
        GenerateWorldRequest? request,
        WorldGenerator generator)
    {
        try
        {
            var req = request ?? new GenerateWorldRequest();
            WorldConfiguration domainConfig = req.ToDomain();

            var world = generator.Generate(domainConfig);
            var fingerprint = WorldFingerprint.Compute(world);

            var response = WorldSnapshotResponse.FromDomain(world, fingerprint);
            return TypedResults.Ok(response);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid world configuration");
        }
    }
}
