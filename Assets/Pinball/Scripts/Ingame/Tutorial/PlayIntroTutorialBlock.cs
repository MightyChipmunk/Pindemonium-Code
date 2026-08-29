using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.Ingame.Map;
using Pinball.Localization;
using Pinball.Settings;
using Pinball.UI;

namespace Pinball.Ingame.Tutorial
{
    public class PlayIntroTutorialBlock
    {
        public record InitParam(
            Func<MapRoot> MapGetter,
            Action<bool> ModalSetter,
            Action<bool> TimeFreezeSetter,
            Func<bool> IsPlayReadyGetter,
            Func<bool> IsPlayStateGetter,
            Func<bool> HasLaunchedGetter,
            Func<bool> HasSlingshotHitGetter,
            Func<bool> HasBallStruckGetter,
            Func<bool> IsBallLostGetter,
            Func<bool> IsComboTriggerReachedGetter,
            Func<bool> IsLastBallLostGetter,
            Func<bool> IsStageClearInterceptedGetter,
            Func<bool> HasNudgedGetter,
            Func<bool> CanNudgeGetter,
            Action NudgeWaitPreparer,
            Action FlipWaitPreparer,
            Action LaunchWaitPreparer,
            Action ComboWaitPreparer,
            float ComboTimeoutSeconds);

        const float NUDGE_SETTLE_SECONDS = 1f;
        const float SLINGSHOT_HIT_DELAY_SECONDS = 0.3f;
        const string SECOND_PAGE_SUFFIX = ".2";

        static readonly TutorialStepCatalogData.EStep[] TOUR_ORDER =
        {
            TutorialStepCatalogData.EStep.PlayGimmickBumper,
            TutorialStepCatalogData.EStep.PlayGimmickSlingshot,
            TutorialStepCatalogData.EStep.PlayGimmickSpinner,
            TutorialStepCatalogData.EStep.PlayGimmickDropTarget,
            TutorialStepCatalogData.EStep.PlayGimmickStandupTarget,
            TutorialStepCatalogData.EStep.PlayGimmickLampBank,
            TutorialStepCatalogData.EStep.PlayGimmickLane,
            TutorialStepCatalogData.EStep.PlayGimmickSaucer,
        };

        readonly InitParam _param;
        readonly TutorialStepCatalogData _catalog;
        readonly TutorialDialogView _dialog;
        readonly TutorialHighlighter _highlighter;
        bool _isNudgeGuidePending = false;
        bool _isFlipperGuidePending = false;
        bool _hasSlingshot = false;
        bool _hasBallLostGuideShown = false;

        public PlayIntroTutorialBlock(InitParam param, TutorialStepCatalogData catalog, TutorialDialogView dialog, TutorialHighlighter highlighter)
        {
            _param = param;
            _catalog = catalog;
            _dialog = dialog;
            _highlighter = highlighter;
        }

        public async UniTask<bool> RunAsync(CancellationToken cancellationToken)
        {
            await ScreenTransition.WaitUntilIdleAsync(cancellationToken);
            await RunStageSelectIntroAsync(cancellationToken);

            await UniTask.WaitUntil(GetIsPlayReady, cancellationToken: cancellationToken);
            await UniTask.NextFrame(cancellationToken);

            _param.ModalSetter(true);
            TutorialStepCatalogData.Step scoreStep = _catalog.GetStep(TutorialStepCatalogData.EStep.PlayScore);
            _highlighter.Apply(scoreStep.Highlight);
            await _dialog.ShowAndWaitClickAsync(scoreStep.Key, scoreStep.Placement, scoreStep.Mascot, cancellationToken);

            await RunGimmickTourAsync(cancellationToken);
            await RunControlIntroAsync(cancellationToken);
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

                if (!_param.IsBallLostGetter())
                    break;

                await RunBallLostGuideAsync(cancellationToken);
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

            if (!_hasBallLostGuideShown)
            {
                TutorialStepCatalogData.Step noticeStep = _catalog.GetStep(TutorialStepCatalogData.EStep.PlayBallLostNotice);
                _highlighter.Apply(noticeStep.Highlight);
                LocalizedTextRef noticeText = new LocalizedTextRef(noticeStep.Key, new object[] { GetKeyName(EKeyAction.Launch) });
                await _dialog.ShowAndWaitClickAsync(noticeText, noticeStep.Placement, noticeStep.Mascot, cancellationToken);
            }

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
            TutorialStepCatalogData.Step welcomeStep = _catalog.GetStep(TutorialStepCatalogData.EStep.PlayWelcome);
            _highlighter.Apply(welcomeStep.Highlight);
            await _dialog.ShowAndWaitClickAsync(welcomeStep.Key, welcomeStep.Placement, welcomeStep.Mascot, cancellationToken);

            StageSelectView view = UIManager.GetActive(UIViewCatalogData.EViewType.StageSelect) as StageSelectView;
            if (null == view)
            {
                _highlighter.Clear();
                _dialog.Hide();
                return;
            }

            await UniTask.WaitWhile(view.IsTransitionPlaying, cancellationToken: cancellationToken);

            TutorialStepCatalogData.Step cardStep = _catalog.GetStep(TutorialStepCatalogData.EStep.PlayStageSelectCard);
            _highlighter.Apply(cardStep.Highlight);
            await _dialog.ShowAndWaitClickAsync(cardStep.Key, cardStep.Placement, cardStep.Mascot, cancellationToken);

            TutorialStepCatalogData.Step startStep = _catalog.GetStep(TutorialStepCatalogData.EStep.PlayStageSelectStart);
            _highlighter.Apply(startStep.Highlight);
            await _dialog.ShowAndWaitClickAsync(startStep.Key, startStep.Placement, startStep.Mascot, cancellationToken);

            _highlighter.Clear();
            _dialog.Hide();
        }

