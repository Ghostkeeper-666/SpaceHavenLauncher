using SH.Content.Xml;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SH.Content.Modding;

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
            progress?.SetNormalized(0.00001);

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

                progress.SetNormalized(count++ / (double)(1 + modDirectories.Count));

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
                Log.Info($@"Mod '{mod.Name}' was loaded successfully", mod.Directory);
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

    private async Task<ModData> TryLoadMod(string modDirectory, CancellationToken ct)
    {
        ModData mod = new();
        try
        {
            bool success = true;

            modDirectory = modDirectory.AsOSPath();

            if (!Directory.Exists(modDirectory))
                return null;

            // Info.xml file:
            string infoXmlPath = Path.Combine(modDirectory, ModdingConstants.INFO_XML);
            mod.InfoXmlPath =
                Directory.GetFiles(modDirectory, "*.*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(path => path.Equals(infoXmlPath, StringComparison.OrdinalIgnoreCase));

            // Background image:
            string[] possibleBackgroundImagePaths =
            [
                Path.Combine(modDirectory, "background.jpg"),
                Path.Combine(modDirectory, "background.png"),
                Path.Combine(modDirectory, "bg.jpg"),
                Path.Combine(modDirectory, "bg.png"),
            ];
            mod.BackgroundImagePath =
                Directory.GetFiles(modDirectory, "*.*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(path => possibleBackgroundImagePaths
                .Any(possiblePath => path.Equals(possiblePath, StringComparison.OrdinalIgnoreCase)));

            // XML library files:
            mod.XmlLibraryDirectory = Path.Combine(modDirectory, SpaceHavenConstants.LIBRARY);
            if (mod.XmlLibraryDirectory.IsNullOrWhiteSpace() || !Directory.Exists(mod.XmlLibraryDirectory))
                mod.XmlLibraryDirectory = null;
            mod.XmlLibraryFilePaths.AddRange(
                mod.XmlLibraryDirectory == null ? [] :
                Directory.GetFiles(mod.XmlLibraryDirectory, "*.*", SearchOption.TopDirectoryOnly)
                .Where(path => !Path.GetFileName(path).StartsWith(ModdingConstants.GENERATED_TEXTURES_XML, StringComparison.OrdinalIgnoreCase)));

            // XML Patch files:
            mod.XmlPatchesDirectory = Path.Combine(modDirectory, "patches");
            if (mod.XmlPatchesDirectory.IsNullOrWhiteSpace() || !Directory.Exists(mod.XmlPatchesDirectory))
                mod.XmlPatchesDirectory = null;
            mod.XmlPatchFilePaths.AddRange(
                mod.XmlPatchesDirectory == null ? [] :
                Directory.GetFiles(mod.XmlPatchesDirectory, "*.*", SearchOption.TopDirectoryOnly));

            // Audio files:
            mod.AudioDirectory = Path.Combine(modDirectory, "audio");
            if (mod.AudioDirectory.IsNullOrWhiteSpace() || !Directory.Exists(mod.AudioDirectory))
                mod.AudioDirectory = null;
            mod.AudioFilePaths.AddRange(
                mod.AudioDirectory == null ? [] :
                Directory.GetFiles(mod.AudioDirectory, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f =>
                    f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)));

            // Texture files:
            mod.TexturesDirectory = Path.Combine(modDirectory, "textures");
            if (mod.TexturesDirectory.IsNullOrWhiteSpace() || !Directory.Exists(mod.TexturesDirectory))
                mod.TexturesDirectory = null;
            mod.TextureFilePaths.AddRange(
                mod.TexturesDirectory == null ? [] :
                Directory.GetFiles(mod.TexturesDirectory, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)));

            // JAR files:
            mod.JavaFilePaths.AddRange(
                Directory.GetFiles(modDirectory, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => f.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)));

            // ALL files:
            mod.AllPaths.AddRange(
                Directory.GetFiles(modDirectory, "*.*", SearchOption.AllDirectories));

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
            mod.Directory = modDirectory;

            // UNIQUE NAME
            mod.Name = root.Element("name")?.Value?.Trim();
            if (mod.Name.IsNullOrWhiteSpace())
            {
                Log.Error($@"Each mod must have a unique name! The XML node <name> is missing or empty in file=""{mod.InfoXmlPath}""", mod.InfoXmlPath);
                return null;
            }
            mod.Name = NormalizeModName(mod.Name);

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
                root.Element("author")?.Value?.Trim() ?? "(unknown)";

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


            // >>> NEW: SPACE HAVEN LAUNCHER Compatibility
            foreach (XElement e in root.Elements("spaceHavenLauncher") ?? root.Elements("spacehavenlauncher") ?? root.Elements("Launcher") ?? root.Elements("launcher") ?? [])
            {
                ct.ThrowIfCancellationRequested();

                VersionInfo version =
                    new(e.Attribute("version")?.Value ?? e.Attribute("v")?.Value ?? e.Value);

                EVersionOperator op =
                    VersionOperatorParser.ToOperator(
                        e.Attribute("operator")?.Value ??
                        e.Attribute("op")?.Value);

                mod.AppCompatibility.Add(new("Space Haven Launcher", version, op));
            }

            // >>> DEPRECATED: MINIMUM REQUIRED SPACE HAVEN VERSION
            string[] gameVersions =
                (root.Element("gameVersion") ?? root.Element("gameVersions"))?.Value?.ToLowerInvariant()?.Trim()?
                .Split(ValueSeparators, StringSplitOptions.RemoveEmptyEntries) ?? [];
            foreach (string gameVersion in gameVersions)
            {
                ct.ThrowIfCancellationRequested();

                VersionInfo version = new(gameVersion.Replace("*", string.Empty).Replace("+", string.Empty));
                mod.SpaceHavenCompatibility.Add(new(SpaceHavenConstants.SpaceHavenName, version, EVersionOperator.gte));
            }

            // >>> NEW: SPACE HAVEN VERSION Compatibility
            foreach (XElement e in root.Elements("spaceHaven") ?? root.Elements("spacehaven") ?? root.Elements("sh") ?? [])
            {
                ct.ThrowIfCancellationRequested();

                VersionInfo version =
                    new(e.Attribute("version")?.Value ?? e.Attribute("v")?.Value ?? e.Value);

                EVersionOperator op =
                    VersionOperatorParser.ToOperator(
                        e.Attribute("operator")?.Value ??
                        e.Attribute("op")?.Value);

                if (op == EVersionOperator.any)
                    op = EVersionOperator.gte;

                mod.SpaceHavenCompatibility.Add(new("Space Haven", version, op));
            }

            // >>> NEW: MOD CONFLICTS
            {
                XElement[] conflicts = (root.Elements("modConflict") ?? root.Elements("modconflict"))?.ToArray() ?? [];
                List<VersionCompatibility> modConflicts = [];
                foreach (XElement e in conflicts)
                {
                    ct.ThrowIfCancellationRequested();

                    string modName =
                        e.Attribute("name")?.Value?.Trim() ??
                        e.Attribute("n")?.Value?.Trim();
                    modName =
                        modName?.Trim();
                    if (modName.IsNullOrWhiteSpace())
                        throw new Exception($@"[{mod.Name}] Missing property ""name"" in <modConflict> node in {ModdingConstants.INFO_XML}, line={e.Line()} file=""{ModdingConstants.INFO_XML}""");

                    VersionInfo version =
                        new(e.Attribute("version")?.Value ?? e.Attribute("v")?.Value ?? e.Value);

                    EVersionOperator op = VersionOperatorParser.ToOperator(
                        e.Attribute("operator")?.Value ??
                        e.Attribute("op")?.Value);

                    modConflicts.Add(new(modName, version, op));
                }
                mod.ModConflicts.AddRange(modConflicts.OrderBy(m => m.Name));
            }

            // >>> NEW: MOD DEPENDENCIES
            {
                XElement[] dependencies = (root.Elements("modDependency") ?? root.Elements("moddependency"))?.ToArray() ?? [];
                List<VersionCompatibility> modDependencies = [];
                foreach (XElement e in dependencies)
                {
                    ct.ThrowIfCancellationRequested();

                    string modName =
                        e.Attribute("name")?.Value?.Trim() ??
                        e.Attribute("n")?.Value?.Trim();
                    modName =
                        modName?.Trim();
                    if (modName.IsNullOrWhiteSpace())
                        throw new Exception($@"[{mod.Name}] Missing property ""name"" in <modDependency> node in {ModdingConstants.INFO_XML}, line={e.Line()} file=""{ModdingConstants.INFO_XML}""");

                    VersionInfo version =
                        new(e.Attribute("version")?.Value ?? e.Attribute("v")?.Value ?? e.Value);

                    EVersionOperator op = VersionOperatorParser.ToOperator(
                        e.Attribute("operator")?.Value ??
                        e.Attribute("op")?.Value);

                    modDependencies.Add(new(modName, version, op));
                }
                mod.ModDependencies.AddRange(modDependencies.OrderBy(m => m.Name));
            }

            // VARIABLES
            XElement[] vars = (root.Element("config") ?? root.Element("vars") ?? root.Element("variables"))?.Elements("var")?.ToArray() ?? [];
            VarData previousModVar = null;
            HashSet<string> duplicateVariables = [];
            foreach (XElement v in vars ?? [])
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
                    Log.Error($"[{modDirectory}] {ex.Message}");
                    success = false;
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
            string mardkdownDescriptionPath = Path.Combine(modDirectory, ModdingConstants.DESCRIPTION_MD);
            mod.MarkdownDescriptionPath =
                Directory.GetFiles(modDirectory, "*.*", SearchOption.TopDirectoryOnly)
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

    private string NormalizeModName(string name)
    {
        const string ValidModNameChars = " 01234567890abcdefghijklmnopqrstuvwxyz-()";
        StringBuilder sb = new();
        for (int i = 0; i < name.Length; ++i)
            sb.Append(ValidModNameChars.Contains(name[i], StringComparison.OrdinalIgnoreCase) ? name[i] : '_');
        return sb.ToString();
    }
}




