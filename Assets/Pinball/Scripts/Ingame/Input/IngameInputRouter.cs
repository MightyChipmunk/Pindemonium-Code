using System;
using Pinball.Settings;
using Pinball.UI;
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
        public event Action OnNudgePressed;
        public event Action OnEscapePressed;

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
            OnNudgePressed = null;
            OnEscapePressed = null;
            _isInited = false;
        }

        void Update()
        {
            if (Blocker.IsBlocking)
                return;

            if (!_isInited)
                return;

            if (null == _keyConfig)
                return;

            Keyboard keyboard = Keyboard.current;
            if (null == keyboard)
                return;

            CheckKey(keyboard, ResolveKey(EKeyAction.FlipperLeft, _keyConfig.FlipperLeftKey), OnFlipperLeftChanged);
            CheckKey(keyboard, ResolveKey(EKeyAction.FlipperRight, _keyConfig.FlipperRightKey), OnFlipperRightChanged);
            CheckKey(keyboard, ResolveKey(EKeyAction.Launch, _keyConfig.LaunchKey), OnLaunchChanged);
            CheckKeyPressed(keyboard, ResolveKey(EKeyAction.Nudge, _keyConfig.NudgeKey), OnNudgePressed);

            if (keyboard.escapeKey.wasPressedThisFrame)
                OnEscapePressed?.Invoke();
        }

        static Key ResolveKey(EKeyAction action, Key fallbackKey)
        {
            if (!GameSettings.IsInitialized)
                return fallbackKey;

            Key key = GameSettings.GetKey(action);
            if (key == Key.None)
                return fallbackKey;

            return key;
        }

        static void CheckKey(Keyboard keyboard, Key key, Action<bool> callback)
        {
            if (key == Key.None)
                return;

            if (keyboard[key].wasPressedThisFrame)
                callback?.Invoke(true);

            if (keyboard[key].wasReleasedThisFrame)
                callback?.Invoke(false);
        }

        static void CheckKeyPressed(Keyboard keyboard, Key key, Action callback)
        {
            if (key == Key.None)
                return;

            if (keyboard[key].wasPressedThisFrame)
                callback?.Invoke();
        }
    }
}
