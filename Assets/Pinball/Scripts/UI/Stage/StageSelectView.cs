using Pinball.Ingame.Stage;
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
            bool IsEndless,
            StageCardView.EState State,
            bool IsFinal,
            int TargetScore,
            float TargetScoreMultiplier,
            float BumperHealthMultiplier,
            int RewardMoney,
            StageGimmickData[] Gimmicks,
            int HiddenGimmickCount,
            int RerollPrice,
            bool CanReroll);

        public record Args(
            int Money,
            int RemainingBalls,
            int RoundNumber,
            int RoundCount,
            bool IsEndless,
            CardArgs[] Cards) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.StageSelect;
        }

        const string MONEY_FORMAT = "$ {0}";
        const string BALLS_FORMAT = "BALLS {0}";
        const string BALLS_KEY = "ui.stage_select.balls";
        const string ROUND_FORMAT = "ROUND {0} OF {1}";
        const string ROUND_KEY = "ui.stage_select.round";
        const string ENDLESS_ROUND_FORMAT = "ENDLESS ROUND {0}";
        const string ENDLESS_ROUND_KEY = "ui.stage_select.endless_round";

        const int NO_CARD_INDEX = -1;

        [SerializeField] TMP_Text _progressLabel = null;
        [SerializeField] TMP_Text _moneyLabel = null;
        [SerializeField] TMP_Text _ballsLabel = null;
        [SerializeField] StageCardView[] _cards = null;

        int _currentCardIndex = NO_CARD_INDEX;

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
            _progressLabel.text = args.IsEndless
                ? LocalizationTable.Format(ENDLESS_ROUND_KEY, ENDLESS_ROUND_FORMAT, args.RoundNumber)
                : LocalizationTable.Format(ROUND_KEY, ROUND_FORMAT, args.RoundNumber, args.RoundCount);
        }

        void BindCards(CardArgs[] cards)
        {
            _currentCardIndex = NO_CARD_INDEX;
            int cardCount = null == cards ? 0 : cards.Length;
            for (int i = 0; i < _cards.Length; ++i)
            {
                if (i >= cardCount || null == cards[i])
                {
                    _cards[i].Hide();
                    continue;
                }

                if (cards[i].State == StageCardView.EState.Current && cards[i].IsVisible)
                    _currentCardIndex = i;

                BindCard(_cards[i], cards[i]);
            }
        }

        public RectTransform GetCurrentCardAnchor()
        {
            if (_currentCardIndex == NO_CARD_INDEX)
                return null;

            return (RectTransform)_cards[_currentCardIndex].transform;
        }

        public RectTransform GetCurrentGimmickAnchor()
        {
            if (_currentCardIndex == NO_CARD_INDEX)
                return null;

            return _cards[_currentCardIndex].GetGimmickAnchor();
        }

        public RectTransform GetCurrentRerollAnchor()
        {
            if (_currentCardIndex == NO_CARD_INDEX)
                return null;

            return _cards[_currentCardIndex].GetRerollAnchor();
        }

        public RectTransform GetCurrentStartAnchor()
        {
            if (_currentCardIndex == NO_CARD_INDEX)
                return null;

            return _cards[_currentCardIndex].GetStartAnchor();
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
                IsEndless: args.IsEndless,
                State: args.State,
                IsFinal: args.IsFinal,
                TargetScore: args.TargetScore,
                TargetScoreMultiplier: args.TargetScoreMultiplier,
                BumperHealthMultiplier: args.BumperHealthMultiplier,
                RewardMoney: args.RewardMoney,
                Gimmicks: args.Gimmicks,
                HiddenGimmickCount: args.HiddenGimmickCount,
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
