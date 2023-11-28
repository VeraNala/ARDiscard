using System.Collections.Generic;
using System.Linq;

namespace ARDiscard.GameData;

internal static class InternalConfiguration
{
    /// <summary>
    /// Not all of these *can* be discarded, but we shouldn't attempt it either.
    /// </summary>
    public static readonly IReadOnlyList<uint> BlacklistedItems = new List<uint>
        {
            2820, // red onion helm

            16039, // ala mhigan earrings
            24589, // aetheryte earrings
            33648, // menphina's earrings

            21197, // UCOB token
            23175, // UWU token
            28633, // TEA token
            36810, // DSR token
            38951, // TOP token
        }
        .Concat(Enumerable.Range(1, 99).Select(x => (uint)x))
        .ToList()
        .AsReadOnly();

    /// <summary>
    /// Items that are unique/untradeable, but should still be possible to discard. This is moreso because
    /// 99% of the unique/untradeable items should NOT be selectable for discard, but these are OK.
    /// </summary>
    public static readonly IReadOnlyList<uint> WhitelistedItems = new List<uint>
    {
        2962, // Onion Doublet
        3279, // Onion Gaskins
        3743, // Onion Patterns

        9387, // Antique Helm
        9388, // Antique Mail
        9389, // Antique Gauntlets
        9390, // Antique Breeches
        9391, // Antique Sollerets

        #region Fate drops used in tribal quests

        7001, // Flamefang Choker (Amalj'aa)
        7002, // Proto Armor Identification Key (Sylphs)
        7003, // Kafre's Tusk (Sylphs)
        7797, // Titan Mythrilshield (Kobolds)
        7798, // Coral Armband (Sahagin)
        7802, // Titan Steelshield (Kobolds)
        7803, // Titan Electrumshield (Kobolds)
        7804, // Coral Band (Sahagin)
        7805, // Coral Necklace (Sahagin)

        #endregion

        #region Normal Raid trash (with no weekly lockout)

        // A1-A4
        12674, // Tarnished Gordian Lens
        12675, // Tarnished Gordian Shaft
        12676, // Tarnished Gordian Crank
        12677, // Tarnished Gordian Spring
        12678, // Tarnished Gordian Pedal
        12680, // Tarnished Gordian Bolt
        13581, // Precision Gordian Bolt
        13583, // Precision Gordian Lens
        13585, // Precision Gordian Spring
        13587, // Precision Gordian Shaft

        // A5-A8
        14301, // Tarnished Midan Lens
        14302, // Tarnished Midan Shaft
        14303, // Tarnished Midan Crank
        14304, // Tarnished Midan Spring
        14305, // Tarnished Midan Pedal
        14307, // Tarnished Midan Bolt

        // A9-A12
        16545, // Alexandrian Gear
        16546, // Tarnished Alexandrian Lens
        16547, // Tarnished Alexandrian Shaft
        16548, // Tarnished Alexandrian Crank
        16549, // Tarnished Alexandrian Spring
        16550, // Tarnished Alexandrian Pedal
        16552, // Tarnished Alexandrian Bolt

        // O1-O4
        19111, // Deltascape Lens
        19112, // Deltascape Shaft
        19113, // Deltascape Crank
        19114, // Deltascape Spring
        19115, // Deltascape Pedal
        19117, // Deltascape Bolt
        19122, // Deltascape Crystalloid

        // O5-O8
        21774, // Sigmascape Lens
        21775, // Sigmascape Shaft
        21776, // Sigmascape Crank
        21777, // Sigmascape Spring
        21778, // Sigmascape Pedal
        21780, // Sigmascape Bolt

        // O9-O12
        23963, // Alphascape Lens
        23964, // Alphascape Shaft
        23965, // Alphascape Crank
        23966, // Alphascape Spring
        23967, // Alphascape Pedal
        23969, // Alphascape Bolt

        // E1-E4
        27393, // Helm of Early Antiquity
        27394, // Armor of Early Antiquity
        27395, // Gauntlets of Early Antiquity
        27396, // Chausses of Early Antiquity
        27397, // Greaves of Early Antiquity
        27399, // Bangle of Early Antiquity

        // E5-E8
        29020, // Helm of Golden Antiquity
        29021, // Armor of Golden Antiquity
        29022, // Gauntlets of Golden Antiquity
        29023, // Chausses of Golden Antiquity
        29024, // Greaves of Golden Antiquity
        29026, // Bangle of Golden Antiquity

        // E9-E12
        32133, // Helm of Lost Antiquity
        32134, // Armor of Lost Antiquity
        32135, // Gauntlets of Lost Antiquity
        32136, // Chausses of Lost Antiquity
        32137, // Greaves of Lost Antiquity
        32139, // Bangle of Lost Antiquity

        // P1-P4
        35817, // Unsung Helm of Asphodelos
        35818, // Unsung Armor of Asphodelos
        35819, // Unsung Gauntlets of Asphodelos
        35820, // Unsung Chausses of Asphodelos
        35821, // Unsung Greaves of Asphodelos
        35822, // Unsung Ring of Asphodelos

        // P5-P8
        38375, // Unsung Helm of Abyssos
        38376, // Unsung Armor of Abyssos
        38377, // Unsung Gauntlets of Abyssos
        38378, // Unsung Chausses of Abyssos
        38379, // Unsung Greaves of Abyssos
        38380, // Unsung Ring of Abyssos
        38385, // Unsung Blade of Abyssos // TODO: Remove in 7.0 as it will no longer drop

        // P9-P12
        40297, // Unsung Helm of Anabaseios
        40298, // Unsung Armor of Anabaseios
        40299, // Unsung Gauntlets of Anabaseios
        40300, // Unsung Chausses of Anabaseios
        40301, // Unsung Greaves of Anabaseios
        40302, // Unsung Ring of Anabaseios
        // 40317, // Unsung Blade of Anabaseios // TODO: Add when the weekly restriction is removed

        #endregion
    }.AsReadOnly();
}
