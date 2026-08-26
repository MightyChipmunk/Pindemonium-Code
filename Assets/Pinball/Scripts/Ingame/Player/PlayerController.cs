using System;
using Pinball.Ingame.Input;
using Pinball.Ingame.Map;
using Pinball.Ingame.Stat;
using UnityEngine;

namespace Pinball.Ingame.Player
{
    public class PlayerController : IDisposable
    {
        public record InitParam(
            Func<bool> IsControlBlockedGetter,
            Func<bool> IsPlayingGetter,
            Func<bool> IsNudgeTargetReadyGetter,
            Func<Vector2, bool> NudgeImpulseApplier,
            Action NudgePresenter,
            Action<bool> NudgeAimPresenter,
            Action<Vector2> NudgeAimDirectionPresenter);

        IngameInputRouter _inputRouter = null;
        MapRoot _map = null;
        Func<bool> _isControlBlockedGetter = null;
        Func<bool> _isPlayingGetter = null;
        Func<bool> _isNudgeTargetReadyGetter = null;
        Func<Vector2, bool> _nudgeImpulseApplier = null;
        Action _nudgePresenter = null;
        Action<bool> _nudgeAimPresenter = null;
        Action<Vector2> _nudgeAimDirectionPresenter = null;
        float _remainingNudgeCooldown = 0f;
        bool _isNudgeAiming = false;
        Vector2 _nudgeAimDirection = Vector2.up;
        float _nudgeAimElapsed = 0f;

        public event Action<float> OnLaunched;
        public event Action OnNudged;

        public PlayerController(InitParam param, IngameInputRouter inputRouter)
        {
            _inputRouter = inputRouter;
            _isControlBlockedGetter = param.IsControlBlockedGetter;
            _isPlayingGetter = param.IsPlayingGetter;
            _isNudgeTargetReadyGetter = param.IsNudgeTargetReadyGetter;
            _nudgeImpulseApplier = param.NudgeImpulseApplier;
            _nudgePresenter = param.NudgePresenter;
            _nudgeAimPresenter = param.NudgeAimPresenter;
            _nudgeAimDirectionPresenter = param.NudgeAimDirectionPresenter;
            _remainingNudgeCooldown = 0f;
            _inputRouter.OnFlipperLeftChanged += HandleFlipperLeftChanged;
            _inputRouter.OnFlipperRightChanged += HandleFlipperRightChanged;
            _inputRouter.OnLaunchChanged += HandleLaunchChanged;
            _inputRouter.OnNudgeChanged += HandleNudgeChanged;
        }

        public void Dispose()
        {
            CancelNudgeAim();

            if (null == _inputRouter)
                return;

            _inputRouter.OnFlipperLeftChanged -= HandleFlipperLeftChanged;
            _inputRouter.OnFlipperRightChanged -= HandleFlipperRightChanged;
            _inputRouter.OnLaunchChanged -= HandleLaunchChanged;
            _inputRouter.OnNudgeChanged -= HandleNudgeChanged;
            _inputRouter = null;
        }

        public void SetMap(MapRoot map)
        {
            _map = map;
        }

        public void Tick(float deltaTime)
        {
            TickNudgeAim();
            TickNudgeCooldown(deltaTime);
        }

        public void ResetNudgeCooldown()
        {
            _remainingNudgeCooldown = 0f;
        }

        public void ResetControlStates()
        {
            CancelNudgeAim();

            if (null == _map)
                return;

            _map.SetFlipperPressed(Flipper.ESide.Left, false);
            _map.SetFlipperPressed(Flipper.ESide.Right, false);
            _map.SetSpringCharging(false);
        }

        void TickNudgeCooldown(float deltaTime)
        {
            if (_remainingNudgeCooldown <= 0f)
                return;

            _remainingNudgeCooldown -= deltaTime;
            if (_remainingNudgeCooldown < 0f)
                _remainingNudgeCooldown = 0f;
        }

        void TickNudgeAim()
        {
            if (!_isNudgeAiming)
                return;

            if (!IsNudgeTargetReady())
            {
                CancelNudgeAim();
                return;
            }

            UpdateNudgeAimDirection();

            _nudgeAimElapsed += Time.unscaledDeltaTime;
            if (_nudgeAimElapsed >= StatProvider.GetNudgeAimSeconds())
                FireNudge();
        }

        void UpdateNudgeAimDirection()
        {
            if (null == _inputRouter)
                return;

            Vector2 input = _inputRouter.ReadNudgeAimInput();
            if (input.sqrMagnitude <= 0f)
                return;

            _nudgeAimDirection = input.normalized;
            _nudgeAimDirectionPresenter?.Invoke(_nudgeAimDirection);
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
            if (_isNudgeAiming && isPressed)
                return;

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
                if (_isNudgeAiming)
                    return;

                _map.SetSpringCharging(true);
                return;
            }

            if (_map.TryLaunch(out float launchPowerRatio))
                OnLaunched?.Invoke(launchPowerRatio);
        }

        void HandleNudgeChanged(bool isPressed)
        {
            if (isPressed)
            {
                BeginNudgeAim();
                return;
            }

            if (_isNudgeAiming)
                FireNudge();
        }

        void BeginNudgeAim()
        {
            if (_isNudgeAiming)
                return;

            if (_remainingNudgeCooldown > 0f)
                return;

            if (IsControlBlocked())
                return;

            if (!IsPlaying())
                return;

            if (!IsNudgeTargetReady())
                return;

            _isNudgeAiming = true;
            _nudgeAimElapsed = 0f;
            _nudgeAimDirection = Vector2.up;
            _nudgeAimPresenter?.Invoke(true);
            _nudgeAimDirectionPresenter?.Invoke(_nudgeAimDirection);
        }

        void FireNudge()
        {
            CancelNudgeAim();

            if (null == _nudgeImpulseApplier)
                return;

            if (!_nudgeImpulseApplier(_nudgeAimDirection * StatProvider.GetNudgeImpulsePower()))
                return;

            _remainingNudgeCooldown = StatProvider.GetNudgeCooldownSeconds();
            _nudgePresenter?.Invoke();
            OnNudged?.Invoke();
        }

        void CancelNudgeAim()
        {
            if (!_isNudgeAiming)
                return;

            _isNudgeAiming = false;
            _nudgeAimPresenter?.Invoke(false);
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

        bool IsNudgeTargetReady()
        {
            if (null == _isNudgeTargetReadyGetter)
                return true;

            return _isNudgeTargetReadyGetter();
        }
    }
}
