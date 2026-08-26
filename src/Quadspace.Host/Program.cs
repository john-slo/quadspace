using Quadspace.Core.Scoring;
using Quadspace.Host;

var builder = WebApplication.CreateBuilder(args);

var configuredDir = builder.Configuration["Scores:Directory"];
var scoresDirectory = string.IsNullOrWhiteSpace(configuredDir)
    ? Path.Combine(builder.Environment.ContentRootPath, "scores")
    : Path.GetFullPath(configuredDir, builder.Environment.ContentRootPath);
builder.Services.AddSingleton(new FileScoreStore(scoresDirectory, TimeProvider.System));

var app = builder.Build();

// Serve the Blazor WebAssembly client via the static-assets endpoint pipeline. MapStaticAssets reads
// the generated endpoints manifest so fingerprinted framework files (e.g. blazor.webassembly.js) are
// resolved from their canonical routes in published/container deployments — UseStaticFiles cannot.
app.MapStaticAssets();

app.MapGet("/api/scores/top", async (FileScoreStore store, int? count, CancellationToken ct) =>
{
    var take = Math.Clamp(count ?? 10, 1, Leaderboard.MaxEntries);
    return Results.Ok(await store.GetTopAsync(take, ct));
});

app.MapPost("/api/scores", async (FileScoreStore store, ScoreSubmissionRequest request, CancellationToken ct) =>
{
    if (!ScoreSubmission.TryNormalize(request.Name, request.Score, out var name, out var error))
    {
        return Results.BadRequest(new { error });
    }

    var response = await store.SubmitAsync(name, request.Score, ct);
    return Results.Ok(response);
});

app.MapFallbackToFile("index.html");

app.Run();
