using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ARDiscard.GameData;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ImGuiNET;

namespace ARDiscard.Windows;

internal sealed class ConfigWindow : Window
{
    private readonly DalamudPluginInterface _pluginInterface;
    private readonly Configuration _configuration;
    private readonly ItemCache _itemCache;
    private readonly IClientState _clientState;
    private readonly ICondition _condition;
    private string _itemName = string.Empty;

    private List<(uint ItemId, string Name)> _searchResults = new();
    private List<(uint ItemId, string Name)> _discarding = new();
    private List<(uint ItemId, string Name)>? _allItems = null;
    private bool _resetKeyboardFocus = true;

    public event EventHandler? DiscardNowClicked;
    public event EventHandler? ConfigSaved;

    public ConfigWindow(DalamudPluginInterface pluginInterface, Configuration configuration, ItemCache itemCache,
        IClientState clientState, ICondition condition)
        : base("Auto Discard###AutoDiscardConfig")
    {
        _pluginInterface = pluginInterface;
        _configuration = configuration;
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

        _discarding.AddRange(_configuration.DiscardingItems
            .Select(x => (x, itemCache.GetItemName(x))).ToList());
    }

    public override void Draw()
    {
        bool runAfterVenture = _configuration.RunAfterVenture;
        if (ImGui.Checkbox("[Global] Run automatically after AutoRetainer's venture", ref runAfterVenture))
        {
            _configuration.RunAfterVenture = runAfterVenture;
            Save();
        }

        ImGui.SameLine(ImGui.GetWindowWidth() - 115 * ImGuiHelpers.GlobalScale);
        ImGui.BeginDisabled(!_clientState.IsLoggedIn ||
                            !(_condition[ConditionFlag.NormalConditions] || _condition[ConditionFlag.Mounted]) ||
                            DiscardNowClicked == null);
        if (ImGui.Button("Preview Discards"))
            DiscardNowClicked!.Invoke(this, EventArgs.Empty);
        ImGui.EndDisabled();

        bool runBeforeLogout = _configuration.RunBeforeLogout;
        if (ImGui.Checkbox("[Global] Run before logging out in Multi-Mode", ref runBeforeLogout))
        {
            _configuration.RunBeforeLogout = runBeforeLogout;
            Save();
        }

        if (ImGui.BeginTabBar("AutoDiscardTabs"))
        {
            DrawDiscardList();
            DrawExcludedCharacters();
            DrawExperimentalSettings();

            ImGui.EndTabBar();
        }
    }

