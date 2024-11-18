using System.Data;
using System.Text.RegularExpressions;
using Dalamud.Plugin.Services;
using LLib;
using Lumina.Excel.Sheets;

namespace ARDiscard.GameData;

internal sealed class GameStrings
{
    public GameStrings(IDataManager dataManager, IPluginLog pluginLog)
    {
        DiscardItem = dataManager.GetRegex<Addon>(110, addon => addon.Text, pluginLog)
                      ?? throw new ConstraintException($"Unable to resolve {nameof(DiscardItem)}");
        DiscardCollectable = dataManager.GetRegex<Addon>(153, addon => addon.Text, pluginLog)
                             ?? throw new ConstraintException($"Unable to resolve {nameof(DiscardCollectable)}");
    }

    public Regex DiscardItem { get; }
    public Regex DiscardCollectable { get; }
}
