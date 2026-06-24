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
    public static XElement GetRootOfAnimations(this XDocument animations) =>
        animations?.Element("AllAnimations")?.Element("animations");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<XElement> GetEveryBa(this XDocument animations) =>
        animations.GetRootOfAnimations()?.Elements("ba");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<XElement> GetEveryAssetPos(this XDocument animations) =>
        GetEveryBa(animations)?.SelectMany(ba => ba.Element("items")?.Elements("assetPos") ?? []);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static XElement GetAssetPosWithA(this XDocument animations, string id, int a) =>
        GetAssetPosWithA(animations, id, a);
}
