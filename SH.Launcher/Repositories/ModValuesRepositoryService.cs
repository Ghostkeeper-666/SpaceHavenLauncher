using SH.Content.Modding;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SH.Launcher.Repositories;

public sealed class ModValuesRepositoryService
{
    public static readonly string MOD_VALUES_TOKEN = "<MOD_NAME>";
    public static string MOD_VALUES => $"{MOD_VALUES_TOKEN}.xml";

    private readonly PathData Paths;
    private readonly ILogger Log;
    private readonly SemaphoreSlim Semaphore = new(1, 1);

    public ModValuesRepositoryService(PathData paths, ILogger logger)
    {
        Paths = paths ?? throw new ArgumentNullException(nameof(paths));
        Log = logger ?? new VoidLogger();
    }

    public async Task<bool> TryLoadCurrentModValuesAsync(ModData mod, CancellationToken ct)
    {
        await Semaphore.WaitAsync(ct);
        try
        {
            string modValuesPath = Path.Combine(Paths.ModValuesDir, MOD_VALUES.Replace(MOD_VALUES_TOKEN, mod.Name));

            if (!File.Exists(modValuesPath))
                return false;

            XDocument doc = await IOUtils.TryLoadXDocumentAsync(modValuesPath, Log, ct);
            if (doc == null)
                return false;

            XElement rootNode = doc.Element("mod");
            if (rootNode == null)
                return false;

            // TODO: Improve deserialization of settings by using version:
            string currentVersion = rootNode.Attribute("version")?.Value;

            mod.IsEnabled = bool.TryParse(rootNode.Attribute("enabled")?.Value ?? "true", out bool enabled) && enabled;
            mod.CustomId = int.TryParse(rootNode.Attribute("customID")?.Value ?? "0", out int customID) ? customID : 0;

            XElement versionNode = rootNode.Elements("version")?.FirstOrDefault(v => v.Attribute("v")?.Value == mod.Version.ToString());
            if (versionNode == null)
                return false;

            bool atLeastOneVariableValueIsMissing = false;

            XElement[] variableNodes = versionNode.Elements("var").ToArray();
            foreach (VarData variable in mod.Variables.Where(variable => !variable.IsSeparator))
            {
                ct.ThrowIfCancellationRequested();

                XElement variableNode = variableNodes.FirstOrDefault(variableNode => variableNode.Attribute("name")?.Value == variable.Name);
                if (variableNode == null)
                {
                    atLeastOneVariableValueIsMissing = true; // read all what we can first!
                    continue;
                }
                variable.CurrentValue = variableNode.Attribute("value")?.Value ?? string.Empty;
            }

            // Done.
            return !atLeastOneVariableValueIsMissing;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.ModValuesDir);
            return false;
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public async Task<bool> TryLoadPreviousModValuesAsync(ModData mod, bool setToCurrentValue, CancellationToken ct)
    {
        await Semaphore.WaitAsync(ct);
        try
        {
            string modValuesPath = Path.Combine(Paths.ModValuesDir, MOD_VALUES.Replace(MOD_VALUES_TOKEN, mod.Name));

            if (!File.Exists(modValuesPath))
                return false;

            XDocument doc = await IOUtils.TryLoadXDocumentAsync(modValuesPath, Log, ct);
            if (doc == null)
                return false;

            XElement rootNode = doc.Element("mod");
            if (rootNode == null)
                return false;

            // TODO: Improve deserialization of settings by using version:
            string oldVersion = rootNode.Attribute("version")?.Value;
            if (setToCurrentValue)
            {
                Log.Warn($@"[{mod.Name}] Importing mod values from previous mod version {oldVersion} to new mod version {mod.Version}");
                mod.IsEnabled = bool.TryParse(rootNode.Attribute("enabled")?.Value ?? "true", out bool enabled) && enabled;
                mod.CustomId = int.TryParse(rootNode.Attribute("customID")?.Value ?? "0", out int customID) ? customID : 0;
            }

            // Get the closest older version:
            XElement versionNode =
                rootNode
                .Elements("version")?
                .Select(node => (node, new VersionInfo(node.Attribute("v")?.Value)))
                .OrderByDescending(t => t.Item2)
                .FirstOrDefault(t => mod.Version > t.Item2)
                .Item1;

            // If no previous version was found:
            if (versionNode == null)
            {
                foreach (VarData variable in mod.Variables)
                    variable.PreviousValue = string.Empty;
                return false;
            }

            // Read previous version values:
            XElement[] variableNodes = versionNode.Elements("var").ToArray();
            foreach (VarData variable in mod.Variables.Where(variable => !variable.IsSeparator))
            {
                ct.ThrowIfCancellationRequested();

                XElement variableNode = variableNodes.FirstOrDefault(variableNode => variableNode.Attribute("name")?.Value == variable.Name);

                variable.PreviousValue =
                    variableNode == null ? string.Empty :
                    variableNode.Attribute("value")?.Value ?? string.Empty;

                if (setToCurrentValue && variableNode != null)
                    variable.CurrentValue = variable.PreviousValue;
            }

            // Done.
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.ModValuesDir);
            return false;
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public async Task<bool> TrySaveModValuesAsync(ModData mod, bool onlyModified, CancellationToken ct)
    {
        await Semaphore.WaitAsync(ct);
        try
        {
            string modValuesPath = Path.Combine(Paths.ModValuesDir, MOD_VALUES.Replace(MOD_VALUES_TOKEN, mod.Name));

            // Read or create document:
            XDocument doc = File.Exists(modValuesPath) ? await IOUtils.TryLoadXDocumentAsync(modValuesPath, Log, ct) ?? new() : new();

            // Read or create root node:
            XElement rootNode = doc.Element("mod");
            if (rootNode == null)
            {
                rootNode = new("mod");
                doc.Add(rootNode);
            }
            rootNode.SetAttributeValue("name", mod.Name);
            rootNode.SetAttributeValue("enabled", mod.IsEnabled);
            rootNode.SetAttributeValue("customID", mod.CustomId);

            // TODO: Improve serialization of settings by using version:
            rootNode.SetAttributeValue("version", "1.0.0.0");

            // Read or create version node:
            XElement versionNode =
                rootNode.Elements("version")?
                .FirstOrDefault(v => mod.Version.Equals(new VersionInfo(v.Attribute("v")?.Value)));
            if (versionNode == null)
            {
                onlyModified = false; // First time saving this version => everything needs to be saved
                versionNode = new XElement("version");
                versionNode.SetAttributeValue("v", mod.Version);
                rootNode.AddFirst(versionNode);
            }

            // Read or create variables:
            XElement[] variableNodes = versionNode.Elements("var").ToArray();
            foreach (VarData variable in mod.Variables.Where(v => !onlyModified || v.IsModified))
            {
                ct.ThrowIfCancellationRequested();

                if (variable.IsSeparator)
                {
                    variable.IsModified = false;
                    continue;
                }
                XElement variableNode = variableNodes.FirstOrDefault(variableNode => variableNode.Attribute("name")?.Value == variable.Name);
                if (variableNode == null)
                {
                    variableNode = new("var");
                    variableNode.SetAttributeValue("name", variable.Name);
                    versionNode.Add(variableNode);
                }
                variableNode.SetAttributeValue("value", variable.CurrentValue ?? string.Empty);
                variable.IsModified = false;
            }
            mod.IsModified = false;

            // Save file:
            if (!await IOUtils.TrySaveXDocumentAsync(modValuesPath, doc, Log, ct))
                return false;

            // Done.
            Log.Debug($@"File saved: ""{modValuesPath}""");
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.ModValuesDir);
            return false;
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public async Task<OrderedDictionary<string, ModData>> TryLoadModSorting(OrderedDictionary<string, ModData> original, CancellationToken ct)
    {
        await Semaphore.WaitAsync(ct);
        try
        {
            XDocument doc = File.Exists(Paths.ModListPath) ? await IOUtils.TryLoadXDocumentAsync(Paths.ModListPath, Log, ct) : null;
            if (doc == null)
            {
                await TrySaveModSortingInternal(original.Values, ct);
                return original;
            }

            XElement rootNode = doc.Element("mods");
            if (rootNode == null)
            {
                await TrySaveModSortingInternal(original.Values, ct);
                return original;
            }

            // TODO: Improve deserialization of settings by using version:
            string version = rootNode.Attribute("version")?.Value;

            // Add existing ones:
            OrderedDictionary<string, ModData> sorted = new();
            foreach (XElement modNode in rootNode.Elements("mod") ?? [])
            {
                ct.ThrowIfCancellationRequested();

                string name = modNode.Attribute("name").Value;
                if (original.TryGetValue(name, out ModData mod))
                    sorted.TryAdd(name, mod);
            }

            // Add missing ones:
            foreach (ModData mod in original.Values.Where(mod => !sorted.ContainsKey(mod.Name)))
                sorted.TryAdd(mod.Name, mod);

            // Update:
            await TrySaveModSortingInternal(sorted.Values, ct);

            // Done.
            return sorted;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.WorkDir);
            return original;
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public async Task<bool> TrySaveModSorting(IEnumerable<ModData> mods, CancellationToken ct)
    {
        await Semaphore.WaitAsync(ct);
        try
        {
            return await TrySaveModSortingInternal(mods, ct);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private async Task<bool> TrySaveModSortingInternal(IEnumerable<ModData> mods, CancellationToken ct)
    {
        try
        {
            XDocument doc = new();
            XElement rootNode = new("mods");

            // TODO: Improve serialization of settings by using version:
            rootNode.SetAttributeValue("version", "1.0.0.0");

            doc.Add(rootNode);

            foreach (ModData mod in mods)
            {
                XElement modNode = new("mod");
                modNode.SetAttributeValue("name", mod.Name);
                rootNode.Add(modNode);
            }

            if (!await IOUtils.TrySaveXDocumentAsync(Paths.ModListPath, doc, Log, ct))
                return false;

            // Done.
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.WorkDir);
            return false;
        }
    }
}