    private void DrawDiscardList()
    {
        if (ImGui.BeginTabItem("Items to Discard"))
        {
            var ws = ImGui.GetWindowSize();
            if (ImGui.BeginChild("Left", new Vector2(Math.Max(10, ws.X / 2), -1), true))
            {
                ImGui.Text("Search");
                ImGui.SetNextItemWidth(ws.X / 2 - 20);
                if (_resetKeyboardFocus)
                {
                    ImGui.SetKeyboardFocusHere();
                    _resetKeyboardFocus = false;
                }

                string previousName = _itemName;
                if (ImGui.InputText("", ref _itemName, 256, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    _resetKeyboardFocus = true;
                    if (_searchResults.Count > 0)
                    {
                        var itemToAdd = _searchResults.FirstOrDefault();
                        if (_discarding.All(x => x.ItemId != itemToAdd.ItemId))
                        {
                            _discarding.Add(itemToAdd);
                        }
                        else
                        {
                            _discarding.Remove(itemToAdd);
                        }

                        Save();
                    }
                }

                if (previousName != _itemName)
                    UpdateResults();

                ImGui.Separator();

                if (string.IsNullOrEmpty(_itemName))
                {
                    ImGui.Text("Type item name...");
                }

                foreach (var (id, name) in _searchResults)
                {
                    bool selected = _discarding.Any(x => x.Item1 == id);
                    if (ImGui.Selectable(name, selected))
                    {
                        if (!selected)
                        {
                            _discarding.Add((id, name));
                        }
                        else
                        {
                            _discarding.Remove((id, name));
                        }

                        Save();
                    }
                }
            }

            ImGui.EndChild();
            ImGui.SameLine();

            if (ImGui.BeginChild("Right", new Vector2(-1, -1), true, ImGuiWindowFlags.NoSavedSettings))
            {
                ImGui.Text("Items that will be automatically discarded");
                ImGui.Separator();

                List<(uint, string)> toRemove = new();
                foreach (var (id, name) in _discarding.OrderBy(x => x.Name.ToLower()))
                {
                    if (ImGui.Selectable(name, true))
                        toRemove.Add((id, name));
                }

                if (toRemove.Count > 0)
                {
                    foreach (var tr in toRemove)
                        _discarding.Remove(tr);

                    Save();
                }
            }

            ImGui.EndChild();
            ImGui.EndTabItem();
        }
    }

    private void DrawExcludedCharacters()
    {
        if (ImGui.BeginTabItem("Excluded Characters"))
        {
            if (_clientState is { IsLoggedIn: true, LocalContentId: > 0 })
            {
                string worldName = _clientState.LocalPlayer?.HomeWorld.GameData?.Name ?? "??";
                ImGui.TextWrapped(
                    $"Current Character: {_clientState.LocalPlayer?.Name} @ {worldName} ({_clientState.LocalContentId:X})");
                ImGui.Indent(30);
                if (_configuration.ExcludedCharacters.Any(x => x.LocalContentId == _clientState.LocalContentId))
                {
                    ImGui.TextColored(ImGuiColors.DalamudRed, "This character is currently excluded.");
                    if (ImGui.Button("Remove exclusion"))
                    {
                        _configuration.ExcludedCharacters.RemoveAll(
                            c => c.LocalContentId == _clientState.LocalContentId);
                        Save();
                    }
                }
                else
                {
                    if (_configuration.RunAfterVenture || _configuration.RunBeforeLogout)
                    {
                        ImGui.TextColored(ImGuiColors.HealerGreen,
                            "This character is currently included (and will be post-processed in autoretainer).");
                    }
                    else
                    {
                        ImGui.TextColored(ImGuiColors.DalamudYellow,
                            "This character is currently included (but running post-processing is disabled globally)");
                    }

                    if (ImGui.Button("Exclude current character"))
                    {
                        _configuration.ExcludedCharacters.Add(new Configuration.CharacterInfo
                        {
                            LocalContentId = _clientState.LocalContentId,
                            CachedPlayerName = _clientState.LocalPlayer?.Name.ToString() ?? "??",
                            CachedWorldName = worldName,
                        });
                        Save();
                    }
                }

                ImGui.Unindent(30);
            }
            else
            {
                ImGui.TextColored(ImGuiColors.DalamudRed, "You are not logged in.");
            }

            ImGui.Separator();
            ImGui.TextWrapped(
                "Characters that won't run auto-cleanup after ventures (/discardall works for excluded characters)");
            ImGui.Spacing();

            ImGui.Indent(30);
            if (_configuration.ExcludedCharacters.Count == 0)
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, "No excluded characters.");
            }
            else
            {
                foreach (var characterInfo in _configuration.ExcludedCharacters)
                {
                    ImGui.Text(
                        $"{characterInfo.CachedPlayerName} @ {characterInfo.CachedWorldName} ({characterInfo.LocalContentId:X})");
                }
            }

            ImGui.Unindent(30);

            ImGui.EndTabItem();
        }
    }

    private void DrawExperimentalSettings()
    {
        if (ImGui.BeginTabItem("Experimental Settings"))
        {
            bool discardFromArmouryChest = _configuration.Armoury.DiscardFromArmouryChest;
            if (ImGui.Checkbox("Discard items from Armoury Chest", ref discardFromArmouryChest))
            {
                _configuration.Armoury.DiscardFromArmouryChest = discardFromArmouryChest;
                Save();
            }

            ImGui.BeginDisabled(!discardFromArmouryChest);
            ImGui.Indent(30);

            bool leftSideGear = _configuration.Armoury.CheckLeftSideGear;
            if (ImGui.Checkbox("Discard when items are found in Head/Body/Hands/Legs/Feet", ref leftSideGear))
            {
                _configuration.Armoury.CheckLeftSideGear = leftSideGear;
                Save();
            }

            bool rightSideGear = _configuration.Armoury.CheckRightSideGear;
            if (ImGui.Checkbox("Discard when items are found in Accessories", ref rightSideGear))
            {
                _configuration.Armoury.CheckRightSideGear = rightSideGear;
                Save();
            }

            ImGui.SetNextItemWidth(ImGuiHelpers.GlobalScale * 100);
            int maximumItemLevel = _configuration.Armoury.MaximumGearItemLevel;
            if (ImGui.InputInt("Ignore items >= this ilvl (Armoury Chest only)",
                    ref maximumItemLevel))
            {
                _configuration.Armoury.MaximumGearItemLevel = Math.Max(0, Math.Min(625, maximumItemLevel));
                Save();
            }

            ImGui.Unindent(30);
            ImGui.EndDisabled();

            ImGui.Separator();

            bool contextMenuEnabled = _configuration.ContextMenu.Enabled;
            if (ImGui.Checkbox("Inventory context menu integration", ref contextMenuEnabled))
            {
                _configuration.ContextMenu.Enabled = contextMenuEnabled;
                Save();
            }

            ImGui.BeginDisabled(!contextMenuEnabled);
            ImGui.Indent(30);
            bool contextMenuOnlyWhenConfigIsOpen = _configuration.ContextMenu.OnlyWhenConfigIsOpen;
            if (ImGui.Checkbox("Only add menu entries while config window is open",
                    ref contextMenuOnlyWhenConfigIsOpen))
            {
                _configuration.ContextMenu.OnlyWhenConfigIsOpen = contextMenuOnlyWhenConfigIsOpen;
                Save();
            }

            ImGui.Unindent(30);
            ImGui.EndDisabled();

            ImGui.Separator();

            ImGui.SetNextItemWidth(ImGuiHelpers.GlobalScale * 100);
            int ignoreItemCountWhenAbove = (int)_configuration.IgnoreItemCountWhenAbove;
            if (ImGui.InputInt("Ignore stacks with >= this number of items", ref ignoreItemCountWhenAbove))
            {
                _configuration.IgnoreItemCountWhenAbove = (uint)Math.Max(2, ignoreItemCountWhenAbove);
                Save();
            }

            ImGui.EndTabItem();
        }
    }

    private void UpdateResults()
    {
        if (string.IsNullOrEmpty(_itemName))
            _searchResults = new();
        else
        {
            _searchResults = EnsureAllItemsLoaded().Where(x =>
                    x.Name.Contains(_itemName, StringComparison.CurrentCultureIgnoreCase)
                    || (uint.TryParse(_itemName, out uint itemId) && x.ItemId == itemId))
                .OrderBy(x => _itemName.EqualsIgnoreCase(x.Name) ? string.Empty : x.Name)
                .ToList();
        }
    }

    private List<(uint ItemId, string Name)> EnsureAllItemsLoaded()
    {
        if (_allItems == null)
        {
            _allItems = _itemCache.AllItems
                .Where(x => InternalConfiguration.WhitelistedItems.Contains(x.ItemId) ||
                            x is { IsUnique: false, IsUntradable: false, IsIndisposable: false })
                .Where(x => x.UiCategory != UiCategories.Currency && x.UiCategory != UiCategories.Crystals &&
                            x.UiCategory != UiCategories.Unobtainable)
                .Select(x => (x.ItemId, x.Name.ToString()))
                .ToList();
        }

        return _allItems;
    }

    private void Save()
    {
        _configuration.DiscardingItems = _discarding.Select(x => x.ItemId).ToList();
        _pluginInterface.SavePluginConfig(_configuration);

        ConfigSaved?.Invoke(this, EventArgs.Empty);
    }

    internal void AddToDiscardList(uint itemId)
    {
        var item = EnsureAllItemsLoaded().SingleOrDefault(x => x.ItemId == itemId);
        if (item.ItemId != 0)
        {
            _discarding.Add(item);
            Save();
        }
    }

    internal void RemoveFromDiscardList(uint itemId)
    {
        if (_discarding.RemoveAll(x => x.ItemId == itemId) > 0)
            Save();
    }

    public bool CanItemBeConfigured(uint itemId)
    {
        return EnsureAllItemsLoaded().SingleOrDefault(x => x.ItemId == itemId).ItemId == itemId;
    }
}
