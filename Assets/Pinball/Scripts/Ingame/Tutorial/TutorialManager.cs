using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.Ingame.Map;
using Pinball.Ingame.Stage;
using Pinball.UI;
using UnityEngine;

namespace Pinball.Ingame.Tutorial
{
    public class TutorialManager : MonoBehaviour
    {
        public record InitParam(
            bool IsNewRun,
            int InterestPerAmount,
            int InterestMax,
            int PassiveSlotCount,
            float ComboTimeoutSeconds,
            Func<int> StageNumberGetter,
            Func<bool> IsEndlessGetter,
            Func<bool> IsPlayStateGetter,
            Action<bool> TimeFreezer,
            Func<bool> CanNudgeGetter,
            Func<int> ComboCountGetter,
            Action NudgeCooldownResetter,
            Action ControlStateResetter,
            Func<RectTransform> ScoreAnchorGetter,
            Func<RectTransform> ComboAnchorGetter);

        const int COMBO_TRIGGER_COUNT = 10;
        const int LAST_BALL_TRIGGER_COUNT = 1;
        const float SHOP_OPEN_WAIT_SECONDS = 1f;

        [SerializeField] TutorialDimmer _dimmer = null;
        [SerializeField] TutorialHighlightLayer _highlightLayer = null;
        [SerializeField] TutorialDialogView _dialog = null;
        [SerializeField] TutorialStepCatalogData _stepCatalog = null;

        InitParam _param = null;
        TutorialProgress _progress = null;
        TutorialHighlighter _highlighter = null;
        PlayIntroTutorialBlock _playIntroBlock = null;
        ShopTutorialBlock _shopBlock = null;
        StageGimmickTutorialBlock _stageGimmickBlock = null;
        BallLostTutorialBlock _ballLostBlock = null;
        CancellationTokenSource _cancellationTokenSource = null;
        MapRoot _currentMap = null;
        bool _hasStageStarted = false;
        bool _hasLaunched = false;
        bool _hasSlingshotHit = false;
        bool _hasBallStruck = false;
        bool _isBallLostPending = false;
        bool _isComboTriggerReached = false;
        bool _isLastBallLost = false;
        bool _hasNudged = false;
        bool _isStageClearIntercepted = false;
        bool _isPlayIntroRunning = false;
        bool _isBallLostGuideRunning = false;
        bool _isModalActive = false;
        bool _isTimeFreezeActive = false;
        bool _isFlipperControlLocked = false;

        public void Init(InitParam param)
        {
            _param = param;
            _progress = new TutorialProgress();
            _dialog.Init();
            _highlighter = new TutorialHighlighter(new TutorialHighlighter.InitParam(
                MapGetter: GetCurrentMap,
                ScoreAnchorGetter: param.ScoreAnchorGetter,
                ComboAnchorGetter: param.ComboAnchorGetter), _dimmer, _highlightLayer);

            _playIntroBlock = new PlayIntroTutorialBlock(new PlayIntroTutorialBlock.InitParam(
                MapGetter: GetCurrentMap,
                ModalSetter: SetModal,
                TimeFreezeSetter: SetTimeFreeze,
                FlipperLockSetter: SetFlipperControlLocked,
                IsPlayReadyGetter: GetIsPlayIntroReady,
                IsPlayStateGetter: param.IsPlayStateGetter,
                HasLaunchedGetter: GetHasLaunched,
                HasSlingshotHitGetter: GetHasSlingshotHit,
                HasBallStruckGetter: GetHasBallStruck,
                IsBallLostGetter: GetIsBallLostPending,
                IsBallLostGuideRunningGetter: GetIsBallLostGuideRunning,
                IsComboTriggerReachedGetter: GetIsComboTriggerReached,
                IsLastBallLostGetter: GetIsLastBallLost,
                IsStageClearInterceptedGetter: GetIsStageClearIntercepted,
                HasNudgedGetter: GetHasNudged,
                CanNudgeGetter: param.CanNudgeGetter,
                NudgeWaitPreparer: PrepareNudgeWait,
                FlipWaitPreparer: PrepareFlipWait,
                ComboWaitPreparer: PrepareComboWait,
                ComboTimeoutSeconds: param.ComboTimeoutSeconds), _stepCatalog, _dialog, _highlighter);
            _shopBlock = new ShopTutorialBlock(new ShopTutorialBlock.InitParam(param.InterestPerAmount, param.InterestMax, param.PassiveSlotCount), _stepCatalog, _dialog, _highlighter);
            _stageGimmickBlock = new StageGimmickTutorialBlock(_stepCatalog, _dialog, _highlighter);
            _ballLostBlock = new BallLostTutorialBlock(new BallLostTutorialBlock.InitParam(
                LaunchWaitPreparer: PrepareLaunchWait,
                HasLaunchedGetter: GetHasLaunched,
                IsPlayStateGetter: param.IsPlayStateGetter,
                IsStageClearInterceptedGetter: GetIsStageClearIntercepted), _stepCatalog, _dialog, _highlighter);

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            WatchPlayIntroAsync(_cancellationTokenSource.Token).Forget();
            WatchShopAsync(_cancellationTokenSource.Token).Forget();
            WatchStageGimmickAsync(_cancellationTokenSource.Token).Forget();
            WatchBallLostAsync(_cancellationTokenSource.Token).Forget();
        }

