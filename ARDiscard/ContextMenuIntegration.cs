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

        if (!(args.ParentAddonName is "Inventory" or "InventoryExpansion" or "InventoryLarge" or "ArmouryBoard"))
            return;

        if (!_configWindow.CanItemBeConfigured(args.ItemId))
            return;

        if (_configuration.DiscardingItems.Contains(args.ItemId))
            args.AddCustomItem(_removeItem);
        else if (_itemCache.TryGetItem(args.ItemId, out ItemCache.CachedItemInfo? cachedItemInfo) &&
                 cachedItemInfo.CanBeDiscarded())
            args.AddCustomItem(_addItem);
    }

    private void AddToDiscardList(InventoryContextMenuItemSelectedArgs args)
    {
        if (_configWindow.AddToDiscardList(args.ItemId))
        {
            _chatGui.Print(new SeString(new UIForegroundPayload(52))
                .Append($"\ue05f ")
                .Append(new UIForegroundPayload(0))
                .Append($"Added ")
                .Append(new UIForegroundPayload(52))
                .Append(_itemCache.GetItemName(args.ItemId))
                .Append(new UIForegroundPayload(0))
                .Append(" to Auto Discard List."));
        }
    }

    private void RemoveFromDiscardList(InventoryContextMenuItemSelectedArgs args)
    {
        if (_configWindow.RemoveFromDiscardList(args.ItemId))
        {
            _chatGui.Print(new SeString(new UIForegroundPayload(52))
                .Append($"\ue05f ")
                .Append(new UIForegroundPayload(0))
                .Append($"Removed ")
                .Append(new UIForegroundPayload(52))
                .Append(_itemCache.GetItemName(args.ItemId))
                .Append(new UIForegroundPayload(0))
                .Append(" from Auto Discard List."));
        }
    }

    public void Dispose()
    {
        _dalamudContextMenu.OnOpenInventoryContextMenu -= OpenInventoryContextMenu;
        _dalamudContextMenu.Dispose();
    }
}
