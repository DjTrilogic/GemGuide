using System.Collections.Generic;

namespace GemGuide;

public class SkillSet
{
    public SkillSet(string name, List<GemSet> gemSets)
    {
        Name = name;
        GemSets = gemSets;
    }

    public string Name { get; set; }
    public List<GemSet> GemSets { get; set; }
}