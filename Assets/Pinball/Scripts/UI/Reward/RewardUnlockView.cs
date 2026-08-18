using Pinball.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class RewardUnlockView : UIViewBase
    {
        public enum ERewardKind
        {
            Flipper = 0,
            Passive = 1,
            Upgrade = 2,
        }

        public record Entry(ERewardKind Kind, Sprite Icon, string DisplayName, string Description);

        public record Args(Entry[] Entries) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.RewardUnlock;
        }

        const string FLIPPER_KIND_TEXT = "FLIPPER";
        const string FLIPPER_KIND_KEY = "ui.reward.kind_flipper";
        const string PASSIVE_KIND_TEXT = "PASSIVE";
        const string PASSIVE_KIND_KEY = "ui.reward.kind_passive";
        const string UPGRADE_KIND_TEXT = "UPGRADE";
        const string UPGRADE_KIND_KEY = "ui.reward.kind_upgrade";

        [SerializeField] RewardTileView[] _tiles = null;
        [SerializeField] ItemTooltipView _tooltip = null;
        [SerializeField] Button _okButton = null;

        Entry[] _boundEntries = null;

        public override void OnGenerate()
        {
            _boundEntries = new Entry[_tiles.Length];
            _okButton.onClick.AddListener(OnOkClicked);
            _tooltip.Hide();
        }

        public override void OnTerminate()
        {
            _okButton.onClick.RemoveListener(OnOkClicked);
            _boundEntries = null;
        }

        public override void OnReturn()
        {
            _tooltip.Hide();
        }

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            _tooltip.Hide();

            int entryCount = null == viewArgs.Entries ? 0 : viewArgs.Entries.Length;
            for (int i = 0; i < _tiles.Length; ++i)
            {
                Entry entry = i < entryCount ? viewArgs.Entries[i] : null;
                if (null == entry)
                    HideTile(i);
                else
                    BindTile(i, entry);
            }
        }

        void BindTile(int tileIndex, Entry entry)
        {
            _boundEntries[tileIndex] = entry;
            _tiles[tileIndex].Bind(new RewardTileView.BindParam(
                TileIndex: tileIndex,
                Icon: entry.Icon,
                DisplayName: entry.DisplayName,
                KindText: GetKindText(entry.Kind),
                HoverCallback: OnTooltipHovered));
        }

        void HideTile(int tileIndex)
        {
            _boundEntries[tileIndex] = null;
            _tiles[tileIndex].Hide();
        }

        static string GetKindText(ERewardKind kind)
        {
            switch (kind)
            {
                case ERewardKind.Flipper:
                    return LocalizationTable.Get(FLIPPER_KIND_KEY, FLIPPER_KIND_TEXT);
                case ERewardKind.Passive:
                    return LocalizationTable.Get(PASSIVE_KIND_KEY, PASSIVE_KIND_TEXT);
                default:
                    return LocalizationTable.Get(UPGRADE_KIND_KEY, UPGRADE_KIND_TEXT);
            }
        }

        void OnTooltipHovered(int tileIndex, Vector3 anchorPosition, bool isEntered)
        {
            Entry entry = GetBoundEntry(tileIndex);
            if (!isEntered || null == entry || string.IsNullOrEmpty(entry.Description))
            {
                _tooltip.Hide();
                return;
            }

            _tooltip.Show(entry.DisplayName, entry.Description, anchorPosition);
        }

        Entry GetBoundEntry(int tileIndex)
        {
            if (null == _boundEntries)
                return null;

            if (tileIndex < 0 || tileIndex >= _boundEntries.Length)
                return null;

            return _boundEntries[tileIndex];
        }

        void OnOkClicked()
        {
            UIManager.Hide(UIViewCatalogData.EViewType.RewardUnlock);
        }
    }
}
