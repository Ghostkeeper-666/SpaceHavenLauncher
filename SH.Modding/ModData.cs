using SH.Content;
using SH.Content.Xml;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SH.Modding;

/// <summary>
/// A class containing loaded mod information and persisted mod variable values
/// </summary>
public sealed class ModData
{
    public bool IsModified { get; set; }
    public bool IsEnabled { get; set; } = true;

    public bool IsXmlMod => XmlLibraryPaths.Count > 0 || XmlPatchPaths.Count > 0;
    public bool IsJavaMod => JarPaths.Count > 0;

    public string UniqueName { get; set; }
    public string DisplayName { get; set; }
    public string InfoXmlDescription { get; set; }
    public string MarkdownDescription { get; set; }
    public VersionInfo Version { get; set; }
    public int ModId { get; set; }
    public int AutoId { get; set; }
    public int CustomId { get; set; }
    public int ID =>
        ModId != 0 && (CustomId == 0 || CustomId == ModId) ? ModId :
        AutoId != 0 && (CustomId == 0 || CustomId == AutoId) ? AutoId :
        CustomId;
    public string Author { get; set; }
    public string ForegroundColor { get; set; }

    public List<VarData> Variables { get; } = [];
    public VersionCompatibilityList AppCompatibility { get; set; } = new();
    public VersionCompatibilityList SpaceHavenCompatibility { get; set; } = new();
    public VersionCompatibilityList ModConflicts { get; } = new();
    public VersionCompatibilityList ModDependencies { get; } = new();

    public string Dir { get; set; }

    public string XmlLibraryDir { get; private set; }
    public string XmlPatchesDir { get; private set; }
    public string AudioDir { get; private set; }
    public string SpritesDir { get; private set; }
    public string SpriteSheetsDir { get; private set; }

    public string InfoXmlPath { get; set; }
    public string MarkdownDescriptionPath { get; set; }
    public string BackgroundImagePath { get; set; }

    public bool HasAudio => AudioPaths.Count > 0;
    public bool HasSprites => SpritePaths.Count > 0;
    public bool HasSpriteSheets => SpriteSheetPaths.Count > 0;
    public bool HasLibraryXml => XmlLibraryPaths.Count > 0;
    public bool HasPatchXml => XmlPatchPaths.Count > 0;
    public bool HasJar => JarPaths.Count > 0;

    public IReadOnlyList<string> AllPaths { get; private set; }
    public IReadOnlyList<string> AudioPaths { get; private set; }
    public IReadOnlyList<string> SpritePaths { get; private set; }
    public IReadOnlyList<string> SpriteSheetPaths { get; private set; }
    public IReadOnlyList<string> XmlLibraryPaths { get; private set; }
    public IReadOnlyList<string> XmlPatchPaths { get; private set; }
    public IReadOnlyList<string> JarPaths { get; private set; }
    public IReadOnlyList<string> OtherFilePaths { get; private set; }

    public IReadOnlyList<string> AllRelativePaths { get; private set; }
    public IReadOnlyList<string> AudioRelativePaths { get; private set; }
    public IReadOnlyList<string> SpriteRelativePaths { get; private set; }
    public IReadOnlyList<string> SpriteSheetRelativePaths { get; private set; }
    public IReadOnlyList<string> XmlLibraryRelativePaths { get; private set; }
    public IReadOnlyList<string> XmlPatchRelativePaths { get; private set; }
    public IReadOnlyList<string> JarRelativePaths { get; private set; }
    public IReadOnlyList<string> OtherFilesRelativePaths { get; private set; }

    public override int GetHashCode() => UniqueName.GetHashCode();
    public override string ToString() => $"{UniqueName} {Version}";

