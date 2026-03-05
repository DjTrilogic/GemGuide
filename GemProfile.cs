using System.Collections.Generic;

namespace GemGuide;

public class GemProfile
{
    public List<SkillSet> SkillSets { get; set; } = [];
    public SkillSetSelection ActiveSet { get; set; }
    /// <summary>Character class for gem acquisition lookup (e.g. Witch). Set from PoB import or manually.</summary>
    public string CharacterClass { get; set; }
}