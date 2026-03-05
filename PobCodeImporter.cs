using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace GemGuide;

public class PobCodeImporter
{
    public static (List<SkillSet> parsedSets, List<string> errors, string className) GetGemSets(string code)
    {
        try
        {
            var xml = PobHelpers.CodeToXml(code);
            var xmlDocument = new XmlDocument();
            //because loading xml securely is surprisingly hard
            xmlDocument.Load(XmlReader.Create(new StringReader(xml)));
            var root = xmlDocument.GetElementsByTagName("PathOfBuilding").Cast<XmlNode>().First();
            var buildNode = root.ChildNodes.Cast<XmlNode>().FirstOrDefault(x => x.Name == "Build");
            var className = buildNode?.Attributes?.GetNamedItem("className")?.Value;
            var skillsNode = root.ChildNodes.Cast<XmlNode>().First(x => x.Name == "Skills");
            var setNodes = skillsNode.ChildNodes.Cast<XmlNode>().Where(x => x.Name == "SkillSet");
            var setAndSkillNodes = setNodes.Select(sn => (sn, skills: sn.ChildNodes.Cast<XmlNode>().Where(x => x.Name == "Skill")));
            var setAndGemNodes = setAndSkillNodes.Select(d =>
                (skillSetNode: d.sn, skills: d.skills.Select(skillNode => (skillNode, gems: skillNode.ChildNodes.Cast<XmlNode>().Where(x => x.Name == "Gem")))));
            var errors = new List<string>();

            T Push<T>(string error, T value)
            {
                errors.Add(error);
                return value;
            }

            var unnamedSetId = 0;
            var parsedSets = setAndGemNodes.Select(x =>
            {
                var setName = x.skillSetNode.Attributes?.GetNamedItem("title")?.Value switch
                {
                    { } s when string.IsNullOrWhiteSpace(s) => $"Unnamed set {unnamedSetId++}",
                    var s => s
                } ?? Push($"{x.skillSetNode.FindXPath()} does not have a set name", $"Unnamed set {unnamedSetId++}");
                var skills = x.skills.Select(skill =>
                {
                    var skillLabel = skill.skillNode.Attributes?.GetNamedItem("label")?.Value ?? "";

                    var gems = skill.gems.Select(g =>
                    {
                        var gemName = g.Attributes?.GetNamedItem("variantId")?.Value ?? Push($"{g.FindXPath()} does not have a gem name", "");
                        var level = g.Attributes?.GetNamedItem("level")?.Value is { } levelString
                            ? int.TryParse(levelString, out var l)
                                ? l
                                : Push($"{g.FindXPath()}({gemName}).level ('{levelString}') cannot be parsed, using 20 as default", 20)
                            : Push($"{g.FindXPath()}({gemName}) does not have the level attribute, using 20 as default", 20);
                        var quality = g.Attributes?.GetNamedItem("quality")?.Value is { } qualityString
                            ? int.TryParse(qualityString, out var q)
                                ? q
                                : Push($"{g.FindXPath()}({gemName}).quality ('{qualityString}') cannot be parsed, using 0 as default", 0)
                            : Push($"{g.FindXPath()}({gemName}) does not have the quality attribute, using 0 as default", 0);
                        var enabled = g.Attributes?.GetNamedItem("enabled")?.Value is { } enabledString
                            ? bool.TryParse(enabledString, out var b)
                                ? b
                                : Push($"{g.FindXPath()}({gemName}).quality ('{enabledString}') cannot be parsed, using true as default", true)
                            : Push($"{g.FindXPath()}({gemName}) does not have the enabled attribute, using true as default", true);
                        var nameSpec = g.Attributes?.GetNamedItem("nameSpec")?.Value;
                        return new Gem(level, gemName, quality, enabled, nameSpec);
                    }).ToList();
                    return new GemSet(skillLabel, gems);
                }).ToList();
                return new SkillSet(setName, skills);
            }).ToList();
            return (parsedSets, errors, className);
        }
        catch (Exception ex)
        {
            return ([], [ex.ToString()], null);
        }
    }

    public static bool IsValidUrl(string url)
    {
        return url.Length > 100 && Convert.TryFromBase64String(url.Replace('-', '+').Replace('_', '/'), new byte[url.Length], out _);
    }
}