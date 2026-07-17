using SH.Content.Enums;
using SH.Content.Xml;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SH.Modding.Build;

internal sealed class AudioBuildData
{
    public static readonly string ATTRIBUTE_NAME = "n";
    public static readonly string ATTRIBUTE_ID = "id";
    public static readonly string ATTRIBUTE_AUDIO_TYPE = "at";
    public static readonly string ATTRIBUTE_SOUND_TYPE = "st";

    public static readonly string ATTRIBUTE_MP3 = "mp3";
    public static readonly string ATTRIBUTE_MP3L = "mp3l";

    public static readonly string ATTRIBUTE_OGG = "ogg";
    public static readonly string ATTRIBUTE_OGGL = "oggl";

    public static readonly string ATTRIBUTE_FILENAME = "filename";

    public AudioBuildData(BuildPathData paths, ModBuildData mod, XElement xml, ILogger log)
    {
        Paths = paths ?? throw new ArgumentNullException(nameof(paths));
        Mod = mod ?? throw new ArgumentNullException(nameof(mod));
        Xml = xml ?? throw new ArgumentNullException(nameof(xml));
        Log = log ?? new VoidLogger();
    }

    public XElement Xml { get; }
    public BuildPathData Paths { get; }
    public ModBuildData Mod { get; }

    private ILogger Log { get; }

    public string Name { get; private set; }
    public int Id { get; private set; }
    public EAudioType AudioType { get; private set; }
    public ESoundType SoundType { get; private set; }
    public EAudioFormat AudioFormat { get; private set; }
    public string FileExtension => AudioFormat.ToString();

    public string Filename => TargetRelativePath.GetFileName();
    public string TargetRelativePath { get; private set; }
    public string SourceRelativePath { get; private set; }
    public string SourceAbsolutePath { get; private set; }

    public bool IsOriginalAudioFile { get; private set; }

    public string LibraryOperation { get; private set; }
    public string PatchOperation { get; private set; }
    public string LastOperation => PatchOperation ?? LibraryOperation ?? Mod?.Name ?? "???";


    public bool TryParse(IEnumerable<ModBuildData> mods)
    {
        ArgumentNullException.ThrowIfNull(mods);
        try
        {
            // Modding metadata:
            LibraryOperation = Xml.Attribute(NodeType.ATTRIBUTE_LIBRARY)?.Value;
            PatchOperation = Xml.Attribute(NodeType.ATTRIBUTE_PATCH)?.Value;

            // Audio name:
            if ((Name = Xml.Attribute(ATTRIBUTE_NAME)?.Value).IsNullOrWhiteSpace())
            {
                Log.Error($@"Missing or invalid '{ATTRIBUTE_NAME}' in {this}", Paths.BuildAudioFile);
                return false;
            }
            if (!Name.Contains('_'))
            {
                // Space Haven's audio system stops working if audio name does not have at least one underscore character...
                Log.Warn($"The '{ATTRIBUTE_NAME}' attribute must have least 1 underscore character '_' otherwise the game's audio system could get muted - in {this}", Paths.BuildAudioFile);
            }

            // Audio ID:
            if (!(Xml.Attribute(ATTRIBUTE_ID)?.Value).TryParse(out int id) || id < 0 || id >= int.MaxValue)
            {
                Log.Error($@"Missing or invalid '{ATTRIBUTE_ID}' attribute in {this}", Paths.BuildAudioFile);
                return false;
            }
            Id = id;

            // Audio type:
            if (!(Xml.Attribute(ATTRIBUTE_AUDIO_TYPE)?.Value).TryParse(out EAudioType audioType))
            {
                Log.Error($@"Missing or invalid '{ATTRIBUTE_AUDIO_TYPE}' attribute in {this}", Paths.BuildAudioFile);
                return false;
            }
            AudioType = audioType;

            // Target relative path:
            string mp3Path = Xml.Attribute(ATTRIBUTE_MP3)?.Value;
            string oggPath = Xml.Attribute(ATTRIBUTE_OGG)?.Value;
            TargetRelativePath = (mp3Path ?? oggPath).AsStdPath();

            // Audio format:
            string fileExtension = TargetRelativePath.GetFileExtension().Trim('.');
            if (!fileExtension.TryParse(out EAudioFormat audioFormat))
            {
                Log.Error($@"Unknown audio format '{fileExtension}' in {this}", Paths.BuildAudioFile);
                return false;
            }

            // Some attributes must match the file extension (congratulations for this design!):
            switch (audioFormat)
            {
                case EAudioFormat.mp3:
                    if ((Xml.Attribute(ATTRIBUTE_MP3)?.Value).IsNullOrWhiteSpace())
                    {
                        Log.Error($@"Attribute '{ATTRIBUTE_MP3}' is undefined for audio file with format '{AudioFormat}' in {this}", Paths.BuildAudioFile);
                        return false;
                    }
                    if ((Xml.Attribute(ATTRIBUTE_MP3L)?.Value).IsNullOrWhiteSpace())
                    {
                        Log.Error($@"Attribute '{ATTRIBUTE_MP3L}' is undefined for audio file with format '{AudioFormat}' in {this}", Paths.BuildAudioFile);
                        return false;
                    }
                    break;

                case EAudioFormat.ogg:
                    if ((Xml.Attribute(ATTRIBUTE_OGG)?.Value).IsNullOrWhiteSpace())
                    {
                        Log.Error($@"Attribute '{ATTRIBUTE_OGG}' is undefined for audio file with format '{AudioFormat}' in {this}", Paths.BuildAudioFile);
                        return false;
                    }
                    if ((Xml.Attribute(ATTRIBUTE_OGGL)?.Value).IsNullOrWhiteSpace())
                    {
                        Log.Error($@"Attribute '{ATTRIBUTE_OGGL}' is undefined for audio file with format '{AudioFormat}' in {this}", Paths.BuildAudioFile);
                        return false;
                    }
                    break;

                default:
                    throw new NotImplementedException($"{nameof(audioFormat)} = {audioFormat}");
            }

            // Warn about filename missing an underscore character:
            if (!Filename.Contains('_'))
            {
                // Space Haven's audio system stops working if audio name does not have at least one underscore character...
                Log.Warn($"The filename '{Filename}' should contain at least 1 underscore character '_' otherwise the game's audio system could get muted - in {this}", Paths.BuildAudioFile);
            }

            // Sound type:
            ESoundType soundType = SoundType = ESoundType.None;
            if (AudioType == EAudioType.Music)
                SoundType = ESoundType.None;
            else if ((Xml.Attribute(ATTRIBUTE_SOUND_TYPE)?.Value).TryParse(out soundType))
                SoundType = soundType;
            else
            {
                Log.Error($@"Missing or invalid '{ATTRIBUTE_SOUND_TYPE}' attribute in {this}", Paths.BuildAudioFile);
                return false;
            }

            // Locate audio file
            if (!TryLocateAudioFile())
                return false;

            // Done.
            return true;
        }
        catch (Exception ex)
        {
            // Failure:
            Log.Error($@"Unable to parse {this}: {Environment.NewLine}{ex}", Paths.BuildAudioFile);
            return false;
        }
    }

