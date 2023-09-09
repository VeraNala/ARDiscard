using System;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace ARDiscard;

public class InventoryUtils : IDisposable
{
    private readonly Configuration _configuration;

    private static readonly InventoryType[] InventoryTypes =
        { InventoryType.Inventory1, InventoryType.Inventory2, InventoryType.Inventory3, InventoryType.Inventory4 };

    private unsafe delegate void DiscardItemDelegate(AgentInventoryContext* inventoryManager, InventoryItem* itemSlot,
        InventoryType inventory, int slot, uint addonId, int position = -1);

    [Signature("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 85 C0 74 ?? 0F B7 48")]
    private DiscardItemDelegate _discardItem = null!;

    public InventoryUtils(Configuration configuration)
    {
        _configuration = configuration;
        SignatureHelper.Initialise(this);
    }

    public void Dispose()
    {
    }

    public unsafe InventoryItem* GetNextItemToDiscard()
    {
        InventoryManager* inventoryManager = InventoryManager.Instance();
        foreach (InventoryType inventoryType in InventoryTypes)
        {
            InventoryContainer* container = inventoryManager->GetInventoryContainer(inventoryType);
            for (int i = 0; i < container->Size; ++i)
            {
                var item = container->GetInventorySlot(i);
                if (item != null)
                {
                    if (_configuration.DiscardingItems.Contains(item->ItemID))
                    {
                        return item;
                    }
                }
            }
        }

        return null;
    }

    public unsafe void Discard(InventoryItem* item)
    {
        _discardItem(AgentInventoryContext.Instance(), item, item->Container, item->Slot, 0);
    }
}
