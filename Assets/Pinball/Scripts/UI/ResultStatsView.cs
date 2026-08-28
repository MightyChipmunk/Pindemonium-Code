using System.Collections.Generic;
using System.Globalization;
using Pinball.Ingame.Shop;
using Pinball.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class ResultStatsView : UIViewBase
    {
        public record ItemEntry(Sprite Icon, int Count, LocalizedTextRef Title, LocalizedTextRef Description, ShopItemData.EPassiveCategory Category);

        public record Args(
            int StageNumber,
            bool IsEndless,
            int Score,
            int Money,
            int UsedBallCount,
            ItemEntry[] Passives,
            ItemEntry[] Upgrades,
            GimmickScoreView.Args GimmickScores) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.ResultStats;
        }

        const string NUMBER_FORMAT = "N0";
        const string COUNT_FORMAT = "x{0}";
        const string ENDLESS_STAGE_FORMAT = "ENDLESS {0}";
        const string ENDLESS_STAGE_KEY = "ui.common.endless_number";
        const int STACKED_COUNT_MIN = 2;
        const int EMPTY_PLACEHOLDER_COUNT = 1;
        const float PASSIVE_SLOT_WIDTH = 112f;
        const float PASSIVE_SLOT_SPACING = 12f;
        const float PASSIVE_GROUP_MAX_WIDTH = 300f;
        const float PASSIVE_GROUP_MAX_SCALE = 1f;

        [SerializeField] TMP_Text _stageLabel = null;
        [SerializeField] TMP_Text _scoreLabel = null;
        [SerializeField] TMP_Text _moneyLabel = null;
        [SerializeField] TMP_Text _ballLabel = null;
        [SerializeField] ItemIconGridView[] _passiveGrids = null;
        [SerializeField] ItemIconGridView _upgradeGrid = null;
        [SerializeField] UIScrollView _upgradeScroll = null;
        [SerializeField] Button _gimmickScoreButton = null;
        [SerializeField] Button _closeButton = null;

        Args _args = null;

        public override void OnGenerate()
        {
            for (int i = 0; i < _passiveGrids.Length; ++i)
                _passiveGrids[i].Init(null);

            _upgradeGrid.Init(null);
            _gimmickScoreButton.onClick.AddListener(OnGimmickScoreClicked);
            _closeButton.onClick.AddListener(OnCloseClicked);
            UITooltip.Hide();
        }

        public override void OnTerminate()
        {
            _gimmickScoreButton.onClick.RemoveListener(OnGimmickScoreClicked);
            _closeButton.onClick.RemoveListener(OnCloseClicked);
            for (int i = 0; i < _passiveGrids.Length; ++i)
                _passiveGrids[i].Release();

            _upgradeGrid.Release();
        }

        public override void OnReturn()
        {
            for (int i = 0; i < _passiveGrids.Length; ++i)
                _passiveGrids[i].Clear();

            _upgradeGrid.Clear();
            _args = null;
            UITooltip.Hide();
        }

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            _args = viewArgs;
            _stageLabel.text = viewArgs.IsEndless
                ? LocalizationTable.Format(ENDLESS_STAGE_KEY, ENDLESS_STAGE_FORMAT, viewArgs.StageNumber)
                : viewArgs.StageNumber.ToString(CultureInfo.InvariantCulture);
            _scoreLabel.text = viewArgs.Score.ToString(NUMBER_FORMAT, CultureInfo.InvariantCulture);
            _moneyLabel.text = viewArgs.Money.ToString(CultureInfo.InvariantCulture);
            _ballLabel.text = viewArgs.UsedBallCount.ToString(CultureInfo.InvariantCulture);
            UITooltip.Hide();
            BindPassiveGrids(viewArgs.Passives);
            BindGrid(_upgradeGrid, viewArgs.Upgrades);
            _upgradeScroll.Refresh();
            _upgradeScroll.ResetPosition();
        }

        void BindPassiveGrids(ItemEntry[] items)
        {
            for (int category = 0; category < _passiveGrids.Length; ++category)
                BindPassiveCategory(category, items);
        }

        void BindPassiveCategory(int category, ItemEntry[] items)
        {
            List<ItemIconGridView.Entry> entries = new List<ItemIconGridView.Entry>();
            int itemCount = null == items ? 0 : items.Length;
            for (int i = 0; i < itemCount; ++i)
            {
                ItemEntry item = items[i];
                if (null == item || (int)item.Category != category)
                    continue;

                entries.Add(new ItemIconGridView.Entry(item.Icon, GetCountText(item.Count), item.Title, item.Description));
            }

            int emptyCount = entries.Count <= 0 ? EMPTY_PLACEHOLDER_COUNT : 0;
            _passiveGrids[category].Bind(entries.ToArray(), emptyCount);
            SlotRowLayout.FitWidth(
                _passiveGrids[category].GetContentRect(),
                Mathf.Max(entries.Count, EMPTY_PLACEHOLDER_COUNT),
                PASSIVE_SLOT_WIDTH,
                PASSIVE_SLOT_SPACING,
                PASSIVE_GROUP_MAX_WIDTH,
                PASSIVE_GROUP_MAX_SCALE);
        }

        static void BindGrid(ItemIconGridView grid, ItemEntry[] items)
        {
            ItemIconGridView.Entry[] entries = BuildGridEntries(items);
            int emptyCount = null == entries || entries.Length <= 0 ? EMPTY_PLACEHOLDER_COUNT : 0;
            grid.Bind(entries, emptyCount);
        }

        static ItemIconGridView.Entry[] BuildGridEntries(ItemEntry[] items)
        {
            if (null == items)
                return null;

            ItemIconGridView.Entry[] entries = new ItemIconGridView.Entry[items.Length];
            for (int i = 0; i < items.Length; ++i)
            {
                ItemEntry item = items[i];
                if (null == item)
                    continue;

                entries[i] = new ItemIconGridView.Entry(item.Icon, GetCountText(item.Count), item.Title, item.Description);
            }

            return entries;
        }

        static string GetCountText(int count)
        {
            if (count < STACKED_COUNT_MIN)
                return string.Empty;

            return string.Format(COUNT_FORMAT, count);
        }

        void OnGimmickScoreClicked()
        {
            if (null == _args || null == _args.GimmickScores)
                return;

            UIManager.Show(_args.GimmickScores);
        }

        void OnCloseClicked()
        {
            UIManager.Hide(UIViewCatalogData.EViewType.GimmickScore);
            UIManager.Hide(UIViewCatalogData.EViewType.ResultStats);
        }
    }
}
