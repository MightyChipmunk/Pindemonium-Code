using System;
using GameCore.Localization;
using GameCore.UI;
using Pinball.Audio;
using Pinball.Localization;
using Pinball.Save;
using Pinball.Settings;
using Pinball.Steam;
using UnityEngine;
using UIViewManager = Pinball.UI.UIManager;

namespace Pinball.Core
{
    /// <summary>
    /// 게임 전역 서비스의 초기화 순서를 정한다. 어떤 씬에서 시작해도 한 번만 실행된다.
    /// </summary>
    public static class GameBootStrap
    {
        const string CONFIG_MISSING_MESSAGE = "[GameBootStrap] GameBootStrapConfig가 없어 서비스 초기화를 건너뜁니다.";
        const string LANGUAGE_PREF_KEY = "Pinball.Localization.Language";

        static readonly int LANGUAGE_COUNT = Enum.GetValues(typeof(ELanguage)).Length;

        static bool _isInitialized = false;

        public static bool IsInitialized => _isInitialized;

        public static void EnsureInit(GameBootStrapConfig config)
        {
            if (_isInitialized)
                return;

            if (null == config)
            {
                Debug.LogError(CONFIG_MISSING_MESSAGE);
                return;
            }

            Init(config);
        }

        static void Init(GameBootStrapConfig config)
        {
            SaveManager.EnsureInit();
            AchievementManager.Init();
            GameSettings.EnsureInit(config.SettingsConfig);
            LocalizationManager.Init(new LocalizationManager.InitParam(LANGUAGE_COUNT, (int)GetSystemLanguage(), LANGUAGE_PREF_KEY));
            LocalizationTable.Init(config.LocalizationTable);
            UIViewManager.EnsureInit(config.ViewCatalog);
            UIButtonSound.Init(PlayButtonHover, PlayButtonClick);
            Blocker.Init();
            ScreenTransition.Init(config.GetTransitionPrefab());
            AudioStorage.Init(config.AudioStorage);
            BgmStorage.Init(config.BgmStorage);
            BgmManager.Init(config.GetBgmPlayerPrefab());
            _isInitialized = true;
            Application.quitting -= Release;
            Application.quitting += Release;
        }

        public static void Release()
        {
            Application.quitting -= Release;

            if (!_isInitialized)
                return;

            _isInitialized = false;
            BgmManager.Release();
            BgmStorage.Release();
            AudioStorage.Release();
            ScreenTransition.Release();
            Blocker.Release();
            UIButtonSound.Release();
            UIViewManager.Release();
            LocalizationTable.Release();
            LocalizationManager.Release();
            GameSettings.Release();
            AchievementManager.Release();
            SaveManager.Release();
        }

        static ELanguage GetSystemLanguage()
        {
            if (Application.systemLanguage == SystemLanguage.Korean)
                return ELanguage.Korean;

            return ELanguage.English;
        }

        static void PlayButtonHover()
        {
            AudioManager.Play(AudioStorage.EAudioClipType.UIButtonHover);
        }

        static void PlayButtonClick()
        {
            AudioManager.Play(AudioStorage.EAudioClipType.UIButtonClick);
        }
    }
}
