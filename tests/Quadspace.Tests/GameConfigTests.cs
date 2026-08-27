using System.Text.Json;
using Quadspace.Core.Configuration;

namespace Quadspace.Tests;

public sealed class GameConfigTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GameConfig_DeserializesFromShippedJson_AllParametersPopulated()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "game-config.json");
        await using var stream = File.OpenRead(path);

        var config = await JsonSerializer.DeserializeAsync<GameConfig>(stream, JsonOptions);

        Assert.NotNull(config);
    }

    [Fact]
    public void ControlsConfig_Absent_UsesDefaults()
    {
        // The controls section is optional; older configs without it must still deserialize.
        const string json = """
            {
              "arena": { "width": 1280, "height": 720 },
              "ship": { "speed": 320, "radius": 16, "startLives": 3, "invulnerabilitySeconds": 2.0 },
              "sphere": { "speed": 140, "radius": 16, "spawnRatePerLevelPerSecond": 1.0, "shrinkSeconds": 0.15, "spawnOnBeat": true, "beatPulse": 0.18 },
              "projectile": { "speed": 640, "radius": 4, "maxOnScreen": 64, "cooldownSeconds": 0.12 },
              "scoring": { "pointsPerSphere": 8, "spheresPerLevelMultiplier": 16 },
              "lives": { "extraLifeEveryLevels": 0, "lifeSphereSpawnChance": 0.02, "maxLives": 100 },
              "levels": { "introSeconds": 2.0 },
              "starfield": { "layers": 3, "starsPerLayer": 60 },
              "audio": { "beatsPerMinute": 128, "secondLayer": true }
            }
            """;

        var config = JsonSerializer.Deserialize<GameConfig>(json, JsonOptions);

        Assert.NotNull(config);
        Assert.Null(config!.Controls);
        // Callers fall back to ControlsConfig defaults when the section is absent.
        var defaults = new ControlsConfig();
        Assert.Equal(0.15, defaults.JoystickDeadZone);
        Assert.True(defaults.AdaptArenaToScreenOnTouch);
    }

    [Fact]
    public void ControlsConfig_Present_IsParsed()
    {
        const string json = """{ "joystickDeadZone": 0.25, "adaptArenaToScreenOnTouch": false }""";

        var controls = JsonSerializer.Deserialize<ControlsConfig>(json, JsonOptions);

        Assert.NotNull(controls);
        Assert.Equal(0.25, controls!.JoystickDeadZone);
        Assert.False(controls.AdaptArenaToScreenOnTouch);
    }
}