        public void Release()
        {
            CancelWatchers();
            ClearOverlay();
            _dialog.Release();
            _isFlipperControlLocked = false;
            _isPlayIntroRunning = false;
            _isBallLostGuideRunning = false;
            _ballLostBlock = null;
            _stageGimmickBlock = null;
            _shopBlock = null;
            _playIntroBlock = null;
            _highlighter = null;
            _progress = null;
            _currentMap = null;
            _param = null;
        }

        public void NotifyStageStarted(int stageNumber)
        {
            if (stageNumber <= 0)
                return;

            _hasStageStarted = true;
        }

        public void NotifyMapLoaded(MapRoot map)
        {
            _currentMap = map;
        }

        public void NotifyBallLaunched()
        {
            _hasLaunched = true;
        }

        public void NotifySlingshotHit()
        {
            _hasSlingshotHit = true;
        }

        public void NotifyBallStruck()
        {
            _hasBallStruck = true;
        }

        public void NotifyBallReady()
        {
            _isBallLostPending = true;
        }

        public void NotifyComboChanged(int comboCount)
        {
            if (comboCount < COMBO_TRIGGER_COUNT)
                return;

            _isComboTriggerReached = true;
        }

        public void NotifyBallCountChanged(int remainingBallCount)
        {
            if (remainingBallCount > LAST_BALL_TRIGGER_COUNT)
                return;

            _isLastBallLost = true;
        }

        public void NotifyNudged()
        {
            _hasNudged = true;
        }

        public bool IsPlayIntroRunning()
        {
            return _isPlayIntroRunning;
        }

        public bool IsFlipperControlLocked()
        {
            return _isPlayIntroRunning && _isFlipperControlLocked;
        }

        public bool IsOverlayActive()
        {
            return _dimmer.IsShowing() || _dialog.IsShowing();
        }

        public async UniTask RunPendingPlayStepsAsync()
        {
            if (!_isPlayIntroRunning)
                return;

            _isStageClearIntercepted = true;
            if (null == _cancellationTokenSource)
                return;

            CancellationToken cancellationToken = _cancellationTokenSource.Token;
            bool isCanceled = await UniTask.WaitUntil(GetIsPlayIntroFinished, cancellationToken: cancellationToken).SuppressCancellationThrow();
            if (isCanceled && _isPlayIntroRunning)
                cancellationToken.ThrowIfCancellationRequested();
        }

        public void StopRunning()
        {
            _isFlipperControlLocked = false;
            _isPlayIntroRunning = false;
            _isBallLostGuideRunning = false;
            CancelWatchers();
            ClearOverlay();
        }

        async UniTaskVoid WatchPlayIntroAsync(CancellationToken cancellationToken)
        {
            if (!_param.IsNewRun || _progress.IsCompleted(TutorialProgress.EBlock.PlayIntro))
                return;

            await UniTask.WaitUntil(GetIsStageSelectOpen, cancellationToken: cancellationToken);
            _isPlayIntroRunning = true;
            bool isCompleted = await _playIntroBlock.RunAsync(cancellationToken);
            _isPlayIntroRunning = false;
            if (isCompleted)
                _progress.Complete(TutorialProgress.EBlock.PlayIntro);
        }

        async UniTaskVoid WatchShopAsync(CancellationToken cancellationToken)
        {
            if (_progress.IsCompleted(TutorialProgress.EBlock.Shop))
                return;

            await UniTask.WaitUntil(GetIsShopOpen, cancellationToken: cancellationToken);
            await UniTask.Delay(TimeSpan.FromSeconds(SHOP_OPEN_WAIT_SECONDS), DelayType.UnscaledDeltaTime, cancellationToken: cancellationToken);
            bool isCompleted = await _shopBlock.RunAsync(cancellationToken);
            if (isCompleted)
                _progress.Complete(TutorialProgress.EBlock.Shop);
        }

