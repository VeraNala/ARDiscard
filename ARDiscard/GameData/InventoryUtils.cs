using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace ARDiscard.GameData;

internal sealed class InventoryUtils
{
    private static readonly InventoryType[] DefaultInventoryTypes =
    {
        InventoryType.Inventory1,
        InventoryType.Inventory2,
        InventoryType.Inventory3,
        InventoryType.Inventory4
    };

    private static readonly InventoryType[] MainHandOffHandInventoryTypes =
    {
        InventoryType.ArmoryMainHand,
        InventoryType.ArmoryOffHand,
    };

    private static readonly InventoryType[] LeftSideGearInventoryTypes =
    {
        InventoryType.ArmoryHead,
        InventoryType.ArmoryBody,
        InventoryType.ArmoryHands,
        InventoryType.ArmoryLegs,
        InventoryType.ArmoryFeets
    };

    private static readonly InventoryType[] RightSideGearInventoryTypes =
    {
        InventoryType.ArmoryEar,
        InventoryType.ArmoryNeck,
        InventoryType.ArmoryWrist,
        InventoryType.ArmoryRings
    };

    private static readonly IReadOnlyList<uint> NoGearsetItems = new List<uint>();

    private readonly Configuration _configuration;
    private readonly ItemCache _itemCache;
    private readonly IPluginLog _pluginLog;

    public InventoryUtils(Configuration configuration, ItemCache itemCache, IPluginLog pluginLog)
    {
        _configuration = configuration;
        _itemCache = itemCache;
        _pluginLog = pluginLog;
    }

    public unsafe List<ItemWrapper> GetAllItemsToDiscard()
    {
        List<ItemWrapper> toDiscard = new List<ItemWrapper>();
        Dictionary<uint, uint> itemCounts = new();

        InventoryManager* inventoryManager = InventoryManager.Instance();
        foreach (InventoryType inventoryType in DefaultInventoryTypes)
            toDiscard.AddRange(GetItemsToDiscard(inventoryManager, inventoryType, itemCounts, NoGearsetItems));

        if (_configuration.Armoury.DiscardFromArmouryChest)
        {
            var gearsetItems = GetAllGearsetItems();
            toDiscard.AddRange(GetArmouryItemsToDiscard(_configuration.Armoury.CheckMainHandOffHand, inventoryManager,
                MainHandOffHandInventoryTypes, itemCounts, gearsetItems));
            toDiscard.AddRange(GetArmouryItemsToDiscard(_configuration.Armoury.CheckLeftSideGear, inventoryManager,
                LeftSideGearInventoryTypes, itemCounts, gearsetItems));
            toDiscard.AddRange(GetArmouryItemsToDiscard(_configuration.Armoury.CheckRightSideGear, inventoryManager,
                RightSideGearInventoryTypes, itemCounts, gearsetItems));
        }

        return toDiscard
            .Where(x => itemCounts[x.InventoryItem->ItemID] < _configuration.IgnoreItemCountWhenAbove)
            .ToList();
    }

    private unsafe ReadOnlyCollection<ItemWrapper> GetArmouryItemsToDiscard(bool condition, InventoryManager* inventoryManager,
        InventoryType[] inventoryTypes, Dictionary<uint, uint> itemCounts, List<uint>? gearsetItems)
    {
        List<ItemWrapper> items = new();
        if (condition)
        {
            foreach (InventoryType inventoryType in inventoryTypes)
                items.AddRange(GetItemsToDiscard(inventoryManager, inventoryType, itemCounts, gearsetItems));
        }

        return items.AsReadOnly();
    }

    public unsafe InventoryItem* GetNextItemToDiscard(ItemFilter? itemFilter)
    {
        List<ItemWrapper> allItemsToDiscard = GetAllItemsToDiscard();
        ItemWrapper? toDiscard = allItemsToDiscard.FirstOrDefault(x =>
            itemFilter == null || itemFilter.ItemIds.Contains(x.InventoryItem->ItemID));
        return toDiscard != null ? toDiscard.InventoryItem : null;
    }

