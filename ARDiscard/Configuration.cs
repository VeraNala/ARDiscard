using System.Collections.Generic;
using Dalamud.Configuration;

namespace ARDiscard;

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public bool RunAfterVenture { get; set; }
    public List<uint> DiscardingItems { get; set; } = new();
}
