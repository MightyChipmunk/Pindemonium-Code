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
            Func<int> StageNumberGetter,
            Func<bool> IsEndlessGetter,
            Func<bool> IsPlayStateGetter,
            Action<bool> TimeFreezer,
            Func<bool> CanNudgeGetter,
            Func<int> ComboCountGetter,
            Action NudgeCooldownResetter,
            Func<RectTransform> ScoreAnchorGetter,
            Func<RectTransform> ComboAnchorGetter);

        const int COMBO_TRIGGER_COUNT = 10;
        const int LAST_BALL_TRIGGER_COUNT = 1;
        const float SHOP_OPEN_WAIT_SECONDS = 1f;

        [SerializeField] TutorialDimmer _dimmer = null;
        [SerializeField] TutorialHighlightLayer _highlightLayer = null;
        [SerializeField] TutorialDialogView _dialog = null;

        InitParam _param = null;
        TutorialProgress _progress = null;
        PlayIntroTutorialBlock _playIntroBlock = null;
        ShopTutorialBlock _shopBlock = null;
        StageGimmickTutorialBlock _stageGimmickBlock = null;
        CancellationTokenSource _cancellationTokenSource = null;
        MapRoot _currentMap = null;
        bool _hasStageStarted = false;
        bool _hasLaunched = false;
        bool _hasSlingshotHit = false;
        bool _isComboTriggerReached = false;
        bool _isLastBallLost = false;
        bool _hasNudged = false;
        bool _isStageClearIntercepted = false;
        bool _isPlayIntroRunning = false;
        bool _isModalActive = false;
        bool _isTimeFreezeActive = false;

        public void Init(InitParam param)
        {
            _param = param;
            _progress = new TutorialProgress();
            _dialog.Init();

            _playIntroBlock = new PlayIntroTutorialBlock(new PlayIntroTutorialBlock.InitParam(
                MapGetter: GetCurrentMap,
                ModalSetter: SetModal,
                TimeFreezeSetter: SetTimeFreeze,
                IsPlayReadyGetter: GetIsPlayIntroReady,
                IsPlayStateGetter: param.IsPlayStateGetter,
                HasLaunchedGetter: GetHasLaunched,
                HasSlingshotHitGetter: GetHasSlingshotHit,
                IsComboTriggerReachedGetter: GetIsComboTriggerReached,
                IsLastBallLostGetter: GetIsLastBallLost,
                IsStageClearInterceptedGetter: GetIsStageClearIntercepted,
                HasNudgedGetter: GetHasNudged,
                CanNudgeGetter: param.CanNudgeGetter,
                NudgeWaitPreparer: PrepareNudgeWait,
                ComboWaitPreparer: PrepareComboWait,
                ScoreAnchorGetter: param.ScoreAnchorGetter,
                ComboAnchorGetter: param.ComboAnchorGetter), _dimmer, _dialog, _highlightLayer);
            _shopBlock = new ShopTutorialBlock(new ShopTutorialBlock.InitParam(param.InterestPerAmount, param.InterestMax, param.PassiveSlotCount), _dimmer, _dialog, _highlightLayer);
            _stageGimmickBlock = new StageGimmickTutorialBlock(_dimmer, _dialog, _highlightLayer);

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            WatchPlayIntroAsync(_cancellationTokenSource.Token).Forget();
            WatchShopAsync(_cancellationTokenSource.Token).Forget();
            WatchStageGimmickAsync(_cancellationTokenSource.Token).Forget();
        }

        public void Release()
        {
            CancelWatchers();
            ClearOverlay();
            _dialog.Release();
            _isPlayIntroRunning = false;
            _stageGimmickBlock = null;
            _shopBlock = null;
            _playIntroBlock = null;
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

            await UniTask.WaitUntil(GetIsPlayIntroFinished, cancellationToken: _cancellationTokenSource.Token).SuppressCancellationThrow();
        }

        public void StopRunning()
        {
            CancelWatchers();
            ClearOverlay();
            _isPlayIntroRunning = false;
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

        void PrepareComboWait()
        {
            _isComboTriggerReached = _param.ComboCountGetter() >= COMBO_TRIGGER_COUNT;
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
