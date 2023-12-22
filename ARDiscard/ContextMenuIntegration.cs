using System;
using ARDiscard.GameData;
using ARDiscard.Windows;
using Dalamud.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace ARDiscard;

internal sealed class ContextMenuIntegration : IDisposable
{
    private readonly IChatGui _chatGui;
    private readonly ItemCache _itemCache;
    private readonly Configuration _configuration;
    private readonly ConfigWindow _configWindow;
    private readonly IGameGui _gameGui;
    private readonly SeString _addItemPayload;
    private readonly SeString _removeItemPayload;
    private readonly InventoryContextMenuItem _addInventoryItem;
    private readonly InventoryContextMenuItem _removeInventoryItem;
    private readonly DalamudContextMenu _dalamudContextMenu;

    public ContextMenuIntegration(DalamudPluginInterface pluginInterface, IChatGui chatGui, ItemCache itemCache,
        Configuration configuration, ConfigWindow configWindow, IGameGui gameGui)
    {
        _chatGui = chatGui;
        _itemCache = itemCache;
        _configuration = configuration;
        _configWindow = configWindow;
        _gameGui = gameGui;
        _addItemPayload =
            new SeString(new UIForegroundPayload(52))
                .Append($"\ue05f ")
                .Append(new UIForegroundPayload(0)).Append("Add to Auto Discard List");
        _removeItemPayload = new SeString(new UIForegroundPayload(52))
            .Append($"\ue05f ")
            .Append(new UIForegroundPayload(0)).Append("Remove from Auto Discard List");
        _addInventoryItem = new InventoryContextMenuItem(_addItemPayload, AddToDiscardList);
        _removeInventoryItem = new InventoryContextMenuItem(_removeItemPayload, RemoveFromDiscardList);

        _dalamudContextMenu = new(pluginInterface);
        _dalamudContextMenu.OnOpenInventoryContextMenu += OpenInventoryContextMenu;
        _dalamudContextMenu.OnOpenGameObjectContextMenu += OpenGameObjectContextMenu;
    }

    private void OpenInventoryContextMenu(InventoryContextMenuOpenArgs args)
    {
        if (!IsEnabled())
            return;

        if (args.ParentAddonName is not ("Inventory" or "InventoryExpansion" or "InventoryLarge" or "ArmouryBoard"))
            return;

        if (!_configWindow.CanItemBeConfigured(args.ItemId))
            return;

        if (_configuration.DiscardingItems.Contains(args.ItemId))
            args.AddCustomItem(_removeInventoryItem);
        else if (_itemCache.TryGetItem(args.ItemId, out ItemCache.CachedItemInfo? cachedItemInfo) &&
                 cachedItemInfo.CanBeDiscarded())
            args.AddCustomItem(_addInventoryItem);
    }

    private void OpenGameObjectContextMenu(GameObjectContextMenuOpenArgs args)
    {
        if (!IsEnabled())
            return;

        if (args.ParentAddonName is not "ChatLog")
            return;

        uint itemId = (uint)_gameGui.HoveredItem;
        if (itemId > 1_000_000)
            itemId -= 1_000_000;

        if (itemId > 500_000)
            itemId -= 500_000;

        if (_configuration.DiscardingItems.Contains(itemId))
            args.AddCustomItem(new GameObjectContextMenuItem(_removeItemPayload, _ => RemoveFromDiscardList(itemId)));
        else if (_itemCache.TryGetItem(itemId, out ItemCache.CachedItemInfo? cachedItemInfo) &&
                 cachedItemInfo.CanBeDiscarded())
            args.AddCustomItem(new GameObjectContextMenuItem(_addItemPayload, _ => AddToDiscardList(itemId)));
    }

    private void AddToDiscardList(InventoryContextMenuItemSelectedArgs args) => AddToDiscardList(args.ItemId);

    private void AddToDiscardList(uint itemId)
    {
        if (_configWindow.AddToDiscardList(itemId))
        {
            _chatGui.Print(new SeString(new UIForegroundPayload(52))
                .Append($"\ue05f ")
                .Append(new UIForegroundPayload(0))
                .Append($"Added ")
                .Append(new UIForegroundPayload(52))
                .Append(new ItemPayload(itemId))
                .Append(_itemCache.GetItemName(itemId))
                .Append(RawPayload.LinkTerminator)
                .Append(new UIForegroundPayload(0))
                .Append(" to Auto Discard List."));
        }
    }

    private void RemoveFromDiscardList(InventoryContextMenuItemSelectedArgs args) => RemoveFromDiscardList(args.ItemId);

    private void RemoveFromDiscardList(uint itemId)
    {
        if (_configWindow.RemoveFromDiscardList(itemId))
        {
            _chatGui.Print(new SeString(new UIForegroundPayload(52))
                .Append($"\ue05f ")
                .Append(new UIForegroundPayload(0))
                .Append($"Removed ")
                .Append(new UIForegroundPayload(52))
                .Append(new ItemPayload(itemId))
                .Append(_itemCache.GetItemName(itemId))
                .Append(RawPayload.LinkTerminator)
                .Append(new UIForegroundPayload(0))
                .Append(" from Auto Discard List."));
        }
    }

    private bool IsEnabled()
    {
        if (!_configuration.ContextMenu.Enabled)
            return false;

        if (_configuration.ContextMenu.OnlyWhenConfigIsOpen && !_configWindow.IsOpen)
            return false;

        return true;
    }

    public void Dispose()
    {
        _dalamudContextMenu.OnOpenGameObjectContextMenu -= OpenGameObjectContextMenu;
        _dalamudContextMenu.OnOpenInventoryContextMenu -= OpenInventoryContextMenu;
        _dalamudContextMenu.Dispose();
    }
}
