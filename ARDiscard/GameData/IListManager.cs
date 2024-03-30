using System.Collections.Generic;

namespace ARDiscard.GameData;

internal interface IListManager
{
    bool IsBlacklisted(uint itemId, bool checkConfiguration = true);

    IReadOnlyList<uint> GetInternalBlacklist();

    bool IsWhitelisted(uint itemId);
}
