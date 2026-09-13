using System;
using Pinball.Settings;
using Pinball.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using GameCore.UI;

namespace Pinball.Ingame.Input
{
    public class IngameInputRouter : MonoBehaviour
    {
        public event Action<bool> OnFlipperLeftChanged;
        public event Action<bool> OnFlipperRightChanged;
        public event Action<bool> OnLaunchChanged;
        public event Action<bool> OnNudgeChanged;

        bool _isInited = false;
        bool _isFlipperLeftHeld = false;
        bool _isFlipperRightHeld = false;
        bool _isLaunchHeld = false;
        bool _isNudgeHeld = false;

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
            ClearHeldKeys();
            _isInited = false;
        }

        void Update()
        {
            if (Blocker.IsBlocking)
            {
                ClearHeldKeys();
                return;
            }

            if (!_isInited)
                return;

            Keyboard keyboard = Keyboard.current;
            if (null == keyboard)
                return;

            CheckKey(keyboard, GameSettings.GetKey(EKeyAction.FlipperLeft), ref _isFlipperLeftHeld, OnFlipperLeftChanged);
            CheckKey(keyboard, GameSettings.GetKey(EKeyAction.FlipperRight), ref _isFlipperRightHeld, OnFlipperRightChanged);
            CheckKey(keyboard, GameSettings.GetKey(EKeyAction.Launch), ref _isLaunchHeld, OnLaunchChanged);
            CheckKey(keyboard, GameSettings.GetKey(EKeyAction.Nudge), ref _isNudgeHeld, OnNudgeChanged);
        }

        void ClearHeldKeys()
        {
            _isFlipperLeftHeld = false;
            _isFlipperRightHeld = false;
            _isLaunchHeld = false;
            _isNudgeHeld = false;
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

        static void CheckKey(Keyboard keyboard, Key key, ref bool isHeld, Action<bool> callback)
        {
            if (key == Key.None)
                return;

            bool isPressed = keyboard[key].isPressed;
            if (isPressed == isHeld)
                return;

            isHeld = isPressed;
            callback?.Invoke(isPressed);
        }
    }
}
