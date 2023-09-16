using System;
using System.Linq;
using ARDiscard.GameData;
using ARDiscard.Windows;
using Dalamud.ContextMenu;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;

namespace ARDiscard;

internal sealed class ContextMenuIntegration : IDisposable
{
    private readonly Configuration _configuration;
    private readonly ConfigWindow _configWindow;
    private readonly InventoryContextMenuItem _addItem;
    private readonly InventoryContextMenuItem _removeItem;
    private readonly DalamudContextMenu _dalamudContextMenu;

    public ContextMenuIntegration(Configuration configuration, ConfigWindow configWindow)
    {
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

        _dalamudContextMenu = new();
        _dalamudContextMenu.OnOpenInventoryContextMenu += OpenInventoryContextMenu;
    }

    private void OpenInventoryContextMenu(InventoryContextMenuOpenArgs args)
    {
        if (!_configuration.ContextMenu.Enabled)
            return;

        if (_configuration.ContextMenu.OnlyWhenConfigIsOpen && !_configWindow.IsOpen)
            return;

        if (!(args.ParentAddonName is "Inventory" or "InventoryExpansion" or "InventoryLarge"))
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
        _configWindow.AddToDiscardList(args.ItemId);
    }

    private void RemoveFromDiscardList(InventoryContextMenuItemSelectedArgs args)
    {
        _configWindow.RemoveFromDiscardList(args.ItemId);
    }

    public void Dispose()
    {
        _dalamudContextMenu.OnOpenInventoryContextMenu -= OpenInventoryContextMenu;
        _dalamudContextMenu.Dispose();
    }
}
