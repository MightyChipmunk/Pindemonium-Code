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

        public record Entry(ERewardKind Kind, Sprite Icon, LocalizedTextRef Name, LocalizedTextRef Description);

        public record Args(Entry[] Entries) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.RewardUnlock;
        }

        const string FLIPPER_KIND_TEXT = "FLIPPER";
        const string FLIPPER_KIND_KEY = "ui.common.flipper";
        const string PASSIVE_KIND_TEXT = "PASSIVE";
        const string PASSIVE_KIND_KEY = "ui.common.passive";
        const string UPGRADE_KIND_TEXT = "UPGRADE";
        const string UPGRADE_KIND_KEY = "ui.common.upgrade";

        [SerializeField] RewardTileView[] _tiles = null;
        [SerializeField] Button _okButton = null;

        public override void OnGenerate()
        {
            _okButton.onClick.AddListener(OnOkClicked);
            UITooltip.Hide();
        }

        public override void OnTerminate()
        {
            _okButton.onClick.RemoveListener(OnOkClicked);
        }

        public override void OnReturn()
        {
            UITooltip.Hide();
        }

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            UITooltip.Hide();

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
            _tiles[tileIndex].Bind(new RewardTileView.BindParam(
                Icon: entry.Icon,
                DisplayName: entry.Name.Resolve(),
                KindText: GetKindText(entry.Kind),
                TooltipTitle: entry.Name,
                TooltipBody: entry.Description));
        }

        void HideTile(int tileIndex)
        {
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

        void OnOkClicked()
        {
            UIManager.Hide(UIViewCatalogData.EViewType.RewardUnlock);
        }
    }
}
