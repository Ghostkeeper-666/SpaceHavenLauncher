using SH.Content;
using SH.Content.Xml;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SH.Modding;

public sealed class ModRepository
{
    private readonly ILogger Log;

    public ModRepository(ILogger logger) =>
        Log = logger ?? new VoidLogger();

    private readonly char[] ValueSeparators = new[] { ',', ';', '|', ' ' };

    /// <summary>
    /// Loads all mods
    /// </summary>
    public async Task<OrderedDictionary<string, ModData>> TryLoadMods(IEnumerable<string> modRootDirectories, CancellationToken ct, IProgressInfo progress)
    {
        OrderedDictionary<string, ModData> mods = new();
        try
        {
            progress?.Start();

            // Locate mods:
            int modErrors = 0;

            List<string> modDirectories = [];

            foreach (string modRootDirectory in modRootDirectories)
            {
                ct.ThrowIfCancellationRequested();

                if (!Directory.Exists(modRootDirectory))
                {
                    Log.Error($@"Mod root directory not found: ""{modRootDirectory}""");
                    continue;
                }
                modDirectories
                    .AddRange(Directory.GetDirectories(modRootDirectory)
                    .Where(modDir => File.Exists(Path.Combine(modDir, ModdingConstants.INFO_XML)))
                    ?? []);
            }

            // Parse mods:
            int count = 0;
            foreach (string modDirectory in modDirectories)
            {
                // Breathe:
                await Task.Yield();

                progress?.SetNormalized(count++ / (1.0 + modDirectories.Count));

                // Load mod:
                ModData mod = await TryLoadMod(modDirectory, ct);
                if (mod == null)
                {
                    ++modErrors;
                    Log.Error($@"This mod contains errors and could not be loaded properly: ""{modDirectory}""", modDirectory);
                    continue;
                }

                // Try add mod:
                if (!mods.TryAdd(mod.Name, mod))
                {
                    ++modErrors;
                    Log.Error($@"The mod '{mod.Name}' could not be loaded twice. Please check for duplicate mods in your mod root directories. Keeping ""{mods[mod.Name].Directory}"" and skipping ""{mod.Directory}""", mod.Directory);
                    continue;
                }
                Log.Debug($@"Mod '{mod.Name}' was loaded successfully", mod.Directory);
            }

            // Breathe:
            await Task.Yield();

            // Initial sorting of mods by name:
            mods = new OrderedDictionary<string, ModData>(mods.OrderBy(kvp => kvp.Key));

            // Done.
            if (modErrors <= 0)
            {
                if (mods.Count > 0) Log.Success($"{mods.Count} mod(s) were successfully loaded");
                else Log.Info($"No mods found");
            }
            else
            {
                if (mods.Count > 0) Log.Error($"Only {mods.Count} mod(s) could be loaded, {modErrors} mod(s) failed to load");
                else Log.Error($"{modErrors} mod(s) failed to load");
            }

            // Done.
            progress?.Complete();
            return mods;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex);
            return null;
        }
        finally
        {
            progress?.Complete();
        }
    }

    private async Task<ModData> TryLoadMod(string modDir, CancellationToken ct)
    {
        ModData mod = new();
        try
        {
            bool success = true;

            mod.Directory = modDir.AsOSPath();
            if (!Directory.Exists(mod.Directory))
                return null;

            // Info.xml file:
            string infoXmlPath = Path.Combine(mod.Directory, ModdingConstants.INFO_XML);
            mod.InfoXmlPath =
                Directory.GetFiles(mod.Directory, "*.*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(path => path.Equals(infoXmlPath, StringComparison.OrdinalIgnoreCase));

            // Background image:
            string[] possibleBackgroundImagePaths =
            [
                Path.Combine(mod.Directory, "background.jpg"),
                Path.Combine(mod.Directory, "background.png"),
                Path.Combine(mod.Directory, "bg.jpg"),
                Path.Combine(mod.Directory, "bg.png"),
            ];
            mod.BackgroundImagePath =
                Directory.GetFiles(mod.Directory, "*.*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(path => possibleBackgroundImagePaths
                .Any(possiblePath => path.Equals(possiblePath, StringComparison.OrdinalIgnoreCase)));

            // XML library files:
            if (Directory.Exists(mod.XmlLibraryDirectory))
                mod.XmlLibraryFilePaths.AddRange(Directory.GetFiles(mod.XmlLibraryDirectory, "*.*", SearchOption.AllDirectories)
                    .Where(path => !Path.GetFileName(path).StartsWith(ModdingConstants.GENERATED_TEXTURES_XML, StringComparison.OrdinalIgnoreCase)));

            // XML Patch files:
            if (Directory.Exists(mod.XmlPatchesDirectory))
                mod.XmlPatchFilePaths.AddRange(Directory.GetFiles(mod.XmlPatchesDirectory, "*.*", SearchOption.AllDirectories));

            // Audio files:
            if (Directory.Exists(mod.AudioDirectory))
                mod.AudioFilePaths.AddRange(Directory.GetFiles(mod.AudioDirectory, "*.*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)));

            // Texture files:
            if (Directory.Exists(mod.TexturesDirectory))
                mod.TextureFilePaths.AddRange(Directory.GetFiles(mod.TexturesDirectory, "*.*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)));

            // JAR files:
            mod.JavaFilePaths.AddRange(Directory.GetFiles(mod.Directory, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)));

            // ALL files:
            mod.AllPaths.AddRange(Directory.GetFiles(mod.Directory, "*.*", SearchOption.AllDirectories));

            // Other files:
            mod.OtherFilePaths.AddRange(
                mod.AllPaths.Where(path =>
                    path != mod.InfoXmlPath &&
                    path != mod.BackgroundImagePath &&
                    !Path.GetFileName(path).Equals(ModdingConstants.DISABLED_TXT, StringComparison.OrdinalIgnoreCase) &&
                    !Path.GetFileName(path).StartsWith(ModdingConstants.CUSTOM_TEXTURE, StringComparison.OrdinalIgnoreCase) &&
                    !mod.XmlLibraryFilePaths.Contains(path) &&
                    !mod.XmlPatchFilePaths.Contains(path) &&
                    !mod.AudioFilePaths.Contains(path) &&
                    !mod.TextureFilePaths.Contains(path) &&
                    !mod.JavaFilePaths.Contains(path)
            ));


            // ----------------------------------------------------------------------
            // INFO.XML
            XDocument doc = await IOUtils.TryLoadXDocumentAsync(mod.InfoXmlPath, Log, ct);
            if (doc == null)
            {
                Log.Error($"Unable to parse {mod.InfoXmlPath}: please check for XML syntax errors", mod.InfoXmlPath);
                return null;
            }
            XElement root = doc.Element("mod");
            if (root == null)
            {
                Log.Error($@"Invalid root node: <mod> is expected, file=""{mod.InfoXmlPath}""", mod.InfoXmlPath);
                return null;
            }

            // UNIQUE NAME
            mod.Name = NormalizeModName(root.Element("name")?.Value);
            if (mod.Name.IsNullOrWhiteSpace())
            {
                Log.Error($@"Each mod must have a unique valid name! The XML node <name> is missing or invalid in file ""{mod.InfoXmlPath}""", mod.InfoXmlPath);
                return null;
            }

            // AUTO ID:
            mod.AutoId = ModAutoId.ComputeMajorId(mod.Name);

            // MOD ID:
            mod.ModId = int.TryParse(root.Element("modid")?.Value?.Trim() ?? "0", out int modId) ? modId : 0;

            // VALIDATE MOD ID:
            if (mod.ModId != 0 && (mod.ModId < ModAutoId.MinValue || mod.ModId > ModAutoId.MaxValue))
            {
                Log.Warn($"[{mod.Name}] MOD ID must be within the range [{ModAutoId.MinValue}, {ModAutoId.MaxValue}] => Assigning an automatic ID instead. This MOD may fail to load in case it heavily depends on its MOD ID", mod.InfoXmlPath);
                mod.ModId = 0;
            }

            // AUTHOR
            mod.Author =
                root.Element("author")?.Value?.Trim();

            // TODO: First make mod author mandatory, then remove this code:
            if (mod.Author.IsNullOrWhiteSpace())
            {
                if (mod.Name.Contains("Bikini Babes", StringComparison.OrdinalIgnoreCase))
                    mod.Author = "Gravelyn";
                else if (mod.Name.Contains("CustomizerPlus"))
                    mod.Author = "r4v4g3 (r0xx0r3r)";
                else
                {
                    mod.Author = string.Empty;
                    Log.Warn($@"Missing mod author in mod ""{mod.Name}""");
                }
            }

            // DESCRIPTION
            mod.InfoXmlDescription = root.Element("description")?.Value?.TrimStart(' ', '\t', '\r', '\n', '~');
            if (mod.InfoXmlDescription == null)
            {
                Log.Error($@"[{mod.Name}] Missing or empty <decription> node, file=""{mod.InfoXmlPath}""", mod.InfoXmlPath);
                return null;
            }

            // MOD VERSION
            mod.Version = new VersionInfo(root.Element("version")?.Value);


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

            //        mod.AppCompatibility.Add(new("Space Haven Launcher", version, op));
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

                    mod.SpaceHavenCompatibility.Add(new(SpaceHavenConstants.SpaceHavenName, version, op));
                }
            }
            else if (gameVersionRootNodes.Count > 0) // DEPRECATED
            {
                foreach (string node in gameVersionRootNodes.SelectMany(n => n?.Elements("v")?.Select(v => v?.Value?.TrimStart('v'))?.Where(str => !str.IsNullOrWhiteSpace()) ?? []))
                {
                    ct.ThrowIfCancellationRequested();

                    VersionInfo version = new(node.Replace("*", string.Empty).Replace("+", string.Empty));
                    mod.SpaceHavenCompatibility.Add(new(SpaceHavenConstants.SpaceHavenName, version, EVersionOperator.gte)); // DEPRECATED
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
                        throw new Exception($@"[{mod.Name}] Missing property ""name"" in <modConflict> node in {ModdingConstants.INFO_XML}, line={node.Line()} file=""{ModdingConstants.INFO_XML}""");

                    VersionInfo version =
                        new(node.Attribute("version")?.Value ?? node.Attribute("v")?.Value ?? node.Value);

                    EVersionOperator op = VersionOperatorParser.ToOperator(
                        node.Attribute("operator")?.Value ??
                        node.Attribute("op")?.Value);

                    modConflicts.Add(new(modName, version, op));
                }
                mod.ModConflicts.AddRange(modConflicts.OrderBy(m => m.Name));
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

                    string modName =
                        node.Attribute("name")?.Value?.Trim() ??
                        node.Attribute("n")?.Value?.Trim();
                    modName =
                        modName?.Trim();
                    if (modName.IsNullOrWhiteSpace())
                        throw new Exception($@"[{mod.Name}] Missing property ""name"" in <modDependency> node in {ModdingConstants.INFO_XML}, line={node.Line()} file=""{ModdingConstants.INFO_XML}""");

                    VersionInfo version =
                        new(node.Attribute("version")?.Value ?? node.Attribute("v")?.Value ?? node.Value);

                    EVersionOperator op = VersionOperatorParser.ToOperator(
                        node.Attribute("operator")?.Value ??
                        node.Attribute("op")?.Value);

                    modDependencies.Add(new(modName, version, op));
                }
                mod.ModDependencies.AddRange(modDependencies.OrderBy(m => m.Name));
            }

            // VARIABLES
            List<XElement> rootVarNodes = [];
            rootVarNodes.AddRange(root.Elements("config"));
            rootVarNodes.AddRange(root.Elements("vars"));
            rootVarNodes.AddRange(root.Elements("variables"));

            VarData previousModVar = null;
            HashSet<string> duplicateVariables = [];

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
                            Log.Error($"[{mod.Name}] A <var> node is missing the 'name' property in {ModdingConstants.INFO_XML}, line={line}", mod.InfoXmlPath);
                            return null;
                        }

                        // Validate reserved variable name:
                        if (ModAutoId.IdVariable.Equals(name, StringComparison.OrdinalIgnoreCase))
                        {
                            Log.Error($"[{mod.Name}] Variable name '{name}' is RESERVED and can NOT be declared in {ModdingConstants.INFO_XML}, line={line}", mod.InfoXmlPath);
                            return null;
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
                        if (!modVar.IsSeparator && mod.Variables.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                        {
                            // 'warn as error', just once for each variable:
                            if (!duplicateVariables.Contains(name))
                                Log.Warn($"[{mod.Name}] Skipping duplicate variable '{name}' -> This is certainly a BUG in this MOD", mod.InfoXmlPath);
                            duplicateVariables.Add(name);
                            continue;
                        }

                        // Add variable:
                        mod.Variables.Add(modVar);
                        previousModVar = modVar;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[{modDir}] {ex.Message}");
                        success = false;
                    }
                }
            }

            // Stop here in case of errors:
            if (success == false)
                return null;

            // Add a separator variable line at the end:
            if (mod.Variables.Count > 0 && previousModVar?.IsSeparator != true)
                mod.Variables.Add(new VarData()
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
            string mardkdownDescriptionPath = Path.Combine(mod.Directory, ModdingConstants.DESCRIPTION_MD);
            mod.MarkdownDescriptionPath =
                Directory.GetFiles(mod.Directory, "*.*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(path => path.Equals(mardkdownDescriptionPath, StringComparison.OrdinalIgnoreCase));
            if (mod.MarkdownDescriptionPath != null)
                mod.MarkdownDescription = await IOUtils.TryReadAllTextAsync(mod.MarkdownDescriptionPath, Log, ct);

            // Mod foreground color:
            mod.ForegroundColor = (root.Element("ForegroundColor") ?? root.Element("foregroundColor") ?? root.Element("foregroundcolor") ?? root.Element("forecolor"))?.Value;

            //Done.
            return mod;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"[{mod.Name}] {ex.Message}", mod.Directory);
            return null;
        }
    }

    private readonly string ValidModNameChars = "01234567890abcdefghijklmnopqrstuvwxyz-()";

    private readonly ImmutableDictionary<char, string> SpecialModNameChars = ImmutableDictionary.CreateRange(new[]
    {
        new KeyValuePair<char, string>('&', "And"),
        new KeyValuePair<char, string>('+', "Plus"),
        new KeyValuePair<char, string>('[', "("),
        new KeyValuePair<char, string>(']', ")"),
        new KeyValuePair<char, string>('{', "("),
        new KeyValuePair<char, string>('}', ")"),
    });

    private string NormalizeModName(string name)
    {
        if (name.IsNullOrWhiteSpace())
            return string.Empty;

        StringBuilder sb = new();
        char prevChar = default;

        for (int i = 0; i < name.Length; ++i)
        {
            char ch = name[i];

            // Valid chars:
            if (ValidModNameChars.Contains(ch, StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(ch);
                continue;
            }

            // Special treatment for other chars:
            if (SpecialModNameChars.TryGetValue(ch, out string replacement))
            {
                sb.Append(replacement);
                continue;
            }

            // Otherwise replace by a whitespace:
            if (prevChar != ' ' && sb.Length > 0)
                sb.Append(' ');
        }

        // Done.
        return sb.ToString().Trim(); // trim whitespaces
    }
}

