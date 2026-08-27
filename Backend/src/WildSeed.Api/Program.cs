using WildSeed.Api.Endpoints;
using WildSeed.Simulation.WorldGeneration;
using WildSeed.Api.Hubs;
using WildSeed.Api.SimulationHosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<WorldGenerator>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton(new SimulationHostOptions());
builder.Services.AddSingleton<SimulationSessionManager>();
builder.Services.AddSignalR();
builder.Services.AddHostedService<SimulationRunnerService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("FrontendDev");

app.MapWorldEndpoints();
app.MapHub<SimulationHub>("/hubs/simulation");

app.Run();

public partial class Program { }
