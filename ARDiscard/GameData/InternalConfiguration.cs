using System.Collections.Generic;

namespace ARDiscard.GameData;

public static class InternalConfiguration
{
    public static IReadOnlyList<uint> BlacklistedItems = new List<uint>
    {
        2820, // red onion helm
    }.AsReadOnly();
}
