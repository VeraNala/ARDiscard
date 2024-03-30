using System.Collections.Generic;
using System.Linq;
using ARDiscard.GameData;

namespace ARDiscard.Windows
{
    internal sealed class DiscardListTab : ItemListTab
    {
        public DiscardListTab(ConfigWindow parent, ItemCache itemCache, List<uint> initialItems)
            : base(parent, itemCache, initialItems)
        {
        }

        protected override string RightSideLabel => "Items that will be automatically discarded";
        internal required ExcludedListTab ExcludedTab { private get; init; }

        public override IEnumerable<uint> ToSavedItems()
        {
            SelectedItems.RemoveAll(x => ExcludedTab.IsBlacklistedInConfiguration(x.ItemId));
            return SelectedItems.Select(x => x.ItemId);
        }

        protected override (string, bool) AsLeftSideDisplay(uint itemId, string name)
        {
            if (ExcludedTab.IsBlacklistedInConfiguration(itemId))
                return ($"{name} (Blacklisted/Excluded Item)", false);

            return base.AsLeftSideDisplay(itemId, name);
        }

        internal bool AddToDiscardList(uint itemId)
        {
            var item = EnsureAllItemsLoaded().SingleOrDefault(x => x.ItemId == itemId);
            if (item.ItemId != 0)
            {
                SelectedItems.Add(item);
                Save();
                return true;
            }

            return false;
        }

        internal bool RemoveFromDiscardList(uint itemId)
        {
            if (SelectedItems.RemoveAll(x => x.ItemId == itemId) > 0)
            {
                Save();
                return true;
            }

            return false;
        }
    }
}
