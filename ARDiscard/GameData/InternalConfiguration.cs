using System.Collections.Generic;

namespace ARDiscard.GameData;

internal static class InternalConfiguration
{
    public static IReadOnlyList<uint> BlacklistedItems = new List<uint>
    {
        2820, // red onion helm
        16039, // ala mhigan earrings
        24589, // aetheryte earrings
        33648, // menphina's earrings
    }.AsReadOnly();
}
