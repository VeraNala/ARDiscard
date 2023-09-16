using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
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
        InventoryType.ArmoryHands,
        InventoryType.ArmoryRings
    };

    private readonly Configuration _configuration;
    private readonly ItemCache _itemCache;

    private unsafe delegate void DiscardItemDelegate(AgentInventoryContext* inventoryManager, InventoryItem* itemSlot,
        InventoryType inventory, int slot, uint addonId, int position = -1);

    [Signature("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 85 C0 74 ?? 0F B7 48")]
    private DiscardItemDelegate _discardItem = null!;

    public InventoryUtils(Configuration configuration, ItemCache itemCache)
    {
        _configuration = configuration;
        _itemCache = itemCache;
        SignatureHelper.Initialise(this);
    }

    public unsafe List<ItemWrapper> GetAllItemsToDiscard()
    {
        List<ItemWrapper> toDiscard = new List<ItemWrapper>();

        InventoryManager* inventoryManager = InventoryManager.Instance();
        foreach (InventoryType inventoryType in DefaultInventoryTypes)
            toDiscard.AddRange(GetItemsToDiscard(inventoryManager, inventoryType, false, null));

        if (_configuration.Armoury.DiscardFromArmouryChest)
        {
            var gearsetItems = GetAllGearsetItems();

            if (_configuration.Armoury.CheckLeftSideGear)
            {
                foreach (InventoryType inventoryType in LeftSideGearInventoryTypes)
                    toDiscard.AddRange(GetItemsToDiscard(inventoryManager, inventoryType, true, gearsetItems));
            }

            if (_configuration.Armoury.CheckRightSideGear)
            {
                foreach (InventoryType inventoryType in RightSideGearInventoryTypes)
                    toDiscard.AddRange(GetItemsToDiscard(inventoryManager, inventoryType, true, gearsetItems));
            }
        }

        return toDiscard;
    }

    public unsafe InventoryItem* GetNextItemToDiscard(ItemFilter? itemFilter)
    {
        List<ItemWrapper> allItemsToDiscard = GetAllItemsToDiscard();
        ItemWrapper? toDiscard = allItemsToDiscard.FirstOrDefault(x =>
            itemFilter == null || itemFilter.ItemIds.Contains(x.InventoryItem->ItemID));
        return toDiscard != null ? toDiscard.InventoryItem : null;
    }

    private unsafe IReadOnlyList<ItemWrapper> GetItemsToDiscard(InventoryManager* inventoryManager,
        InventoryType inventoryType, bool doGearChecks, IReadOnlyList<uint>? gearsetItems)
    {
        List<ItemWrapper> toDiscard = new List<ItemWrapper>();
        InventoryContainer* container = inventoryManager->GetInventoryContainer(inventoryType);
        //PluginLog.Verbose($"Checking {inventoryType}, {container->Size}");
        for (int i = 0; i < container->Size; ++i)
        {
            var item = container->GetInventorySlot(i);
            if (item != null)
            {
                if (InternalConfiguration.BlacklistedItems.Contains(item->ItemID))
                    continue;

                if (doGearChecks)
                {
                    if (gearsetItems == null || gearsetItems.Contains(item->ItemID))
                        continue;

                    ItemCache.CachedItemInfo? itemInfo = _itemCache.GetItem(item->ItemID);
                    if (itemInfo == null)
                        continue; // no info, who knows what that item is

                    if (itemInfo.ILvl >= _configuration.Armoury.MaximumGearItemLevel)
                        continue;
                }

                //PluginLog.Verbose($"{i} → {item->ItemID}");
                if (_configuration.DiscardingItems.Contains(item->ItemID))
                {
                    PluginLog.Information(
                        $"Found item {item->ItemID} to discard in inventory {inventoryType} in slot {i}");
                    toDiscard.Add(new ItemWrapper { InventoryItem = item });
                }
            }
            else
            {
                //PluginLog.Verbose($"{i} → none");
            }
        }

        return toDiscard;
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
                    gearset->RightLeft, // why is this called RightLeft
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
        if (InternalConfiguration.BlacklistedItems.Contains(item->ItemID))
            throw new Exception($"Can't discard {item->ItemID}");

        _discardItem(AgentInventoryContext.Instance(), item, item->Container, item->Slot, 0);
    }

    public sealed unsafe class ItemWrapper
    {
        public required InventoryItem* InventoryItem { get; init; }
    }
}
