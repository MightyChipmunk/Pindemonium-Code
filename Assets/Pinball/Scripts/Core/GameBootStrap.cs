using Pinball.Audio;
using Pinball.Localization;
using Pinball.Save;
using Pinball.Settings;
using Pinball.UI;
using UnityEngine;

namespace Pinball.Core
{
    public static class GameBootStrap
    {
        const string CONFIG_MISSING_MESSAGE = "[GameBootStrap] GameBootStrapConfig가 없어 서비스 초기화를 건너뜁니다.";

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
            GameSettings.EnsureInit(config.SettingsConfig);
            LocalizationManager.EnsureInit();
            LocalizationTable.EnsureInit(config.LocalizationTable);
            UIManager.EnsureInit(config.ViewCatalog);
            Blocker.EnsureInit(config.BlockerPrefab);
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
            Blocker.Release();
            UIManager.Release();
            LocalizationTable.Release();
            LocalizationManager.Release();
            GameSettings.Release();
            SaveManager.Release();
        }
    }
}
