using SH.Content;
using SH.Framework.IO;
using SH.Framework.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Modding.Models;

public sealed class ConfigJsonFile
{
    public static ConfigJsonFile GetDefaultForSteam()
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

    public static ConfigJsonFile GetDefaultForGOG()
    {
        ConfigJsonFile config = new()
        {
            ClassPath = [$"{SpaceHavenConstants.SPACEHAVEN_JAR}",],
            MainClass = "fi.bugbyte.spacehaven.gog.SpacehavenGOG",
            VMArgs = ["-Xmx4G",],
        };
        if (OS.IsMac)
            config.VMArgs.Insert(0, "-XstartOnFirstThread");
        return config;
    }

    public static async Task<ConfigJsonFile> TryLoadAsync(string configJsonPath, ILogger log, CancellationToken ct)
    {
        string content = await IOUtils.TryReadAllTextAsync(configJsonPath, log, ct);
        if (content == null)
        {
            log?.Error($"Unable to read template config.json file");
            return null;
        }
        
        ConfigJsonFile config = JsonSerializer.Deserialize<ConfigJsonFile>(content);
        if (config == null)
        {
            log?.Error($"Unable to parse template config.json file content: {Environment.NewLine}{content}");
            return null;
        }
        return config;
    }

    [JsonPropertyName("classPath")]
    [JsonPropertyOrder(0)]
    public List<string> ClassPath { get; set; } = [];

    [JsonPropertyName("mainClass")]
    [JsonPropertyOrder(1)]
    public string MainClass { get; set; }

    [JsonPropertyName("vmArgs")]
    [JsonPropertyOrder(2)]
    public List<string> VMArgs { get; set; } = [];

    // For any other future fields which are not yet accounted for:
    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; } = [];

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
