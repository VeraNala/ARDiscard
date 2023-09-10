using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ECommons;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace ARDiscard;

public class ConfigWindow : Window
{
    private readonly DalamudPluginInterface _pluginInterface;
    private readonly Configuration _configuration;
    private readonly DataManager _dataManager;
    private readonly ClientState _clientState;
    private string _itemName = string.Empty;

    private List<(uint ItemId, string Name)> _searchResults = new();
    private List<(uint ItemId, string Name)> _discarding = new();
    private List<(uint ItemId, string Name)>? _allItems = null;
    private bool _resetKeyboardFocus = true;

    public ConfigWindow(DalamudPluginInterface pluginInterface, Configuration configuration, DataManager dataManager,
        ClientState clientState)
        : base("Auto Discard###AutoDiscardConfig")
    {
        _pluginInterface = pluginInterface;
        _configuration = configuration;
        _dataManager = dataManager;
        _clientState = clientState;

        Size = new Vector2(600, 400);
        SizeCondition = ImGuiCond.FirstUseEver;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 400),
            MaximumSize = new Vector2(9999, 9999),
        };

        _discarding.AddRange(_configuration.DiscardingItems
            .Select(x => (x, dataManager.GetExcelSheet<Item>()?.GetRow(x)?.Name?.ToString() ?? x.ToString())).ToList());
    }

    public override void Draw()
    {
        bool runAfterVenture = _configuration.RunAfterVenture;
        if (ImGui.Checkbox("[Global] Run automatically after AutoRetainer's venture", ref runAfterVenture))
        {
            _configuration.RunAfterVenture = runAfterVenture;
            Save();
        }

        if (ImGui.BeginTabBar("AutoDiscardTabs"))
        {
            DrawDiscardList();
            DrawExcludedCharacters();

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
            if (_clientState.IsLoggedIn && _clientState.LocalContentId > 0)
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
                    if (_configuration.RunAfterVenture)
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

    private void UpdateResults()
    {
        if (string.IsNullOrEmpty(_itemName))
            _searchResults = new();
        else
        {
            if (_allItems == null)
            {
                _allItems = _dataManager.GetExcelSheet<Item>()!
                    .Where(x => x.RowId != 0)
                    .Where(x => !x.IsUnique && !x.IsUntradable)
                    .Where(x => x.ItemUICategory?.Value?.Name?.ToString() != "Currency" &&
                                x.ItemUICategory?.Value?.Name?.ToString() != "Crystal")
                    .Where(x => !string.IsNullOrEmpty(x.Name.ToString()))
                    .Select(x => (x.RowId, x.Name.ToString()))
                    .ToList();
            }

            _searchResults = _allItems.Where(x => x.Name.Contains(_itemName, StringComparison.CurrentCultureIgnoreCase)
                                                  || (uint.TryParse(_itemName, out uint itemId) && x.ItemId == itemId))
                .OrderBy(x => _itemName.EqualsIgnoreCase(x.Name) ? string.Empty : x.Name)
                .ToList();
        }
    }

    private void Save()
    {
        _configuration.DiscardingItems = _discarding.Select(x => x.ItemId).ToList();
        _pluginInterface.SavePluginConfig(_configuration);
    }
}
