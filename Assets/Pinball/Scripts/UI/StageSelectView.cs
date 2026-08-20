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
            StageCardView.EState State,
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
            int RoundNumber,
            int RoundCount,
            CardArgs[] Cards) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.StageSelect;
        }

        const string MONEY_FORMAT = "$ {0}";
        const string BALLS_FORMAT = "BALLS {0}";
        const string BALLS_KEY = "ui.stage_select.balls";
        const string ROUND_FORMAT = "ROUND {0} OF {1}";
        const string ROUND_KEY = "ui.stage_select.round";

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
            _progressLabel.text = LocalizationTable.Format(ROUND_KEY, ROUND_FORMAT, args.RoundNumber, args.RoundCount);
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
                State: args.State,
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
