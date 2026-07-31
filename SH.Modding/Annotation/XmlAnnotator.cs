using SH.Content;
using SH.Content.Enums;
using SH.Content.Xml;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SH.Modding.Annotation;

public sealed class XmlAnnotator
{
    private const string ANNOTATE_TEXT = "_text";
    private const string ANNOTATED_FILE_SUFFIX = "_annotated";

    private readonly ILogger Log;

    private readonly SortedDictionary<string, Audio> Audios = [];
    private readonly SortedDictionary<string, string> IdToText = [];

    private readonly SortedDictionary<string, Element> Elements = [];
    private readonly SortedDictionary<string, Product> Products = [];
    private readonly SortedDictionary<string, Item> Items = [];
    private readonly SortedDictionary<string, Tech> Techs = [];
    private readonly SortedDictionary<string, Robot> Robots = [];
    private readonly SortedDictionary<string, Condition> Conditions = [];
    private readonly SortedDictionary<string, Craft> Crafts = [];

    private ELanguage Language;
    private CancellationToken CT;


    private XmlFile HavenXml;
    private XmlFile TextsXml;
    private XmlFile AudioXml;
    private XmlFile TexturesXml;
    private XmlFile AnimationsXml;


    public XmlAnnotator(ILogger log)
    {
        Log = log ?? new VoidLogger();
    }

