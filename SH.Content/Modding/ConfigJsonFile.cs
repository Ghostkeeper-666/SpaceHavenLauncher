using SH.Framework.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SH.Content.Modding;

public sealed class ConfigJsonFile
{
    public static ConfigJsonFile GetOriginal()
    {
        ConfigJsonFile config = new()
        {
            ClassPath = [$"{SpaceHavenConstants.SPACEHAVEN_JAR}",],
            MainClass = "fi.bugbyte.spacehaven.steam.SpacehavenSteam",
            VMArgs = ["-Xmx4G",],
        };
        if (OS.IsMac)
            config.VMArgs.Insert(0, "-XstartOnFirstThread");
        return config;
    }

    [JsonPropertyName("classPath")]
    [JsonPropertyOrder(0)]
    public List<string> ClassPath { get; set; } = [];

    [JsonPropertyName("mainClass")]
    [JsonPropertyOrder(1)]
    public string MainClass { get; set; } = "fi.bugbyte.spacehaven.steam.SpacehavenSteam";

    [JsonPropertyName("vmArgs")]
    [JsonPropertyOrder(2)]
    public List<string> VMArgs { get; set; } = [];

    public string ToJsonString() => JsonSerializer.Serialize(this, new JsonSerializerOptions
    {
        WriteIndented = true,
        IndentCharacter = ' ',
        IndentSize = 2,
        AllowTrailingCommas = false,
    });

    public override string ToString() =>
        ToJsonString().Replace("\n", " ").Replace("  ", " ").Replace("  ", " ");
}
