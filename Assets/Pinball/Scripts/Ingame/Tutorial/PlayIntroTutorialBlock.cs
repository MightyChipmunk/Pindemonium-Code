using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.Ingame.Map;
using Pinball.Settings;
using Pinball.UI;
using GameCore.Localization;
using GameCore.Settings;
using GameCore.UI;

namespace Pinball.Ingame.Tutorial
{
    public class PlayIntroTutorialBlock
    {
        public record InitParam(
            Func<MapRoot> MapGetter,
            Action<bool> ModalSetter,
            Action<bool> TimeFreezeSetter,
            Action<bool> FlipperLockSetter,
            Func<bool> IsPlayReadyGetter,
            Func<bool> IsPlayStateGetter,
            Func<bool> HasLaunchedGetter,
            Func<bool> HasSlingshotHitGetter,
            Func<bool> HasBallStruckGetter,
            Func<bool> IsBallLostGetter,
            Func<bool> IsBallLostGuideRunningGetter,
            Func<bool> IsComboTriggerReachedGetter,
            Func<bool> IsLastBallLostGetter,
            Func<bool> IsStageClearInterceptedGetter,
            Func<bool> HasNudgedGetter,
            Func<bool> CanNudgeGetter,
            Action NudgeWaitPreparer,
            Action FlipWaitPreparer,
            Action ComboWaitPreparer,
            float ComboTimeoutSeconds);

        const float NUDGE_SETTLE_SECONDS = 1f;
        const float SLINGSHOT_HIT_DELAY_SECONDS = 0.3f;

        readonly InitParam _param;
        readonly TutorialStepCatalogData _catalog;
        readonly TutorialDialogView _dialog;
        readonly TutorialHighlighter _highlighter;
        readonly TutorialStepRunner _runner;
        bool _isNudgeGuidePending = false;
        bool _isFlipperGuidePending = false;
        bool _hasSlingshot = false;

        public PlayIntroTutorialBlock(InitParam param, TutorialStepCatalogData catalog, TutorialDialogView dialog, TutorialHighlighter highlighter)
        {
            _param = param;
            _catalog = catalog;
            _dialog = dialog;
            _highlighter = highlighter;
            _runner = new TutorialStepRunner(dialog, highlighter);
        }

        public async UniTask<bool> RunAsync(CancellationToken cancellationToken)
        {
            _param.FlipperLockSetter(true);

            await ScreenTransition.WaitUntilIdleAsync(cancellationToken);
            await RunStageSelectIntroAsync(cancellationToken);

            await UniTask.WaitUntil(GetIsPlayReady, cancellationToken: cancellationToken);
            await UniTask.NextFrame(cancellationToken);

            _param.ModalSetter(true);
            await RunBriefingAsync(cancellationToken);
            _param.ModalSetter(false);

            await RunLaunchGuideAsync(cancellationToken);

            await RunPlayGuidesAsync(cancellationToken);
            if (!_param.IsPlayStateGetter())
                return false;

            _param.ComboWaitPreparer();
            while (true)
            {
                await UniTask.WaitUntil(GetIsComboWaitFinished, cancellationToken: cancellationToken);
                if (!_param.IsPlayStateGetter())
                    return false;

                if (!_param.IsBallLostGetter() || GetIsPlayInterrupted())
                    break;

                await WaitBallLostHandledAsync(cancellationToken);
            }

            _param.ModalSetter(true);
            TutorialStepCatalogData.Step comboStep = _catalog.GetStep(TutorialStepCatalogData.EStep.PlayCombo);
            if (_param.IsComboTriggerReachedGetter())
                _highlighter.Apply(comboStep.Highlight);
            else
                _highlighter.Apply(TutorialStepCatalogData.EHighlight.FullScreen);

            LocalizedTextRef comboText = new LocalizedTextRef(comboStep.Key, new object[] { _param.ComboTimeoutSeconds });
            await _dialog.ShowAndWaitClickAsync(comboText, comboStep.Placement, comboStep.Mascot, cancellationToken);
            _highlighter.Clear();

            if (!_param.IsStageClearInterceptedGetter())
            {
                TutorialStepCatalogData.Step freePlayStep = _catalog.GetStep(TutorialStepCatalogData.EStep.PlayFreePlay);
                _highlighter.Apply(freePlayStep.Highlight);
                await _dialog.ShowAndWaitClickAsync(freePlayStep.Key, freePlayStep.Placement, freePlayStep.Mascot, cancellationToken);
            }

            _param.ModalSetter(false);

            _highlighter.Clear();
            _dialog.Hide();
            return true;
        }

