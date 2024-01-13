using System.Collections.Generic;
using System.Collections.Immutable;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace ARDiscard.External;

internal sealed class AutoDiscardIpc
{
    private const string ItemsToDiscard = "ARDiscard.GetItemsToDiscard";

    private readonly Configuration _configuration;
    private readonly ICallGateProvider<IReadOnlySet<uint>> _getItemsToDiscard;

    public AutoDiscardIpc(DalamudPluginInterface pluginInterface, Configuration configuration)
    {
        _configuration = configuration;

        _getItemsToDiscard = pluginInterface.GetIpcProvider<IReadOnlySet<uint>>(ItemsToDiscard);
        _getItemsToDiscard.RegisterFunc(GetItemsToDiscard);
    }

    public void Dispose()
    {
        _getItemsToDiscard.UnregisterFunc();
    }

    private IReadOnlySet<uint> GetItemsToDiscard()
    {
        return _configuration.DiscardingItems.ToImmutableHashSet();
    }
}
