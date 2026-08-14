using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class ResultStatsView : UIViewBase
    {
        public record ItemCountEntry(string Name, int Count);

        public record Args(
            int StageNumber,
            int Score,
            int Money,
            int UsedBallCount,
            string[] PassiveNames,
            ItemCountEntry[] Upgrades) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.ResultStats;
        }

        const string STAGE_FORMAT = "Stage : {0}";
        const string SCORE_FORMAT = "Score : {0}";
        const string MONEY_FORMAT = "Money : {0}";
        const string USED_BALL_FORMAT = "Balls Used : {0}";
        const string PASSIVE_FORMAT = "Passive : {0}";
        const string UPGRADE_FORMAT = "Upgrade : {0}";
        const string UPGRADE_COUNT_FORMAT = "{0} x{1}";
        const string ITEM_SEPARATOR = ", ";
        const string EMPTY_ITEMS_LABEL = "-";

        [SerializeField] TMP_Text _stageLabel = null;
        [SerializeField] TMP_Text _scoreLabel = null;
        [SerializeField] TMP_Text _moneyLabel = null;
        [SerializeField] TMP_Text _ballLabel = null;
        [SerializeField] TMP_Text _passiveLabel = null;
        [SerializeField] TMP_Text _upgradeLabel = null;
        [SerializeField] Button _closeButton = null;

        public override void OnGenerate()
        {
            _closeButton.onClick.AddListener(OnCloseClicked);
        }

        public override void OnTerminate()
        {
            _closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            _stageLabel.text = string.Format(STAGE_FORMAT, viewArgs.StageNumber);
            _scoreLabel.text = string.Format(SCORE_FORMAT, viewArgs.Score);
            _moneyLabel.text = string.Format(MONEY_FORMAT, viewArgs.Money);
            _ballLabel.text = string.Format(USED_BALL_FORMAT, viewArgs.UsedBallCount);
            _passiveLabel.text = string.Format(PASSIVE_FORMAT, FormatPassives(viewArgs.PassiveNames));
            _upgradeLabel.text = string.Format(UPGRADE_FORMAT, FormatUpgrades(viewArgs.Upgrades));
        }

        void OnCloseClicked()
        {
            UIManager.Hide(UIViewCatalogData.EViewType.ResultStats);
        }

        static string FormatPassives(string[] passiveNames)
        {
            if (null == passiveNames)
                return EMPTY_ITEMS_LABEL;

            if (passiveNames.Length <= 0)
                return EMPTY_ITEMS_LABEL;

            return string.Join(ITEM_SEPARATOR, passiveNames);
        }

        static string FormatUpgrades(ItemCountEntry[] upgrades)
        {
            if (null == upgrades)
                return EMPTY_ITEMS_LABEL;

            if (upgrades.Length <= 0)
                return EMPTY_ITEMS_LABEL;

            string[] entryTexts = new string[upgrades.Length];
            for (int i = 0; i < upgrades.Length; ++i)
                entryTexts[i] = string.Format(UPGRADE_COUNT_FORMAT, upgrades[i].Name, upgrades[i].Count);

            return string.Join(ITEM_SEPARATOR, entryTexts);
        }
    }
}
