using Pinball.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class SettingsView : UIViewBase
    {
        public record Args : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.Settings;
        }

        const string CAPTURE_WAITING_LABEL = "PRESS ANY KEY";
        const int RESOLUTION_PREV_DIRECTION = -1;
        const int RESOLUTION_NEXT_DIRECTION = 1;
        const string FULLSCREEN_LABEL = "FULLSCREEN";
        const string WINDOWED_LABEL = "WINDOWED";

        [SerializeField] Button _flipperLeftButton = null;
        [SerializeField] TMP_Text _flipperLeftLabel = null;
        [SerializeField] Button _flipperRightButton = null;
        [SerializeField] TMP_Text _flipperRightLabel = null;
        [SerializeField] Button _launchButton = null;
        [SerializeField] TMP_Text _launchLabel = null;
        [SerializeField] Button _resolutionPrevButton = null;
        [SerializeField] Button _resolutionNextButton = null;
        [SerializeField] TMP_Text _resolutionLabel = null;
        [SerializeField] Button _fullscreenButton = null;
        [SerializeField] TMP_Text _fullscreenLabel = null;
        [SerializeField] Button _closeButton = null;

        TMP_Text _capturingLabel = null;
        string _capturingLabelBackup = string.Empty;
        EKeyAction _capturingAction = EKeyAction.FlipperLeft;
        bool _isCapturing = false;

        public override void OnGenerate()
        {
            _flipperLeftButton.onClick.AddListener(OnFlipperLeftClicked);
            _flipperRightButton.onClick.AddListener(OnFlipperRightClicked);
            _launchButton.onClick.AddListener(OnLaunchClicked);
            _resolutionPrevButton.onClick.AddListener(OnResolutionPrevClicked);
            _resolutionNextButton.onClick.AddListener(OnResolutionNextClicked);
            _fullscreenButton.onClick.AddListener(OnFullscreenClicked);
            _closeButton.onClick.AddListener(OnCloseClicked);
        }

        public override void OnTerminate()
        {
            _flipperLeftButton.onClick.RemoveListener(OnFlipperLeftClicked);
            _flipperRightButton.onClick.RemoveListener(OnFlipperRightClicked);
            _launchButton.onClick.RemoveListener(OnLaunchClicked);
            _resolutionPrevButton.onClick.RemoveListener(OnResolutionPrevClicked);
            _resolutionNextButton.onClick.RemoveListener(OnResolutionNextClicked);
            _fullscreenButton.onClick.RemoveListener(OnFullscreenClicked);
            _closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        public override void OnReturn()
        {
            CancelCapture();
        }

        public override void Bind(UIViewArgs args)
        {
            if (!GameSettings.IsInitialized)
                return;

            RefreshLabels();
        }

        void RefreshLabels()
        {
            SetKeyLabel(_flipperLeftLabel, GameSettings.GetKey(EKeyAction.FlipperLeft));
            SetKeyLabel(_flipperRightLabel, GameSettings.GetKey(EKeyAction.FlipperRight));
            SetKeyLabel(_launchLabel, GameSettings.GetKey(EKeyAction.Launch));
            _resolutionLabel.text = GameSettings.GetResolutionLabel(GameSettings.ResolutionIndex);
            _fullscreenLabel.text = GameSettings.IsFullscreen ? FULLSCREEN_LABEL : WINDOWED_LABEL;
        }

        static void SetKeyLabel(TMP_Text label, Key key)
        {
            label.text = key.ToString().ToUpperInvariant();
        }

        void Update()
        {
            if (!_isCapturing)
                return;

            Keyboard keyboard = Keyboard.current;
            if (null == keyboard)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                CancelCapture();
                return;
            }

            Key capturedKey = FindPressedKey(keyboard);
            if (capturedKey == Key.None)
                return;

            FinishCapture(capturedKey);
        }

        void OnFlipperLeftClicked()
        {
            StartCapture(EKeyAction.FlipperLeft, _flipperLeftLabel);
        }

        void OnFlipperRightClicked()
        {
            StartCapture(EKeyAction.FlipperRight, _flipperRightLabel);
        }

        void OnLaunchClicked()
        {
            StartCapture(EKeyAction.Launch, _launchLabel);
        }

        void OnResolutionPrevClicked()
        {
            GameSettings.SetResolutionIndex(GameSettings.ResolutionIndex + RESOLUTION_PREV_DIRECTION);
            RefreshLabels();
        }

        void OnResolutionNextClicked()
        {
            GameSettings.SetResolutionIndex(GameSettings.ResolutionIndex + RESOLUTION_NEXT_DIRECTION);
            RefreshLabels();
        }

        void OnFullscreenClicked()
        {
            GameSettings.SetFullscreen(!GameSettings.IsFullscreen);
            RefreshLabels();
        }

        void OnCloseClicked()
        {
            UIManager.Hide(UIViewCatalogData.EViewType.Settings);
        }

        void StartCapture(EKeyAction action, TMP_Text label)
        {
            if (_isCapturing)
                return;

            _isCapturing = true;
            _capturingAction = action;
            _capturingLabel = label;
            _capturingLabelBackup = label.text;
            label.text = CAPTURE_WAITING_LABEL;
        }

        void FinishCapture(Key capturedKey)
        {
            _isCapturing = false;
            _capturingLabel = null;
            GameSettings.SetKey(_capturingAction, capturedKey);
            RefreshLabels();
        }

        void CancelCapture()
        {
            if (!_isCapturing)
                return;

            _isCapturing = false;

            if (null != _capturingLabel)
                _capturingLabel.text = _capturingLabelBackup;

            _capturingLabel = null;
        }

        static Key FindPressedKey(Keyboard keyboard)
        {
            for (int i = 0; i < keyboard.allKeys.Count; ++i)
            {
                KeyControl control = keyboard.allKeys[i];
                if (!control.wasPressedThisFrame)
                    continue;

                if (control.keyCode == Key.Escape)
                    continue;

                return control.keyCode;
            }

            return Key.None;
        }
    }
}