    public async Task<bool> TryRunAsync(string baseDir, ELanguage language, CancellationToken ct, IProgressInfo progress)
    {
        try
        {
            Language = language;
            CT = ct;

            progress.Max = 500;

            // Load XML:
            if (!await LoadAllAsync(baseDir, progress, 160))
                return false;

            // Mad text ID to text content:
            MapTexts(language);

            // Mad audio:
            MapAudio();

            // Haven "tid" attributes:
            AnnotateHavenTextIDs();

            // Haven <Item> section:
            AnnotateHavenItems();

            // Haven <Product> section:
            AnnotateHavenProducts();

            // Haven <Element> section:
            AnnotateHavenElements();
            progress.Increment(20);

            // Haven <Tech> section:
            AnnotateHavenTech();

            // Haven <Robot> section:
            AnnotateHavenRobot();

            // Haven <Craft> section:
            AnnotateHavenCraft();

            // Haven <CharacterCondition> section:
            AnnotateHavenCondition();

            // Annotate generic attributes
            AnnotateHavenGenericAttributes(progress, 220);

            // Save XML:
            if (!await SaveAllAsync(baseDir))
                return false;

            // Done.
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

    private string GetPrettyDescription(string fullDescription) =>
        fullDescription?.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
    private string GetPrettyName(string name) =>
        name.IsNullOrWhiteSpace() ? "?" : name;

    private async Task<bool> LoadAllAsync(string baseDir, IProgressInfo progress, int progressMax)
    {
        string inputHavenXmlPath = baseDir.CombineAsOSPath(SpaceHavenConstants.LIBRARY, SpaceHavenConstants.HAVEN);
        string inputTextsXmlPath = baseDir.CombineAsOSPath(SpaceHavenConstants.LIBRARY, SpaceHavenConstants.TEXTS);
        string inputAudioXmlPath = baseDir.CombineAsOSPath(SpaceHavenConstants.LIBRARY, SpaceHavenConstants.AUDIO);
        string inputTexturesXmlPath = baseDir.CombineAsOSPath(SpaceHavenConstants.LIBRARY, SpaceHavenConstants.TEXTURES);
        string inputAnimationsXmlPath = baseDir.CombineAsOSPath(SpaceHavenConstants.LIBRARY, SpaceHavenConstants.ANIMATIONS);

        HavenXml = new(EXmlFileType.Haven, baseDir, inputHavenXmlPath);
        if (!await HavenXml.TryLoadAsync(Log, CT))
            return false;
        progress.Increment(5 * progressMax / 10);

        TextsXml = new(EXmlFileType.Texts, baseDir, inputTextsXmlPath);
        if (!await TextsXml.TryLoadAsync(Log, CT))
            return false;
        progress.Increment(2 * progressMax / 10);

        AudioXml = new(EXmlFileType.Audio, baseDir, inputAudioXmlPath);
        if (!await AudioXml.TryLoadAsync(Log, CT))
            return false;
        progress.Increment(1 * progressMax / 10);

        TexturesXml = new(EXmlFileType.Textures, baseDir, inputTexturesXmlPath);
        if (!await TexturesXml.TryLoadAsync(Log, CT))
            return false;
        progress.Increment(1 * progressMax / 10);

        AnimationsXml = new(EXmlFileType.Animations, baseDir, inputAnimationsXmlPath);
        if (!await AnimationsXml.TryLoadAsync(Log, CT))
            return false;
        progress.Increment(1 * progressMax / 10);

        return true;
    }

    private async Task<bool> SaveAllAsync(string baseDir)
    {
        string outputHavenXmlPath = baseDir.CombineAsOSPath(SpaceHavenConstants.LIBRARY, $"{SpaceHavenConstants.HAVEN}{ANNOTATED_FILE_SUFFIX}");
        string outputTextsXmlPath = baseDir.CombineAsOSPath(SpaceHavenConstants.LIBRARY, $"{SpaceHavenConstants.TEXTS}{ANNOTATED_FILE_SUFFIX}");
        string outputAudioXmlPath = baseDir.CombineAsOSPath(SpaceHavenConstants.LIBRARY, $"{SpaceHavenConstants.AUDIO}{ANNOTATED_FILE_SUFFIX}");
        string outputTexturesXmlPath = baseDir.CombineAsOSPath(SpaceHavenConstants.LIBRARY, $"{SpaceHavenConstants.TEXTURES}{ANNOTATED_FILE_SUFFIX}");
        string outputAnimationsXmlPath = baseDir.CombineAsOSPath(SpaceHavenConstants.LIBRARY, $"{SpaceHavenConstants.ANIMATIONS}{ANNOTATED_FILE_SUFFIX}");

        if (!await HavenXml.TrySaveToAsync(outputHavenXmlPath, Log, CT))
            return false;
        if (!await TextsXml.TrySaveToAsync(outputTextsXmlPath, Log, CT))
            return false;
        if (!await AudioXml.TrySaveToAsync(outputAudioXmlPath, Log, CT))
            return false;
        if (!await TexturesXml.TrySaveToAsync(outputTexturesXmlPath, Log, CT))
            return false;
        if (!await AnimationsXml.TrySaveToAsync(outputAnimationsXmlPath, Log, CT))
            return false;

        return true;
    }

    private void MapTexts(ELanguage language)
    {
        string lang = language.ToString();
        string en = ELanguage.EN.ToString();
        foreach (XElement t in TextsXml.Root.Elements("t"))
        {
            CT.ThrowIfCancellationRequested();

            string id = t.Attribute("id")?.Value;
            if (id.IsNullOrWhiteSpace())
                continue;
            string text = t.Element(lang)?.Value;
            text ??= t.Element(en)?.Value; // Try EN before skipping
            if (text == null)
                continue;
            IdToText[id] = text;
        }
    }

    private void MapAudio()
    {
        foreach (XElement a in AudioXml.Root.Elements("a"))
        {
            CT.ThrowIfCancellationRequested();

            Audio audio = new()
            {
                XML = a,
                ID = a.Attribute("id")?.Value,
                Name = a.Attribute("n")?.Value,
            };
            Audios[audio.ID] = audio;
        }
    }










    private void AnnotateHavenTextIDs()
    {
        foreach (XElement node in HavenXml.Root.Descendants())
        {
            CT.ThrowIfCancellationRequested();

            XAttribute tid = node.Attribute("tid");
            if ((tid?.Value).IsNullOrWhiteSpace())
                continue;
            if (!IdToText.TryGetValue(tid.Value, out string text))
                text = string.Empty; // force empty string if not found
            if (node.Name == "name")
                node.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(text));
            else if (node.Name == "desc")
                node.SetAttributeValue(ANNOTATE_TEXT, GetPrettyDescription(text));
            else
                node.SetAttributeValue(ANNOTATE_TEXT, GetPrettyDescription(text));
        }
    }










    private void AnnotateHavenItems()
    {
        XElement rootItem = HavenXml.Root.Element("Item");
        foreach (XElement i in rootItem?.Elements("item") ?? [])
        {
            CT.ThrowIfCancellationRequested();

            if (i.Element("duplicate") != null)
                continue; // skip duplicates for now!

            Item item = new() { XML = i };

            // Get mid:
            item.MID = i.Attribute("mid")?.Value;
            if (item.MID.IsNullOrWhiteSpace())
                continue;

            // Get name:
            string nameTID = i.Element("name")?.Attribute("tid")?.Value;
            if (!nameTID.IsNullOrWhiteSpace() && IdToText.TryGetValue(nameTID, out string name))
                item.Name = name;
            i.AddBeforeSelf(new XComment($" {GetPrettyName(item.Name)} "));

            // Get description:
            string descriptionTID = i.Element("desc")?.Attribute("tid")?.Value;
            if (!descriptionTID.IsNullOrWhiteSpace() && IdToText.TryGetValue(descriptionTID, out string desc))
                item.Description = desc;

            // Add:
            Items[item.MID] = item;
        }

        foreach (XElement i in rootItem?.Elements("item") ?? [])
        {
            CT.ThrowIfCancellationRequested();

            if (i.Element("duplicate") == null)
                continue; // only duplicates now!

            Item item = new() { XML = i };

            // Get mid:
            item.MID = i.Attribute("mid")?.Value;
            if (item.MID.IsNullOrWhiteSpace())
                continue;

            // Get Duplicate:
            item.DuplicateMID = i.Element("duplicate")?.Attribute("refrenceItem")?.Value; // yes, it's "refrenceItem" !!!
            item.Quality = i.Element("duplicate")?.Attribute("quality")?.Value;

            // Add:
            Items[item.MID] = item;
        }

        foreach (Item item in Items.Values.Where(item => !item.DuplicateMID.IsNullOrWhiteSpace()).ToArray())
        {
            CT.ThrowIfCancellationRequested();

            // Get Duplicate:
            if (!Items.TryGetValue(item.DuplicateMID, out Item referenced))
            {
                Log.Warn($"Item mid={item.MID} is a duplicate item, but it's referenced item mid={item.DuplicateMID} could not be found");
                continue; // ignore bad data
            }
            string quality =

            // Set name and description:
            item.Name = $"{GetPrettyName(referenced.Name)} ({item.Quality})";
            item.Description = referenced.Description;
            item.XML.AddBeforeSelf(new XComment($" {item.Name} "));

            XElement nameNode = item.XML.Element("name");
            nameNode?.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(item.Name));

            XElement descriptionNode = item.XML.Element("desc");
            descriptionNode?.SetAttributeValue(ANNOTATE_TEXT, GetPrettyDescription(item.Description));

            // Add:
            Items[item.MID] = item;
        }
    }



























