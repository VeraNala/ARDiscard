using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ARDiscard.GameData;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;
using LLib.ImGui;

namespace ARDiscard.Windows;

internal sealed class ConfigWindow : LWindow
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly Configuration _configuration;
    private readonly ItemCache _itemCache;
    private readonly IListManager _listManager;
    private readonly IClientState _clientState;
    private readonly ICondition _condition;
    private readonly DiscardListTab _discardListTab;
    private readonly ExcludedListTab _excludedListTab;

    private List<(uint ItemId, string Name)>? _allItems;

    public event EventHandler? DiscardNowClicked;
    public event EventHandler? ConfigSaved;

    public ConfigWindow(IDalamudPluginInterface pluginInterface, Configuration configuration, ItemCache itemCache,
        IListManager listManager, IClientState clientState, ICondition condition)
        : base("Auto Discard###AutoDiscardConfig")
    {
        _pluginInterface = pluginInterface;
        _configuration = configuration;
        _itemCache = itemCache;
        _listManager = listManager;
        _clientState = clientState;
        _condition = condition;

        Size = new Vector2(600, 400);
        SizeCondition = ImGuiCond.FirstUseEver;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 400),
            MaximumSize = new Vector2(9999, 9999),
        };

        _excludedListTab = new ExcludedListTab(this, itemCache, _configuration.BlacklistedItems, listManager);
        _discardListTab = new DiscardListTab(this, itemCache, _configuration.DiscardingItems)
        {
            ExcludedTab = _excludedListTab,
        };
    }

    public override void DrawContent()
    {
        bool runAfterVenture = _configuration.RunAfterVenture;
        if (ImGui.Checkbox("[Global] Run automatically after AutoRetainer's venture", ref runAfterVenture))
        {
            _configuration.RunAfterVenture = runAfterVenture;
            Save();
        }

        ImGui.SameLine(ImGui.GetContentRegionAvail().X +
                       ImGui.GetStyle().WindowPadding.X -
                       ImGui.CalcTextSize("Preview Discards").X -
                       ImGui.GetStyle().ItemSpacing.X);
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
            DrawExcludedItems();
            DrawExperimentalSettings();

            ImGui.EndTabBar();
        }
    }

    private void DrawDiscardList()
    {
        if (ImGui.BeginTabItem("Items to Discard"))
        {
            _discardListTab.Draw();
            ImGui.EndTabItem();
        }
    }

    private void DrawExcludedCharacters()
    {
        if (ImGui.BeginTabItem("Excluded Characters"))
        {
            if (_clientState is { IsLoggedIn: true, LocalContentId: > 0 })
            {
                string worldName = _clientState.LocalPlayer?.HomeWorld.ValueNullable?.Name .ToString() ?? "??";
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

    private void DrawExcludedItems()
    {
        if (ImGui.BeginTabItem("Excluded Items"))
        {
            ImGui.Text(
                "Items configured here will never be discarded, and have priority over the 'Items to Discard' tab.");
            ImGui.Text("Some items (such as Ultimate weapons) can not be un-blacklisted.");

            _excludedListTab.Draw();
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

            bool mainHandOffHand = _configuration.Armoury.CheckMainHandOffHand;
            if (ImGui.Checkbox("Discard when items are found in Main Hand/Off Hand (Weapons and Tools)",
                    ref mainHandOffHand))
            {
                _configuration.Armoury.CheckMainHandOffHand = mainHandOffHand;
                Save();
            }

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
                _configuration.Armoury.MaximumGearItemLevel =
                    Math.Max(0, Math.Min(_itemCache.MaxDungeonItemLevel, maximumItemLevel));
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

            bool ignoreItemWithSignature = _configuration.IgnoreItemWithSignature;
            if (ImGui.Checkbox(
                    "Ignore items with a 'crafted by' signature (manually crafted by yourself or someone else)",
                    ref ignoreItemWithSignature))
            {
                _configuration.IgnoreItemWithSignature = ignoreItemWithSignature;
                Save();
            }

            ImGui.Separator();

            bool groupPreviewByCategory = _configuration.Preview.GroupByCategory;
            if (ImGui.Checkbox("Group items in 'Preview' by category", ref groupPreviewByCategory))
            {
                _configuration.Preview.GroupByCategory = groupPreviewByCategory;
                Save();
            }

            bool showIconsInPreview = _configuration.Preview.ShowIcons;
            if (ImGui.Checkbox("Show icons in 'Preview'", ref showIconsInPreview))
            {
                _configuration.Preview.ShowIcons = showIconsInPreview;
                Save();
            }

            ImGui.EndTabItem();
        }
    }

    internal List<(uint ItemId, string Name)> EnsureAllItemsLoaded()
    {
        if (_allItems == null)
        {
            _allItems = _itemCache.AllItems
                .Where(x => x.CanBeDiscarded(_listManager, false))
                .Select(x => (x.ItemId, x.Name.ToString()))
                .ToList();
        }

        return _allItems;
    }

    internal void Save()
    {
        _configuration.DiscardingItems = _discardListTab.ToSavedItems().ToList();
        _configuration.BlacklistedItems = _excludedListTab.ToSavedItems().ToList();
        _pluginInterface.SavePluginConfig(_configuration);

        ConfigSaved?.Invoke(this, EventArgs.Empty);
    }

    internal bool AddToDiscardList(uint itemId) => _discardListTab.AddToDiscardList(itemId);

    internal bool RemoveFromDiscardList(uint itemId) => _discardListTab.RemoveFromDiscardList(itemId);

    public bool CanItemBeConfigured(uint itemId)
    {
        return EnsureAllItemsLoaded().SingleOrDefault(x => x.ItemId == itemId).ItemId == itemId;
    }
}
