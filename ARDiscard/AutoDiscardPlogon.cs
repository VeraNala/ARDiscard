using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using AutoRetainerAPI;
using ClickLib.Clicks;
using Dalamud.Data;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Memory;
using Dalamud.Plugin;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ARDiscard;

public class AutoDiscardPlogon : IDalamudPlugin
{
    private readonly WindowSystem _windowSystem = new(nameof(AutoDiscardPlogon));
    private readonly Configuration _configuration;
    private readonly ConfigWindow _configWindow;

    private readonly DalamudPluginInterface _pluginInterface;
    private readonly ChatGui _chatGui;
    private readonly CommandManager _commandManager;
    private readonly InventoryUtils _inventoryUtils;
    private readonly AutoRetainerApi _autoRetainerApi;
    private readonly TaskManager _taskManager;

    public AutoDiscardPlogon(DalamudPluginInterface pluginInterface, CommandManager commandManager, ChatGui chatGui,
        DataManager dataManager)
    {
        _pluginInterface = pluginInterface;
        _configuration = (Configuration?)_pluginInterface.GetPluginConfig() ?? new Configuration();
        _chatGui = chatGui;
        _commandManager = commandManager;
        _commandManager.AddHandler("/discardconfig", new CommandInfo(OpenConfig));
        _commandManager.AddHandler("/discardall", new CommandInfo(ProcessCommand));
        _inventoryUtils = new(_configuration);

        _pluginInterface.UiBuilder.Draw += _windowSystem.Draw;
        _pluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
        _configWindow = new(_pluginInterface, _configuration, dataManager);
        _windowSystem.AddWindow(_configWindow);

        ECommonsMain.Init(_pluginInterface, this);
        _autoRetainerApi = new();
        _taskManager = new();

        _autoRetainerApi.OnRetainerReadyToPostprocess += DoPostProcess;
        _autoRetainerApi.OnRetainerPostprocessStep += CheckPostProcess;
    }

    public string Name => "Discard after AutoRetainer";

    private unsafe void CheckPostProcess(string retainerName)
    {
        if (_configuration.RunAfterVenture && _inventoryUtils.GetNextItemToDiscard() != null)
            _autoRetainerApi.RequestPostprocess();
    }

    private void DoPostProcess(string retainerName)
    {
        _taskManager.Enqueue(() => DiscardNextItem(true));
    }

    private void OpenConfig(string command, string arguments) => OpenConfigUi();

    private void OpenConfigUi()
    {
        _configWindow.IsOpen = true;
    }

    private void ProcessCommand(string command, string arguments)
    {
        _taskManager.Enqueue(() => DiscardNextItem(false));
    }

    private unsafe void DiscardNextItem(bool finishRetainerAction)
    {
        PluginLog.Information($"DiscardNextItem (retainer = {finishRetainerAction})");
        InventoryItem* nextItem = _inventoryUtils.GetNextItemToDiscard();
        if (nextItem == null)
        {
            PluginLog.Information($"No item to discard found");
            if (finishRetainerAction)
                _autoRetainerApi.FinishPostProcess();
            else
                _chatGui.Print("Done discarding.");
        }
        else
        {
            var (inventoryType, slot) = (nextItem->Container, nextItem->Slot);

            PluginLog.Information(
                $"Discarding itemId {nextItem->ItemID} in slot {nextItem->Slot} of container {nextItem->Container}.");
            _inventoryUtils.Discard(nextItem);

            _taskManager.DelayNext(20);
            _taskManager.Enqueue(() => ConfirmDiscardItem(finishRetainerAction, inventoryType, slot));
        }
    }

    private unsafe void ConfirmDiscardItem(bool finishRetainerAction, InventoryType inventoryType, short slot)
    {
        var addon = GetDiscardAddon();
        if (addon != null)
        {
            PluginLog.Information("Addon is visible, clicking 'yes'");
            ((AddonSelectYesno*)addon)->YesButton->AtkComponentBase.SetEnabledState(true);
            ClickSelectYesNo.Using((nint)addon).Yes();

            _taskManager.DelayNext(20);
            _taskManager.Enqueue(() => ContinueAfterDiscard(finishRetainerAction, inventoryType, slot));
        }
        else
        {
            InventoryItem* nextItem = _inventoryUtils.GetNextItemToDiscard();
            if (nextItem == null)
            {
                PluginLog.Information("Addon is not visible, but next item is also no longer set");
                if (finishRetainerAction)
                    _autoRetainerApi.FinishPostProcess();
                else
                    _chatGui.Print("Done discarding.");
            }
            else if (nextItem->Container == inventoryType && nextItem->Slot == slot)
            {
                PluginLog.Information(
                    $"Addon is not (yet) visible, still trying to discard item in slot {slot} in inventory {inventoryType}");
                _taskManager.DelayNext(100);
                _taskManager.Enqueue(() => ConfirmDiscardItem(finishRetainerAction, inventoryType, slot));
            }
            else
            {
                PluginLog.Information(
                    $"Addon is not (yet) visible, but slot or inventory type changed, retrying from start");
                _taskManager.DelayNext(100);
                _taskManager.Enqueue(() => DiscardNextItem(finishRetainerAction));
            }
        }
    }

    private unsafe void ContinueAfterDiscard(bool finishRetainerAction, InventoryType inventoryType, short slot)
    {
        InventoryItem* nextItem = _inventoryUtils.GetNextItemToDiscard();
        if (nextItem == null)
        {
            PluginLog.Information($"Continuing after discard: no next item (retainer = {finishRetainerAction})");
            if (finishRetainerAction)
                _autoRetainerApi.FinishPostProcess();
            else
                _chatGui.Print("Done discarding.");
        }
        else if (nextItem->Container == inventoryType && nextItem->Slot == slot)
        {
            PluginLog.Information($"ContinueAfterDiscard: Waiting for server response");
            _taskManager.DelayNext(20);
            _taskManager.Enqueue(() => ContinueAfterDiscard(finishRetainerAction, inventoryType, slot));
        }
        else
        {
            PluginLog.Information($"ContinueAfterDiscard: Discovered different item to discard");
            _taskManager.EnqueueImmediate(() => DiscardNextItem(finishRetainerAction));
        }
    }

    public void Dispose()
    {
        _autoRetainerApi.Dispose();
        ECommonsMain.Dispose();

        _inventoryUtils.Dispose();
        _pluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        _pluginInterface.UiBuilder.Draw -= _windowSystem.Draw;
        _commandManager.RemoveHandler("/discardall");
        _commandManager.RemoveHandler("/discardconfig");
    }

    private static unsafe AtkUnitBase* GetDiscardAddon()
    {
        for (int i = 1; i < 100; i++)
        {
            try
            {
                var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("SelectYesno", i);
                if (addon == null) return null;
                if (addon->IsVisible && addon->UldManager.LoadedState == AtkLoadState.Loaded)
                {
                    var textNode = addon->UldManager.NodeList[15]->GetAsAtkTextNode();
                    var text = MemoryHelper.ReadSeString(&textNode->NodeText).ExtractText();
                    PluginLog.Information($"YesNo prompt: {text}");
                    if (text.StartsWith("Discard"))
                    {
                        return addon;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        return null;
    }
}
