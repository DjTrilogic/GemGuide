namespace GemGuide;

public class Gem(int level, string variantId, int quality, bool enabled)
{
    public int Level { get; set; } = level;
    public string VariantId { get; set; } = variantId;
    public int Quality { get; set; } = quality;
    public bool Enabled { get; set; } = enabled;
}