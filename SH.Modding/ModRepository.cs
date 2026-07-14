using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Modding;

public sealed class ModRepository
{
    private readonly ILogger Log;

    public ModRepository(ILogger log)
    {
        Log = log ?? new VoidLogger();
    }

    /// <summary>
    /// Loads all mods
    /// </summary>
    public async Task<OrderedDictionary<string, ModData>> TryLoadMods(IEnumerable<string> modsRootDirs, CancellationToken ct, IProgressInfo progress)
    {
        OrderedDictionary<string, ModData> mods = new();
        try
        {
            progress?.Start();

            // Locate mods:
            int modErrors = 0;
            List<string> modDirs = [];
            foreach (string modsRootDir in modsRootDirs)
            {
                ct.ThrowIfCancellationRequested();

                string evaluatedModsRootDir = modsRootDir.EvaluatePath();
                if (!IOUtils.DirExists(evaluatedModsRootDir))
                {
                    Log.Error($@"Mods ROOT directory not found: ""{modsRootDir}""");
                    continue;
                }
                modDirs.AddRange(
                    modsRootDir.GetDirs(ESearchOption.TopDir)
                    .Where(modDir =>
                        IOUtils.FileExists(IOUtils.CombineAsOSPath(modDir, ModdingConstants.INFO_XML)) ||
                        IOUtils.FileExists(IOUtils.CombineAsOSPath(modDir, ModdingConstants.INFO_XML.GetFileNameWithoutExtension()))
                ));
            }

            // Parse mods:
            int count = 0;
            foreach (string modDir in modDirs)
            {
                ct.ThrowIfCancellationRequested();
                progress?.SetNormalized(count++ / (1.0 + modDirs.Count));

                // Load mod:
                ModData mod = await ModData.TryLoad(modDir, Log, ct);
                if (mod == null)
                {
                    ++modErrors;
                    Log.Error($@"This mod contains errors and could not be loaded properly: ""{modDir}""", modDir);
                    continue;
                }

                // Add mod:
                if (!mods.TryAdd(mod.Name, mod))
                {
                    ++modErrors;
                    Log.Error($@"The mod '{mod.Name}' could not be loaded twice. Please check for duplicate mods in your mod root directories. Keeping ""{mods[mod.Name].Dir}"" and skipping ""{mod.Dir}""", mod.Dir);
                    continue;
                }
                Log.Debug($@"Mod '{mod.Name}' was loaded successfully", mod.Dir);
            }

            // Initially just sort by name (will be sorted again somewhere else):
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
}
