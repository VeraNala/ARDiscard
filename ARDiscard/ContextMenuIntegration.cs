using System;
using System.Linq;
using ARDiscard.GameData;
using ARDiscard.GameData.Agents;
using ARDiscard.Windows;
using Dalamud.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace ARDiscard;

internal sealed class ContextMenuIntegration : IDisposable
{
    private readonly IChatGui _chatGui;
    private readonly ItemCache _itemCache;
    private readonly Configuration _configuration;
    private readonly ConfigWindow _configWindow;
    private readonly InventoryContextMenuItem _addItem;
    private readonly InventoryContextMenuItem _removeItem;
    private readonly DalamudContextMenu _dalamudContextMenu;

    public ContextMenuIntegration(DalamudPluginInterface pluginInterface, IChatGui chatGui, ItemCache itemCache,
        Configuration configuration, ConfigWindow configWindow)
    {
        _chatGui = chatGui;
        _itemCache = itemCache;
        _configuration = configuration;
        _configWindow = configWindow;
        _addItem = new InventoryContextMenuItem(
            new SeString(new UIForegroundPayload(52))
                .Append($"\ue05f ")
                .Append(new UIForegroundPayload(0)).Append("Add to Auto Discard List"),
            AddToDiscardList);
        _removeItem = new InventoryContextMenuItem(
            new SeString(new UIForegroundPayload(52))
                .Append($"\ue05f ")
                .Append(new UIForegroundPayload(0)).Append("Remove from Auto Discard List"),
            RemoveFromDiscardList);

        _dalamudContextMenu = new(pluginInterface);
        _dalamudContextMenu.OnOpenInventoryContextMenu += OpenInventoryContextMenu;
    }

    private unsafe void OpenInventoryContextMenu(InventoryContextMenuOpenArgs args)
    {
        if (!_configuration.ContextMenu.Enabled)
            return;

        if (_configuration.ContextMenu.OnlyWhenConfigIsOpen && !_configWindow.IsOpen)
            return;

        if (args.ParentAddonName == "ArmouryBoard")
        {
            var agent = AgentModule.Instance()->GetAgentByInternalId(AgentId.ArmouryBoard);
            if (agent == null || !agent->IsAgentActive())
                return;

            // don't add it in the main/off hand weapon tabs, as we don't use these for discarding
            var agentArmouryBoard = (AgentArmouryBoard*)agent;
            if (agentArmouryBoard->CurrentTab is 0 or 6)
                return;
        }
        else if (!(args.ParentAddonName is "Inventory" or "InventoryExpansion" or "InventoryLarge"))
            return;

        if (!_configWindow.CanItemBeConfigured(args.ItemId))
            return;

        if (_configuration.DiscardingItems.Contains(args.ItemId))
            args.AddCustomItem(_removeItem);
        else if (!InternalConfiguration.BlacklistedItems.Contains(args.ItemId))
            args.AddCustomItem(_addItem);
    }

    private void AddToDiscardList(InventoryContextMenuItemSelectedArgs args)
    {
        if (_configWindow.AddToDiscardList(args.ItemId))
        {
            _chatGui.Print(new SeString(new UIForegroundPayload(52))
                .Append($"\ue05f ")
                .Append(new UIForegroundPayload(0))
                .Append($"Added '{_itemCache.GetItemName(args.ItemId)}' to Auto Discard List."));
        }
    }

    private void RemoveFromDiscardList(InventoryContextMenuItemSelectedArgs args)
    {
        if (_configWindow.RemoveFromDiscardList(args.ItemId))
        {
            _chatGui.Print(new SeString(new UIForegroundPayload(52))
                .Append($"\ue05f ")
                .Append(new UIForegroundPayload(0))
                .Append($"Removed '{_itemCache.GetItemName(args.ItemId)}' from Auto Discard List."));
        }
    }

    public void Dispose()
    {
        _dalamudContextMenu.OnOpenInventoryContextMenu -= OpenInventoryContextMenu;
        _dalamudContextMenu.Dispose();
    }
}
