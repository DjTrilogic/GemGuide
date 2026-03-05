namespace GemGuide;

public class Gem(int level, string variantId, int quality, bool enabled, string? name = null)
{
    public int Level { get; set; } = level;
    public string VariantId { get; set; } = variantId;
    /// <summary>Display name from PoB XML nameSpec attribute, or VariantId if not set.</summary>
    public string Name { get; set; } = name ?? variantId;
    public int Quality { get; set; } = quality;
    public bool Enabled { get; set; } = enabled;
}