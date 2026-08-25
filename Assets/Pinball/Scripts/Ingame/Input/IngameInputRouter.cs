using System;
using Pinball.Settings;
using Pinball.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pinball.Ingame.Input
{
    public class IngameInputRouter : MonoBehaviour
    {
        public event Action<bool> OnFlipperLeftChanged;
        public event Action<bool> OnFlipperRightChanged;
        public event Action<bool> OnLaunchChanged;
        public event Action<bool> OnNudgeChanged;
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
            OnNudgeChanged = null;
            OnEscapePressed = null;
            _isInited = false;
        }

        void Update()
        {
            if (Blocker.IsBlocking)
                return;

            if (!_isInited)
                return;

            Keyboard keyboard = Keyboard.current;
            if (null == keyboard)
                return;

            CheckKey(keyboard, GameSettings.GetKey(EKeyAction.FlipperLeft), OnFlipperLeftChanged);
            CheckKey(keyboard, GameSettings.GetKey(EKeyAction.FlipperRight), OnFlipperRightChanged);
            CheckKey(keyboard, GameSettings.GetKey(EKeyAction.Launch), OnLaunchChanged);
            CheckKey(keyboard, GameSettings.GetKey(EKeyAction.Nudge), OnNudgeChanged);

            if (keyboard.escapeKey.wasPressedThisFrame)
                OnEscapePressed?.Invoke();
        }

        public Vector2 ReadNudgeAimInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (null == keyboard)
                return Vector2.zero;

            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                input.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                input.y -= 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                input.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                input.x += 1f;
            return input;
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
    }
}
