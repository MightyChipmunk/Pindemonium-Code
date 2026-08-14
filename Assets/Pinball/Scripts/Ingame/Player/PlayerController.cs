using System;
using Pinball.Ingame.InputModule;
using Pinball.Ingame.Map;
using UnityEngine;

namespace Pinball.Ingame
{
    public class PlayerController
    {
        public record InitParam(
            IngameInputRouter InputRouter,
            Func<bool> IsControlBlockedGetter,
            Func<bool> IsPlayingGetter,
            Func<Vector2, bool> NudgeImpulseApplier,
            Action NudgePresenter);

        const float NUDGE_COOLDOWN_SECONDS = 3f;
        const float NUDGE_IMPULSE_POWER = 2.5f;
        const float FULL_CIRCLE_RADIAN = 6.2831853f;

        IngameInputRouter _inputRouter = null;
        MapRoot _map = null;
        Func<bool> _isControlBlockedGetter = null;
        Func<bool> _isPlayingGetter = null;
        Func<Vector2, bool> _nudgeImpulseApplier = null;
        Action _nudgePresenter = null;
        float _remainingNudgeCooldown = 0f;

        public event Action OnLaunched;

        public void Init(InitParam param)
        {
            _inputRouter = param.InputRouter;
            _isControlBlockedGetter = param.IsControlBlockedGetter;
            _isPlayingGetter = param.IsPlayingGetter;
            _nudgeImpulseApplier = param.NudgeImpulseApplier;
            _nudgePresenter = param.NudgePresenter;
            _remainingNudgeCooldown = 0f;
            _inputRouter.OnFlipperLeftChanged += HandleFlipperLeftChanged;
            _inputRouter.OnFlipperRightChanged += HandleFlipperRightChanged;
            _inputRouter.OnLaunchChanged += HandleLaunchChanged;
            _inputRouter.OnNudgePressed += HandleNudgePressed;
        }

        public void Release()
        {
            if (null != _inputRouter)
            {
                _inputRouter.OnFlipperLeftChanged -= HandleFlipperLeftChanged;
                _inputRouter.OnFlipperRightChanged -= HandleFlipperRightChanged;
                _inputRouter.OnLaunchChanged -= HandleLaunchChanged;
                _inputRouter.OnNudgePressed -= HandleNudgePressed;
                _inputRouter = null;
            }

            OnLaunched = null;
            _isControlBlockedGetter = null;
            _isPlayingGetter = null;
            _nudgeImpulseApplier = null;
            _nudgePresenter = null;
            _map = null;
        }

        public void SetMap(MapRoot map)
        {
            _map = map;
        }

        public void Tick(float deltaTime)
        {
            if (_remainingNudgeCooldown <= 0f)
                return;

            _remainingNudgeCooldown -= deltaTime;
            if (_remainingNudgeCooldown < 0f)
                _remainingNudgeCooldown = 0f;
        }

        public void ResetNudgeCooldown()
        {
            _remainingNudgeCooldown = 0f;
        }

        public void ResetControlStates()
        {
            if (null == _map)
                return;

            _map.SetFlipperPressed(Flipper.ESide.Left, false);
            _map.SetFlipperPressed(Flipper.ESide.Right, false);
            _map.SetSpringCharging(false);
        }

        void HandleFlipperLeftChanged(bool isPressed)
        {
            SetFlipperPressed(Flipper.ESide.Left, isPressed);
        }

        void HandleFlipperRightChanged(bool isPressed)
        {
            SetFlipperPressed(Flipper.ESide.Right, isPressed);
        }

        void SetFlipperPressed(Flipper.ESide side, bool isPressed)
        {
            if (IsControlBlocked())
                return;

            if (null == _map)
                return;

            _map.SetFlipperPressed(side, isPressed);
        }

        void HandleLaunchChanged(bool isPressed)
        {
            if (IsControlBlocked())
                return;

            if (null == _map)
                return;

            if (isPressed)
            {
                _map.SetSpringCharging(true);
                return;
            }

            if (_map.TryLaunch())
                OnLaunched?.Invoke();
        }

        void HandleNudgePressed()
        {
            if (_remainingNudgeCooldown > 0f)
                return;

            if (IsControlBlocked())
                return;

            if (!IsPlaying())
                return;

            if (null == _nudgeImpulseApplier)
                return;

            if (!_nudgeImpulseApplier(CreateRandomNudgeImpulse()))
                return;

            _remainingNudgeCooldown = NUDGE_COOLDOWN_SECONDS;
            _nudgePresenter?.Invoke();
        }

        static Vector2 CreateRandomNudgeImpulse()
        {
            float angle = UnityEngine.Random.Range(0f, FULL_CIRCLE_RADIAN);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * NUDGE_IMPULSE_POWER;
        }

        bool IsControlBlocked()
        {
            if (null == _isControlBlockedGetter)
                return false;

            return _isControlBlockedGetter();
        }

        bool IsPlaying()
        {
            if (null == _isPlayingGetter)
                return false;

            return _isPlayingGetter();
        }
    }
}
