using Pinball.Localization;
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
        const string CAPTURE_WAITING_KEY = "ui.settings.press_any_key";
        const int PREV_DIRECTION = -1;
        const int NEXT_DIRECTION = 1;
        const string FULLSCREEN_LABEL = "FULLSCREEN";
        const string FULLSCREEN_KEY = "ui.settings.fullscreen";
        const string WINDOWED_LABEL = "WINDOWED";
        const string WINDOWED_KEY = "ui.settings.windowed";

        [SerializeField] Button _flipperLeftButton = null;
        [SerializeField] TMP_Text _flipperLeftLabel = null;
        [SerializeField] Button _flipperRightButton = null;
        [SerializeField] TMP_Text _flipperRightLabel = null;
        [SerializeField] Button _launchButton = null;
        [SerializeField] TMP_Text _launchLabel = null;
        [SerializeField] Button _nudgeButton = null;
        [SerializeField] TMP_Text _nudgeLabel = null;
        [SerializeField] Button _resolutionPrevButton = null;
        [SerializeField] Button _resolutionNextButton = null;
        [SerializeField] TMP_Text _resolutionLabel = null;
        [SerializeField] Button _fullscreenPrevButton = null;
        [SerializeField] Button _fullscreenNextButton = null;
        [SerializeField] TMP_Text _fullscreenLabel = null;
        [SerializeField] Button _languagePrevButton = null;
        [SerializeField] Button _languageNextButton = null;
        [SerializeField] TMP_Text _languageLabel = null;
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
            _nudgeButton.onClick.AddListener(OnNudgeClicked);
            _resolutionPrevButton.onClick.AddListener(OnResolutionPrevClicked);
            _resolutionNextButton.onClick.AddListener(OnResolutionNextClicked);
            _fullscreenPrevButton.onClick.AddListener(OnFullscreenClicked);
            _fullscreenNextButton.onClick.AddListener(OnFullscreenClicked);
            _languagePrevButton.onClick.AddListener(OnLanguagePrevClicked);
            _languageNextButton.onClick.AddListener(OnLanguageNextClicked);
            _closeButton.onClick.AddListener(OnCloseClicked);
        }

        public override void OnTerminate()
        {
            _flipperLeftButton.onClick.RemoveListener(OnFlipperLeftClicked);
            _flipperRightButton.onClick.RemoveListener(OnFlipperRightClicked);
            _launchButton.onClick.RemoveListener(OnLaunchClicked);
            _nudgeButton.onClick.RemoveListener(OnNudgeClicked);
            _resolutionPrevButton.onClick.RemoveListener(OnResolutionPrevClicked);
            _resolutionNextButton.onClick.RemoveListener(OnResolutionNextClicked);
            _fullscreenPrevButton.onClick.RemoveListener(OnFullscreenClicked);
            _fullscreenNextButton.onClick.RemoveListener(OnFullscreenClicked);
            _languagePrevButton.onClick.RemoveListener(OnLanguagePrevClicked);
            _languageNextButton.onClick.RemoveListener(OnLanguageNextClicked);
            _closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        public override void OnReturn()
        {
            CancelCapture();
        }

        public override void Bind(UIViewArgs args)
        {
            RefreshLabels();
        }

        void RefreshLabels()
        {
            _languageLabel.text = LocalizationManager.GetDisplayName(LocalizationManager.CurrentLanguage);

            if (!GameSettings.IsInitialized)
                return;

            SetKeyLabel(_flipperLeftLabel, GameSettings.GetKey(EKeyAction.FlipperLeft));
            SetKeyLabel(_flipperRightLabel, GameSettings.GetKey(EKeyAction.FlipperRight));
            SetKeyLabel(_launchLabel, GameSettings.GetKey(EKeyAction.Launch));
            SetKeyLabel(_nudgeLabel, GameSettings.GetKey(EKeyAction.Nudge));
            _resolutionLabel.text = GameSettings.GetResolutionLabel(GameSettings.ResolutionIndex);
            _fullscreenLabel.text = GameSettings.IsFullscreen ? LocalizationTable.Get(FULLSCREEN_KEY, FULLSCREEN_LABEL) : LocalizationTable.Get(WINDOWED_KEY, WINDOWED_LABEL);
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

        void OnNudgeClicked()
        {
            StartCapture(EKeyAction.Nudge, _nudgeLabel);
        }

        void OnResolutionPrevClicked()
        {
            GameSettings.SetResolutionIndex(GameSettings.ResolutionIndex + PREV_DIRECTION);
            RefreshLabels();
        }

        void OnResolutionNextClicked()
        {
            GameSettings.SetResolutionIndex(GameSettings.ResolutionIndex + NEXT_DIRECTION);
            RefreshLabels();
        }

        void OnLanguagePrevClicked()
        {
            ShiftLanguage(PREV_DIRECTION);
        }

        void OnLanguageNextClicked()
        {
            ShiftLanguage(NEXT_DIRECTION);
        }

        void ShiftLanguage(int direction)
        {
            int index = LocalizationManager.GetLanguageIndex(LocalizationManager.CurrentLanguage) + direction;
            LocalizationManager.SetLanguage(LocalizationManager.GetLanguage(index));
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
            label.text = LocalizationTable.Get(CAPTURE_WAITING_KEY, CAPTURE_WAITING_LABEL);
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
