using SH.Content.Enums;
using SH.Content.Xml;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using System;
using System.IO;
using System.Xml.Linq;

namespace SH.Content.Modding.Build;

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

    public AudioBuildData(BuildPathData paths, ModBuildData mod, XmlFile modXmlFile, XElement xml)
    {
        Paths = paths ?? throw new ArgumentNullException(nameof(paths));
        Mod = mod ?? throw new ArgumentNullException(nameof(mod));
        ModXmlFile = modXmlFile ?? throw new ArgumentNullException(nameof(modXmlFile));
        Xml = xml ?? throw new ArgumentNullException(nameof(xml));
    }

    public XmlFile ModXmlFile { get; }
    public XElement Xml { get; }
    public BuildPathData Paths { get; }
    public ModBuildData Mod { get; }

    private ILogger Log => Mod.Log;

    public string Name { get; private set; }
    public int Id { get; private set; }
    public EAudioType AudioType { get; private set; }
    public ESoundType SoundType { get; private set; }
    public EAudioFormat AudioFormat { get; private set; }
    public string FileExtension => AudioFormat.ToString();

    public string TargetRelativePath { get; private set; }
    public string SourceRelativePath { get; private set; }
    public string SourceAbsolutePath { get; private set; }

    public bool IsExistingAudioFile { get; private set; }

    public string XmlLocation => $@"in file=""{ModXmlFile.RelativePath}"" line={Xml.Line()}";

    public bool TryParse()
    {
        try
        {
            // Audio name:
            if ((Name = Xml.Attribute(ATTRIBUTE_NAME)?.Value).IsNullOrWhiteSpace())
            {
                Log.Error($@"Audio entry has missing or invalid '{ATTRIBUTE_NAME}' attribute {XmlLocation}", ModXmlFile.Path);
                return false;
            }

            if (!Name.Contains('_'))
            {
                // Space Haven's audio system stops working if new audio entries do not have at least one underscore character...
                Log.Warn($"Audio entry name '{Name}' does not have at least 1 underscore character '_' => this could stop the game's audio system completely!", ModXmlFile.Path);
            }

            // Audio ID:
            if (!(Xml.Attribute(ATTRIBUTE_ID)?.Value).TryParse(out int id) || id < 0 || id >= int.MaxValue)
            {
                Log.Error($@"Audio entry has missing or invalid '{ATTRIBUTE_ID}' attribute {XmlLocation}. {Environment.NewLine}The id attribute must be a number within the range (0 - {int.MaxValue - 1})", ModXmlFile.Path);
                return false;
            }
            Id = id;

            // Audio type:
            if (!(Xml.Attribute(ATTRIBUTE_AUDIO_TYPE)?.Value).TryParse(out EAudioType audioType))
            {
                Log.Error($@"Audio entry has missing or invalid '{ATTRIBUTE_AUDIO_TYPE}' attribute {XmlLocation}", ModXmlFile.Path);
                return false;
            }
            AudioType = audioType;

            // Target relative path:
            string mp3Path = Xml.Attribute(ATTRIBUTE_MP3)?.Value;
            string oggPath = Xml.Attribute(ATTRIBUTE_OGG)?.Value;
            TargetRelativePath = (mp3Path ?? oggPath).AsStdPath();

            // Audio codec:
            AudioFormat =
                mp3Path != null ? EAudioFormat.mp3 :
                oggPath != null ? EAudioFormat.ogg :
                throw new NotImplementedException(nameof(AudioFormat));

            // Validate audio format:
            string actualFileExtension = Path.GetExtension(TargetRelativePath)?.TrimStart('.')?.ToLowerInvariant() ?? string.Empty;
            if (actualFileExtension != AudioFormat.ToString())
            {
                Log.Error($@"Audio entry has a conflicting audio format '{AudioFormat}' with either '{ATTRIBUTE_MP3}' or '{ATTRIBUTE_OGG}' attribute {XmlLocation}", ModXmlFile.Path);
                return false;
            }

            // Validate audio length:
            if (AudioFormat == EAudioFormat.mp3 && Xml.Attribute(ATTRIBUTE_MP3L)?.Value == null ||
                AudioFormat == EAudioFormat.ogg && Xml.Attribute(ATTRIBUTE_OGGL)?.Value == null)
            {
                Log.Error($@"Audio entry has a conflicting audio format '{AudioFormat}' with either '{ATTRIBUTE_MP3L}' or '{ATTRIBUTE_OGGL}' attribute {XmlLocation}", ModXmlFile.Path);
                return false;
            }

            // Sound type:
            ESoundType soundType = SoundType = ESoundType.None;
            if (AudioType == EAudioType.Music)
                SoundType = ESoundType.None;
            else if ((Xml.Attribute(ATTRIBUTE_SOUND_TYPE)?.Value).TryParse(out soundType))
                SoundType = soundType;
            else
            {
                Log.Error($@"Audio entry has missing or invalid '{ATTRIBUTE_SOUND_TYPE}' attribute {XmlLocation}", ModXmlFile.Path);
                return false;
            }

            // Locate audio file
            if (!TryFindAudioFile())
                return false;

            // Done.
            return true;
        }
        catch (Exception ex)
        {
            // Failure:
            Log.Error($@"Unable to parse audio entry {ATTRIBUTE_NAME}='{Name}' {XmlLocation}: {Environment.NewLine}{ex}", ModXmlFile.Path);
            return false;
        }
    }

    private bool TryFindAudioFile()
    {
        try
        {
            string relativePathPrefix = "library";
            string relativePath = TargetRelativePath?.RemovePrefix(relativePathPrefix).TrimStart('/', '\\');

            // Source relative path provided by attribute:
            SourceRelativePath = Xml.Attribute(ATTRIBUTE_FILENAME)?.Value?.AsOSPath();
            if (!SourceRelativePath.IsNullOrWhiteSpace())
            {
                SourceAbsolutePath = IOUtils.CombinePathAsOS(Mod.AudioDirectory, SourceRelativePath);
                if (IOUtils.FileExists(SourceAbsolutePath))
                {
                    Log.Debug($"Audio entry '{Name}' was mapped to audio file '{SourceAbsolutePath}'", ModXmlFile.Path);
                    return true;
                }
                else
                {
                    // Fail, since the explicitly defined path was not found:
                    Log.Error($@"Audio entry has an invalid relative path defined by the '{ATTRIBUTE_FILENAME}' attribute {XmlLocation}", ModXmlFile.Path);
                    return false;
                }
            }

            // In same relative directory?
            SourceRelativePath = relativePath.AsOSPath();
            SourceAbsolutePath = IOUtils.CombinePathAsOS(Mod.AudioDirectory, SourceRelativePath);
            if (IOUtils.FileExists(SourceAbsolutePath))
            {
                Log.Debug($"Audio entry '{Name}' was mapped to audio file '{SourceAbsolutePath}'", ModXmlFile.Path);
                return true;
            }

            // In mod's root audio directory?
            SourceRelativePath = Path.GetFileName(relativePath).AsOSPath();
            SourceAbsolutePath = IOUtils.CombinePathAsOS(Mod.AudioDirectory, SourceRelativePath);
            if (IOUtils.FileExists(SourceAbsolutePath))
            {
                Log.Debug($"Audio entry '{Name}' was mapped to audio file '{SourceAbsolutePath}'", ModXmlFile.Path);
                return true;
            }

            // Is the audio entry using an existing audio file?
            SourceRelativePath = relativePath.AsOSPath();
            SourceAbsolutePath = IOUtils.CombinePathAsOS(Paths.BuildStageLibraryDirectory, relativePath);
            if (IOUtils.FileExists(SourceAbsolutePath))
            {
                // Warn, since this could eventually not be the intention in this mod:
                Log.Warn($"Audio entry '{Name}' was mapped to the existing audio file '{SourceAbsolutePath}' because no such file was found within the mod's audio directory", ModXmlFile.Path);
                return true;
            }

            // Not found.
            Log.Error($@"Unable to locate audio file '{TargetRelativePath}' referenced by audio entry '{Name}' {XmlLocation}. {Environment.NewLine}Check for case-sensitive paths, lowercase file extensions, or missing audio file", ModXmlFile.Path);
            return false;
        }
        catch (Exception ex)
        {
            // Failure:
            Log.Error($@"Unable to locate audio file '{TargetRelativePath}' referenced by audio entry '{Name}' {XmlLocation}: {Environment.NewLine}{ex}", ModXmlFile.Path);
            return false;
        }
    }


    public override string ToString() => Name;
}
