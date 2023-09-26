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
}
