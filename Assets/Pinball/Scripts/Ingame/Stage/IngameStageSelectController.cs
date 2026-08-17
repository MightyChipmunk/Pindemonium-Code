using System;
using Pinball.UI;

namespace Pinball.Ingame.Stage
{
    public class IngameStageSelectController
    {
        public record InitParam(
            StageCatalogData StageCatalog,
            StageProgressTracker Tracker,
            StageGimmickRoller Roller,
            StageGimmickResolver Resolver,
            MoneyWallet Wallet,
            Func<int> RemainingBallGetter,
            int StageClearBaseMoney);

        StageCatalogData _stageCatalog = null;
        StageProgressTracker _tracker = null;
        StageGimmickRoller _roller = null;
        StageGimmickResolver _resolver = null;
        MoneyWallet _wallet = null;
        Func<int> _remainingBallGetter = null;
        int _stageClearBaseMoney = 0;
        bool _isOpen = false;

        public event Action OnStageStartRequested;
        public event Action OnGimmickRerolled;

        public void Init(InitParam param)
        {
            _stageCatalog = param.StageCatalog;
            _tracker = param.Tracker;
            _roller = param.Roller;
            _resolver = param.Resolver;
            _wallet = param.Wallet;
            _remainingBallGetter = param.RemainingBallGetter;
            _stageClearBaseMoney = param.StageClearBaseMoney;
            _isOpen = false;
            UIManager.OnEvent += HandleUIEvent;
        }

        public void Release()
        {
            UIManager.OnEvent -= HandleUIEvent;
            OnStageStartRequested = null;
            OnGimmickRerolled = null;
            _isOpen = false;
            _remainingBallGetter = null;
            _wallet = null;
            _resolver = null;
            _roller = null;
            _tracker = null;
            _stageCatalog = null;
        }

        public void Open()
        {
            _isOpen = true;
            RefreshView();
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
                    CloseAndRequestStart();
                    break;
            }
        }

        void RefreshView()
        {
            int remainingBalls = null == _remainingBallGetter ? 0 : _remainingBallGetter();
            UIManager.Show(new StageSelectView.Args(_wallet.Current, remainingBalls, _tracker.StageNumber, _stageCatalog.Count));

            int currentStageIndex = _tracker.StageNumber - 1;
            for (int slot = 0; slot < StageSelectView.CARD_COUNT; ++slot)
                UIManager.Show(BuildCardArgs(slot, currentStageIndex + slot));
        }

        StageSelectView.CardArgs BuildCardArgs(int slotIndex, int stageIndex)
        {
            if (stageIndex >= _stageCatalog.Count)
                return BuildHiddenCardArgs(slotIndex);

            StageData stage = _stageCatalog.GetStage(stageIndex);
            if (null == stage)
                return BuildHiddenCardArgs(slotIndex);

            bool isCurrent = slotIndex == 0;
            IStageGimmickSource source = isCurrent ? _roller : stage;
            string[] gimmickLabels = new string[source.GimmickCount];
            for (int i = 0; i < gimmickLabels.Length; ++i)
                gimmickLabels[i] = StageGimmickLabelFormatter.Format(source.GetGimmick(i));

            return new StageSelectView.CardArgs(
                SlotIndex: slotIndex,
                IsVisible: true,
                StageNumber: stageIndex + 1,
                IsCurrent: isCurrent,
                IsFinal: stageIndex == _stageCatalog.Count - 1,
                TargetScore: _resolver.GetTargetScore(source, stage.TargetScore),
                TargetScoreMultiplier: _resolver.GetTargetScoreMultiplier(source),
                RewardMoney: _stageClearBaseMoney,
                GimmickLabels: gimmickLabels,
                RerollPrice: _roller.CurrentRerollPrice,
                CanReroll: isCurrent && _roller.CanReroll && _wallet.Current >= _roller.CurrentRerollPrice);
        }

        StageSelectView.CardArgs BuildHiddenCardArgs(int slotIndex)
        {
            return new StageSelectView.CardArgs(
                SlotIndex: slotIndex,
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

            if (!_wallet.TrySpend(_roller.CurrentRerollPrice))
                return;

            _roller.Reroll();
            OnGimmickRerolled?.Invoke();
            RefreshView();
        }

        void CloseAndRequestStart()
        {
            if (!_isOpen)
                return;

            _isOpen = false;
            OnStageStartRequested?.Invoke();
        }
    }
}
