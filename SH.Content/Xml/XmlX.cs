using SH.Framework.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;

namespace SH.Content.Xml;

public static class XmlX
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<XElement> CloneXElements(this IEnumerable<XElement> items)
    {
        List<XElement> list = [];
        foreach (XElement item in items)
            list.Add(new(item));
        return list;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetNodePath(this XElement element) =>
    element == null ? null : "/" + element.AncestorsAndSelf().Reverse().Select(e => e.Name.LocalName).JoinToString("/");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Line(this XElement e) =>
        (e as IXmlLineInfo)?.LineNumber ?? 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasAttribute(this XElement e, string attributeName) =>
        e?.Attribute(attributeName) != null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RemoveAttribute(this XElement e, string attributeName)
    {
        try { e?.SetAttributeValue(attributeName, null); } catch { }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NodeType GetNodeType(this XElement node) =>
        NodeType.RegisteredTypes.TryGetValue(node.GetNodePath(), out NodeType type) ? type : null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static XElement GetRootOfAnimations(this XDocument animations) =>
        animations?.Element("AllAnimations")?.Element("animations");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<XElement> GetEveryBa(this XDocument animations) =>
        animations.GetRootOfAnimations()?.Elements("ba");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static XElement GetBa(this XDocument animations, string id) =>
        GetEveryBa(animations)?.FirstOrDefault(ba => ba.Attribute("id").Value.Equals(id, StringComparison.Ordinal));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<XElement> GetEveryAssetPos(this XDocument animations) =>
        GetEveryBa(animations)?.SelectMany(ba => ba.Element("items")?.Elements("assetPos") ?? []);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<XElement> GetAllAssetPos(this XDocument animations, string id) =>
        GetBa(animations, id)?.Element("items")?.Elements("assetPos");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<XElement> GetAllAssetPos(this XElement ba) =>
        ba?.Element("items")?.Elements("assetPos");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static XElement GetAssetPosWithA(this XDocument animations, string id, int a) =>
        GetAssetPosWithA(animations, id, a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static XElement GetAssetPosWithA(this XDocument animations, string id, string a) =>
        GetAllAssetPos(animations, id)?.FirstOrDefault(assetPos => assetPos.Attribute("a").Value.Equals(a, StringComparison.Ordinal));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static XElement GetAssetPosWithAN(this XDocument animations, string id, string an) =>
        GetAllAssetPos(animations, id)?
        .FirstOrDefault(ba => ba.Attribute("an").Value.Equals(an, StringComparison.Ordinal));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static XElement GetAssetPosWithFilename(this XDocument animations, string id, string filename) =>
        GetAllAssetPos(animations, id)?
        .FirstOrDefault(ba => ba?.Attribute("filename")?.Value?.Equals(filename, StringComparison.Ordinal) ?? false);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static XElement GetRootOfAudio(this XDocument audio) =>
        audio?.Element("audio");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<XElement> GetEveryAudio(this XDocument audio) =>
        audio.GetRootOfAudio()?.Elements("a");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static XElement GetAudioById(this XDocument audio, int id) =>
        GetEveryAudio(audio)?.FirstOrDefault(a => a?.Attribute("id")?.Value?.Equals(id.ToString(), StringComparison.Ordinal) ?? false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static XElement GetAudioByName(this XDocument audio, string n) =>
        GetEveryAudio(audio)?.FirstOrDefault(a => a.Attribute("id")?.Value?.Equals(n, StringComparison.Ordinal) ?? false);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static XElement GetRootOfTexts(this XDocument texts) =>
        texts?.Element("t");

}
