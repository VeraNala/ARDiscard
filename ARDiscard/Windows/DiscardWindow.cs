using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ARDiscard.GameData;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Common.Math;
using ImGuiNET;
using LLib;
using LLib.ImGui;

namespace ARDiscard.Windows;

internal sealed class DiscardWindow : LWindow
{
    private readonly InventoryUtils _inventoryUtils;
    private readonly ItemCache _itemCache;
    private readonly IconCache _iconCache;
    private readonly IClientState _clientState;
    private readonly ICondition _condition;
    private readonly Configuration _configuration;

    private List<SelectableItem> _displayedItems = new();

    public event EventHandler? OpenConfigurationClicked;
    public event EventHandler<ItemFilter>? DiscardAllClicked;

    public DiscardWindow(InventoryUtils inventoryUtils, ItemCache itemCache, IconCache iconCache,
        IClientState clientState, ICondition condition, Configuration configuration)
        : base("Discard Items###AutoDiscardDiscard")
    {
        _inventoryUtils = inventoryUtils;
        _itemCache = itemCache;
        _iconCache = iconCache;
        _clientState = clientState;
        _condition = condition;
        _configuration = configuration;

        Size = new Vector2(600, 400);
        SizeCondition = ImGuiCond.FirstUseEver;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 400),
            MaximumSize = new Vector2(9999, 9999),
        };
    }

    public bool Locked { get; set; }

    public override void Draw()
    {
        ImGui.Text("With your current configuration, the following items would be discarded:");

        ImGui.BeginDisabled(Locked);
        if (ImGui.BeginChild("Right", new Vector2(-1, -30), true, ImGuiWindowFlags.NoSavedSettings))
        {
            if (!_clientState.IsLoggedIn)
            {
                ImGui.Text("Not logged in.");
            }
            else if (_displayedItems.Count == 0)
            {
                ImGui.Text("No items to discard.");
            }
            else
            {
                if (_configuration.Preview.GroupByCategory)
                {
                    foreach (var category in _displayedItems.OrderBy(x => x.UiCategory)
                                 .GroupBy(x => new { x.UiCategory, x.UiCategoryName }))
                    {
                        ImGui.Text($"{category.Key.UiCategoryName}");
                        ImGui.Indent();
                        foreach (var displayedItem in category)
                            DrawItem(displayedItem);
                        ImGui.Unindent();
                    }
                }
                else
                {
                    foreach (var displayedItem in _displayedItems)
                        DrawItem(displayedItem);
                }
            }
        }

        ImGui.EndDisabled();

        ImGui.EndChild();

        ImGui.BeginDisabled(OpenConfigurationClicked == null);
        if (ImGui.Button("Open Configuration"))
            OpenConfigurationClicked!.Invoke(this, EventArgs.Empty);
        ImGui.EndDisabled();
        ImGui.SameLine(ImGui.GetWindowWidth() - 160 * ImGuiHelpers.GlobalScale);
        ImGui.BeginDisabled(Locked ||
                            !_clientState.IsLoggedIn ||
                            !(_condition[ConditionFlag.NormalConditions] || _condition[ConditionFlag.Mounted]) ||
                            !_displayedItems.Any(x => x.Selected) ||
                            DiscardAllClicked == null);
        if (ImGui.Button("Discard all selected items"))
        {
            DiscardAllClicked!.Invoke(this, new ItemFilter
            {
                ItemIds = _displayedItems.Where(x => x.Selected).Select(x => x.ItemId).ToList()
            });
        }

        ImGui.EndDisabled();
    }

    private void DrawItem(SelectableItem displayedItem)
    {
        if (_configuration.Preview.ShowIcons)
        {
            IDalamudTextureWrap? icon = _iconCache.GetIcon(displayedItem.IconId);
            if (icon != null)
            {
                ImGui.Image(icon.ImGuiHandle, new Vector2(23, 23));
                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);
            }
        }

        if (ImGui.Selectable(displayedItem.ToString(), displayedItem.Selected))
            displayedItem.Selected = !displayedItem.Selected;
    }

    public override void OnOpen() => RefreshInventory(false);

    public override void OnClose() => _displayedItems.Clear();

    public unsafe void RefreshInventory(bool keepSelected)
    {
        if (!IsOpen)
            return;

        List<uint> notSelected = new();
        if (keepSelected)
        {
            notSelected.AddRange(_displayedItems
                .Where(x => !x.Selected)
                .Select(x => x.ItemId));
        }

        _displayedItems = _inventoryUtils.GetAllItemsToDiscard()
            .GroupBy(x => new
            {
                ItemId = x.InventoryItem->ItemID,
                ItemInfo = _itemCache.GetItem(x.InventoryItem->ItemID),
            })
            .Where(x => x.Key.ItemInfo != null)
            .Select(x => new SelectableItem
            {
                ItemId = x.Key.ItemId,
                Name = x.Key.ItemInfo!.Name,
                IconId = x.Key.ItemInfo!.IconId,
                Quantity = x.Sum(y => y.InventoryItem->Quantity),
                UiCategory = x.Key.ItemInfo!.UiCategory,
                UiCategoryName = x.Key.ItemInfo!.UiCategoryName,
                Selected = !notSelected.Contains(x.Key.ItemId),
            })
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed class SelectableItem
    {
        public required uint ItemId { get; init; }
        public required string Name { get; init; }
        public required ushort IconId { get; init; }
        public required long Quantity { get; init; }
        public required uint UiCategory { get; init; }
        public required string UiCategoryName { get; init; }
        public required bool Selected { get; set; }

        public override string ToString()
        {
            if (Quantity > 1)
                return $"{Name} ({Quantity}x)";

            return Name;
        }
    }

    public void Login() => RefreshInventory(false);

    public void Logout() => _displayedItems.Clear();
}
