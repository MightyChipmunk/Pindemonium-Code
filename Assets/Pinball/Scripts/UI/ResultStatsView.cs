using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class ResultStatsView : UIViewBase
    {
        public record ItemEntry(Sprite Icon, int Count, string TitleKey, string DescriptionKey);

        public record Args(
            int StageNumber,
            int Score,
            int Money,
            int UsedBallCount,
            ItemEntry[] Passives,
            ItemEntry[] Upgrades) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.ResultStats;
        }

        const string NUMBER_FORMAT = "N0";
        const string COUNT_FORMAT = "x{0}";
        const int STACKED_COUNT_MIN = 2;
        const int EMPTY_PLACEHOLDER_COUNT = 1;

        [SerializeField] TMP_Text _stageLabel = null;
        [SerializeField] TMP_Text _scoreLabel = null;
        [SerializeField] TMP_Text _moneyLabel = null;
        [SerializeField] TMP_Text _ballLabel = null;
        [SerializeField] ItemIconGridView _passiveGrid = null;
        [SerializeField] ItemIconGridView _upgradeGrid = null;
        [SerializeField] Button _closeButton = null;

        public override void OnGenerate()
        {
            _passiveGrid.Init(null);
            _upgradeGrid.Init(null);
            _closeButton.onClick.AddListener(OnCloseClicked);
            UITooltip.Hide();
        }

        public override void OnTerminate()
        {
            _closeButton.onClick.RemoveListener(OnCloseClicked);
            _passiveGrid.Release();
            _upgradeGrid.Release();
        }

        public override void OnReturn()
        {
            _passiveGrid.Clear();
            _upgradeGrid.Clear();
            UITooltip.Hide();
        }

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            _stageLabel.text = viewArgs.StageNumber.ToString(CultureInfo.InvariantCulture);
            _scoreLabel.text = viewArgs.Score.ToString(NUMBER_FORMAT, CultureInfo.InvariantCulture);
            _moneyLabel.text = viewArgs.Money.ToString(CultureInfo.InvariantCulture);
            _ballLabel.text = viewArgs.UsedBallCount.ToString(CultureInfo.InvariantCulture);
            UITooltip.Hide();
            BindGrid(_passiveGrid, viewArgs.Passives);
            BindGrid(_upgradeGrid, viewArgs.Upgrades);
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

                entries[i] = new ItemIconGridView.Entry(item.Icon, GetCountText(item.Count), item.TitleKey, item.DescriptionKey);
            }

            return entries;
        }

        static string GetCountText(int count)
        {
            if (count < STACKED_COUNT_MIN)
                return string.Empty;

            return string.Format(COUNT_FORMAT, count);
        }

        void OnCloseClicked()
        {
            UIManager.Hide(UIViewCatalogData.EViewType.ResultStats);
        }
    }
}
