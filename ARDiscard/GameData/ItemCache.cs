using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace ARDiscard.GameData;

internal sealed class ItemCache
{
    private readonly Dictionary<uint, CachedItemInfo> _items = new();

    public ItemCache(IDataManager dataManager, ListManager listManager)
    {
        foreach (var item in dataManager.GetExcelSheet<Item>())
        {
            if (item.RowId == 0)
                continue;

            _items[item.RowId] = new CachedItemInfo
            {
                ItemId = item.RowId,
                Name = item.Name.ToString(),
                IconId = item.Icon,
                ILvl = item.LevelItem.RowId,
                Rarity = item.Rarity,
                IsUnique = item.IsUnique,
                IsUntradable = item.IsUntradable,
                IsIndisposable = item.IsIndisposable,
                Level = item.LevelEquip,
                UiCategory = item.ItemUICategory.RowId,
                UiCategoryName = item.ItemUICategory.Value.Name.ToString(),
                EquipSlotCategory = item.EquipSlotCategory.RowId,
            };

            if (item is { Rarity: 3, MateriaSlotCount: 3, RowId: < 33154 or > 33358 })
                listManager.AddToInternalBlacklist(item.RowId);

            if (item is { ItemSearchCategory.RowId: 79, ItemUICategory.RowId: >= 101 and <= 104 })
                listManager.AddToInternalBlacklist(item.RowId);
        }

        foreach (var shopItem in dataManager.GetSubrowExcelSheet<GilShopItem>().SelectMany(x => x))
        {
            // exclude base ARR relics, not strictly necessary since we don't allow discarding weapons anyway
            if (shopItem.Item.Value.Rarity == 4)
                continue;

            // the item can be discarded already
            if (!_items.TryGetValue(shopItem.Item.RowId, out CachedItemInfo? cachedItemInfo) ||
                cachedItemInfo.CanBeDiscarded(listManager))
                continue;

            if (shopItem.AchievementRequired.RowId != 0)
                continue;

            // has a quest required to unlock from the shop
            if (!shopItem.QuestRequired.Any(CanDiscardItemsFromQuest))
                continue;

            cachedItemInfo.CanBeBoughtFromCalamitySalvager = true;
        }

        foreach (var collectableItem in dataManager.GetSubrowExcelSheet<CollectablesShopItem>().SelectMany(x => x))
        {
            if (collectableItem.RowId == 0)
                continue;

            listManager.AddToInternalWhitelist(collectableItem.Item.RowId);
        }

        // only look at msq + regional side quests
        foreach (var quest in dataManager.GetExcelSheet<Quest>().Where(x =>
                     x.JournalGenre.ValueNullable?.JournalCategory.ValueNullable?.JournalSection.RowId is 0 or 1 or 3))
        {
            foreach (var itemRef in quest.Reward.Where(x => x.RowId > 0))
            {
                var item = itemRef.GetValueOrDefault<Item>();
                if (item is { Rarity: 1, ItemAction.RowId: 388 } && item.Value.RowId != 38809 && item.Value.RowId != 29679)
                    listManager.AddToInternalWhitelist(item.Value.RowId);
            }
        }

        MaxDungeonItemLevel = _items.Values.Where(x => x.Rarity == 2)
            .Select(x => (int)x.ILvl)
            .Max();
    }

    public int MaxDungeonItemLevel { get; }

    private bool CanDiscardItemsFromQuest(RowRef<Quest> quest)
    {
        return quest is { RowId: > 0, IsValid: true } &&
               quest.ValueNullable?.JournalGenre.ValueNullable?.JournalCategory.ValueNullable?.JournalSection
                   .RowId is 0 or 1 or 6; // pre-EW MSQ, EW MSQ or Job/Class quest
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

        public bool CanBeDiscarded(IListManager listManager, bool checkConfiguration = true)
        {
            if (listManager.IsBlacklisted(ItemId, checkConfiguration))
                return false;

            if (UiCategory is UiCategories.Currency or UiCategories.Crystals or UiCategories.Unobtainable)
                return false;

            if (listManager.IsWhitelisted(ItemId))
                return true;

            return CanBeBoughtFromCalamitySalvager ||
                   this is { IsUnique: false, IsUntradable: false, IsIndisposable: false };
        }
    }
}
