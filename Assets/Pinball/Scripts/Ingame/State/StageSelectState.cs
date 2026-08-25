using System;
using Pinball.Audio;
using Pinball.Ingame.Stage;
using Pinball.UI;

namespace Pinball.Ingame.State
{
    public class StageSelectState : IngameStateBase
    {
        public record InitParam(
            StageCatalogData StageCatalog,
            EndlessConfigData EndlessConfig,
            Func<int> StageNumberGetter,
            Func<int> MoneyGetter,
            Func<int, bool> MoneySpender,
            Func<int> RemainingBallGetter,
            Action NextStagePreparer,
            Action<bool> ProgressSaver,
            int StageClearMoneyAdd);

        public record EnterParam(bool IsAfterShop);

        StageCatalogData _stageCatalog = null;
        EndlessConfigData _endlessConfig = null;
        Func<int> _stageNumberGetter = null;
        StageGimmickRoller _roller = null;
        StageGimmickResolver _resolver = null;
        Func<int> _moneyGetter = null;
        Func<int, bool> _moneySpender = null;
        Func<int> _remainingBallGetter = null;
        Action _nextStagePreparer = null;
        Action<bool> _progressSaver = null;
        int _stageClearMoneyAdd = 0;
        bool _isOpen = false;

        public override EIngameState Key => EIngameState.StageSelect;

        public StageSelectState(InitParam param, StageGimmickRoller roller, StageGimmickResolver resolver)
        {
            _stageCatalog = param.StageCatalog;
            _endlessConfig = param.EndlessConfig;
            _stageNumberGetter = param.StageNumberGetter;
            _roller = roller;
            _resolver = resolver;
            _moneyGetter = param.MoneyGetter;
            _moneySpender = param.MoneySpender;
            _remainingBallGetter = param.RemainingBallGetter;
            _nextStagePreparer = param.NextStagePreparer;
            _progressSaver = param.ProgressSaver;
            _stageClearMoneyAdd = param.StageClearMoneyAdd;
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

            BgmManager.Play(BgmStorage.EBgmType.StageSelect);
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
            bool isEndless = _stageCatalog.IsEndlessStageIndex(currentStageIndex);
            int roundIndex = StageCatalogData.GetRoundIndex(currentStageIndex);
            int roundStartStageIndex = StageCatalogData.GetRoundStartStageIndex(roundIndex);
            StageSelectView.CardArgs[] cards = new StageSelectView.CardArgs[StageCatalogData.STAGES_PER_ROUND];
            for (int slot = 0; slot < cards.Length; ++slot)
                cards[slot] = BuildCardArgs(roundStartStageIndex + slot, currentStageIndex);

            UIManager.Show(new StageSelectView.Args(
                Money: _moneyGetter(),
                RemainingBalls: remainingBalls,
                RoundNumber: isEndless ? roundIndex - _stageCatalog.RoundCount + 1 : roundIndex + 1,
                RoundCount: _stageCatalog.RoundCount,
                IsEndless: isEndless,
                Cards: cards));
        }

        StageSelectView.CardArgs BuildCardArgs(int stageIndex, int currentStageIndex)
        {
            StageData stage = _stageCatalog.GetStage(stageIndex);
            if (null == stage)
                return BuildHiddenCardArgs();

            bool isEndless = _stageCatalog.IsEndlessStageIndex(stageIndex);
            StageCardView.EState state = GetCardState(stageIndex, currentStageIndex);
            bool isCurrent = state == StageCardView.EState.Current;
            int baseTargetScore = GetBaseTargetScore(stage, stageIndex, isEndless);

            return new StageSelectView.CardArgs(
                IsVisible: true,
                StageNumber: isEndless ? _stageCatalog.GetEndlessStageNumber(stageIndex) : stageIndex + 1,
                IsEndless: isEndless,
                State: state,
                IsFinal: !isEndless && stageIndex == _stageCatalog.Count - 1,
                TargetScore: isCurrent ? _resolver.GetTargetScore(baseTargetScore) : baseTargetScore,
                TargetScoreMultiplier: isCurrent ? _resolver.GetTargetScoreMultiplier() : StageGimmickResolver.DEFAULT_TARGET_SCORE_MULTIPLIER,
                RewardMoney: StageData.CalculateClearMoney(stage, _stageClearMoneyAdd),
                Gimmicks: isCurrent ? BuildCurrentGimmicks() : null,
                HiddenGimmickCount: isCurrent ? 0 : stage.GimmickRollCount,
                RerollPrice: _roller.CurrentRerollPrice,
                CanReroll: isCurrent && _roller.CanReroll && _moneyGetter() >= _roller.CurrentRerollPrice);
        }

        int GetBaseTargetScore(StageData stage, int stageIndex, bool isEndless)
        {
            if (!isEndless || null == _endlessConfig)
                return stage.TargetScore;

            return _endlessConfig.GetTargetScore(_stageCatalog.GetLastStageTargetScore(), _stageCatalog.GetEndlessStageNumber(stageIndex));
        }

        StageGimmickData[] BuildCurrentGimmicks()
        {
            StageGimmickData[] gimmicks = new StageGimmickData[_roller.GimmickCount];
            for (int i = 0; i < gimmicks.Length; ++i)
                gimmicks[i] = _roller.GetGimmick(i);

            return gimmicks;
        }

        static StageCardView.EState GetCardState(int stageIndex, int currentStageIndex)
        {
            if (stageIndex < currentStageIndex)
                return StageCardView.EState.Cleared;

            if (stageIndex == currentStageIndex)
                return StageCardView.EState.Current;

            return StageCardView.EState.Upcoming;
        }

        static StageSelectView.CardArgs BuildHiddenCardArgs()
        {
            return new StageSelectView.CardArgs(
                IsVisible: false,
                StageNumber: 0,
                IsEndless: false,
                State: StageCardView.EState.Upcoming,
                IsFinal: false,
                TargetScore: 0,
                TargetScoreMultiplier: StageGimmickResolver.DEFAULT_TARGET_SCORE_MULTIPLIER,
                RewardMoney: 0,
                Gimmicks: null,
                HiddenGimmickCount: 0,
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
