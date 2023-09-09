using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Data;
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
    private string _itemName = string.Empty;

    private List<(uint ItemId, string Name)> _searchResults = new();
    private List<(uint ItemId, string Name)> _discarding = new();

    public ConfigWindow(DalamudPluginInterface pluginInterface, Configuration configuration, DataManager dataManager)
        : base("Auto Discard###AutoDiscardConfig")
    {
        _pluginInterface = pluginInterface;
        _configuration = configuration;
        _dataManager = dataManager;

        Size = new Vector2(600, 400);
        SizeCondition = ImGuiCond.FirstUseEver;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 400),
            MaximumSize = new Vector2(9999, 9999),
        };

        _discarding = _configuration.DiscardingItems
            .Select(x => (x, dataManager.GetExcelSheet<Item>()?.GetRow(x)?.Name?.ToString() ?? x.ToString())).ToList();
    }

    public override void Draw()
    {
        var ws = ImGui.GetWindowSize();
        if (ImGui.BeginChild("Left", new Vector2(Math.Max(10, ws.X / 2), -1), true))
        {
            ImGui.Text("Search");
            ImGui.SetNextItemWidth(ws.X / 2 - 20);
            if (ImGui.InputText("", ref _itemName, 256))
                UpdateResults();

            ImGui.Separator();

            if (_searchResults.Count == 0)
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

                    _configuration.DiscardingItems = _discarding.Select(x => x.ItemId).ToList();
                    _pluginInterface.SavePluginConfig(_configuration);
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

                _configuration.DiscardingItems = _discarding.Select(x => x.ItemId).ToList();
                _pluginInterface.SavePluginConfig(_configuration);
            }
        }

        ImGui.EndChild();
    }

    private void UpdateResults()
    {
        if (string.IsNullOrEmpty(_itemName))
            _searchResults = new();
        else
        {
            _searchResults = _dataManager.GetExcelSheet<Item>()
                .Where(x => x.RowId != 0)
                .Where(x => !x.IsUnique && !x.IsUntradable)
                .Where(x => x.ItemUICategory?.Value?.Name?.ToString() != "Currency")
                .Where(x => !string.IsNullOrEmpty(x.Name) &&
                            x.Name.ToString().Contains(_itemName, StringComparison.CurrentCultureIgnoreCase))
                .Select(x => (x.RowId, x.Name.ToString()))
                .ToList();
        }
    }
}
