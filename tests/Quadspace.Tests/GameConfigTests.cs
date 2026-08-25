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
}
