using System.Collections.Generic;
using Dalamud.Configuration;

namespace ARDiscard;

internal sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public bool RunAfterVenture { get; set; }
    public bool RunBeforeLogout { get; set; }
    public List<uint> DiscardingItems { get; set; } = new();
    public List<CharacterInfo> ExcludedCharacters { get; set; } = new();

    public ArmouryConfiguration Armoury { get; set; } = new();
    public ContextMenuConfiguration ContextMenu { get; set; } = new();
    public PreviewConfiguration Preview { get; set; } = new();
    public uint IgnoreItemCountWhenAbove { get; set; } = 50;

    public sealed class CharacterInfo
    {
        public ulong LocalContentId { get; set; }
        public string? CachedPlayerName { get; set; }
        public string? CachedWorldName { get; set; }
    }

    public sealed class ArmouryConfiguration
    {
        public bool DiscardFromArmouryChest { get; set; } = false;
        public bool CheckLeftSideGear { get; set; } = false;
        public bool CheckRightSideGear { get; set; } = false;
        public int MaximumGearItemLevel { get; set; } = 45;
    }

    public sealed class ContextMenuConfiguration
    {
        public bool Enabled { get; set; } = false;
        public bool OnlyWhenConfigIsOpen { get; set; } = true;
    }

    public sealed class PreviewConfiguration
    {
        public bool GroupByCategory { get; set; } = true;
        public bool ShowIcons { get; set; } = true;
    }
}
