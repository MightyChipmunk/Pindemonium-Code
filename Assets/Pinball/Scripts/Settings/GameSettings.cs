using GameCore.Settings;
using UnityEngine.InputSystem;

namespace Pinball.Settings
{
    /// <summary>
    /// GameCore의 Display/Volume/KeyBinding 설정을 게임 PlayerPrefs 접두사로 묶어 초기화하는 파사드.
    /// </summary>
    public static class GameSettings
    {
        const string PREF_PREFIX = "Pinball.Settings";

        static bool _isInitialized = false;

        public static bool IsInitialized => _isInitialized;
        public static int ResolutionIndex => DisplaySettings.ResolutionIndex;
        public static int RefreshRateIndex => DisplaySettings.RefreshRateIndex;
        public static bool IsVSyncEnabled => DisplaySettings.IsVSyncEnabled;
        public static bool IsFullscreen => DisplaySettings.IsFullscreen;
        public static float MasterVolume => VolumeSettings.MasterVolume;
        public static float SfxVolume => VolumeSettings.SfxVolume;
        public static float BgmVolume => VolumeSettings.BgmVolume;

        public static void EnsureInit(SettingsConfigData config)
        {
            if (_isInitialized || null == config)
                return;

            DisplaySettings.Init(config, PREF_PREFIX);
            VolumeSettings.Init(config, PREF_PREFIX);
            KeyBindingSettings<EKeyAction>.Init(config.GetDefaultKey, PREF_PREFIX);
            _isInitialized = true;
        }

        public static void Release()
        {
            if (!_isInitialized)
                return;

            KeyBindingSettings<EKeyAction>.Release();
            VolumeSettings.Release();
            DisplaySettings.Release();
            _isInitialized = false;
        }

        public static Key GetKey(EKeyAction action)
        {
            return KeyBindingSettings<EKeyAction>.GetKey(action);
        }

        public static void SetKey(EKeyAction action, Key key)
        {
            KeyBindingSettings<EKeyAction>.SetKey(action, key);
        }

        public static bool IsBindableKey(Key key)
        {
            return KeyBindingSettings<EKeyAction>.IsBindableKey(key);
        }

        public static string GetResolutionLabel(int index)
        {
            return DisplaySettings.GetResolutionLabel(index);
        }

        public static void SetResolutionIndex(int index)
        {
            DisplaySettings.SetResolutionIndex(index);
        }

        public static string GetRefreshRateLabel(int index)
        {
            return DisplaySettings.GetRefreshRateLabel(index);
        }

        public static void SetRefreshRateIndex(int index)
        {
            DisplaySettings.SetRefreshRateIndex(index);
        }

        public static void SetVSync(bool isEnabled)
        {
            DisplaySettings.SetVSync(isEnabled);
        }

        public static void SetFullscreen(bool isFullscreen)
        {
            DisplaySettings.SetFullscreen(isFullscreen);
        }

        public static string GetVolumeLabel(float volume)
        {
            return VolumeSettings.GetVolumeLabel(volume);
        }

        public static void SetMasterVolume(float volume)
        {
            VolumeSettings.SetMasterVolume(volume);
        }

        public static void SetSfxVolume(float volume)
        {
            VolumeSettings.SetSfxVolume(volume);
        }

        public static void SetBgmVolume(float volume)
        {
            VolumeSettings.SetBgmVolume(volume);
        }

        public static void ShiftMasterVolume(int direction)
        {
            VolumeSettings.ShiftMasterVolume(direction);
        }

        public static void ShiftSfxVolume(int direction)
        {
            VolumeSettings.ShiftSfxVolume(direction);
        }

        public static void ShiftBgmVolume(int direction)
        {
            VolumeSettings.ShiftBgmVolume(direction);
        }
    }
}
