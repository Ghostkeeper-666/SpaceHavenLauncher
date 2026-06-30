using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SH.Content.Modding;

public sealed class ModsJsonFile
{
    [JsonPropertyOrder(0)]
    public string Build { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");

    [JsonPropertyOrder(1)]
    public List<string> AOPLibs { get; set; } = [];

    [JsonPropertyOrder(2)]
    public List<ModInfo> Mods { get; set; } = [];

    public string ToJsonString() => JsonSerializer.Serialize(this, new JsonSerializerOptions
    {
        WriteIndented = true,
        IndentCharacter = ' ',
        IndentSize = 2,
        AllowTrailingCommas = false,
    });
}


public sealed class ModInfo
{
    [JsonPropertyOrder(0)]
    public string SchemaVersion { get; set; }

    [JsonPropertyOrder(1)]
    public string Name { get; set; }

    [JsonPropertyOrder(2)]
    public string Version { get; set; }

    [JsonPropertyOrder(3)]
    public string Directory { get; set; }

    [JsonPropertyOrder(4)]
    public int ID { get; set; }

    [JsonPropertyOrder(5)]
    public List<string> Textures { get; set; } = [];

    [JsonPropertyOrder(6)]
    public List<string> Audio { get; set; } = [];

    [JsonPropertyOrder(7)]
    public List<string> Java { get; set; } = [];

    [JsonPropertyOrder(8)]
    public List<string> Other { get; set; } = [];

    [JsonPropertyOrder(9)]
    public List<VarInfo> Vars { get; set; } = [];
}


public sealed class VarInfo
{
    [JsonPropertyOrder(0)]
    public string Name { get; set; }

    [JsonPropertyOrder(1)]
    public EVariableType Type { get; set; }

    [JsonPropertyOrder(2)]
    public string Value { get; set; }

}