    internal static async Task<ModData> TryLoad(string modDir, ILogger log, CancellationToken ct)
    {
        ModData mod = new();
        try
        {
            mod.Dir = modDir.AsEvaluatedOSPath();
            if (!IOUtils.DirExists(mod.Dir))
                return null;

            if (!mod.MapPaths())
                return null;

            if (!await mod.TryParseInfoXml(modDir, log, ct))
                return null;

            return mod;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
        catch (Exception ex)
        {
            log?.Error($"[{mod.UniqueName}] {ex.Message}", mod.Dir);
            return null;
        }
    }

    private bool MapPaths()
    {
        // info.xml:
        InfoXmlPath = Dir
            .GetFiles(ESearchOption.TopDir, equalsAny: [ModdingConstants.INFO_XML, ModdingConstants.INFO_XML.GetFileNameWithoutExtension()])
            .FirstOrDefault();
        if (InfoXmlPath == null)
            return false;

        // description.md:
        MarkdownDescriptionPath = Dir.CombineAsOSPath(ModdingConstants.DESCRIPTION_MD).FindFile();

        // Background image:
        BackgroundImagePath = Dir
            .GetFiles(ESearchOption.TopDir, equalsAny: ["background.jpg", "background.png"])
            .FirstOrDefault();

        // XML library:
        XmlLibraryDir = Dir.CombineAsOSPath(ModdingConstants.LIBRARY).FindDir();
        XmlLibraryRelativePaths =
            XmlLibraryDir.GetRelativeFiles(search: ESearchOption.All)
            .Where(path => !path.FilenameStartsWith(ModdingConstants.GENERATED_TEXTURES_XML))
            .ToList();
        XmlLibraryPaths = XmlLibraryRelativePaths.Select(p => XmlLibraryDir.CombineAsOSPath(p)).ToList();

        // XML Patch:
        XmlPatchesDir = Dir.CombineAsOSPath(ModdingConstants.PATCHES).FindDir();
        XmlPatchRelativePaths = XmlPatchesDir.GetRelativeFiles(search: ESearchOption.All);
        XmlPatchPaths = XmlPatchRelativePaths.Select(p => XmlPatchesDir.CombineAsOSPath(p)).ToList();

        // Audio:
        AudioDir = Dir.CombineAsOSPath(ModdingConstants.AUDIO).FindDir();
        AudioRelativePaths = AudioDir.GetRelativeFiles(search: ESearchOption.All, endsWithAny: [".mp3", ".ogg"]);
        AudioPaths = AudioRelativePaths.Select(p => AudioDir.CombineAsOSPath(p)).ToList();

        // Sprites:
        SpritesDir = Dir.CombineAsOSPath(ModdingConstants.TEXTURES).FindDir();
        SpriteRelativePaths = SpritesDir.GetRelativeFiles(search: ESearchOption.All);
        SpritePaths = SpriteRelativePaths.Select(p => SpritesDir.CombineAsOSPath(p)).ToList();

        // Sprite Sheets:
        SpriteSheetsDir = Dir.CombineAsOSPath(ModdingConstants.CIM).FindDir();
        SpriteSheetRelativePaths = SpriteSheetsDir.GetRelativeFiles(search: ESearchOption.All);
        SpriteSheetPaths = SpriteSheetRelativePaths.Select(p => SpriteSheetsDir.CombineAsOSPath(p)).ToList();

        // JAR files (only those directly under the mod dir):
        JarRelativePaths = Dir.GetRelativeFiles(search: ESearchOption.TopDir, endsWithAny: [".jar"]);
        JarPaths = JarRelativePaths.Select(p => Dir.CombineAsOSPath(p)).ToList();

        // ALL files:
        AllRelativePaths = Dir.GetRelativeFiles(search: ESearchOption.All);
        AllPaths = AllRelativePaths.Select(p => Dir.CombineAsOSPath(p)).ToList();

        // Other files:
        OtherFilePaths =
            AllPaths
            .Where(path =>
                path != InfoXmlPath &&
                path != BackgroundImagePath &&
                !XmlLibraryPaths.Contains(path) &&
                !XmlPatchPaths.Contains(path) &&
                !AudioPaths.Contains(path) &&
                !SpritePaths.Contains(path) &&
                !SpriteSheetPaths.Contains(path) &&
                !JarPaths.Contains(path) &&
                !path.FilenameEquals(ModdingConstants.DISABLED_TXT) &&
                !path.FilenameStartsWith(ModdingConstants.CUSTOM_TEXTURE)
            ).ToArray();
        OtherFilesRelativePaths =
            OtherFilePaths
            .Select(path => path.RemovePrefix($"{Dir}{IOUtils.DirSeparator}"))
            .ToArray();

        // Done.
        return true;
    }

    private async Task<bool> TryParseInfoXml(string modDir, ILogger log, CancellationToken ct)
    {
        XDocument doc = await IOUtils.TryLoadXDocumentAsync(InfoXmlPath, log, ct);
        if (doc == null)
        {
            log?.Error($"Unable to parse {InfoXmlPath}: please check for XML syntax errors", InfoXmlPath);
            return false;
        }
        XElement root = doc.Element("mod");
        if (root == null)
        {
            log?.Error($@"Invalid root node: <mod> is expected, file=""{InfoXmlPath}""", InfoXmlPath);
            return false;
        }

        // UNIQUE MOD NAME
        {
            DisplayName = root.Element("name")?.Value;
            if (DisplayName.IsNullOrWhiteSpace())
            {
                log?.Error($@"Each mod must have a unique valid name! A mod has undefined or blank name, in file ""{InfoXmlPath}""", InfoXmlPath);
                return false;
            }
            else if (ModUniqueNameGenerator.Generate(DisplayName, out string uniqueName))
            {
                UniqueName = uniqueName; // valid
            }
            else
            {
                log?.Error($@"Each mod must have a unique valid name! The mod name '{DisplayName}' was evaluated as a blank unique name, in file ""{InfoXmlPath}""", InfoXmlPath);
                return false;
            }
        }

        // AUTO ID:
        AutoId = ModAutoId.ComputeMajorId(UniqueName);

        // MOD ID:
        ModId = int.TryParse(root.Element("modid")?.Value?.Trim(), out int modId) ? modId : 0;

        // VALIDATE MOD ID:
        if (ModId != 0 && (ModId < ModAutoId.MinValue || ModId > ModAutoId.MaxValue))
        {
            log?.Warn($"[{UniqueName}] MOD ID must be within the range [{ModAutoId.MinValue}, {ModAutoId.MaxValue}] => Assigning an automatic ID instead. This MOD may fail to load in case it heavily depends on its MOD ID", InfoXmlPath);
            ModId = 0;
        }

        // AUTHOR
        Author = root.Element("author")?.Value?.Trim();

        // TODO: First make mod author mandatory, then remove this code:
        if (Author.IsNullOrWhiteSpace())
        {
            if (UniqueName.Contains("Bikini Babes", StringComparison.OrdinalIgnoreCase))
                Author = "Gravelyn";
            else if (UniqueName.Contains("Customizer+"))
                Author = "r4v4g3 (r0xx0r3r)";
            else
            {
                Author = string.Empty;
                log?.Warn($@"[{UniqueName}] Missing mod author");
            }
        }

        // DESCRIPTION
        InfoXmlDescription = root.Element("description")?.Value?.TrimStart(' ', '\t', '\r', '\n', '~');
        if (InfoXmlDescription == null)
        {
            log?.Error($@"[{UniqueName}] Missing or empty <decription> node, file=""{InfoXmlPath}""", InfoXmlPath);
            return false;
        }

        // MOD VERSION
        Version = new VersionInfo(root.Element("version")?.Value);


        ct.ThrowIfCancellationRequested();


        // SKIP this for the time being...
        // >>> NEW: SPACE HAVEN LAUNCHER Compatibility
        //{
        //    List<XElement> appNodes = [];
        //    appNodes.AddRange(root.Elements("spacehavenlauncher"));
        //    appNodes.AddRange(root.Elements("spaceHavenLauncher"));
        //    appNodes.AddRange(root.Elements("launcher"));
        //    appNodes.AddRange(root.Elements("Launcher"));
        //    foreach (XElement appNode in appNodes)
        //    {
        //        ct.ThrowIfCancellationRequested();

        //        VersionInfo version =
        //            new(appNode.Attribute("version")?.Value ?? appNode.Attribute("v")?.Value ?? appNode.Value);

        //        EVersionOperator op =
        //            VersionOperatorParser.ToOperator(
        //                appNode.Attribute("operator")?.Value ??
        //                appNode.Attribute("op")?.Value);

        //        AppCompatibility.Add(new("Space Haven Launcher", version, op));
        //    }
        //}

        // >>> SPACE HAVEN Version Compatibility
        List<XElement> spaceHavenNodes = [];
        spaceHavenNodes.AddRange(root.Elements("spacehaven"));
        spaceHavenNodes.AddRange(root.Elements("spaceHaven"));
        spaceHavenNodes.AddRange(root.Elements("sh"));

        List<XElement> gameVersionRootNodes = []; // DEPRECATED
        gameVersionRootNodes.AddRange(root.Elements("gameversion"));
        gameVersionRootNodes.AddRange(root.Elements("gameVersion"));
        gameVersionRootNodes.AddRange(root.Elements("gameversions"));
        gameVersionRootNodes.AddRange(root.Elements("gameVersions"));

        if (spaceHavenNodes.Count > 0)
        {
            foreach (XElement node in spaceHavenNodes)
            {
                ct.ThrowIfCancellationRequested();

                VersionInfo version =
                    new(node.Attribute("version")?.Value ?? node.Attribute("v")?.Value ?? node.Value);

                EVersionOperator op =
                    VersionOperatorParser.ToOperator(
                        node.Attribute("operator")?.Value ??
                        node.Attribute("op")?.Value);

                if (op == EVersionOperator.any)
                    op = EVersionOperator.gte;

                SpaceHavenCompatibility.Add(new(SpaceHavenConstants.SpaceHavenName, version, op));
            }
        }
        else if (gameVersionRootNodes.Count > 0) // DEPRECATED
        {
            foreach (string node in gameVersionRootNodes.SelectMany(n => n?.Elements("v")?.Select(v => v?.Value?.TrimStart('v'))?.Where(str => !str.IsNullOrWhiteSpace()) ?? []))
            {
                ct.ThrowIfCancellationRequested();

                VersionInfo version = new(node.Replace("*", string.Empty).Replace("+", string.Empty));
                SpaceHavenCompatibility.Add(new(SpaceHavenConstants.SpaceHavenName, version, EVersionOperator.gte)); // DEPRECATED
            }
        }

        // >>> NEW: MOD CONFLICTS
        {
            List<XElement> conflictNodes = [];
            conflictNodes.AddRange(root.Elements("modconflict"));
            conflictNodes.AddRange(root.Elements("modConflict"));

            List<VersionCompatibility> modConflicts = [];
            foreach (XElement node in conflictNodes)
            {
                ct.ThrowIfCancellationRequested();

                string modName =
                    node.Attribute("name")?.Value?.Trim() ??
                    node.Attribute("n")?.Value?.Trim();
                modName =
                    modName?.Trim();
                if (modName.IsNullOrWhiteSpace())
                    throw new Exception($@"[{UniqueName}] Missing property ""name"" in <modConflict> node in {ModdingConstants.INFO_XML}, line={node.Line()} file=""{ModdingConstants.INFO_XML}""");

                VersionInfo version =
                    new(node.Attribute("version")?.Value ?? node.Attribute("v")?.Value ?? node.Value);

                EVersionOperator op = VersionOperatorParser.ToOperator(
                    node.Attribute("operator")?.Value ??
                    node.Attribute("op")?.Value);

                modConflicts.Add(new(modName, version, op));
            }
            ModConflicts.AddRange(modConflicts.OrderBy(m => m.Name));
        }

        // >>> NEW: MOD DEPENDENCIES
        {
            List<XElement> dependencyNodes = [];
            dependencyNodes.AddRange(root.Elements("moddependency"));
            dependencyNodes.AddRange(root.Elements("modDependency"));

            List<VersionCompatibility> modDependencies = [];
            foreach (XElement node in dependencyNodes)
            {
                ct.ThrowIfCancellationRequested();

                string varName =
                    node.Attribute("name")?.Value?.Trim() ??
                    node.Attribute("n")?.Value?.Trim();
                varName =
                    varName?.Trim();
                if (varName.IsNullOrWhiteSpace())
                    throw new Exception($@"[{UniqueName}] Missing property ""name"" in <modDependency> node in {ModdingConstants.INFO_XML}, line={node.Line()} file=""{ModdingConstants.INFO_XML}""");

                VersionInfo version =
                    new(node.Attribute("version")?.Value ?? node.Attribute("v")?.Value ?? node.Value);

                EVersionOperator op = VersionOperatorParser.ToOperator(
                    node.Attribute("operator")?.Value ??
                    node.Attribute("op")?.Value);

                modDependencies.Add(new(varName, version, op));
            }
            ModDependencies.AddRange(modDependencies.OrderBy(m => m.Name));
        }

        // VARIABLES
        List<XElement> rootVarNodes = [];
        rootVarNodes.AddRange(root.Elements("config"));
        rootVarNodes.AddRange(root.Elements("vars"));
        rootVarNodes.AddRange(root.Elements("variables"));

        VarData previousModVar = null;
        HashSet<string> duplicateVariables = [];
        bool success = true;
        foreach (XElement rootVarNode in rootVarNodes)
        {
            foreach (XElement v in rootVarNode.Elements("var") ?? [])
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    int line = v.Line();

                    string name = v.Attribute("name")?.Value?.Trim('{', '}', ' ');
                    if (name.IsNullOrWhiteSpace())
                    {
                        log?.Error($"[{UniqueName}] A <var> node is missing the 'name' property in {ModdingConstants.INFO_XML}, line={line}", InfoXmlPath);
                        return false;
                    }

                    // Validate reserved variable name:
                    if (ModAutoId.IdVariable.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        log?.Error($"[{UniqueName}] Variable name '{name}' is RESERVED and can NOT be declared in {ModdingConstants.INFO_XML}, line={line}", InfoXmlPath);
                        return false;
                    }

                    bool isSeparator = name.Equals("separator", StringComparison.OrdinalIgnoreCase);
                    if (isSeparator)
                        name = string.Empty;

                    string description = isSeparator ? string.Empty :
                        v.Value?.TrimStart(' ', '\t', '\r', '\n') ?? string.Empty;

                    // TODO: Remove this cleanup code after the "My Mod" series descriptions are simplified:
                    string[] splittedDescription = description.Split('[');
                    if (splittedDescription.Length > 0 && splittedDescription.Last().Contains("default", StringComparison.OrdinalIgnoreCase) && splittedDescription.Last().Contains("suggested", StringComparison.OrdinalIgnoreCase))
                        description = splittedDescription.SkipLast(1).JoinToString("[").Trim();

                    string original = isSeparator ? string.Empty : (
                        v.Attribute("original")?.Value ??
                        v.Attribute("default")?.Value ??
                        v.Attribute("value")?.Value
                        )?.Trim('{', '}', ' ') ?? string.Empty;

                    string suggested = isSeparator ? string.Empty : (
                        v.Attribute("suggested")?.Value ??
                        v.Attribute("value")?.Value ??
                        original
                        )?.Trim('{', '}', ' ') ?? string.Empty;

                    string previous = string.Empty;

                    string current = isSeparator ? string.Empty : suggested;

                    VarData modVar = new()
                    {
                        IsSeparator = isSeparator,
                        Name = name,
                        Description = description,
                        OriginalValue = original,
                        SuggestedValue = suggested,
                        CurrentValue = current,
                        PreviousValue = previous,
                        Line = line,
                    };

                    // TODO: validation of strong-typed variables?

                    // Skip multiple separators:
                    if (modVar.IsSeparator && (previousModVar?.IsSeparator ?? true))
                        continue;

                    // Duplicate variables
                    if (!modVar.IsSeparator && Variables.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    {
                        // 'warn as error', just once for each variable:
                        if (!duplicateVariables.Contains(name))
                            log?.Warn($"[{UniqueName}] Skipping duplicate variable '{name}' -> This is certainly a BUG in this MOD", InfoXmlPath);
                        duplicateVariables.Add(name);
                        continue;
                    }

                    // Add variable:
                    Variables.Add(modVar);
                    previousModVar = modVar;
                }
                catch (Exception ex)
                {
                    log?.Error($"[{modDir}] {ex.Message}");
                    success = false;
                }
            }
        }

        // Stop here in case of errors:
        if (success == false)
            return false;

        // Add a separator variable line at the end:
        if (Variables.Count > 0 && previousModVar?.IsSeparator != true)
            Variables.Add(new VarData()
            {
                IsSeparator = true,
                Name = "Separator",
                Description = string.Empty,
                OriginalValue = string.Empty,
                SuggestedValue = string.Empty,
                CurrentValue = string.Empty,
                Line = 0,
            });

        // Read markdown mod description:
        if (MarkdownDescriptionPath != null)
            MarkdownDescription = await IOUtils.TryReadAllTextAsync(MarkdownDescriptionPath, log, ct);

        // Mod foreground color:
        ForegroundColor = (root.Element("ForegroundColor") ?? root.Element("foregroundColor") ?? root.Element("foregroundcolor") ?? root.Element("forecolor"))?.Value;

        // Done.
        return true;
    }






}
