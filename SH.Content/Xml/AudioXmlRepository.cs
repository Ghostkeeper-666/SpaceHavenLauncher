using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Content.Enums;
using SH.Content.Xml.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SH.Content.Xml;

public sealed class AudioXmlRepository
{
    public AudioXmlRepository(ILogger log)
    {
        Log = log ?? new VoidLogger();
    }

    private readonly ILogger Log;

    public OrderedDictionary<string, AudioXml> ByName { get; } = [];
    public OrderedDictionary<int, AudioXml> ById { get; } = [];

    public async Task<bool> TryReadAsync(string audioXmlPath, CancellationToken ct, IProgressInfo progress)
    {
        ByName.Clear();
        try
        {
            XDocument doc = await IOUtils.TryLoadXDocumentAsync(audioXmlPath, Log, ct);
            if (doc == null)
                return false;
            XElement root = doc?.Element("audio") ?? throw new Exception("Invalid XML root");
            List<XElement> audios = root?.Elements("a")?.ToList() ?? [];

            double delta = 1.0 / audios.Count;

            foreach (XElement a in audios)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    AudioXml audio = new()
                    {
                        Name = a.Attribute("n").Value,
                        Id = Convert.ToInt32(a.Attribute("id").Value),
                        Type = Enum.Parse<EAudioType>(a.Attribute("at").Value),
                        Path = a.Attribute("mp3")?.Value ?? a.Attribute("ogg")?.Value,
                        Format =
                            a.Attribute("mp3") != null ? EAudioFormat.mp3 : a.Attribute("ogg") != null ? EAudioFormat.ogg : throw new Exception("Invalid audio format"),
                        Length = Convert.ToSingle(a.Attribute("mp3l")?.Value ?? a.Attribute("oggl")?.Value ?? "0"),
                        Scope = Enum.Parse<ESoundType>(a.Attribute("st")?.Value ?? "None"),
                        Volume = Convert.ToInt32(a.Attribute("vo").Value),
                    };
                    ByName[audio.Name] = audio;
                    ById[audio.Id] = audio;
                }
                finally
                {
                    progress?.IncrementNormalized(delta);
                }
            }

            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex);
            return false;
        }

    }
}
