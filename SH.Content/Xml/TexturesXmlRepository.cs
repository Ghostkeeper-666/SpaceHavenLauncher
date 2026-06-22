using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Content.Xml.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SH.Content.Xml;

public sealed class TexturesXmlRepository
{
    public TexturesXmlRepository(ILogger logger)
    {
        Log = logger ?? new VoidLogger();
    }

    private readonly ILogger Log;

    public OrderedDictionary<int, TextureXml> ById { get; } = [];

    public async Task<bool> TryReadAsync(string texturesXmlPath, CancellationToken ct, IProgressInfo progress)
    {
        try
        {
            ById.Clear();

            XDocument doc = await IOUtils.TryLoadXDocumentAsync(texturesXmlPath, Log, ct);
            if (doc == null)
                return false;
            XElement root = doc?.Element("AllTexturesAndRegions") ?? throw new Exception("Invalid XML root");
            List<XElement> textures = root?.Element("textures")?.Elements("t")?.ToList() ?? [];
            List<XElement> regions = root?.Element("regions")?.Elements("re")?.ToList() ?? [];

            double delta = 1.0 / (textures.Count + regions.Count);

            foreach (XElement t in textures)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    TextureXml texture = new(
                        (int)t.Attribute("i"),
                        (int)t.Attribute("w"),
                        (int)t.Attribute("h"),
                        (int?)t.Attribute("f") ?? 0,
                        (int?)t.Attribute("min") ?? 0,
                        (int?)t.Attribute("max") ?? 0);

                    ById.Add(texture.Id, texture);
                }
                finally
                {
                    progress.IncrementNormalized(delta);
                }
            }

            foreach (XElement x in regions)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    if (!int.TryParse(x.Attribute("t").Value, out int textureId) ||
                        textureId < 0 || textureId > ById.Count ||
                        !ById.TryGetValue(textureId, out TextureXml texture))
                    {
                        Log.Error($"Unexpected texture id={textureId} in: {x}");
                        continue;
                    }

                    TextureRegionXml region = new(
                        texture,
                        Convert.ToInt32(x.Attribute("n").Value),
                        Convert.ToInt32(x.Attribute("id").Value),
                        Convert.ToInt32(x.Attribute("x").Value),
                        Convert.ToInt32(x.Attribute("y").Value),
                        Convert.ToInt32(x.Attribute("w").Value),
                        Convert.ToInt32(x.Attribute("h").Value)
                    );

                    texture.RegionsByName.Add(region.Name, region);
                }
                finally
                {
                    progress?.IncrementNormalized(delta);
                }
            }

            progress?.Complete();
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
