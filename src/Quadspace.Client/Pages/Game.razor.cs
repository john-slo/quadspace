using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Quadspace.Core.Configuration;
using Quadspace.Core.Engine;
using Quadspace.Core.Scoring;

namespace Quadspace.Client.Pages;

public partial class Game : IAsyncDisposable
{
    [Inject]
    private GameConfig Config { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Inject]
    private HttpClient Http { get; set; } = default!;

    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    private GameEngine _engine = default!;
    private ElementReference _canvas;
    private DotNetObjectReference<Game>? _selfRef;
    private IJSObjectReference? _module;
    private IJSObjectReference? _loop;
    private int _width;
    private int _height;

    private bool _gameOver;
    private int _finalScore;
    private string _playerName = string.Empty;
    private bool _submitting;
    private string? _submitError;

    protected override void OnInitialized()
    {
        _engine = new GameEngine(Config);
        _width = (int)Config.Arena.Width;
        _height = (int)Config.Arena.Height;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _selfRef = DotNetObjectReference.Create(this);
        // Import with a stable query so the static-web-asset import map (which rewrites "./js/game.js"
        // to a fingerprinted path the host's static-file middleware does not serve) is bypassed and the
        // physical module is loaded directly.
        _module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/game.js?v=1");
        _loop = await _module.InvokeAsync<IJSObjectReference>(
            "start",
            _canvas,
            new { width = _width, height = _height },
            new { layers = Config.Starfield.Layers, starsPerLayer = Config.Starfield.StarsPerLayer },
            new
            {
                beatsPerMinute = Config.Audio.BeatsPerMinute,
                secondLayer = Config.Audio.SecondLayer,
                beatPulse = Config.Sphere.BeatPulse,
            },
            _selfRef);
    }

    /// <summary>Called once per animation frame from JS: advances the engine and returns what to draw.</summary>
    [JSInvokable]
    public RenderModel Tick(double dtSeconds, double moveX, double moveY)
    {
        _engine.Update(dtSeconds, moveX, moveY);
        return BuildRenderModel();
    }

    /// <summary>Fires a shot in the given axis direction (invoked on a non-repeating arrow keydown).</summary>
    [JSInvokable]
    public void Fire(double directionX, double directionY) => _engine.Fire(directionX, directionY);

    /// <summary>Releases beat-quantized sphere spawns (invoked by the audio layer on each beat).</summary>
    [JSInvokable]
    public void OnBeat() => _engine.OnBeat();

    /// <summary>Invoked by JS when the run ends; shows the name-entry overlay.</summary>
    [JSInvokable]
    public void EndGame()
    {
        _gameOver = true;
        _finalScore = _engine.Score;
        StateHasChanged();
    }

    private async Task SubmitScoreAsync()
    {
        var name = _playerName.Trim();
        if (name.Length == 0)
        {
            _submitError = "Enter a name.";
            return;
        }

        _submitting = true;
        _submitError = null;
        try
        {
            await Http.PostAsJsonAsync("api/scores", new ScoreSubmissionRequest(name, _finalScore));
            Nav.NavigateTo("/");
        }
        catch (HttpRequestException)
        {
            _submitError = "Could not save score. Try again.";
            _submitting = false;
        }
    }

    private RenderModel BuildRenderModel()
    {
        var spheres = new List<SphereModel>(_engine.Spheres.Count);
        foreach (var s in _engine.Spheres)
        {
            spheres.Add(new SphereModel(s.X, s.Y, s.Radius * s.ShrinkFraction, s.IsLifeSphere));
        }

        var projectiles = new List<ProjectileModel>(_engine.Projectiles.Count);
        foreach (var p in _engine.Projectiles)
        {
            var length = Math.Sqrt((p.VelocityX * p.VelocityX) + (p.VelocityY * p.VelocityY));
            var dirX = length > 0 ? p.VelocityX / length : 0;
            var dirY = length > 0 ? p.VelocityY / length : 0;
            projectiles.Add(new ProjectileModel(p.X, p.Y, p.Radius, dirX, dirY));
        }

        return new RenderModel(
            _engine.ShipX,
            _engine.ShipY,
            Config.Ship.Radius,
            spheres,
            projectiles,
            _engine.Score,
            _engine.Level,
            _engine.Lives,
            _engine.IsLevelIntro,
            _engine.IsShipInvulnerable,
            _engine.IsGameOver);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_loop is not null)
            {
                await _loop.InvokeVoidAsync("stop");
                await _loop.DisposeAsync();
            }

            if (_module is not null)
            {
                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
            // The browser side is already gone (navigation/teardown) — nothing to clean up.
        }

        _selfRef?.Dispose();
    }

    /// <summary>Per-frame render payload marshaled to JS (camelCase).</summary>
    public sealed record RenderModel(
        double ShipX,
        double ShipY,
        double ShipRadius,
        IReadOnlyList<SphereModel> Spheres,
        IReadOnlyList<ProjectileModel> Projectiles,
        int Score,
        int Level,
        int Lives,
        bool IsLevelIntro,
        bool ShipInvulnerable,
        bool IsGameOver);

    /// <summary>A sphere to draw (radius already reflects any shrink animation).</summary>
    public sealed record SphereModel(double X, double Y, double Radius, bool IsLife);

    /// <summary>A projectile to draw (with normalized travel direction for its tail).</summary>
    public sealed record ProjectileModel(double X, double Y, double Radius, double DirX, double DirY);
}
