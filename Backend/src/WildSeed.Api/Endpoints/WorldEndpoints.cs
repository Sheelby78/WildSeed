using Microsoft.AspNetCore.Http.HttpResults;
using WildSeed.Api.Contracts;
using WildSeed.Domain.World;
using WildSeed.Simulation.WorldGeneration;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Determinism;
using WildSeed.Api.SimulationHosting;

namespace WildSeed.Api.Endpoints;

public static class WorldEndpoints
{
    public static IEndpointRouteBuilder MapWorldEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/world/generate", GenerateWorld)
            .WithName("GenerateWorld");

        return app;
    }

    private static Results<Ok<GenerateWorldResponse>, ProblemHttpResult> GenerateWorld(
        GenerateWorldRequest? request,
        WorldGenerator generator,
        SimulationSessionManager sessions)
    {
        try
        {
            var req = request ?? new GenerateWorldRequest();
            WorldConfiguration domainConfig = req.ToDomain();

            var world = generator.Generate(domainConfig);
            var fingerprint = WorldFingerprint.Compute(world);

            var session = sessions.Create(SimulationStateFactory.Create(world));
            var response = new GenerateWorldResponse(session.Token, WorldSnapshotResponse.FromDomain(world, fingerprint), session.CreateResponse());
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
