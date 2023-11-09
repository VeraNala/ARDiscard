using System;
using System.Collections.Generic;
using System.Linq;
using ARDiscard.GameData;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Common.Math;
using ImGuiNET;
using LLib;

namespace ARDiscard.Windows;

internal sealed class DiscardWindow : LImGui.LWindow
{
    private readonly InventoryUtils _inventoryUtils;
    private readonly ItemCache _itemCache;
    private readonly IClientState _clientState;
    private readonly ICondition _condition;

    private List<SelectableItem> _displayedItems = new();

    public event EventHandler? OpenConfigurationClicked;
    public event EventHandler<ItemFilter>? DiscardAllClicked;

    public DiscardWindow(InventoryUtils inventoryUtils, ItemCache itemCache,
        IClientState clientState, ICondition condition)
        : base("Discard Items###AutoDiscardDiscard")
    {
        _inventoryUtils = inventoryUtils;
        _itemCache = itemCache;
        _clientState = clientState;
        _condition = condition;

        Size = new Vector2(600, 400);
        SizeCondition = ImGuiCond.FirstUseEver;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 400),
            MaximumSize = new Vector2(9999, 9999),
        };
    }

    public bool Locked { get; set; } = false;

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
                foreach (var displayedItem in _displayedItems)
                {
                    if (ImGui.Selectable(displayedItem.ToString(), displayedItem.Selected))
                        displayedItem.Selected = !displayedItem.Selected;
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
                            _displayedItems.Count(x => x.Selected) == 0 ||
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
            .GroupBy(x => x.InventoryItem->ItemID)
            .Select(x => new SelectableItem
            {
                ItemId = x.Key,
                Name = _itemCache.GetItemName(x.Key),
                Quantity = x.Sum(y => y.InventoryItem->Quantity),
                Selected = !notSelected.Contains(x.Key),
            })
            .OrderBy(x => x.Name.ToLower())
            .ToList();
    }

    private sealed class SelectableItem
    {
        public required uint ItemId { get; init; }
        public required string Name { get; init; }
        public required long Quantity { get; init; }
        public bool Selected { get; set; } = true;

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
