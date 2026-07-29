using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pinball.Ingame.InputModule
{
    public class IngameInputRouter : MonoBehaviour
    {
        [SerializeField] IngameKeyConfigData _keyConfig = null;

        public event Action<bool> OnFlipperLeftChanged;
        public event Action<bool> OnFlipperRightChanged;
        public event Action<bool> OnLaunchChanged;

        bool _isInited = false;

        public void Init()
        {
            _isInited = true;
        }

        public void Release()
        {
            OnFlipperLeftChanged = null;
            OnFlipperRightChanged = null;
            OnLaunchChanged = null;
            _isInited = false;
        }

        void Update()
        {
            if (!_isInited)
                return;

            if (null == _keyConfig)
                return;

            Keyboard keyboard = Keyboard.current;
            if (null == keyboard)
                return;

            if (keyboard[_keyConfig.FlipperLeftKey].wasPressedThisFrame)
                OnFlipperLeftChanged?.Invoke(true);

            if (keyboard[_keyConfig.FlipperLeftKey].wasReleasedThisFrame)
                OnFlipperLeftChanged?.Invoke(false);

            if (keyboard[_keyConfig.FlipperRightKey].wasPressedThisFrame)
                OnFlipperRightChanged?.Invoke(true);

            if (keyboard[_keyConfig.FlipperRightKey].wasReleasedThisFrame)
                OnFlipperRightChanged?.Invoke(false);

            if (keyboard[_keyConfig.LaunchKey].wasPressedThisFrame)
                OnLaunchChanged?.Invoke(true);

            if (keyboard[_keyConfig.LaunchKey].wasReleasedThisFrame)
                OnLaunchChanged?.Invoke(false);
        }
    }
}