        async UniTaskVoid WatchStageGimmickAsync(CancellationToken cancellationToken)
        {
            if (_progress.IsCompleted(TutorialProgress.EBlock.StageGimmick))
                return;

            await UniTask.WaitUntil(GetIsStageGimmickReady, cancellationToken: cancellationToken);
            bool isCompleted = await _stageGimmickBlock.RunAsync(cancellationToken);
            if (isCompleted)
                _progress.Complete(TutorialProgress.EBlock.StageGimmick);
        }

        async UniTaskVoid WatchBallLostAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                await UniTask.WaitUntil(GetIsBallLostGuideReady, cancellationToken: cancellationToken);

                bool isFirstTime = !_progress.IsCompleted(TutorialProgress.EBlock.BallLost);
                _isBallLostGuideRunning = true;
                bool isCompleted = await _ballLostBlock.RunAsync(isFirstTime, cancellationToken);
                _isBallLostGuideRunning = false;

                if (isFirstTime && isCompleted)
                    _progress.Complete(TutorialProgress.EBlock.BallLost);
            }
        }

        bool GetIsBallLostGuideReady()
        {
            if (_isBallLostGuideRunning || !_isBallLostPending)
                return false;

            if (!_param.IsPlayStateGetter())
                return false;

            return _isPlayIntroRunning || !_progress.IsCompleted(TutorialProgress.EBlock.BallLost);
        }

        bool GetIsBallLostGuideRunning()
        {
            return _isBallLostGuideRunning;
        }

        bool GetIsPlayIntroReady()
        {
            return _hasStageStarted && null != _currentMap && _param.IsPlayStateGetter();
        }

        static bool GetIsShopOpen()
        {
            return UIManager.IsOpen(UIViewCatalogData.EViewType.Shop);
        }

        static bool GetIsStageSelectOpen()
        {
            return UIManager.IsOpen(UIViewCatalogData.EViewType.StageSelect);
        }

        bool GetIsStageGimmickReady()
        {
            if (!UIManager.IsOpen(UIViewCatalogData.EViewType.StageSelect))
                return false;

            if (_param.IsEndlessGetter())
                return false;

            return StageCatalogData.IsRoundLastStage(_param.StageNumberGetter() - 1);
        }

        bool GetIsPlayIntroFinished()
        {
            return !_isPlayIntroRunning;
        }

        MapRoot GetCurrentMap()
        {
            return _currentMap;
        }

        bool GetHasLaunched()
        {
            return _hasLaunched;
        }

        bool GetHasSlingshotHit()
        {
            return _hasSlingshotHit;
        }

        bool GetHasBallStruck()
        {
            return _hasBallStruck;
        }

        bool GetIsBallLostPending()
        {
            return _isBallLostPending;
        }

        bool GetIsComboTriggerReached()
        {
            return _isComboTriggerReached;
        }

        bool GetIsLastBallLost()
        {
            return _isLastBallLost;
        }

        bool GetHasNudged()
        {
            return _hasNudged;
        }

        bool GetIsStageClearIntercepted()
        {
            return _isStageClearIntercepted;
        }

        void PrepareNudgeWait()
        {
            _hasNudged = false;
            _param.NudgeCooldownResetter();
        }

        void PrepareFlipWait()
        {
            _hasBallStruck = false;
        }

        void PrepareLaunchWait()
        {
            _isBallLostPending = false;
            _hasLaunched = false;
        }

        void PrepareComboWait()
        {
            _isComboTriggerReached = _param.ComboCountGetter() >= COMBO_TRIGGER_COUNT;
        }

        void SetFlipperControlLocked(bool isLocked)
        {
            if (_isFlipperControlLocked == isLocked)
                return;

            _isFlipperControlLocked = isLocked;
            if (isLocked)
                _param.ControlStateResetter();
        }

        void SetModal(bool isModal)
        {
            if (_isModalActive == isModal)
                return;

            _isModalActive = isModal;
            if (isModal)
                Blocker.Block();
            else
                Blocker.Unblock();

            if (isModal)
                _param.ControlStateResetter();

            ApplyTimeFreeze();
        }

        void SetTimeFreeze(bool isFrozen)
        {
            if (_isTimeFreezeActive == isFrozen)
                return;

            _isTimeFreezeActive = isFrozen;
            ApplyTimeFreeze();
        }

        void ApplyTimeFreeze()
        {
            _param.TimeFreezer(_isModalActive || _isTimeFreezeActive);
        }

        void CancelWatchers()
        {
            if (null == _cancellationTokenSource)
                return;

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        void ClearOverlay()
        {
            SetTimeFreeze(false);
            SetModal(false);
            _dimmer.Hide();
            _highlightLayer.Clear();
            _dialog.Hide();
        }
    }
}
