using Pinball.Localization;
using TMPro;
using UnityEngine;

namespace Pinball.UI
{
    public class StageSelectView : UIViewBase
    {
        public record CardArgs(
            bool IsVisible,
            int StageNumber,
            bool IsCurrent,
            bool IsFinal,
            int TargetScore,
            float TargetScoreMultiplier,
            int RewardMoney,
            string[] GimmickLabels,
            int RerollPrice,
            bool CanReroll);

        public record Args(
            int Money,
            int RemainingBalls,
            int CurrentStageNumber,
            int TotalStageCount,
            CardArgs[] Cards) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.StageSelect;
        }

        public const int CARD_COUNT = 3;

        const string MONEY_FORMAT = "$ {0}";
        const string BALLS_FORMAT = "BALLS {0}";
        const string BALLS_KEY = "ui.stage_select.balls";
        const string PROGRESS_RANGE_FORMAT = "STAGE {0} - {1} OF {2}";
        const string PROGRESS_RANGE_KEY = "ui.stage_select.progress_range";
        const string PROGRESS_SINGLE_FORMAT = "STAGE {0} OF {1}";
        const string PROGRESS_SINGLE_KEY = "ui.stage_select.progress_single";

        [SerializeField] TMP_Text _progressLabel = null;
        [SerializeField] TMP_Text _moneyLabel = null;
        [SerializeField] TMP_Text _ballsLabel = null;
        [SerializeField] StageCardView[] _cards = null;

        public override void OnGenerate()
        {
            for (int i = 0; i < _cards.Length; ++i)
                _cards[i].Init(i, OnStartClicked, OnRerollClicked);
        }

        public override void OnTerminate()
        {
            for (int i = 0; i < _cards.Length; ++i)
                _cards[i].Release();
        }

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            BindMain(viewArgs);
            BindCards(viewArgs.Cards);
        }

        void BindMain(Args args)
        {
            _moneyLabel.text = string.Format(MONEY_FORMAT, args.Money);
            _ballsLabel.text = LocalizationTable.Format(BALLS_KEY, BALLS_FORMAT, args.RemainingBalls);

            int lastPreviewNumber = Mathf.Min(args.CurrentStageNumber + CARD_COUNT - 1, args.TotalStageCount);
            if (lastPreviewNumber > args.CurrentStageNumber)
                _progressLabel.text = LocalizationTable.Format(PROGRESS_RANGE_KEY, PROGRESS_RANGE_FORMAT, args.CurrentStageNumber, lastPreviewNumber, args.TotalStageCount);
            else
                _progressLabel.text = LocalizationTable.Format(PROGRESS_SINGLE_KEY, PROGRESS_SINGLE_FORMAT, args.CurrentStageNumber, args.TotalStageCount);
        }

        void BindCards(CardArgs[] cards)
        {
            int cardCount = null == cards ? 0 : cards.Length;
            for (int i = 0; i < _cards.Length; ++i)
            {
                if (i < cardCount && null != cards[i])
                    BindCard(_cards[i], cards[i]);
                else
                    _cards[i].Hide();
            }
        }

        void BindCard(StageCardView card, CardArgs args)
        {
            if (!args.IsVisible)
            {
                card.Hide();
                return;
            }

            card.Bind(new StageCardView.BindParam(
                StageNumber: args.StageNumber,
                IsCurrent: args.IsCurrent,
                IsFinal: args.IsFinal,
                TargetScore: args.TargetScore,
                TargetScoreMultiplier: args.TargetScoreMultiplier,
                RewardMoney: args.RewardMoney,
                GimmickLabels: args.GimmickLabels,
                RerollPrice: args.RerollPrice,
                CanReroll: args.CanReroll));
        }

        void OnStartClicked(int slotIndex)
        {
            UIManager.Hide(UIViewCatalogData.EViewType.StageSelect);
            UIManager.RaiseEvent(UIManager.EEvent.StageSelectStartRequested);
        }

        void OnRerollClicked(int slotIndex)
        {
            UIManager.RaiseEvent(UIManager.EEvent.StageSelectRerollRequested);
        }
    }
}
