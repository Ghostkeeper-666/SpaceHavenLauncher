using SH.Content.Enums;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SH.Modding.Models;

public sealed class ModsJsonFile
{
    [JsonPropertyOrder(0)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string SchemaVersion { get; set; } = "1";

    [JsonPropertyOrder(1)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public BasePaths BasePaths { get; set; } = new();

    [JsonPropertyOrder(2)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public GameEnvironment GameEnvironment { get; set; } = new();

    [JsonPropertyOrder(3)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public List<ModInfo> Mods { get; set; } = [];



    public string ToJsonString() => JsonSerializer.Serialize(this, new JsonSerializerOptions
    {
        WriteIndented = true,
        IndentCharacter = ' ',
        IndentSize = 2,
        AllowTrailingCommas = false,
    });
}

public sealed class GameEnvironment
{
    [JsonPropertyOrder(1)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string SpaceHavenVersion { get; set; }

    [JsonPropertyOrder(2)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string SpaceHavenLauncherVersion { get; set; }

    [JsonPropertyOrder(4)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Aspectj { get; set; }

    [JsonPropertyOrder(5)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string AspectjWeaver { get; set; }

    [JsonPropertyOrder(6)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public EGamePlatform GamePlatform { get; set; }
}



public sealed class BasePaths
{
    [JsonPropertyOrder(1)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string OriginalGameJarDir { get; set; }

    [JsonPropertyOrder(2)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string ClassicModsDir { get; set; }

    [JsonPropertyOrder(3)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string SteamModsDir { get; set; }
}

public sealed class ModInfo
{
    [JsonPropertyOrder(1)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Name { get; set; }

    [JsonPropertyOrder(2)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Version { get; set; }

    [JsonPropertyOrder(3)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public int ID { get; set; }

    [JsonPropertyOrder(4)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Dir { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string InfoXml { get; set;}

    [JsonPropertyOrder(5)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public List<string> SpriteTextures { get; set; } = [];

    [JsonPropertyOrder(6)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public List<string> SpriteSheetTextures { get; set; } = [];

    [JsonPropertyOrder(7)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public List<string> AudioFiles { get; set; } = [];

    [JsonPropertyOrder(8)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public List<string> JarFiles { get; set; } = [];

    [JsonPropertyOrder(9)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public List<string> OtherFiles { get; set; } = [];

    [JsonPropertyOrder(10)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public List<VarInfo> Vars { get; set; } = [];
}

public sealed class VarInfo
{
    [JsonPropertyOrder(1)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Name { get; set; }

    [JsonPropertyOrder(2)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public EVariableType Type { get; set; }

    [JsonPropertyOrder(3)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Value { get; set; }

}