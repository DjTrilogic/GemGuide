using System.Collections.Generic;

namespace GemGuide;

public class GemSet
{
    public GemSet(string label, List<Gem> gems)
    {
        Label = label;
        Gems = gems;
    }

    public string Label { get; set; }
    public List<Gem> Gems { get; set; }
}