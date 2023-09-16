using System.Collections.Generic;

namespace ARDiscard.GameData;

internal class ItemFilter
{
    public static ItemFilter? None = null;

    public required List<uint> ItemIds { get; init; }
}
