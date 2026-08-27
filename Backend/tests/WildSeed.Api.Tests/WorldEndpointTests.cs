using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WildSeed.Api.Contracts;
using Xunit;

namespace WildSeed.Api.Tests;

public sealed class WorldEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public WorldEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GenerateWorld_WithValidRequest_ReturnsOkWithSnapshot()
    {
        var request = new GenerateWorldRequest
        {
            Seed = 1337,
            Width = 128,
            Height = 128,
            InitialHerbivores = 50,
            InitialCarnivores = 10,
            VegetationDensity = 0.6f,
            WaterLevel = 0.4f,
            MutationProbability = 0.05f,
            MutationStrength = 0.1f,
        };

        var response = await _client.PostAsJsonAsync("/api/world/generate", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var generated = await response.Content.ReadFromJsonAsync<GenerateWorldResponse>();
        Assert.NotNull(generated);
        var snapshot = generated.StaticWorld;
        Assert.Equal(128, snapshot.Width);
        Assert.Equal(128, snapshot.Height);
        Assert.Equal(128, snapshot.Tiles.Length);
        Assert.Equal(128, snapshot.Tiles[0].Length);
        Assert.Equal(60, snapshot.Organisms.Length);
        Assert.StartsWith("v1:", snapshot.Fingerprint);
        Assert.StartsWith("v3:", generated.Snapshot.Fingerprint);
    }

    [Fact]
    public async Task GenerateWorld_WithEmptyBody_AppliesDefaultsAndReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/api/world/generate", new GenerateWorldRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var generated = await response.Content.ReadFromJsonAsync<GenerateWorldResponse>();
        Assert.NotNull(generated);
        var snapshot = generated.StaticWorld;
        Assert.Equal(128, snapshot.Width);
        Assert.Equal(128, snapshot.Height);
        Assert.NotEmpty(snapshot.Fingerprint);
    }

    [Fact]
    public async Task GenerateWorld_SameSeedAndConfig_ReturnsIdenticalFingerprint()
    {
        var request = new GenerateWorldRequest
        {
            Seed = 4242,
            Width = 128,
            Height = 128,
            InitialHerbivores = 30,
            InitialCarnivores = 5,
        };

        var response1 = await _client.PostAsJsonAsync("/api/world/generate", request);
        var response2 = await _client.PostAsJsonAsync("/api/world/generate", request);

        var snapshot1 = await response1.Content.ReadFromJsonAsync<GenerateWorldResponse>();
        var snapshot2 = await response2.Content.ReadFromJsonAsync<GenerateWorldResponse>();

        Assert.NotNull(snapshot1);
        Assert.NotNull(snapshot2);
        Assert.Equal(snapshot1.StaticWorld.Fingerprint, snapshot2.StaticWorld.Fingerprint);
    }

    [Fact]
    public async Task GenerateWorld_WithInvalidConfig_ReturnsBadRequest()
    {
        var request = new GenerateWorldRequest
        {
            Width = 32, // Invalid: min is 64
        };

        var response = await _client.PostAsJsonAsync("/api/world/generate", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void SimulationSession_AdvancesAndTracksPredationDeaths()
    {
        var tiles = new Domain.Terrain.Tile[64, 64];
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
            tiles[x, y] = new Domain.Terrain.Tile(x, y, Domain.Terrain.TerrainType.Grass, 0.0f);

        var config = new Domain.World.WorldConfiguration(42, 64, 64, 1, 1, 0.0f, 0.1f, 0.05f, 0.1f);
        var world = new Domain.World.WorldMap(config, tiles,
        [
            new Domain.Organisms.Organism(Guid.NewGuid(), Domain.Organisms.Species.Carnivore, new Domain.Organisms.Genome(1.0f), 10.0f, 10.0f),
            new Domain.Organisms.Organism(Guid.NewGuid(), Domain.Organisms.Species.Herbivore, new Domain.Organisms.Genome(1.0f), 10.5f, 10.5f)
        ]);

        var state = WildSeed.Simulation.Engine.SimulationStateFactory.Create(world);
        state.Organisms[0].Needs = new Domain.Organisms.OrganismNeeds(hunger: 500, thirst: 0, energy: 800);

        var session = new WildSeed.Api.SimulationHosting.SimulationSession("test-token", state);
        session.Start("1x");

        var result = session.Advance();
        Assert.NotNull(result);

        var response = session.CreateResponse();
        Assert.True(response.Deaths.ContainsKey("Predation") || response.Population == 1);
    }
}