    private unsafe ReadOnlyCollection<ItemWrapper> GetItemsToDiscard(InventoryManager* inventoryManager,
        InventoryType inventoryType, Dictionary<uint, uint> itemCounts,
        IReadOnlyList<uint>? gearsetItems)
    {
        List<ItemWrapper> toDiscard = new List<ItemWrapper>();
        InventoryContainer* container = inventoryManager->GetInventoryContainer(inventoryType);
        //PluginLog.Verbose($"Checking {inventoryType}, {container->Size}");
        for (int i = 0; i < container->Size; ++i)
        {
            var item = container->GetInventorySlot(i);
            if (item != null && item->ItemID != 0)
            {
                if (itemCounts.TryGetValue(item->ItemID, out uint itemCount))
                    itemCounts[item->ItemID] = itemCount + item->Quantity;
                else
                    itemCounts[item->ItemID] = item->Quantity;

                if (InternalConfiguration.BlacklistedItems.Contains(item->ItemID) ||
                    InternalConfiguration.UltimateWeapons.Contains(item->ItemID))
                    continue;

                if (!_itemCache.TryGetItem(item->ItemID, out ItemCache.CachedItemInfo? itemInfo) ||
                    !itemInfo.CanBeDiscarded())
                    continue; // no info, who knows what that item is

                // skip gear if we're unable to load gearsets or it is used in a gearset
                if (itemInfo.EquipSlotCategory > 0 && (gearsetItems == null || gearsetItems.Contains(item->ItemID)))
                    continue;

                if (itemInfo is { EquipSlotCategory: > 0, CanBeBoughtFromCalamitySalvager: false } &&
                    itemInfo.ILvl >= _configuration.Armoury.MaximumGearItemLevel)
                    continue;

                if (_configuration.IgnoreItemWithSignature && item->CrafterContentID != 0)
                    continue;

                //PluginLog.Verbose($"{i} → {item->ItemID}");
                if (_configuration.DiscardingItems.Contains(item->ItemID))
                {
                    _pluginLog.Verbose(
                        $"Found item {item->ItemID} to discard in inventory {inventoryType} in slot {i}");
                    toDiscard.Add(new ItemWrapper { InventoryItem = item });
                }
            }
            else
            {
                //PluginLog.Verbose($"{i} → none");
            }
        }

        return toDiscard.AsReadOnly();
    }

    private unsafe List<uint>? GetAllGearsetItems()
    {
        var gearsetModule = RaptureGearsetModule.Instance();
        if (gearsetModule == null)
            return null;

        List<uint> allGearsetItems = new();
        for (int i = 0; i < 100; ++i)
        {
            var gearset = gearsetModule->GetGearset(i);
            if (gearset != null && gearset->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists))
            {
                var gearsetItems = new[]
                {
                    gearset->MainHand,
                    gearset->OffHand,
                    gearset->Head,
                    gearset->Body,
                    gearset->Hands,
                    gearset->Legs,
                    gearset->Feet,
                    gearset->Ears,
                    gearset->Neck,
                    gearset->Wrists,
                    gearset->RingRight,
                    gearset->RingLeft,
                };
                foreach (var gearsetItem in gearsetItems)
                {
                    if (gearsetItem.ItemID != 0)
                        allGearsetItems.Add(gearsetItem.ItemID);
                }
            }
        }

        return allGearsetItems;
    }

    public unsafe void Discard(InventoryItem* item)
    {
        if (InternalConfiguration.BlacklistedItems.Contains(item->ItemID) ||
            InternalConfiguration.UltimateWeapons.Contains(item->ItemID))
            throw new ArgumentException($"Can't discard {item->ItemID}", nameof(item));

        AgentInventoryContext.Instance()->DiscardItem(item, item->Container, item->Slot, 0);
    }

    public sealed unsafe class ItemWrapper
    {
        public required InventoryItem* InventoryItem { get; init; }
    }
}
