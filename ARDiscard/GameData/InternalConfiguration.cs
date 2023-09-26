using System.Collections.Generic;

namespace ARDiscard.GameData;

internal static class InternalConfiguration
{
    public static readonly IReadOnlyList<uint> BlacklistedItems = new List<uint>
    {
        2820, // red onion helm
        16039, // ala mhigan earrings
        24589, // aetheryte earrings
        33648, // menphina's earrings

        21197, // UCOB
        23175, // UWU
        28633, // TEA
        36810, // DSR
        38951, // TOP
    }.AsReadOnly();

    /// <summary>
    /// Items that are unique/untradeable, but should still be possible to discard. This is moreso because
    /// 99% of the unique/untradeable items should NOT be selectable for discard, but these are OK.
    /// </summary>
    public static readonly IReadOnlyList<uint> WhitelistedItems = new List<uint>
    {
        2962, // Onion Doublet
        3279, // Onion Gaskins
        3743, // Onion Patterns
    }.AsReadOnly();
}