    private void AnnotateHavenProducts()
    {
        // Elementary products first:
        XElement rootProduct = HavenXml.Root.Element("Product");
        foreach (XElement p in rootProduct?.Elements("product")?.Where(p => p != null && p.Attribute("type")?.Value == EProductType.Elementary.ToString()) ?? [])
        {
            CT.ThrowIfCancellationRequested();

            Product product = new()
            {
                Type = EProductType.Elementary,
            };

            // Get EID:
            product.EID = p.Attribute("eid")?.Value;
            if (product.EID.IsNullOrWhiteSpace())
                continue;

            // Get item:
            product.Item = p.Attribute("item")?.Value;

            // Get name:
            string nameTID = p.Element("name")?.Attribute("tid")?.Value;
            if (!nameTID.IsNullOrWhiteSpace() && IdToText.TryGetValue(nameTID, out string name))
                product.Name = name;
            p.AddBeforeSelf(new XComment($" {GetPrettyName(product.Name)} "));

            // Get description:
            string descriptionTID = p.Element("desc")?.Attribute("tid")?.Value;
            if (!descriptionTID.IsNullOrWhiteSpace() && IdToText.TryGetValue(descriptionTID, out string desc))
                product.Description = desc;

            // Add:
            Products[product.EID] = product;
        }

        // Process products now:
        foreach (XElement p in rootProduct?.Elements("product")?.Where(p => p != null && p.Attribute("type")?.Value == EProductType.Process.ToString()) ?? [])
        {
            CT.ThrowIfCancellationRequested();

            Product product = new()
            {
                Type = EProductType.Process,
                XML = p,
            };

            // Get EID:
            product.EID = p.Attribute("eid")?.Value;
            if (product.EID.IsNullOrWhiteSpace())
                continue;

            // Get name:
            string nameTID = p.Element("name")?.Attribute("tid")?.Value;
            if (!nameTID.IsNullOrWhiteSpace() && IdToText.TryGetValue(nameTID, out string name))
                product.Name = name;

            // Get description:
            string descriptionTID = p.Element("desc")?.Attribute("tid")?.Value;
            if (!descriptionTID.IsNullOrWhiteSpace() && IdToText.TryGetValue(descriptionTID, out string desc))
                product.Description = desc;

            // input:
            XElement needsNode = p.Element("needs");
            if (needsNode != null)
            {
                foreach (XElement l in needsNode.Elements("l"))
                {
                    string mid = l.Attribute("element")?.Value;
                    if (mid.IsNullOrWhiteSpace())
                        continue;
                    if (Products.TryGetValue(mid, out Product inputProduct))
                        product.Inputs.Add(inputProduct.Name);
                    else if (Items.TryGetValue(mid, out Item inputItem))
                        product.Inputs.Add(inputItem.Name);
                }
            }

            // output:
            XElement productsNode = p.Element("products");
            if (productsNode != null)
            {
                foreach (XElement l in productsNode.Elements("l"))
                {
                    string mid = l.Attribute("element")?.Value;
                    if (mid.IsNullOrWhiteSpace())
                        continue;
                    if (Products.TryGetValue(mid, out Product outputProduct))
                    {
                        product.Outputs.Add(outputProduct.Name);
                        l.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(outputProduct.Name));
                        //l.AddBeforeSelf(new XComment($" {GetPrettyName(outputProduct.Name)} "));
                    }
                    else if (Items.TryGetValue(mid, out Item outputItem))
                    {
                        product.Outputs.Add(outputItem.Name);
                        l.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(outputItem.Name));
                        //l.AddBeforeSelf(new XComment($" {GetPrettyName(outputItem.Name)} "));
                    }
                }
            }

