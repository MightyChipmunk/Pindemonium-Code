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
        [SerializeField] Button _okButton = null;

        public override void OnGenerate()
        {
            _okButton.onClick.AddListener(OnOkClicked);
        }

        public override void OnTerminate()
        {
            _okButton.onClick.RemoveListener(OnOkClicked);
        }

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            int entryCount = null == viewArgs.Entries ? 0 : viewArgs.Entries.Length;
            for (int i = 0; i < _tiles.Length; ++i)
            {
                if (i >= entryCount || !TryBuildTileParam(viewArgs.Entries[i], out RewardTileView.BindParam param))
                {
                    _tiles[i].Hide();
                    continue;
                }

                _tiles[i].Bind(param);
            }
        }

        bool TryBuildTileParam(Entry entry, out RewardTileView.BindParam param)
        {
            param = null;
            if (null == entry)
                return false;

            if (entry.Kind == ERewardKind.Flipper)
                return TryBuildFlipperParam(entry.CatalogIndex, out param);

            return TryBuildItemParam(entry.CatalogIndex, out param);
        }

        bool TryBuildFlipperParam(int index, out RewardTileView.BindParam param)
        {
            param = null;
            FlipperData flipper = null == _flipperCatalog ? null : _flipperCatalog.GetFlipper(index);
            if (null == flipper)
                return false;

            param = new RewardTileView.BindParam(flipper.GetIcon(), flipper.GetDisplayName(), LocalizationTable.Get(FLIPPER_KIND_KEY, FLIPPER_KIND_TEXT));
            return true;
        }

        bool TryBuildItemParam(int index, out RewardTileView.BindParam param)
        {
            param = null;
            ShopItemData item = null == _shopCatalog ? null : _shopCatalog.GetItem(index);
            if (null == item)
                return false;

            string kindText = item.Kind == ShopItemData.EItemKind.Passive ? LocalizationTable.Get(PASSIVE_KIND_KEY, PASSIVE_KIND_TEXT) : LocalizationTable.Get(UPGRADE_KIND_KEY, UPGRADE_KIND_TEXT);
            param = new RewardTileView.BindParam(item.GetIcon(), item.GetDisplayName(), kindText);
            return true;
        }

        void OnOkClicked()
        {
            UIManager.Hide(UIViewCatalogData.EViewType.RewardUnlock);
        }
    }
}
