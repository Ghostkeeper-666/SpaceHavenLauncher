using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Content.Art;
using SH.Content.Video;
using SH.Content.Xml;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace SH.Content.Test;

public sealed class LibraryTest
{
    public AnimationsXmlRepository AnimationsXmlRepository { get; }
    public AudioXmlRepository AudioXmlRepository { get; }
    public HavenXmlRepository HavenXmlRepository { get; }
    public TextsXmlRepository TextsXmlRepository { get; }
    public TexturesXmlRepository TexturesXmlRepository { get; }

    public ArtRepository ArtRepository { get; }

    public AnimationRenderer AnimationRenderer { get; private set; }


    private readonly ILogger Log;



    public LibraryTest(ILogger logger)
    {
        Log = logger ?? new VoidLogger();
        AnimationsXmlRepository = new(Log);
        AudioXmlRepository = new(Log);
        HavenXmlRepository = new(Log);
        TextsXmlRepository = new(Log);
        TexturesXmlRepository = new(Log);
        ArtRepository = new(TexturesXmlRepository, AnimationsXmlRepository, Log);
    }



    public async Task<bool> TryRunAsync()
    {
        try
        {
            Log.Info("=============================================================================");
            Log.Info($"Current Directory: {Directory.GetCurrentDirectory()}");
            Log.Info("=============================================================================");

            string localAppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string laucherDir = IOUtils.CombineAsOSPath(localAppDataDir, @"SpaceHavenLauncher");

            string originalDir = IOUtils.CombineAsOSPath(laucherDir, @"template\base\library");
            string modifiedDir = IOUtils.CombineAsOSPath(laucherDir, @"build\output\library");

            string inputDir = modifiedDir;
            string exportDir = IOUtils.CombineAsOSPath(laucherDir, "exported");
            string texturesExportDir = IOUtils.CombineAsOSPath(exportDir, "textures");
            string animationsExportDir = IOUtils.CombineAsOSPath(laucherDir, "animations");

            string animationsXmlPath = IOUtils.CombineAsOSPath(inputDir, "animations");
            string audioXmlPath = IOUtils.CombineAsOSPath(inputDir, "audio");
            string havenXmlPath = IOUtils.CombineAsOSPath(inputDir, "haven");
            string textsXmlPath = IOUtils.CombineAsOSPath(inputDir, "texts");
            string texturesXmlPath = IOUtils.CombineAsOSPath(inputDir, "textures");

            CancellationTokenSource cts = new();
            CancellationToken ct = cts.Token;
            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = ct,
            };

            // Create Base Output Directory
            if (!IOUtils.TryCreateDir(exportDir, Log))
                return false;

            // Textures
            Log.Info($"[textures.xml] Reading texture XML information from {texturesXmlPath}");
            ProgressInfo loadATexturesXml = new("Load textures XML");
            loadATexturesXml.ProgressChanged += ProgressChanged;
            if (!await TexturesXmlRepository.TryReadAsync(texturesXmlPath, ct, loadATexturesXml))
                throw new Exception("[textures.xml] Unable to read all textures XML information");
            Log.Info($"[textures.xml] {TexturesXmlRepository.ById.Count} texture definitions");
            Log.Info($"[textures.xml] {TexturesXmlRepository.ById.Values.Sum(t => t.RegionsByName.Count)} sprite definitions");

            // Animations
            Log.Info($@"[animations.xml] Reading animation XML information from ""{animationsXmlPath}""...");
            ProgressInfo loadAnimationsXml = new("Load animations XML");
            loadAnimationsXml.ProgressChanged += ProgressChanged;
            if (!await AnimationsXmlRepository.TryReadAsync(animationsXmlPath, ct, loadAnimationsXml))
                throw new Exception("[animations.xml] Unable to read all animations XML information");
            Log.Info($"[animations.xml] {AnimationsXmlRepository.ByName.Count} animation definitions");

            // Audio
            Log.Info($"[audio.xml] Reading audio XML information from {audioXmlPath}");
            ProgressInfo loadAudioXml = new("Load audio XML");
            loadAudioXml.ProgressChanged += ProgressChanged;
            if (!await AudioXmlRepository.TryReadAsync(audioXmlPath, ct, loadAudioXml))
                throw new Exception("[audio.xml] Unable to read all audio XML information");
            Log.Info($"[audio.xml] {AudioXmlRepository.ByName.Count} audio definitions");

            // Texts
            Log.Info($"[texts.xml] Reading text XML information from {textsXmlPath}");
            ProgressInfo loadTextsXml = new("Load texts XML");
            loadTextsXml.ProgressChanged += ProgressChanged;
            if (!await TextsXmlRepository.TryReadAsync(textsXmlPath, ct, loadTextsXml))
                throw new Exception("[texts.xml] Unable to read all texts XML information");
            Log.Info($"[texts.xml] {TextsXmlRepository.ById.Count} text definitions");


            // Load Art
            Log.Info($"Loading game art...");
            ProgressInfo loadGameArt = new("Load Game Art");
            loadGameArt.ProgressChanged += ProgressChanged;
            if (!await ArtRepository.TryLoadAsync(inputDir, ct, loadGameArt))
                throw new Exception("Unable to load all game art");
            Log.Info($"Game art was loaded");


            // Export all textures and all sprites to PNG files:
            Log.Info($"Exporting game art...");
            ProgressInfo exportSpriteSheets = new("Export Sprite Sheets");
            exportSpriteSheets.ProgressChanged += ProgressChanged;
            if (!await ArtRepository.TryExportSpriteSheetsToPngAsync(texturesExportDir, parallelOptions, exportSpriteSheets))
                throw new Exception("Unable to export all sprite sheets to PNG");

            ProgressInfo exportSprites = new("Export Sprites");
            exportSprites.ProgressChanged += ProgressChanged;
            if (!await ArtRepository.TryExportSpritesToPngAsync(texturesExportDir, parallelOptions, exportSprites))
                throw new Exception("Unable to export all sprites to PNG");
            Log.Info($"Game art was successfully exported");


            // Load Animations:
            Log.Info($"Loading animations...");
            ProgressInfo loadAnimations = new("Load Animations");
            loadAnimations.ProgressChanged += ProgressChanged;
            if (!ArtRepository.TryLoadAnimations(ct, loadAnimations))
                throw new Exception("Unable to load animations");
            Log.Info($"{ArtRepository.AnimationsByName.Count} animation(s) were loaded");


            // Render all animations to WEBP files:
            Log.Info($"Rendering animations...");
            ProgressInfo renderAnimations = new("Render Animations");
            renderAnimations.ProgressChanged += ProgressChanged;
            AnimationRenderer = new(ArtRepository.AnimationsByName, ArtRepository.SpritesByName, Log);
            await RenderAllAnimationsAsync(IOUtils.CombineAsOSPath(exportDir, "animations"), ct, renderAnimations);


            // Render just a specific animation:
            await ExportAnimationToWEBPAsync("airlockTestPiece", animationsExportDir, ct);

            // Haven
            Log.Info($"[haven.xml] Reading haven XML information from {texturesXmlPath}");
            if (!HavenXmlRepository.TryRead(havenXmlPath))
                throw new Exception("[haven.xml] Unable to read haven XML information");
            Log.Info($"[haven.xml] {HavenXmlRepository.Products.Count} product definitions");


            // Done.
            return true;
        }
        catch (Exception ex)
        {
            Log.Info(ex);
            return false;
        }
    }

    private async Task RenderAllAnimationsAsync(string renderedAnimationsDir, CancellationToken ct, IProgressInfo progress)
    {
        

        // Generate a progress update only 100 times:
        int pi = 0;
        double p = 0.0;
        double delta = 100.0 / (ArtRepository.AnimationsByName.Count);

        foreach (string animationName in ArtRepository.AnimationsByName.Keys)
        {
            string filename = IOUtils.CombineAsOSPath(renderedAnimationsDir, $"{animationName}.webp");
            if (IOUtils.FileExists(filename))
                continue;
            try
            {
                await ExportAnimationToWEBPAsync(animationName, renderedAnimationsDir, ct);
            }
            catch (Exception ex)
            {
                Log.Info(ex);
            }
            finally
            {
                // Generate a progress update only 100 times:
                if (pi < (int)(p += delta)) progress?.SetNormalized((pi = (int)p) / 100.0);
            }
        }
    }

    private void ProgressChanged(object sender, ProgressEventArgs e) =>
        Log.Debug($"{e.Progress.Name} ({e.Progress.NormalizedValue:0%})");

    private async Task ExportAnimationToWEBPAsync(string animationName, string outputDir, CancellationToken ct)
    {
        Clip clip = await AnimationRenderer.RenderClipAsync(animationName, 512, 512, TimeSpan.FromSeconds(10), false, ct);
        await WebpExporter.TryExportAsync(clip, outputDir, 16.0, 1.0, Log, default);
        clip.Dispose();
    }

}