            // Especially hard-coded products!
            XElement foodProcessingNode = p.Element("foodProcessing");
            if (foodProcessingNode != null)
                product.Name = "Food Processor";
            XElement itemFabNode = p.Element("itemFab");
            if (itemFabNode != null)
                product.Name = "Item Fabricator";
            if (bool.TryParse(p.Attribute("smelter")?.Value, out bool smelter) && smelter)
                product.Name = "Smelter";
            if (bool.TryParse(p.Attribute("scrapper")?.Value, out bool scrapper) && scrapper)
                product.Name = "Scrapper";
            if (bool.TryParse(p.Attribute("composter")?.Value, out bool composter) && composter)
                product.Name = "Composter";

            // Define the process a name:
            product.Name ??= string.Empty;
            if (product.Inputs.Count > 0 || product.Outputs.Count > 0)
            {
                StringBuilder sb = new();
                if (product.Name.Length > 0)
                    sb.Append(": ");
                if (product.Inputs.Count > 0)
                    sb.Append(product.Inputs.JoinToString(" + "));
                else sb.Append("()");
                sb.Append(" ➜ ");
                if (product.Outputs.Count > 0)
                    sb.Append(product.Outputs.JoinToString(" + "));
                product.Name = sb.ToString();
            }
            p.AddBeforeSelf(new XComment($" {GetPrettyName(product.Name)} "));

