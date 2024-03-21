using System.Collections.Generic;

namespace ARDiscard.GameData;

internal sealed class ItemFilter
{
    public const ItemFilter? None = null;

    public required List<uint> ItemIds { get; init; }
}