        async UniTask RunStageSelectIntroAsync(CancellationToken cancellationToken)
        {
            StageSelectView view = UIManager.GetActive(UIViewCatalogData.EViewType.StageSelect) as StageSelectView;
            bool hasWaitedTransition = false;

            for (int i = 0, count = _catalog.GetFlowStepCount(TutorialStepCatalogData.EFlow.PlayStageSelectIntro); i < count; ++i)
            {
                TutorialStepCatalogData.Step step = _catalog.GetFlowStep(TutorialStepCatalogData.EFlow.PlayStageSelectIntro, i);
                if (!hasWaitedTransition && step.Highlight != TutorialStepCatalogData.EHighlight.FullScreen)
                {
                    if (null == view)
                        break;

                    hasWaitedTransition = true;
                    await UniTask.WaitWhile(view.IsTransitionPlaying, cancellationToken: cancellationToken);
                }

                await _runner.ShowAsync(step, null, cancellationToken);
            }

            _highlighter.Clear();
            _dialog.Hide();
        }

        bool GetIsPlayReady()
        {
            return _param.IsPlayReadyGetter();
        }

        async UniTask RunBriefingAsync(CancellationToken cancellationToken)
        {
            for (int i = 0, count = _catalog.GetFlowStepCount(TutorialStepCatalogData.EFlow.PlayBriefing); i < count; ++i)
            {
                TutorialStepCatalogData.Step step = _catalog.GetFlowStep(TutorialStepCatalogData.EFlow.PlayBriefing, i);
                await _runner.ShowAsync(step, GetBriefingArgs(step.Id), cancellationToken);
            }
        }

        static object[] GetBriefingArgs(TutorialStepCatalogData.EStep id)
        {
            if (id != TutorialStepCatalogData.EStep.PlaySpring)
                return null;

            return new object[] { GetKeyName(EKeyAction.Launch) };
        }

        async UniTask RunLaunchGuideAsync(CancellationToken cancellationToken)
        {
            TutorialStepCatalogData.Step step = _catalog.GetStep(TutorialStepCatalogData.EStep.PlayLaunchGuide);
            _highlighter.Apply(step.Highlight);

            LocalizedTextRef guideText = new LocalizedTextRef(step.Key, new object[] { GetKeyName(EKeyAction.Launch) });
            _dialog.ShowGuide(guideText, step.Placement, step.Mascot);
            await UniTask.WaitUntil(GetIsLaunchWaitFinished, cancellationToken: cancellationToken);
            _dialog.Hide();
        }

        async UniTask RunPlayGuidesAsync(CancellationToken cancellationToken)
        {
            MapRoot map = _param.MapGetter();
            _hasSlingshot = null != map && HasActiveGimmick(map, MapInteractable.EGimmickType.Slingshot);
            _isNudgeGuidePending = true;
            _isFlipperGuidePending = null != map;
            SetFlipperApproachArmed(true);

            await RunPlayGuideLoopAsync(cancellationToken);

            _param.FlipperLockSetter(false);
            SetFlipperApproachArmed(false);
        }

        void SetFlipperApproachArmed(bool isArmed)
        {
            MapRoot map = _param.MapGetter();
            if (null == map)
                return;

            if (isArmed)
                map.ArmFlipperApproachZone();
            else
                map.DisarmFlipperApproachZone();
        }

        async UniTask RunPlayGuideLoopAsync(CancellationToken cancellationToken)
        {
            while (_isNudgeGuidePending || _isFlipperGuidePending)
            {
                await UniTask.WaitUntil(GetIsPlayGuideWaitFinished, cancellationToken: cancellationToken);
                if (!_param.IsPlayStateGetter())
                    return;

                if (GetIsPlayInterrupted())
                    return;

                if (_param.IsBallLostGetter() || _param.IsBallLostGuideRunningGetter())
                {
                    await WaitBallLostHandledAsync(cancellationToken);
                    continue;
                }

                if (_isNudgeGuidePending && GetIsNudgeTriggerReached())
                {
                    _isNudgeGuidePending = false;
                    SetFlipperApproachArmed(false);
                    await DelayAfterSlingshotHitAsync(cancellationToken);
                    await RunNudgeGuideAsync(cancellationToken);
                    SetFlipperApproachArmed(true);
                    continue;
                }

                _isFlipperGuidePending = false;
                await RunFlipperGuideAsync(cancellationToken);
            }
        }

        async UniTask WaitBallLostHandledAsync(CancellationToken cancellationToken)
        {
            await UniTask.WaitUntil(GetIsBallLostHandled, cancellationToken: cancellationToken);
        }

        bool GetIsBallLostHandled()
        {
            if (GetIsPlayInterrupted())
                return true;

            return !_param.IsBallLostGetter() && !_param.IsBallLostGuideRunningGetter();
        }

        async UniTask DelayAfterSlingshotHitAsync(CancellationToken cancellationToken)
        {
            if (!_param.HasSlingshotHitGetter())
                return;

            await UniTask.Delay(TimeSpan.FromSeconds(SLINGSHOT_HIT_DELAY_SECONDS), cancellationToken: cancellationToken);
        }

