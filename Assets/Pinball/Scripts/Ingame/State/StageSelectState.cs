using System;
using Pinball.Ingame.Stage;
using Pinball.UI;

namespace Pinball.Ingame.State
{
    public class StageSelectState : IngameStateBase
    {
        public record InitParam(
            StageCatalogData StageCatalog,
            Func<int> StageNumberGetter,
            Func<int> MoneyGetter,
            Func<int, bool> MoneySpender,
            Func<int> RemainingBallGetter,
            Action NextStagePreparer,
            Action<bool> ProgressSaver,
            int StageClearBaseMoney);

        public record EnterParam(bool IsAfterShop);

        StageCatalogData _stageCatalog = null;
        Func<int> _stageNumberGetter = null;
        StageGimmickRoller _roller = null;
        StageGimmickResolver _resolver = null;
        Func<int> _moneyGetter = null;
        Func<int, bool> _moneySpender = null;
        Func<int> _remainingBallGetter = null;
        Action _nextStagePreparer = null;
        Action<bool> _progressSaver = null;
        int _stageClearBaseMoney = 0;
        bool _isOpen = false;

        public override EIngameState Key => EIngameState.StageSelect;

        public StageSelectState(InitParam param, StageGimmickRoller roller, StageGimmickResolver resolver)
        {
            _stageCatalog = param.StageCatalog;
            _stageNumberGetter = param.StageNumberGetter;
            _roller = roller;
            _resolver = resolver;
            _moneyGetter = param.MoneyGetter;
            _moneySpender = param.MoneySpender;
            _remainingBallGetter = param.RemainingBallGetter;
            _nextStagePreparer = param.NextStagePreparer;
            _progressSaver = param.ProgressSaver;
            _stageClearBaseMoney = param.StageClearBaseMoney;
            _isOpen = false;
        }

        protected override void OnEnter(object param)
        {
            EnterParam enterParam = param as EnterParam;
            if (null != enterParam && enterParam.IsAfterShop)
            {
                _nextStagePreparer?.Invoke();
                _progressSaver?.Invoke(false);
            }

            UIManager.OnEvent += HandleUIEvent;
            _isOpen = true;
            RefreshView();
        }

        protected override void OnExit()
        {
            UIManager.OnEvent -= HandleUIEvent;
            _isOpen = false;
            UIManager.Hide(UIViewCatalogData.EViewType.StageSelect);
        }

        public void RefreshOpenedView()
        {
            if (!_isOpen)
                return;

            RefreshView();
        }

        void HandleUIEvent(UIManager.EEvent uiEvent, int payload)
        {
            switch (uiEvent)
            {
                case UIManager.EEvent.StageSelectRerollRequested:
                    TryReroll();
                    break;
                case UIManager.EEvent.StageSelectStartRequested:
                    RequestStart();
                    break;
            }
        }

        void RefreshView()
        {
            int remainingBalls = null == _remainingBallGetter ? 0 : _remainingBallGetter();
            int currentStageIndex = _stageNumberGetter() - 1;
            StageSelectView.CardArgs[] cards = new StageSelectView.CardArgs[StageSelectView.CARD_COUNT];
            for (int slot = 0; slot < cards.Length; ++slot)
                cards[slot] = BuildCardArgs(slot, currentStageIndex + slot);

            UIManager.Show(new StageSelectView.Args(
                Money: _moneyGetter(),
                RemainingBalls: remainingBalls,
                CurrentStageNumber: _stageNumberGetter(),
                TotalStageCount: _stageCatalog.Count,
                Cards: cards));
        }

        StageSelectView.CardArgs BuildCardArgs(int slotIndex, int stageIndex)
        {
            if (stageIndex >= _stageCatalog.Count)
                return BuildHiddenCardArgs();

            StageData stage = _stageCatalog.GetStage(stageIndex);
            if (null == stage)
                return BuildHiddenCardArgs();

            bool isCurrent = slotIndex == 0;
            IStageGimmickSource source = isCurrent ? _roller : stage;
            string[] gimmickLabels = new string[source.GimmickCount];
            for (int i = 0; i < gimmickLabels.Length; ++i)
                gimmickLabels[i] = StageGimmickLabelFormatter.Format(source.GetGimmick(i));

            return new StageSelectView.CardArgs(
                IsVisible: true,
                StageNumber: stageIndex + 1,
                IsCurrent: isCurrent,
                IsFinal: stageIndex == _stageCatalog.Count - 1,
                TargetScore: _resolver.GetTargetScore(source, stage.TargetScore),
                TargetScoreMultiplier: _resolver.GetTargetScoreMultiplier(source),
                RewardMoney: _stageClearBaseMoney,
                GimmickLabels: gimmickLabels,
                RerollPrice: _roller.CurrentRerollPrice,
                CanReroll: isCurrent && _roller.CanReroll && _moneyGetter() >= _roller.CurrentRerollPrice);
        }

        static StageSelectView.CardArgs BuildHiddenCardArgs()
        {
            return new StageSelectView.CardArgs(
                IsVisible: false,
                StageNumber: 0,
                IsCurrent: false,
                IsFinal: false,
                TargetScore: 0,
                TargetScoreMultiplier: StageGimmickResolver.DEFAULT_TARGET_SCORE_MULTIPLIER,
                RewardMoney: 0,
                GimmickLabels: null,
                RerollPrice: 0,
                CanReroll: false);
        }

        void TryReroll()
        {
            if (!_isOpen)
                return;

            if (!_roller.CanReroll)
                return;

            if (!_moneySpender(_roller.CurrentRerollPrice))
                return;

            _roller.Reroll();
            _progressSaver?.Invoke(false);
            RefreshView();
        }

        void RequestStart()
        {
            if (!_isOpen)
                return;

            ChangeState(EIngameState.Play);
        }
    }
}
