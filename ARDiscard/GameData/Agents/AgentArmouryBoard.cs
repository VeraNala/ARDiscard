using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace ARDiscard.GameData.Agents;

[Agent(AgentId.ArmouryBoard)]
[StructLayout(LayoutKind.Explicit, Size = 0x2E)]
public struct AgentArmouryBoard
{
    [FieldOffset(0x2C)] public byte CurrentTab;
}
