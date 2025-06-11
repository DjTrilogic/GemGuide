using System.Collections.Generic;

namespace GemGuide;

public class GemProfile
{
    public List<SkillSet> SkillSets { get; set; } = [];
    public SkillSetSelection ActiveSet { get; set; }
}