using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Quadspace.Core.Configuration;
using Quadspace.Core.Engine;

namespace Quadspace.Client.Pages;

public partial class Game : IAsyncDisposable
{
    [Inject]
    private GameConfig Config { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private GameEngine _engine = default!;
    private ElementReference _canvas;
    private DotNetObjectReference<Game>? _selfRef;
    private IJSObjectReference? _module;
    private IJSObjectReference? _loop;
    private int _width;
    private int _height;

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
        _module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/game.js");
        _loop = await _module.InvokeAsync<IJSObjectReference>(
            "start",
            _canvas,
            new { width = _width, height = _height },
            new { layers = Config.Starfield.Layers, starsPerLayer = Config.Starfield.StarsPerLayer },
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

    private RenderModel BuildRenderModel()
    {
        var spheres = new List<SphereModel>(_engine.Spheres.Count);
        foreach (var s in _engine.Spheres)
        {
            spheres.Add(new SphereModel(s.X, s.Y, s.Radius * s.ShrinkFraction));
        }

        var projectiles = new List<ProjectileModel>(_engine.Projectiles.Count);
        foreach (var p in _engine.Projectiles)
        {
            projectiles.Add(new ProjectileModel(p.X, p.Y, p.Radius));
        }

        return new RenderModel(
            _engine.ShipX,
            _engine.ShipY,
            Config.Ship.Radius,
            spheres,
            projectiles,
            _engine.Score,
            _engine.Level,
            _engine.Lives);
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
        int Lives);

    /// <summary>A sphere to draw (radius already reflects any shrink animation).</summary>
    public sealed record SphereModel(double X, double Y, double Radius);

    /// <summary>A projectile to draw.</summary>
    public sealed record ProjectileModel(double X, double Y, double Radius);
}