            // Add:
            Products[product.EID] = product;
        }

        // List of processes:
        foreach (Product product in Products.Values.Where(p => p.Type == EProductType.Process))
        {
            CT.ThrowIfCancellationRequested();

            XElement[] children = product.XML.Element("list")?.Element("processes")?.Elements("l")?.ToArray() ?? Array.Empty<XElement>();
            if (children.Length <= 0)
                continue;

            foreach (XElement l in children)
            {
                string mid = l.Attribute("process")?.Value;
                if (mid == null)
                    continue;
                if (!Products.TryGetValue(mid, out Product child))
                    continue;
                l.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(child.Name));
                //l.AddBeforeSelf(new XComment($" {GetPrettyName(child.Name)} "));
            }
        }
    }


































    private void AnnotateHavenElements()
    {
        XElement rootElement = HavenXml.Root.Element("Element");
        foreach (XElement me in rootElement?.Elements("me") ?? [])
        {
            CT.ThrowIfCancellationRequested();

            Element element = new() { XML = me };

            // Get mid:
            element.MID = me.Attribute("mid")?.Value;
            if (element.MID.IsNullOrWhiteSpace())
                continue;

            // Map links:
            XElement linkedNode = me.Element("linked");
            if (linkedNode != null)
            {
                foreach (XElement l in linkedNode.Elements("l").Where(l => l != null))
                {
                    string id = l.Attribute("id")?.Value;
                    if (id.IsNullOrWhiteSpace())
                        continue;
                    element.LinksTo[id] = null; // don't set a value for the key yet
                }
            }

            // Get name:
            string nameTID = me.Element("objectInfo")?.Element("name")?.Attribute("tid")?.Value;
            if (!nameTID.IsNullOrWhiteSpace() && IdToText.TryGetValue(nameTID, out string name))
                element.Name = name;

            // Get description:
            string descriptionTID = me.Element("objectInfo")?.Element("desc")?.Attribute("tid")?.Value;
            if (!descriptionTID.IsNullOrWhiteSpace() && IdToText.TryGetValue(descriptionTID, out string desc))
                element.Description = desc;

            // Annotate "customPrice":
            foreach(XElement l in me.Descendants("customPrice")?.SelectMany(node => node.Elements("l")) ?? [])
            {
                string elementId = l.Attribute("elementId")?.Value;
                if (elementId.IsNullOrWhiteSpace())
                    continue;
                if (Items.TryGetValue(elementId, out Item i))
                    l.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(i.Name));
                else if (Products.TryGetValue(elementId, out Product p))
                    l.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(p.Name));
            }

            // Annotate "produces":
            foreach(XElement l in me.Descendants("produces")?.SelectMany(node => node.Elements("l")) ?? [])
            {
                string product = l.Attribute("product")?.Value;
                if (product.IsNullOrWhiteSpace())
                    continue;
                if (Items.TryGetValue(product, out Item i))
                    l.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(i.Name));
                else if (Products.TryGetValue(product, out Product p))
                    l.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(p.Name));
            }

            // Add Element:
            Elements[element.MID] = element;
        }

        // Complete all direct links:
        foreach (Element e1 in Elements.Values)
        {
            CT.ThrowIfCancellationRequested();

            if (e1.LinksTo.Count <= 0)
                continue;

            foreach (string mid in e1.LinksTo.Keys.ToArray()) // clone keys first!
            {
                if (Elements.TryGetValue(mid, out Element e2))
                {
                    // complete the link
                    e1.LinksTo[mid] = e2;
                    e2.LinkedBy[e1.MID] = e1;
                }
                else
                {
                    // bad link
                    Log.Warn($@"Element me with mid=""{e1.MID}"" links to mid=""{mid}"", but the linked element does not exist!");
                    e1.LinksTo.Remove(mid);
                }
            }
        }

        // Map all direct + indirect links:
        foreach (Element e in Elements.Values)
        {
            CT.ThrowIfCancellationRequested();

            if (e.LinksTo.Count > 0)
                e.MapAllLinks();
            if (e.LinkedBy.Count > 0)
                e.MapAllLinkedBy();
        }

        // Try to locate name from linked nodes:
        foreach (Element e1 in Elements.Values.Where(e => e.Name == null && e.LinkedBy.Count <= 0))
        {
            CT.ThrowIfCancellationRequested();

            e1.Name = e1.LinksTo.Values.FirstOrDefault(e2 => e2.Name != null)?.Name;
            if (e1.Name != null)
                continue;
            e1.Name = e1.LinksToAll.Values.FirstOrDefault(e2 => e2.Name != null)?.Name;
        }

        // Add XML comments to Element:
        foreach (Element e1 in Elements.Values)
        {
            CT.ThrowIfCancellationRequested();

            // I'm missing an "OrderedHash<string>" here...
            OrderedDictionary<string, int> namesDict = [];
            if (e1.Name != null)
                namesDict[e1.Name] = 0;
            foreach (string name in e1.LinkedByAll.Values.Where(e2 => e2.Name != null).Select(e2 => e2.Name))
                namesDict[name] = 0;
            e1.XML.AddBeforeSelf(new XComment($" {GetPrettyName(namesDict.Keys.JoinToString(", "))} "));

            List<Element> roots = e1.LinkedByAll.Values.Where(e2 => e2.LinkedBy.Count <= 0).ToList();
            if (roots.Count <= 0)
                roots.Add(e1);
            e1.XML.AddBeforeSelf(new XComment($" {Translations.RootNode[Language]}: {roots.Select(e => e.MID).JoinToString(", ")} "));

            string directlyLinkedBy = e1.LinkedBy.Keys.JoinToString(", ");
            if (!directlyLinkedBy.IsNullOrWhiteSpace())
                e1.XML.AddBeforeSelf(new XComment($" {Translations.LinkedBy[Language]}: {directlyLinkedBy} "));

            string directlyLinksTo = e1.LinksTo.Keys.JoinToString(", ");
            if (!directlyLinksTo.IsNullOrWhiteSpace())
                e1.XML.AddBeforeSelf(new XComment($" {Translations.LinksTo[Language]}: {directlyLinksTo} "));

            string indirectlyLinkedBy = e1.LinkedByAll.Keys.Where(key => !e1.LinkedBy.ContainsKey(key)).JoinToString(", ");
            if (!indirectlyLinkedBy.IsNullOrWhiteSpace())
                e1.XML.AddBeforeSelf(new XComment($" {Translations.IndirectlyLinkedBy[Language]}: {indirectlyLinkedBy} "));

            string indirectlyLinksTo = e1.LinksToAll.Keys.Where(key => !e1.LinksTo.ContainsKey(key)).JoinToString(", ");
            if (!indirectlyLinksTo.IsNullOrWhiteSpace())
                e1.XML.AddBeforeSelf(new XComment($" {Translations.IndirectlyLinksTo[Language]}: {indirectlyLinksTo} "));

            e1.XML.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(namesDict.Keys.JoinToString(",")));
            e1.XML.SetAttributeValue("_root", roots.Select(e => e.Name).JoinToString(","));
            e1.XML.SetAttributeValue("_requires", e1.LinksToAll.Keys.JoinToString(","));
            e1.XML.SetAttributeValue("_requiredBy", e1.LinkedByAll.Keys.JoinToString(","));
        }



    }









    private void AnnotateHavenTech()
    {
        XElement rootTech = HavenXml.Root.Element("Tech");
        foreach (XElement t in rootTech.Elements("tech") ?? [])
        {
            CT.ThrowIfCancellationRequested();

            Tech tech = new() { XML = t };

            // Get id:
            tech.ID = t.Attribute("id")?.Value;
            if (tech.ID.IsNullOrWhiteSpace())
                continue;

            // Get name:
            string nameTID = t.Element("name")?.Attribute("tid")?.Value;
            if (!nameTID.IsNullOrWhiteSpace() && IdToText.TryGetValue(nameTID, out string name))
                tech.Name = name;
            t.AddBeforeSelf(new XComment($" {GetPrettyName(tech.Name)} "));

            // Get description:
            string descriptionTID = t.Element("desc")?.Attribute("tid")?.Value;
            if (!descriptionTID.IsNullOrWhiteSpace() && IdToText.TryGetValue(descriptionTID, out string desc))
                tech.Description = desc;

            // Annotate experiment items:
            XElement[] experimentNodes = t.Descendants("l")?.Where(l => l.Attribute("type")?.Value == "Experiment").ToArray() ?? [];
            foreach (XElement l in experimentNodes)
            {
                string id = l.Attribute("itemId")?.Value;
                if (id.IsNullOrWhiteSpace())
                    continue;
                if (Products.TryGetValue(id, out Product p))
                    l.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(p.Name));
                else if (Items.TryGetValue(id, out Item i))
                    l.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(i.Name));
            }

            // Annotate Building unlocks:
            XElement[] unlocksBuildingNodes = t.Descendants("l")?.Where(l => l.Attribute("type")?.Value == "Building").ToArray() ?? [];
            foreach (XElement l in unlocksBuildingNodes)
            {
                string id = l.Attribute("buildingId")?.Value;
                if (id.IsNullOrWhiteSpace())
                    continue;
                if (Elements.TryGetValue(id, out Element e))
                    l.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(e.Name));
            }

            // Annotate Item unlocks:
            XElement[] unlocksItemNodes = t.Descendants("l")?.Where(l => l.Attribute("type")?.Value == "Item").ToArray() ?? [];
            foreach (XElement l in unlocksItemNodes)
            {
                string id = l.Attribute("itemId")?.Value;
                if (id.IsNullOrWhiteSpace())
                    continue;
                if (Products.TryGetValue(id, out Product p))
                    l.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(p.Name));
                else if (Items.TryGetValue(id, out Item i))
                    l.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(i.Name));
            }

            // Annotate Recipe unlocks:
            XElement[] unlocksRecipeNodes = t.Descendants("l")?.Where(l => l.Attribute("type")?.Value == "Recipe").ToArray() ?? [];
            foreach (XElement l in unlocksRecipeNodes)
            {
                string id = l.Attribute("recipeId")?.Value;
                if (id.IsNullOrWhiteSpace())
                    continue;
                if (Products.TryGetValue(id, out Product p))
                    l.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(p.Name));
            }

            // Add:
            Techs[tech.ID] = tech;
        }

        // Tech Tree:
        XElement techTreeLinkNode = HavenXml.Root.Element("TechTree")?.Element("tree")?.Element("items");
        foreach (XElement l in techTreeLinkNode?.Elements("i") ?? [])
        {
            CT.ThrowIfCancellationRequested();

            string tid = l.Attribute("tid")?.Value; // this is tech id, not text id
            Techs.TryGetValue(tid, out Tech tech);
            l.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(tech.Name));
        }
        foreach (XElement l in techTreeLinkNode?.Elements("l") ?? [])
        {
            CT.ThrowIfCancellationRequested();

            string fromId = l.Attribute("fromId")?.Value;
            string toId = l.Attribute("toId")?.Value;
            Techs.TryGetValue(fromId, out Tech from);
            Techs.TryGetValue(toId, out Tech to);
            l.SetAttributeValue(ANNOTATE_TEXT, $"{GetPrettyName(from.Name)} ➜ {GetPrettyName(to.Name)}");
        }
    }





    private void AnnotateHavenRobot()
    {
        XElement root = HavenXml.Root.Element("Robot");
        foreach (XElement r in root.Elements("robot") ?? [])
        {
            CT.ThrowIfCancellationRequested();

            Robot robot = new() { XML = r };

            // Get id:
            robot.CID = r.Attribute("cid")?.Value;
            if (robot.CID.IsNullOrWhiteSpace())
                continue;

            // Get name:
            if (Items.TryGetValue(r.Element("carriedItem")?.Attribute("itemId")?.Value ?? string.Empty, out Item i))
            {
                robot.Name = i.Name;
                r.AddBeforeSelf(new XComment(GetPrettyName(robot.Name)));
            }

            // Add:
            Robots[robot.CID] = robot;
        }
    }









    private void AnnotateHavenCondition()
    {
        XElement root = HavenXml.Root.Element("CharacterCondition");
        foreach (XElement c in root.Elements("condition") ?? [])
        {
            CT.ThrowIfCancellationRequested();

            Condition condition = new() { XML = c };

            // Get id:
            condition.ID = c.Attribute("id")?.Value;
            if (condition.ID.IsNullOrWhiteSpace())
                continue;

            // Get name:
            string nameTID = c.Element("name")?.Attribute("tid")?.Value;
            if (!nameTID.IsNullOrWhiteSpace() && IdToText.TryGetValue(nameTID, out string name))
                condition.Name = name;
            c.AddBeforeSelf(new XComment($" {GetPrettyName(condition.Name)} "));

            // Get description:
            string descriptionTID = c.Element("desc")?.Attribute("tid")?.Value;
            if (!descriptionTID.IsNullOrWhiteSpace() && IdToText.TryGetValue(descriptionTID, out string desc))
                condition.Description = desc;

            // Add:
            Conditions[condition.ID] = condition;
        }
    }











    private void AnnotateHavenCraft()
    {
        XElement root = HavenXml.Root.Element("Craft");
        foreach (XElement c in root.Elements("craft") ?? [])
        {
            CT.ThrowIfCancellationRequested();

            Craft craft = new() { XML = c };

            // Get id:
            craft.CID = c.Attribute("cid")?.Value;
            if (craft.CID.IsNullOrWhiteSpace())
                continue;

            // Get name:
            string nameTID = c.Element("name")?.Attribute("tid")?.Value;
            if (!nameTID.IsNullOrWhiteSpace() && IdToText.TryGetValue(nameTID, out string name))
                craft.Name = name;
            c.AddBeforeSelf(new XComment($" {GetPrettyName(craft.Name)} "));

            // Get description:
            string descriptionTID = c.Element("desc")?.Attribute("tid")?.Value;
            if (!descriptionTID.IsNullOrWhiteSpace() && IdToText.TryGetValue(descriptionTID, out string desc))
                craft.Description = desc;

            // Add:
            Crafts[craft.CID] = craft;
        }
    }











    private void AnnotateHavenGenericAttributes(IProgressInfo progress, long progressMax)
    {
        List<XElement> nodes = HavenXml.Root.Descendants().ToList();
        long count = 0;
        int prevProgress = 0;
        int currProgress;

        foreach (XElement node in nodes)
        {
            CT.ThrowIfCancellationRequested();

            if (Audios.TryGetValue(node.Attribute("auid")?.Value ?? string.Empty, out Audio a))
                node.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(a?.Name));

            else if(Items.TryGetValue(node.Attribute("element")?.Value ?? string.Empty, out Item i0))
                node.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(i0?.Name));

            else if(Products.TryGetValue(node.Attribute("element")?.Value ?? string.Empty, out Product p0))
                node.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(p0?.Name));

            else if(Elements.TryGetValue(node.Attribute("element")?.Value ?? string.Empty, out Element e0))
                node.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(e0?.Name));

            else if (Conditions.TryGetValue(node.Attribute("condition")?.Value ?? string.Empty, out Condition c))
                node.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(c?.Name));

            else if (Items.TryGetValue(node.Attribute("corpseItem")?.Value ?? string.Empty, out Item i1))
                node.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(i1?.Name));

            else if (Items.TryGetValue(node.Attribute("carriedItem")?.Value ?? string.Empty, out Item i2))
                node.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(i2?.Name));

            else if (Products.TryGetValue(node.Attribute("carriedItem")?.Value ?? string.Empty, out Product p1))
                node.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(p1?.Name));

            else if (Items.TryGetValue(node.Attribute("itemId")?.Value ?? string.Empty, out Item i3))
                node.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(i3?.Name));

            else if (Products.TryGetValue(node.Attribute("itemId")?.Value ?? string.Empty, out Product p2))
                node.SetAttributeValue(ANNOTATE_TEXT, GetPrettyName(p2?.Name));

            currProgress = (int)(++count * progressMax / nodes.Count);
            if (currProgress > prevProgress)
            {
                progress.Increment(currProgress - prevProgress);
                prevProgress = currProgress;
            }
        }
    }







}
