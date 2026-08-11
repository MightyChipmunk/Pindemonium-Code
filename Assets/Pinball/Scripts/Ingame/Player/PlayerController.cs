using System;
using Pinball.Ingame.InputModule;
using Pinball.Ingame.Map;

namespace Pinball.Ingame
{
    public class PlayerController
    {
        IngameInputRouter _inputRouter = null;
        MapRoot _map = null;
        Func<bool> _isControlBlockedGetter = null;

        public event Action OnLaunched;

        public void Init(IngameInputRouter inputRouter, Func<bool> isControlBlockedGetter)
        {
            _inputRouter = inputRouter;
            _isControlBlockedGetter = isControlBlockedGetter;
            _inputRouter.OnFlipperLeftChanged += HandleFlipperLeftChanged;
            _inputRouter.OnFlipperRightChanged += HandleFlipperRightChanged;
            _inputRouter.OnLaunchChanged += HandleLaunchChanged;
        }

        public void Release()
        {
            if (null != _inputRouter)
            {
                _inputRouter.OnFlipperLeftChanged -= HandleFlipperLeftChanged;
                _inputRouter.OnFlipperRightChanged -= HandleFlipperRightChanged;
                _inputRouter.OnLaunchChanged -= HandleLaunchChanged;
                _inputRouter = null;
            }

            OnLaunched = null;
            _isControlBlockedGetter = null;
            _map = null;
        }

        public void SetMap(MapRoot map)
        {
            _map = map;
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

        bool IsControlBlocked()
        {
            if (null == _isControlBlockedGetter)
                return false;

            return _isControlBlockedGetter();
        }
    }
}
