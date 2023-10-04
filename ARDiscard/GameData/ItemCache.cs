using System.Collections.Generic;
using Dalamud.Plugin.Services;
using Lumina.Excel.GeneratedSheets;

namespace ARDiscard.GameData;

internal sealed class ItemCache
{
    private readonly Dictionary<uint, CachedItemInfo> _items = new();

    public ItemCache(IDataManager dataManager)
    {
        foreach (var item in dataManager.GetExcelSheet<Item>()!)
        {
            if (item.RowId == 0)
                continue;

            _items[item.RowId] = new CachedItemInfo
            {
                ItemId = item.RowId,
                Name = item.Name.ToString(),
                ILvl = item.LevelItem.Row,
                Rarity = item.Rarity,
                IsUnique = item.IsUnique,
                IsUntradable = item.IsUntradable,
                IsIndisposable = item.IsIndisposable,
                Level = item.LevelEquip,
                UiCategory = item.ItemUICategory.Row,
            };
        }
    }

    public IEnumerable<CachedItemInfo> AllItems => _items.Values;

    public CachedItemInfo? GetItem(uint itemId)
    {
        if (_items.TryGetValue(itemId, out var item))
            return item;
        return null;
    }

    public string GetItemName(uint itemId)
    {
        if (_items.TryGetValue(itemId, out var item))
            return item.Name;
        return string.Empty;
    }

    public sealed class CachedItemInfo
    {
        public required uint ItemId { get; init; }
        public required string Name { get; init; }
        public required uint ILvl { get; init; }
        public required uint Level { get; init; }
        public required byte Rarity { get; init; }
        public required bool IsUnique { get; init; }
        public required bool IsUntradable { get; init; }

        /// <summary>
        /// Whether this item can be discarded at all. "Discard" is greyed out e.g. for the preorder EXP earrings.
        /// </summary>
        public required bool IsIndisposable { get; init; }

        public required uint UiCategory { get; init; }
    }
}