        bool GetIsPlayReady()
        {
            return _param.IsPlayReadyGetter();
        }

        async UniTask RunGimmickTourAsync(CancellationToken cancellationToken)
        {
            for (int i = 0; i < TOUR_ORDER.Length; ++i)
            {
                TutorialStepCatalogData.Step step = _catalog.GetStep(TOUR_ORDER[i]);
                if (!_highlighter.Apply(step.Highlight))
                    continue;

                await _dialog.ShowAndWaitClickAsync(step.Key, step.Placement, step.Mascot, cancellationToken);

                string secondPageKey = step.Key + SECOND_PAGE_SUFFIX;
                if (!string.IsNullOrEmpty(LocalizationTable.Get(secondPageKey, string.Empty)))
                    await _dialog.ShowAndWaitClickAsync(secondPageKey, step.Placement, step.Mascot, cancellationToken);
            }
        }

        async UniTask RunControlIntroAsync(CancellationToken cancellationToken)
        {
            TutorialStepCatalogData.Step step = _catalog.GetStep(TutorialStepCatalogData.EStep.PlaySpring);
            if (!_highlighter.Apply(step.Highlight))
                return;

            LocalizedTextRef springText = new LocalizedTextRef(step.Key, new object[] { GetKeyName(EKeyAction.Launch) });
            await _dialog.ShowAndWaitClickAsync(springText, step.Placement, step.Mascot, cancellationToken);
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
            if (null != map)
                map.ArmFlipperApproachZone();

            while (_isNudgeGuidePending || _isFlipperGuidePending)
            {
                await UniTask.WaitUntil(GetIsPlayGuideWaitFinished, cancellationToken: cancellationToken);
                if (!_param.IsPlayStateGetter())
                    return;

                if (_param.IsBallLostGetter())
                {
                    await RunBallLostGuideAsync(cancellationToken);
                    continue;
                }

                if (GetIsPlayInterrupted())
                    return;

                if (_isNudgeGuidePending && GetIsNudgeTriggerReached())
                {
                    _isNudgeGuidePending = false;
                    await DelayAfterSlingshotHitAsync(cancellationToken);
                    await RunNudgeGuideAsync(cancellationToken);
                    continue;
                }

                _isFlipperGuidePending = false;
                await RunFlipperGuideAsync(cancellationToken);
            }
        }

        async UniTask RunBallLostGuideAsync(CancellationToken cancellationToken)
        {
            _hasBallLostGuideShown = true;
            TutorialStepCatalogData.Step step = _catalog.GetStep(TutorialStepCatalogData.EStep.PlayBallLost);
            _highlighter.Apply(step.Highlight);
            _param.LaunchWaitPreparer();
            LocalizedTextRef lostText = new LocalizedTextRef(step.Key, new object[] { GetKeyName(EKeyAction.Launch) });
            _dialog.ShowGuide(lostText, step.Placement, step.Mascot);
            await UniTask.WaitUntil(GetIsRelaunchWaitFinished, cancellationToken: cancellationToken);
            _dialog.Hide();
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

        bool GetIsRelaunchWaitFinished()
        {
            if (_param.HasLaunchedGetter() || _param.IsStageClearInterceptedGetter())
                return true;

            return !_param.IsPlayStateGetter();
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
