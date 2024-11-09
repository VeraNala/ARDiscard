using System.Collections.Generic;
using Dalamud.Configuration;

namespace ARDiscard;

internal sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 3;
    public bool RunAfterVenture { get; set; }
    public bool RunBeforeLogout { get; set; }
    public List<uint> DiscardingItems { get; set; } = new();
    public List<uint> BlacklistedItems { get; set; } = new();
    public List<CharacterInfo> ExcludedCharacters { get; set; } = new();

    public ArmouryConfiguration Armoury { get; set; } = new();
    public ContextMenuConfiguration ContextMenu { get; set; } = new();
    public PreviewConfiguration Preview { get; set; } = new();
    public uint IgnoreItemCountWhenAbove { get; set; } = 50;
    public bool IgnoreItemWithSignature { get; set; }

    public sealed class CharacterInfo
    {
        public ulong LocalContentId { get; set; }
        public string? CachedPlayerName { get; set; }
        public string? CachedWorldName { get; set; }
    }

    public sealed class ArmouryConfiguration
    {
        public bool DiscardFromArmouryChest { get; set; }
        public bool CheckMainHandOffHand { get; set; }
        public bool CheckLeftSideGear { get; set; }
        public bool CheckRightSideGear { get; set; }
        public int MaximumGearItemLevel { get; set; } = 45;
    }

    public sealed class ContextMenuConfiguration
    {
        public bool Enabled { get; set; } = true;
        public bool OnlyWhenConfigIsOpen { get; set; }
    }

    public sealed class PreviewConfiguration
    {
        public bool GroupByCategory { get; set; } = true;
        public bool ShowIcons { get; set; } = true;
    }

    public static Configuration CreateNew()
    {
        return new Configuration
        {
            BlacklistedItems = [2820]
        };
    }
}
