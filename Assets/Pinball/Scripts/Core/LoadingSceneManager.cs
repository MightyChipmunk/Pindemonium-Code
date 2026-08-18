using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.Localization;
using Pinball.Save;
using Pinball.Settings;
using Pinball.UI;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Core
{
    public class LoadingSceneManager : BehaviourBase
    {
        const float MIN_DISPLAY_SECONDS = 0.5f;

        [SerializeField] SettingsConfigData _settingsConfig = null;
        [SerializeField] LocalizationTableData _localizationTable = null;
        [SerializeField] UIViewCatalogData _viewCatalog = null;

        void Start()
        {
            BootAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        async UniTaskVoid BootAsync(CancellationToken cancellationToken)
        {
            SaveManager.EnsureInit();
            GameSettings.EnsureInit(_settingsConfig);
            LocalizationManager.EnsureInit();
            LocalizationTable.EnsureInit(_localizationTable);
            UIManager.EnsureInit(_viewCatalog);
            await UniTask.Delay(TimeSpan.FromSeconds(MIN_DISPLAY_SECONDS), cancellationToken: cancellationToken);
            await SceneFlow.LoadLobbyAsync();
        }
    }
}
