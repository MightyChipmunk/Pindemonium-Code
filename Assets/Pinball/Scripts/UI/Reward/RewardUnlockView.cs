using Pinball.Ingame;
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
            ShopItem = 1,
        }

        public record Entry(ERewardKind Kind, int CatalogIndex);

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

        [SerializeField] FlipperCatalogData _flipperCatalog = null;
        [SerializeField] ShopCatalogData _shopCatalog = null;
        [SerializeField] RewardTileView[] _tiles = null;
        [SerializeField] ItemTooltipView _tooltip = null;
        [SerializeField] Button _okButton = null;

        string[] _tooltipTitles = null;
        string[] _tooltipBodies = null;

        public override void OnGenerate()
        {
            _tooltipTitles = new string[_tiles.Length];
            _tooltipBodies = new string[_tiles.Length];
            _okButton.onClick.AddListener(OnOkClicked);
            _tooltip.Hide();
        }

        public override void OnTerminate()
        {
            _okButton.onClick.RemoveListener(OnOkClicked);
            _tooltipTitles = null;
            _tooltipBodies = null;
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
                if (i >= entryCount || !TryBindTile(i, viewArgs.Entries[i]))
                    HideTile(i);
        }

        bool TryBindTile(int tileIndex, Entry entry)
        {
            if (null == entry)
                return false;

            if (entry.Kind == ERewardKind.Flipper)
                return TryBindFlipperTile(tileIndex, entry.CatalogIndex);

            return TryBindItemTile(tileIndex, entry.CatalogIndex);
        }

        bool TryBindFlipperTile(int tileIndex, int index)
        {
            FlipperData flipper = null == _flipperCatalog ? null : _flipperCatalog.GetFlipper(index);
            if (null == flipper)
                return false;

            BindTile(
                tileIndex,
                flipper.GetIcon(),
                flipper.GetDisplayName(),
                LocalizationTable.Get(FLIPPER_KIND_KEY, FLIPPER_KIND_TEXT),
                flipper.GetAbilityDescription());
            return true;
        }

        bool TryBindItemTile(int tileIndex, int index)
        {
            ShopItemData item = null == _shopCatalog ? null : _shopCatalog.GetItem(index);
            if (null == item)
                return false;

            string kindText = item.Kind == ShopItemData.EItemKind.Passive ? LocalizationTable.Get(PASSIVE_KIND_KEY, PASSIVE_KIND_TEXT) : LocalizationTable.Get(UPGRADE_KIND_KEY, UPGRADE_KIND_TEXT);
            BindTile(tileIndex, item.GetIcon(), item.GetDisplayName(), kindText, item.GetDescription());
            return true;
        }

        void BindTile(int tileIndex, Sprite icon, string displayName, string kindText, string description)
        {
            _tooltipTitles[tileIndex] = displayName;
            _tooltipBodies[tileIndex] = description;
            _tiles[tileIndex].Bind(new RewardTileView.BindParam(
                TileIndex: tileIndex,
                Icon: icon,
                DisplayName: displayName,
                KindText: kindText,
                HoverCallback: OnTooltipHovered));
        }

        void HideTile(int tileIndex)
        {
            _tooltipTitles[tileIndex] = null;
            _tooltipBodies[tileIndex] = null;
            _tiles[tileIndex].Hide();
        }

        void OnTooltipHovered(int tileIndex, Vector3 anchorPosition, bool isEntered)
        {
            if (!isEntered || tileIndex < 0 || tileIndex >= _tooltipBodies.Length)
            {
                _tooltip.Hide();
                return;
            }

            if (string.IsNullOrEmpty(_tooltipBodies[tileIndex]))
            {
                _tooltip.Hide();
                return;
            }

            _tooltip.Show(_tooltipTitles[tileIndex], _tooltipBodies[tileIndex], anchorPosition);
        }

        void OnOkClicked()
        {
            UIManager.Hide(UIViewCatalogData.EViewType.RewardUnlock);
        }
    }
}
