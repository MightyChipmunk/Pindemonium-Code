using Pinball.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using GameCore.Localization;
using GameCore.Settings;
using GameCore.UI;

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
        const string FULLSCREEN_LABEL = "FULLSCREEN";
        const string FULLSCREEN_KEY = "ui.settings.fullscreen";
        const string WINDOWED_LABEL = "WINDOWED";
        const string WINDOWED_KEY = "ui.settings.windowed";
        const string ON_LABEL = "ON";
        const string ON_KEY = "ui.settings.on";
        const string OFF_LABEL = "OFF";
        const string OFF_KEY = "ui.settings.off";

        [SerializeField] SettingsKeyRowView[] _keyRows = null;
        [SerializeField] SettingsOptionRowView[] _optionRows = null;
        [SerializeField] Button _closeButton = null;
        [SerializeField] UIScrollView _systemScroll = null;

        EKeyAction _capturingAction = EKeyAction.FlipperLeft;
        bool _isCapturing = false;
        bool _isOpened = false;

        public override void OnGenerate()
        {
            for (int i = 0; i < _keyRows.Length; ++i)
                _keyRows[i].Init(OnKeyRowClicked);

            for (int i = 0; i < _optionRows.Length; ++i)
                _optionRows[i].Init(OnOptionShifted, OnOptionSliderChanged);

            _closeButton.onClick.AddListener(OnCloseClicked);
        }

        public override void OnTerminate()
        {
            for (int i = 0; i < _keyRows.Length; ++i)
                _keyRows[i].Release();

            for (int i = 0; i < _optionRows.Length; ++i)
                _optionRows[i].Release();

            _closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        public override void OnReturn()
        {
            CancelCapture();
            _isOpened = false;
        }

        public override void Bind(UIViewArgs args)
        {
            RefreshLabels();
            _systemScroll.Refresh();

            if (_isOpened)
                return;

            _isOpened = true;
            _systemScroll.ResetPosition();
        }

        public override bool TryCloseByEscape()
        {
            if (_isCapturing)
            {
                CancelCapture();
                return true;
            }

            OnCloseClicked();
            return true;
        }

        void RefreshLabels()
        {
            for (int i = 0; i < _optionRows.Length; ++i)
                RefreshOptionRow(_optionRows[i]);

            if (!GameSettings.IsInitialized)
                return;

            for (int i = 0; i < _keyRows.Length; ++i)
                _keyRows[i].SetValue(GetKeyLabel(_keyRows[i].KeyAction));

            if (_isCapturing)
                SetKeyRowValue(_capturingAction, LocalizationTable.Get(CAPTURE_WAITING_KEY, CAPTURE_WAITING_LABEL));
        }

        static void RefreshOptionRow(SettingsOptionRowView row)
        {
            SettingsOptionRowView.EOptionType optionType = row.OptionType;
            if (!GameSettings.IsInitialized && optionType != SettingsOptionRowView.EOptionType.Language)
                return;

            row.SetValue(GetOptionLabel(optionType));

            if (SettingsOptionRowView.HasSlider(optionType))
                row.SetSliderValue(GetOptionSliderValue(optionType));

            if (optionType == SettingsOptionRowView.EOptionType.RefreshRate)
                row.SetInteractable(!GameSettings.IsVSyncEnabled);
        }

        static string GetOptionLabel(SettingsOptionRowView.EOptionType optionType)
        {
            switch (optionType)
            {
                case SettingsOptionRowView.EOptionType.Resolution:
                    return GameSettings.GetResolutionLabel(GameSettings.ResolutionIndex);
                case SettingsOptionRowView.EOptionType.Fullscreen:
                    return GameSettings.IsFullscreen ? LocalizationTable.Get(FULLSCREEN_KEY, FULLSCREEN_LABEL) : LocalizationTable.Get(WINDOWED_KEY, WINDOWED_LABEL);
                case SettingsOptionRowView.EOptionType.RefreshRate:
                    return GameSettings.GetRefreshRateLabel(GameSettings.RefreshRateIndex);
                case SettingsOptionRowView.EOptionType.VSync:
                    return GameSettings.IsVSyncEnabled ? LocalizationTable.Get(ON_KEY, ON_LABEL) : LocalizationTable.Get(OFF_KEY, OFF_LABEL);
                case SettingsOptionRowView.EOptionType.MasterVolume:
                    return GameSettings.GetVolumeLabel(GameSettings.MasterVolume);
                case SettingsOptionRowView.EOptionType.SfxVolume:
                    return GameSettings.GetVolumeLabel(GameSettings.SfxVolume);
                case SettingsOptionRowView.EOptionType.BgmVolume:
                    return GameSettings.GetVolumeLabel(GameSettings.BgmVolume);
                case SettingsOptionRowView.EOptionType.Language:
                    return LocalizationTable.GetLanguageName(LocalizationManager.CurrentLanguageIndex);
                default:
                    return string.Empty;
            }
        }

        static float GetOptionSliderValue(SettingsOptionRowView.EOptionType optionType)
        {
            switch (optionType)
            {
                case SettingsOptionRowView.EOptionType.MasterVolume:
                    return GameSettings.MasterVolume;
                case SettingsOptionRowView.EOptionType.SfxVolume:
                    return GameSettings.SfxVolume;
                case SettingsOptionRowView.EOptionType.BgmVolume:
                    return GameSettings.BgmVolume;
                default:
                    return 0f;
            }
        }

        static string GetKeyLabel(EKeyAction action)
        {
            return KeyLabel.Get(GameSettings.GetKey(action));
        }

        void Update()
        {
            if (!_isCapturing)
                return;

            Keyboard keyboard = Keyboard.current;
            if (null == keyboard)
                return;

            Key capturedKey = FindPressedKey(keyboard);
            if (capturedKey == Key.None)
                return;

            FinishCapture(capturedKey);
        }

        void OnKeyRowClicked(EKeyAction action)
        {
            if (_isCapturing)
                return;

            _isCapturing = true;
            _capturingAction = action;
            SetKeyRowValue(action, LocalizationTable.Get(CAPTURE_WAITING_KEY, CAPTURE_WAITING_LABEL));
        }

        void SetKeyRowValue(EKeyAction action, string value)
        {
            for (int i = 0; i < _keyRows.Length; ++i)
                if (_keyRows[i].KeyAction == action)
                    _keyRows[i].SetValue(value);
        }

        void OnOptionShifted(SettingsOptionRowView.EOptionType optionType, int direction)
        {
            switch (optionType)
            {
                case SettingsOptionRowView.EOptionType.Resolution:
                    GameSettings.SetResolutionIndex(GameSettings.ResolutionIndex + direction);
                    break;
                case SettingsOptionRowView.EOptionType.Fullscreen:
                    GameSettings.SetFullscreen(!GameSettings.IsFullscreen);
                    break;
                case SettingsOptionRowView.EOptionType.RefreshRate:
                    GameSettings.SetRefreshRateIndex(GameSettings.RefreshRateIndex + direction);
                    break;
                case SettingsOptionRowView.EOptionType.VSync:
                    GameSettings.SetVSync(!GameSettings.IsVSyncEnabled);
                    break;
                case SettingsOptionRowView.EOptionType.MasterVolume:
                    GameSettings.ShiftMasterVolume(direction);
                    break;
                case SettingsOptionRowView.EOptionType.SfxVolume:
                    GameSettings.ShiftSfxVolume(direction);
                    break;
                case SettingsOptionRowView.EOptionType.BgmVolume:
                    GameSettings.ShiftBgmVolume(direction);
                    break;
                case SettingsOptionRowView.EOptionType.Language:
                    ShiftLanguage(direction);
                    break;
            }

            RefreshLabels();
        }

        void OnOptionSliderChanged(SettingsOptionRowView.EOptionType optionType, float normalizedValue)
        {
            switch (optionType)
            {
                case SettingsOptionRowView.EOptionType.MasterVolume:
                    GameSettings.SetMasterVolume(normalizedValue);
                    break;
                case SettingsOptionRowView.EOptionType.SfxVolume:
                    GameSettings.SetSfxVolume(normalizedValue);
                    break;
                case SettingsOptionRowView.EOptionType.BgmVolume:
                    GameSettings.SetBgmVolume(normalizedValue);
                    break;
            }

            RefreshLabels();
        }

        static void ShiftLanguage(int direction)
        {
            LocalizationManager.ShiftLanguage(direction);
        }

        void OnCloseClicked()
        {
            UIManager.Hide(UIViewCatalogData.EViewType.Settings);
        }

        void FinishCapture(Key capturedKey)
        {
            _isCapturing = false;
            GameSettings.SetKey(_capturingAction, capturedKey);
            RefreshLabels();
        }

        void CancelCapture()
        {
            if (!_isCapturing)
                return;

            _isCapturing = false;
            RefreshLabels();
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

                if (!GameSettings.IsBindableKey(control.keyCode))
                    continue;

                return control.keyCode;
            }

            return Key.None;
        }
    }
}