        async UniTask RunFlipperGuideAsync(CancellationToken cancellationToken)
        {
            MapRoot map = _param.MapGetter();
            if (null == map)
                return;

            TutorialStepCatalogData.Step flipperStep = _catalog.GetStep(TutorialStepCatalogData.EStep.PlayFlipper);
            _param.FlipperLockSetter(false);
            _highlighter.Apply(flipperStep.Highlight);

            _param.FlipWaitPreparer();
            LocalizedTextRef flipperText = new LocalizedTextRef(flipperStep.Key, new object[] { GetKeyName(EKeyAction.FlipperLeft), GetKeyName(EKeyAction.FlipperRight) });
            _dialog.ShowGuide(flipperText, flipperStep.Placement, flipperStep.Mascot);
            await UniTask.WaitUntil(GetIsFlipWaitFinished, cancellationToken: cancellationToken);
            map.DisarmFlipperApproachZone();
            _dialog.Hide();
            _highlighter.Clear();

            if (!_param.IsPlayStateGetter())
                return;

            TutorialStepCatalogData.Step parryStep = _catalog.GetStep(TutorialStepCatalogData.EStep.PlayParry);
            _param.ModalSetter(true);
            _highlighter.Apply(parryStep.Highlight);
            await _dialog.ShowAndWaitClickAsync(parryStep.Key, parryStep.Placement, parryStep.Mascot, cancellationToken);
            _param.ModalSetter(false);
            _dialog.Hide();
        }

        async UniTask RunNudgeGuideAsync(CancellationToken cancellationToken)
        {
            TutorialStepCatalogData.Step nudgeStep = _catalog.GetStep(TutorialStepCatalogData.EStep.PlayNudge);
            _param.ModalSetter(true);
            _highlighter.Apply(nudgeStep.Highlight);
            LocalizedTextRef nudgeText = new LocalizedTextRef(nudgeStep.Key, new object[] { GetKeyName(EKeyAction.Nudge) });
            await _dialog.ShowAndWaitClickAsync(nudgeText, nudgeStep.Placement, nudgeStep.Mascot, cancellationToken);
            _param.ModalSetter(false);
            _dialog.Hide();

            if (!_param.CanNudgeGetter() || !_param.IsPlayStateGetter())
                return;

            TutorialStepCatalogData.Step tryStep = _catalog.GetStep(TutorialStepCatalogData.EStep.PlayNudgeTry);
            _param.NudgeWaitPreparer();
            _param.TimeFreezeSetter(true);
            _highlighter.Apply(tryStep.Highlight);
            LocalizedTextRef tryText = new LocalizedTextRef(tryStep.Key, new object[] { GetKeyName(EKeyAction.Nudge) });
            _dialog.ShowGuide(tryText, tryStep.Placement, tryStep.Mascot);
            await UniTask.WaitUntil(GetIsNudgeWaitFinished, cancellationToken: cancellationToken);
            _param.TimeFreezeSetter(false);
            _dialog.Hide();

            if (_param.HasNudgedGetter() && _param.IsPlayStateGetter())
                await UniTask.Delay(TimeSpan.FromSeconds(NUDGE_SETTLE_SECONDS), cancellationToken: cancellationToken);
        }

        static bool HasActiveGimmick(MapRoot map, MapInteractable.EGimmickType gimmickType)
        {
            for (int i = 0, count = map.GetInteractableCount(); i < count; ++i)
            {
                MapInteractable interactable = map.GetInteractable(i);
                if (null == interactable || interactable.GimmickType != gimmickType)
                    continue;

                if (!interactable.IsDeactivated)
                    return true;
            }

            return false;
        }

        bool GetIsPlayInterrupted()
        {
            if (_param.IsLastBallLostGetter() || _param.IsStageClearInterceptedGetter())
                return true;

            return !_param.IsPlayStateGetter();
        }

        bool GetIsLaunchWaitFinished()
        {
            return _param.HasLaunchedGetter() || GetIsPlayInterrupted();
        }

        bool GetIsNudgeTriggerReached()
        {
            return !_hasSlingshot || _param.HasSlingshotHitGetter();
        }

        bool GetIsFlipperTriggerReached()
        {
            MapRoot map = _param.MapGetter();
            return null != map && map.IsHoldingBallOnFlipperApproach();
        }

        bool GetIsPlayGuideWaitFinished()
        {
            if (GetIsPlayInterrupted() || _param.IsBallLostGetter())
                return true;

            if (_isNudgeGuidePending && GetIsNudgeTriggerReached())
                return true;

            return _isFlipperGuidePending && GetIsFlipperTriggerReached();
        }

        bool GetIsFlipWaitFinished()
        {
            return _param.HasBallStruckGetter() || GetIsPlayInterrupted();
        }

        bool GetIsComboWaitFinished()
        {
            return _param.IsComboTriggerReachedGetter() || _param.IsBallLostGetter() || GetIsPlayInterrupted();
        }

        bool GetIsNudgeWaitFinished()
        {
            return _param.HasNudgedGetter() || !_param.IsPlayStateGetter();
        }

        static string GetKeyName(EKeyAction action)
        {
            return KeyLabel.Get(GameSettings.GetKey(action));
        }
    }
}
