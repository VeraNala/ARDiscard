using System.Collections.Generic;

namespace ARDiscard.GameData;

internal static class InternalConfiguration
{
    /// <summary>
    /// Not all of these *can* be discarded, but we shouldn't attempt it either.
    /// </summary>
    public static readonly IReadOnlyList<uint> BlacklistedItems = new List<uint>
    {
        2820, // red onion helm

        16039, // ala mhigan earrings
        24589, // aetheryte earrings
        33648, // menphina's earrings

        21197, // UCOB token
        23175, // UWU token
        28633, // TEA token
        36810, // DSR token
        38951, // TOP token
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

        9387, // Antique Helm
        9388, // Antique Mail
        9389, // Antique Gauntlets
        9390, // Antique Breeches
        9391, // Antique Sollerets

        // Fate drops used in tribal quests
        7001, // Flamefang Choker (Amalj'aa)
        7002, // Proto Armor Identification Key (Sylphs)
        7003, // Kafre's Tusk (Sylphs)
        7797, // Titan Mythrilshield (Kobolds)
        7798, // Coral Armband (Sahagin)
        7802, // Titan Steelshield (Kobolds)
        7803, // Titan Electrumshield (Kobolds)
        7804, // Coral Band (Sahagin)
        7805, // Coral Necklace (Sahagin)
    }.AsReadOnly();
}
