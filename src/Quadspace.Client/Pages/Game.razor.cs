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
        return new RenderModel(_engine.ShipX, _engine.ShipY, Config.Ship.Radius);
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

    /// <summary>Minimal per-frame render payload marshaled to JS (camelCase).</summary>
    public sealed record RenderModel(double ShipX, double ShipY, double ShipRadius);
}
