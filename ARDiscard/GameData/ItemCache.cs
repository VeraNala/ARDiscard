using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dalamud.Plugin.Services;
using Lumina.Excel;
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
                IconId = item.Icon,
                ILvl = item.LevelItem.Row,
                Rarity = item.Rarity,
                IsUnique = item.IsUnique,
                IsUntradable = item.IsUntradable,
                IsIndisposable = item.IsIndisposable,
                Level = item.LevelEquip,
                UiCategory = item.ItemUICategory.Row,
                UiCategoryName = item.ItemUICategory.Value!.Name.ToString(),
                EquipSlotCategory = item.EquipSlotCategory.Row,
            };

            if (item is { Rarity: 3, MateriaSlotCount: 3, RowId: < 33154 or > 33358 })
            {
                InternalConfiguration.UltimateWeapons.Add(item.RowId);
            }
        }

        foreach (var shopItem in dataManager.GetExcelSheet<GilShopItem>()!)
        {
            // exclude base ARR relics, not strictly necessary since we don't allow discarding weapons anyway
            if (shopItem.Item.Value!.Rarity == 4)
                continue;

            // the item can be discarded already
            if (!_items.TryGetValue(shopItem.Item.Row, out CachedItemInfo? cachedItemInfo) ||
                cachedItemInfo.CanBeDiscarded())
                continue;

            if (shopItem.AchievementRequired.Row != 0)
                continue;

            // has a quest required to unlock from the shop
            if (!shopItem.QuestRequired.Any(CanDiscardItemsFromQuest))
                continue;

            cachedItemInfo.CanBeBoughtFromCalamitySalvager = true;
        }

        // only look at msq + regional side quests
        foreach (var quest in dataManager.GetExcelSheet<Quest>()!.Where(x => x.JournalGenre.Value?.JournalCategory.Value?.JournalSection.Row is 0 or 1 or 3))
        {
            foreach (var itemId in quest.ItemReward.Where(x => x > 0))
            {
                var item = dataManager.GetExcelSheet<Item>()!.GetRow(itemId);
                if (item is { Rarity: 1, ItemAction.Row: 388 } && item.RowId != 38809 && item.RowId != 29679)
                    InternalConfiguration.DiscardableGearCoffers.Add(item.RowId);
            }
        }
    }

    private bool CanDiscardItemsFromQuest(LazyRow<Quest> quest)
    {
        return quest.Row > 0 &&
               quest.Value?.JournalGenre.Value?.JournalCategory.Value?.JournalSection
                   .Row is 0 or 1 or 6; // pre-EW MSQ, EW MSQ or Job/Class quest
    }

    public IEnumerable<CachedItemInfo> AllItems => _items.Values;


    public bool TryGetItem(uint itemId, [NotNullWhen(true)] out CachedItemInfo? item)
        => _items.TryGetValue(itemId, out item);

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

    public ushort GetItemIconId(uint itemId)
    {
        if (_items.TryGetValue(itemId, out var item))
            return item.IconId;
        return ushort.MinValue;
    }

    public sealed class CachedItemInfo
    {
        public required uint ItemId { get; init; }
        public required string Name { get; init; }
        public required ushort IconId { get; init; }
        public required uint ILvl { get; init; }
        public required uint Level { get; init; }
        public required byte Rarity { get; init; }
        public required bool IsUnique { get; init; }
        public required bool IsUntradable { get; init; }

        /// <summary>
        /// Whether this item can be discarded at all. "Discard" is greyed out e.g. for the preorder EXP earrings.
        /// </summary>
        public required bool IsIndisposable { get; init; }

        public bool CanBeBoughtFromCalamitySalvager { get; set; }

        public required uint UiCategory { get; init; }
        public required string UiCategoryName { get; init; }
        public required uint EquipSlotCategory { get; init; }

        public bool CanBeDiscarded()
        {
            if (InternalConfiguration.BlacklistedItems.Contains(ItemId) ||
                InternalConfiguration.UltimateWeapons.Contains(ItemId))
                return false;

            if (UiCategory is UiCategories.Currency or UiCategories.Crystals or UiCategories.Unobtainable)
                return false;

            if (InternalConfiguration.WhitelistedItems.Contains(ItemId) ||
                InternalConfiguration.DiscardableGearCoffers.Contains(ItemId))
                return true;

            return CanBeBoughtFromCalamitySalvager ||
                   this is { IsUnique: false, IsUntradable: false, IsIndisposable: false };
        }
    }
}