    private bool TryLocateAudioFile()
    {
        try
        {
            // Path provided by the 'filename' attribute:
            SourceRelativePath = Xml.Attribute(ATTRIBUTE_FILENAME)?.Value.AsOSPath();
            if (!SourceRelativePath.IsNullOrWhiteSpace())
            {
                SourceAbsolutePath = IOUtils.CombineAsOSPath(Mod.AudioDir, SourceRelativePath).FindFile();

                // Invalid path:
                if (SourceAbsolutePath.IsNullOrWhiteSpace())
                {
                    // Fail, since the explicitly defined path could not be found:
                    Log.Error($@"Invalid mod audio file path ""{SourceRelativePath}"" defined by attribute '{ATTRIBUTE_FILENAME}' in {this}", Paths.BuildAudioFile);
                    return false;
                }

                // Validate paths escaping mod audio dir:
                if (!SourceAbsolutePath.EscapesDirectory(Mod.AudioDir))
                {
                    Log.Error($@"Audio file path ""{SourceRelativePath}"" escapes mod directory ""{Mod.AudioDir}"", defined by attribute '{ATTRIBUTE_FILENAME}' in {this}", Paths.BuildAudioFile);
                    return false;
                }

                // Done.
                Log.Debug($@"Using mod audio file ""{SourceAbsolutePath}"" for {this}", Paths.BuildAudioDirectory);
                return true;
            }

            string relativePathPrefix = "library/";
            SourceRelativePath = TargetRelativePath.AsStdPath().RemovePrefix(relativePathPrefix).AsOSPath();
            string filename = SourceRelativePath.GetFileName();

            // Relative path within the mod's audio directory:
            SourceAbsolutePath = IOUtils.CombineAsOSPath(Mod.AudioDir, SourceRelativePath).FindFile();
            if (!SourceAbsolutePath.IsNullOrWhiteSpace())
            {
                Log.Debug($@"Using mod audio file ""{SourceAbsolutePath}"" for {this}", Paths.BuildAudioFile);
                return true;
            }

            // File directly under the mod's audio directory:
            SourceAbsolutePath = IOUtils.CombineAsOSPath(Mod.AudioDir, filename).FindFile();
            if (!SourceAbsolutePath.IsNullOrWhiteSpace())
            {
                SourceRelativePath = filename;
                Log.Debug($@"Using mod audio file ""{SourceAbsolutePath}"" for {this}", Paths.BuildAudioFile);
                return true;
            }

            // Original audio file (uncommon, warn!):
            SourceAbsolutePath = IOUtils.CombineAsOSPath(Paths.BuildStageLibraryDirectory, SourceRelativePath).FindFile();
            if (!SourceAbsolutePath.IsNullOrWhiteSpace())
            {
                IsOriginalAudioFile = true;
                Log.Warn($@"Using the ORIGINAL audio file ""{SourceAbsolutePath}"" for {this}", Paths.BuildAudioFile);
                return true;
            }

            // Audio file not found!
            Log.Error($@"Unable to locate audio file '{TargetRelativePath}' referenced by {this}", Paths.BuildAudioFile);
            return false;
        }
        catch (Exception ex)
        {
            // Failure:
            Log.Error($@"Unable to locate audio file '{TargetRelativePath}' referenced by {this}: {Environment.NewLine}{ex}", Paths.BuildAudioFile);
            return false;
        }
    }

    public override string ToString() =>
        $@"audio entry {ATTRIBUTE_ID}={Id} {ATTRIBUTE_NAME}=""{Name}"" in audio XML file line {Xml.Line()}, last modified by {LastOperation}";
}
