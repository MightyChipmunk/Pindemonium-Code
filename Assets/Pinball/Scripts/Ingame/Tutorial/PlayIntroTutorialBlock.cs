using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.Ingame.Map;
using Pinball.Localization;
using Pinball.Settings;
using Pinball.UI;
using UnityEngine;

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
            Func<bool> IsComboTriggerReachedGetter,
            Func<bool> HasBallLostGetter,
            Func<bool> IsStageClearInterceptedGetter,
            Func<bool> HasNudgedGetter,
            Func<bool> CanNudgeGetter,
            Action NudgeWaitPreparer,
            Func<RectTransform> ScoreAnchorGetter,
            Func<RectTransform> ComboAnchorGetter);

        const float WORLD_FOCUS_PADDING = 14f;
        const float NUDGE_SETTLE_SECONDS = 1f;
        const string SECOND_PAGE_SUFFIX = ".2";
        const string STAGE_SELECT_KEY = "ui.tutorial.play.stage_select";
        const string STAGE_START_KEY = "ui.tutorial.play.stage_start";
        const string SCORE_KEY = "ui.tutorial.play.score";
        const string FLIPPER_KEY = "ui.tutorial.play.flipper";
        const string PARRY_KEY = "ui.tutorial.play.parry";
        const string SPRING_KEY = "ui.tutorial.play.spring";
        const string LAUNCH_GUIDE_KEY = "ui.tutorial.play.launch_guide";
        const string COMBO_KEY = "ui.tutorial.play.combo";
        const string NUDGE_KEY = "ui.tutorial.play.nudge";
        const string NUDGE_TRY_KEY = "ui.tutorial.play.nudge_try";
        const string FREE_PLAY_KEY = "ui.tutorial.play.free_play";
        const string GIMMICK_BUMPER_KEY = "ui.tutorial.play.gimmick.bumper";
        const string GIMMICK_SLINGSHOT_KEY = "ui.tutorial.play.gimmick.slingshot";
        const string GIMMICK_SPINNER_KEY = "ui.tutorial.play.gimmick.spinner";
        const string GIMMICK_DROP_TARGET_KEY = "ui.tutorial.play.gimmick.drop_target";
        const string GIMMICK_STANDUP_TARGET_KEY = "ui.tutorial.play.gimmick.standup_target";
        const string GIMMICK_LAMP_BANK_KEY = "ui.tutorial.play.gimmick.lamp_bank";
        const string GIMMICK_LANE_KEY = "ui.tutorial.play.gimmick.lane";
        const string GIMMICK_SAUCER_KEY = "ui.tutorial.play.gimmick.saucer";

        static readonly MapInteractable.EGimmickType[] TOUR_ORDER =
        {
            MapInteractable.EGimmickType.Bumper,
            MapInteractable.EGimmickType.Slingshot,
            MapInteractable.EGimmickType.Spinner,
            MapInteractable.EGimmickType.DropTarget,
            MapInteractable.EGimmickType.StandupTarget,
            MapInteractable.EGimmickType.LampBank,
            MapInteractable.EGimmickType.Lane,
            MapInteractable.EGimmickType.Saucer,
        };

        static readonly Vector2 FALLBACK_FLIPPER_EXTENTS = new Vector2(1.1f, 0.6f);
        static readonly Vector2 FALLBACK_SPRING_EXTENTS = new Vector2(0.5f, 1.2f);
        static readonly Vector2 FALLBACK_GIMMICK_EXTENTS = new Vector2(0.6f, 0.6f);

        readonly InitParam _param;
        readonly TutorialDimmer _dimmer;
        readonly TutorialDialogView _dialog;
        readonly TutorialHighlightLayer _highlightLayer;
        readonly List<Bounds> _worldBoundsBuffer = new List<Bounds>();
        readonly List<MapInteractable.SpriteSnapshot> _spriteBuffer = new List<MapInteractable.SpriteSnapshot>();
        readonly List<MapInteractable.LineSnapshot> _lineBuffer = new List<MapInteractable.LineSnapshot>();

        public PlayIntroTutorialBlock(InitParam param, TutorialDimmer dimmer, TutorialDialogView dialog, TutorialHighlightLayer highlightLayer)
        {
            _param = param;
            _dimmer = dimmer;
            _dialog = dialog;
            _highlightLayer = highlightLayer;
        }

        public async UniTask<bool> RunAsync(CancellationToken cancellationToken)
        {
            await RunStageSelectIntroAsync(cancellationToken);

            await UniTask.WaitUntil(GetIsPlayReady, cancellationToken: cancellationToken);
            await UniTask.NextFrame(cancellationToken);

            _param.ModalSetter(true);
            FocusUI(_param.ScoreAnchorGetter());
            await _dialog.ShowAndWaitClickAsync(SCORE_KEY, TutorialDialogView.EPlacement.Bottom, cancellationToken);

            await RunGimmickTourAsync(cancellationToken);
            await RunControlIntroAsync(cancellationToken);
            _param.ModalSetter(false);

            await RunLaunchGuideAsync(cancellationToken);

            await UniTask.WaitUntil(GetIsFreePlayInterrupted, cancellationToken: cancellationToken);
            if (!_param.IsPlayStateGetter())
                return false;

            _param.ModalSetter(true);
            if (_param.IsComboTriggerReachedGetter())
                FocusUI(_param.ComboAnchorGetter());
            else
                ShowFullDim();

            await _dialog.ShowAndWaitClickAsync(COMBO_KEY, TutorialDialogView.EPlacement.Center, cancellationToken);
            HideOverlay();
            _param.ModalSetter(false);

            await RunNudgeGuideAsync(cancellationToken);

            _param.ModalSetter(true);
            await _dialog.ShowAndWaitClickAsync(FREE_PLAY_KEY, TutorialDialogView.EPlacement.Center, cancellationToken);
            _param.ModalSetter(false);

            HideOverlay();
            _dialog.Hide();
            return true;
        }

        async UniTask RunStageSelectIntroAsync(CancellationToken cancellationToken)
        {
            StageSelectView view = UIManager.GetActive(UIViewCatalogData.EViewType.StageSelect) as StageSelectView;
            if (null == view)
                return;

            FocusUI(view.GetCurrentCardAnchor());
            await _dialog.ShowAndWaitClickAsync(STAGE_SELECT_KEY, TutorialDialogView.EPlacement.Top, cancellationToken);

            FocusUI(view.GetCurrentStartAnchor());
            await _dialog.ShowAndWaitClickAsync(STAGE_START_KEY, TutorialDialogView.EPlacement.Top, cancellationToken);

            HideOverlay();
            _dialog.Hide();
        }

        bool GetIsPlayReady()
        {
            return _param.IsPlayReadyGetter();
        }

        async UniTask RunGimmickTourAsync(CancellationToken cancellationToken)
        {
            MapRoot map = _param.MapGetter();
            if (null == map)
                return;

            for (int i = 0; i < TOUR_ORDER.Length; ++i)
            {
                MapInteractable.EGimmickType gimmickType = TOUR_ORDER[i];
                CollectGimmickTargets(map, gimmickType);
                if (_worldBoundsBuffer.Count <= 0 && _spriteBuffer.Count <= 0)
                    continue;

                FocusWorldTargets();

                TutorialDialogView.EPlacement placement;
                if (gimmickType == MapInteractable.EGimmickType.Slingshot)
                    placement = TutorialDialogView.EPlacement.Top;
                else
                    placement = TutorialDialogView.EPlacement.Bottom;

                string gimmickKey = GetGimmickKey(gimmickType);
                await _dialog.ShowAndWaitClickAsync(gimmickKey, placement, cancellationToken);

                string secondPageKey = gimmickKey + SECOND_PAGE_SUFFIX;
                if (!string.IsNullOrEmpty(LocalizationTable.Get(secondPageKey, string.Empty)))
                    await _dialog.ShowAndWaitClickAsync(secondPageKey, placement, cancellationToken);
            }
        }

        async UniTask RunControlIntroAsync(CancellationToken cancellationToken)
        {
            MapRoot map = _param.MapGetter();
            if (null == map)
                return;

            _worldBoundsBuffer.Clear();
            _worldBoundsBuffer.Add(GetFlipperBounds(map, true));
            _worldBoundsBuffer.Add(GetFlipperBounds(map, false));
            _spriteBuffer.Clear();
            _lineBuffer.Clear();
            map.CollectLeftFlipperSprites(_spriteBuffer);
            map.CollectRightFlipperSprites(_spriteBuffer);
            FocusWorldTargets();
            LocalizedTextRef flipperText = new LocalizedTextRef(FLIPPER_KEY, new object[] { GetKeyName(EKeyAction.FlipperLeft), GetKeyName(EKeyAction.FlipperRight) });
            await _dialog.ShowAndWaitClickAsync(flipperText, TutorialDialogView.EPlacement.Top, cancellationToken);

            await _dialog.ShowAndWaitClickAsync(PARRY_KEY, TutorialDialogView.EPlacement.Top, cancellationToken);

            FocusSpring(map);
            LocalizedTextRef springText = new LocalizedTextRef(SPRING_KEY, new object[] { GetKeyName(EKeyAction.Launch) });
            await _dialog.ShowAndWaitClickAsync(springText, TutorialDialogView.EPlacement.Top, cancellationToken);
        }

        async UniTask RunLaunchGuideAsync(CancellationToken cancellationToken)
        {
            MapRoot map = _param.MapGetter();
            if (null != map)
                FocusSpring(map);

            LocalizedTextRef guideText = new LocalizedTextRef(LAUNCH_GUIDE_KEY, new object[] { GetKeyName(EKeyAction.Launch) });
            _dialog.ShowGuide(guideText, TutorialDialogView.EPlacement.Top);
            await UniTask.WaitUntil(GetIsLaunchWaitFinished, cancellationToken: cancellationToken);
            HideOverlay();
            _dialog.Hide();
        }

        async UniTask RunNudgeGuideAsync(CancellationToken cancellationToken)
        {
            if (!_param.CanNudgeGetter() || !_param.IsPlayStateGetter())
            {
                _param.ModalSetter(true);
                LocalizedTextRef nudgeText = new LocalizedTextRef(NUDGE_KEY, new object[] { GetKeyName(EKeyAction.Nudge) });
                await _dialog.ShowAndWaitClickAsync(nudgeText, TutorialDialogView.EPlacement.Top, cancellationToken);
                _param.ModalSetter(false);
                _dialog.Hide();
                return;
            }

            _param.NudgeWaitPreparer();
            _param.TimeFreezeSetter(true);
            LocalizedTextRef tryText = new LocalizedTextRef(NUDGE_TRY_KEY, new object[] { GetKeyName(EKeyAction.Nudge) });
            _dialog.ShowGuide(tryText, TutorialDialogView.EPlacement.Top);
            await UniTask.WaitUntil(GetIsNudgeWaitFinished, cancellationToken: cancellationToken);
            _param.TimeFreezeSetter(false);
            _dialog.Hide();

            if (_param.HasNudgedGetter() && _param.IsPlayStateGetter())
                await UniTask.Delay(TimeSpan.FromSeconds(NUDGE_SETTLE_SECONDS), cancellationToken: cancellationToken);
        }

        void FocusSpring(MapRoot map)
        {
            _worldBoundsBuffer.Clear();
            if (map.TryGetSpringBounds(out Bounds springBounds))
                _worldBoundsBuffer.Add(springBounds);
            else
                _worldBoundsBuffer.Add(CreateFallbackBounds(map.GetSpringPosition(), FALLBACK_SPRING_EXTENTS));

            _spriteBuffer.Clear();
            _lineBuffer.Clear();
            map.CollectSpringSprites(_spriteBuffer);
            FocusWorldTargets();
        }

        void CollectGimmickTargets(MapRoot map, MapInteractable.EGimmickType gimmickType)
        {
            _worldBoundsBuffer.Clear();
            _spriteBuffer.Clear();
            _lineBuffer.Clear();
            for (int i = 0, count = map.GetInteractableCount(); i < count; ++i)
            {
                MapInteractable interactable = map.GetInteractable(i);
                if (null == interactable || interactable.GimmickType != gimmickType)
                    continue;

                if (interactable.IsDeactivated)
                    continue;

                if (gimmickType == MapInteractable.EGimmickType.Lane)
                    interactable.CollectHighlightLines(_lineBuffer);
                else
                    interactable.CollectHighlightSprites(_spriteBuffer);

                if (interactable.TryGetWorldBounds(out Bounds worldBounds))
                    _worldBoundsBuffer.Add(worldBounds);
                else
                    _worldBoundsBuffer.Add(CreateFallbackBounds(interactable.transform.position, FALLBACK_GIMMICK_EXTENTS));
            }
        }

        void FocusWorldTargets()
        {
            if (_spriteBuffer.Count > 0 || _lineBuffer.Count > 0)
            {
                _dimmer.ShowFull();
                _highlightLayer.ShowWorldTargets(_spriteBuffer, _lineBuffer);
                return;
            }

            _highlightLayer.Clear();
            _dimmer.FocusWorldBounds(_worldBoundsBuffer, WORLD_FOCUS_PADDING);
        }

        void FocusUI(RectTransform target)
        {
            _dimmer.ShowFull();
            _highlightLayer.ShowUITarget(target);
        }

        void ShowFullDim()
        {
            _highlightLayer.Clear();
            _dimmer.ShowFull();
        }

        void HideOverlay()
        {
            _highlightLayer.Clear();
            _dimmer.Hide();
        }

        static Bounds GetFlipperBounds(MapRoot map, bool isLeft)
        {
            if (isLeft && map.TryGetLeftFlipperBounds(out Bounds leftBounds))
                return leftBounds;

            if (!isLeft && map.TryGetRightFlipperBounds(out Bounds rightBounds))
                return rightBounds;

            Vector2 fallbackCenter = isLeft ? map.GetLeftFlipperPosition() : map.GetRightFlipperPosition();
            return CreateFallbackBounds(fallbackCenter, FALLBACK_FLIPPER_EXTENTS);
        }

        static Bounds CreateFallbackBounds(Vector2 worldCenter, Vector2 worldExtents)
        {
            return new Bounds(worldCenter, worldExtents * 2f);
        }

        bool GetIsFreePlayInterrupted()
        {
            if (_param.IsComboTriggerReachedGetter() || _param.HasBallLostGetter())
                return true;

            if (_param.IsStageClearInterceptedGetter())
                return true;

            return !_param.IsPlayStateGetter();
        }

        bool GetIsLaunchWaitFinished()
        {
            return _param.HasLaunchedGetter() || GetIsFreePlayInterrupted();
        }

        bool GetIsNudgeWaitFinished()
        {
            return _param.HasNudgedGetter() || !_param.IsPlayStateGetter();
        }

        static string GetKeyName(EKeyAction action)
        {
            return GameSettings.GetKey(action).ToString().ToUpperInvariant();
        }

        static string GetGimmickKey(MapInteractable.EGimmickType gimmickType)
        {
            switch (gimmickType)
            {
                case MapInteractable.EGimmickType.Bumper:
                    return GIMMICK_BUMPER_KEY;
                case MapInteractable.EGimmickType.Slingshot:
                    return GIMMICK_SLINGSHOT_KEY;
                case MapInteractable.EGimmickType.Spinner:
                    return GIMMICK_SPINNER_KEY;
                case MapInteractable.EGimmickType.DropTarget:
                    return GIMMICK_DROP_TARGET_KEY;
                case MapInteractable.EGimmickType.StandupTarget:
                    return GIMMICK_STANDUP_TARGET_KEY;
                case MapInteractable.EGimmickType.LampBank:
                    return GIMMICK_LAMP_BANK_KEY;
                case MapInteractable.EGimmickType.Lane:
                    return GIMMICK_LANE_KEY;
                default:
                    return GIMMICK_SAUCER_KEY;
            }
        }
    }
}
