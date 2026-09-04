using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Content.Xml.Animations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using SH.Framework.Extensions;

namespace SH.Content.Xml;

public sealed class AnimationsXmlRepository
{
    public AnimationsXmlRepository(ILogger log)
    {
        Log = log ?? new VoidLogger();
    }

    private readonly ILogger Log;

    public OrderedDictionary<int, AnimationXml> ById { get; } = [];
    public OrderedDictionary<string, AnimationXml> ByName { get; } = [];

    public async Task<bool> TryReadAsync(string xmlPath, CancellationToken ct, IProgressInfo progress)
    {
        try
        {
            ById.Clear();

            XDocument doc = await IOUtils.TryLoadXDocumentAsync(xmlPath, Log, ct);
            if (doc == null)
                return false;
            XElement root = doc?.Element("AllAnimations") ?? throw new Exception("Invalid XML root");
            List<XElement> animations = root?.Element("animations")?.Elements("ba")?.ToList() ?? [];

            double delta = 1.0 / animations.Count;

            foreach (XElement ba in animations)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    AnimationXml animation = new()
                    {
                        Name = ba.Attribute("n").Value,
                        Id = Convert.ToInt32(ba.Attribute("id").Value),
                        FrameRate = Convert.ToInt32(ba.Attribute("f").Value),
                    };

                    animation.KeyFrames.AddRange(
                        ba.Attribute("ks").Value
                        .Split(',').Select(str => Convert.ToInt32(str))
                    );

                    XElement b = ba.Element("bones").Element("b");
                    if (!TryReadAnimationXmlBone(animation, b, out AnimationXml_Bone bone))
                        return false;
                    animation.Bone = bone;

                    foreach (XElement node in ba.Element("items").Elements("assetPos"))
                    {
                        if (!TryReadAnimationXmlItem(animation, node, out AnimationXml_Item item))
                            return false;

                        animation.Items.Add(item);
                    }

                    if (ByName.TryGetValue(animation.Name, out AnimationXml existingAnimation2))
                    {
                        Log.Info($"Ignoring animation with id={animation.Id} with SAME NAME={animation.Name} as animation with id={existingAnimation2.Id}");
                        continue;
                    }
                    else ByName[animation.Name] = animation;

                    // ID is not the primary key, so we ignore errors here...
                    if (ById.TryGetValue(animation.Id, out AnimationXml existingAnimation1))
                        Log.Debug($@"Animation with name=""{animation.Name}"" has SAME ID={animation.Id} as animation with name=""{existingAnimation1.Name}""");
                    else ById[animation.Id] = animation;
                }
                finally
                {
                    progress?.IncrementNormalized(delta);
                }
            }

            // Done.
            progress.Complete();
            return true;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex);
            return false;
        }

    }

    public bool TryReadAnimationXmlBone(AnimationXml animation, XElement b, out AnimationXml_Bone bone)
    {
        try
        {
            bone = new()
            {
                Animation = animation,
                Id = Convert.ToInt32(b.Attribute("id").Value),
            };

            foreach (XElement p in b.Element("pos").Elements("p"))
            {
                AnimationXml_BonePosition position = new()
                {
                    Bone = bone,
                    FrameId = Convert.ToInt32(p.Attribute("f").Value),
                    X = Convert.ToSingle(p.Attribute("x").Value),
                    Y = Convert.ToSingle(p.Attribute("y").Value),
                    ScaleX = Convert.ToSingle(p.Attribute("sx").Value),
                    ScaleY = Convert.ToSingle(p.Attribute("sy").Value),
                    Rotation = Convert.ToInt32(p.Attribute("r").Value),
                    ColorMask = Convert.ToInt32(p.Attribute("col").Value),
                };

                bone.Positions.Add(position);
            }

            foreach (XElement cb in b.Element("c")?.Elements("b") ?? [])
            {
                if (!TryReadAnimationXmlBone(animation, cb, out AnimationXml_Bone child))
                    return false;

                bone.Children.Add(child);
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            bone = null;
            return false;
        }
    }

    public bool TryReadAnimationXmlItem(AnimationXml animation, XElement assetPos, out AnimationXml_Item item)
    {
        try
        {
            string str;

            Dictionary<int, bool> visibility = new();

            item = new()
            {
                Animation = animation,
                BoneId = Convert.ToInt32(assetPos.Attribute("bi").Value),
                X = Convert.ToSingle(assetPos.Attribute("x").Value),
                Y = Convert.ToSingle(assetPos.Attribute("y").Value),
                ScaleX = Convert.ToSingle(assetPos.Attribute("sx").Value),
                ScaleY = Convert.ToSingle(assetPos.Attribute("sy").Value),
                Rotation = Convert.ToSingle(assetPos.Attribute("r").Value),
                SpriteName = (str = assetPos.Attribute("a")?.Value).IsNullOrWhiteSpace() ? -1 : Convert.ToInt32(str),

                Loop = !(str = assetPos.Attribute("l")?.Value).IsNullOrWhiteSpace() && Convert.ToInt32(str) != 0,
                StartFrame = (str = assetPos.Attribute("sf")?.Value).IsNullOrWhiteSpace() ? 0 : (float)Convert.ToSingle(str),
                EndFrame = (str = assetPos.Attribute("se")?.Value).IsNullOrWhiteSpace() ? 0 : (float)Convert.ToSingle(str),
                AnimationName = assetPos.Attribute("an")?.Value,
                Visibility = visibility,
            };

            foreach (string vf in assetPos.Attribute("vf")?.Value?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [])
            {
                string[] pair = vf.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (pair.Length != 2)
                {
                    Log.Debug($@"Ignoring assetPos ""{item.AnimationName ?? item.SpriteName.ToString()}"" malformed vf information in animation ""{animation.Name}""");
                    continue;
                }
                int frameId = Convert.ToInt32(pair[0]);
                if (visibility.ContainsKey(frameId))
                {
                    Log.Debug($@"Ignoring assetPos ""{item.AnimationName ?? item.SpriteName.ToString()}"" duplicate vf entry for frameId={frameId} in animation ""{animation.Name}""");
                    continue;
                }
                bool isVisible = Convert.ToInt32(pair[1]) != 0;
                visibility[frameId] = isVisible;
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            item = null;
            return false;
        }
    }
}
