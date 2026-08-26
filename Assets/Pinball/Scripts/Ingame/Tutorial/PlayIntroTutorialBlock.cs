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
            Func<bool> IsPlayReadyGetter,
            Func<bool> IsPlayStateGetter,
            Func<bool> HasLaunchedGetter,
            Func<bool> IsComboTriggerReachedGetter,
            Func<bool> HasBallLostGetter,
            Func<bool> IsStageClearInterceptedGetter,
            Func<RectTransform> ScoreAnchorGetter,
            Func<RectTransform> ComboAnchorGetter);

        const float UI_FOCUS_PADDING = 12f;
        const float WORLD_FOCUS_PADDING = 14f;
        const string STAGE_SELECT_KEY = "ui.tutorial.play.stage_select";
        const string SCORE_KEY = "ui.tutorial.play.score";
        const string FLIPPER_KEY = "ui.tutorial.play.flipper";
        const string PARRY_KEY = "ui.tutorial.play.parry";
        const string SPRING_KEY = "ui.tutorial.play.spring";
        const string LAUNCH_GUIDE_KEY = "ui.tutorial.play.launch_guide";
        const string COMBO_KEY = "ui.tutorial.play.combo";
        const string NUDGE_KEY = "ui.tutorial.play.nudge";
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
        readonly List<Bounds> _worldBoundsBuffer = new List<Bounds>();

        public PlayIntroTutorialBlock(InitParam param, TutorialDimmer dimmer, TutorialDialogView dialog)
        {
            _param = param;
            _dimmer = dimmer;
            _dialog = dialog;
        }

        public async UniTask<bool> RunAsync(CancellationToken cancellationToken)
        {
            await RunStageSelectIntroAsync(cancellationToken);

            await UniTask.WaitUntil(GetIsPlayReady, cancellationToken: cancellationToken);
            await UniTask.NextFrame(cancellationToken);

            _param.ModalSetter(true);
            _dimmer.FocusUI(_param.ScoreAnchorGetter(), UI_FOCUS_PADDING);
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
                _dimmer.FocusUI(_param.ComboAnchorGetter(), UI_FOCUS_PADDING);
            else
                _dimmer.ShowFull();

            await _dialog.ShowAndWaitClickAsync(COMBO_KEY, TutorialDialogView.EPlacement.Center, cancellationToken);

            _dimmer.ShowFull();
            LocalizedTextRef nudgeText = new LocalizedTextRef(NUDGE_KEY, new object[] { GetKeyName(EKeyAction.Nudge) });
            await _dialog.ShowAndWaitClickAsync(nudgeText, TutorialDialogView.EPlacement.Center, cancellationToken);
            _param.ModalSetter(false);

            _dimmer.Hide();
            _dialog.Hide();
            return true;
        }

        async UniTask RunStageSelectIntroAsync(CancellationToken cancellationToken)
        {
            StageSelectView view = UIManager.GetActive(UIViewCatalogData.EViewType.StageSelect) as StageSelectView;
            if (null == view)
                return;

            _dimmer.FocusUI(view.GetCurrentCardAnchor(), UI_FOCUS_PADDING);
            await _dialog.ShowAndWaitClickAsync(STAGE_SELECT_KEY, TutorialDialogView.EPlacement.Bottom, cancellationToken);
            _dimmer.Hide();
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
                CollectGimmickBounds(map, gimmickType);
                if (_worldBoundsBuffer.Count <= 0)
                    continue;

                _dimmer.FocusWorldBounds(_worldBoundsBuffer, WORLD_FOCUS_PADDING);
                await _dialog.ShowAndWaitClickAsync(GetGimmickKey(gimmickType), TutorialDialogView.EPlacement.Bottom, cancellationToken);
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
            _dimmer.FocusWorldBounds(_worldBoundsBuffer, WORLD_FOCUS_PADDING);
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
            _dimmer.Hide();
            _dialog.Hide();
        }

        void FocusSpring(MapRoot map)
        {
            _worldBoundsBuffer.Clear();
            if (map.TryGetSpringBounds(out Bounds springBounds))
                _worldBoundsBuffer.Add(springBounds);
            else
                _worldBoundsBuffer.Add(CreateFallbackBounds(map.GetSpringPosition(), FALLBACK_SPRING_EXTENTS));

            _dimmer.FocusWorldBounds(_worldBoundsBuffer, WORLD_FOCUS_PADDING);
        }

        void CollectGimmickBounds(MapRoot map, MapInteractable.EGimmickType gimmickType)
        {
            _worldBoundsBuffer.Clear();
            for (int i = 0, count = map.GetInteractableCount(); i < count; ++i)
            {
                MapInteractable interactable = map.GetInteractable(i);
                if (null == interactable || interactable.GimmickType != gimmickType)
                    continue;

                if (interactable.IsDeactivated)
                    continue;

                if (interactable.TryGetWorldBounds(out Bounds worldBounds))
                    _worldBoundsBuffer.Add(worldBounds);
                else
                    _worldBoundsBuffer.Add(CreateFallbackBounds(interactable.transform.position, FALLBACK_GIMMICK_EXTENTS));
            }
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
