using GameCore.Audio;
using GameCore.UI;
using Pinball.Audio;
using Pinball.Localization;
using Pinball.Settings;
using Pinball.UI;
using UnityEngine;
using GameCore.Localization;

namespace Pinball.Core
{
    [CreateAssetMenu(fileName = "GameBootStrapConfig", menuName = "Pinball/Core/GameBootStrapConfig")]
    public class GameBootStrapConfig : ScriptableObject
    {
        [SerializeField] SettingsConfigData _settingsConfig = null;
        [SerializeField] LocalizationTableData _localizationTable = null;
        [SerializeField] UIViewCatalogData _viewCatalog = null;
        [SerializeField] ScreenTransitionView _transitionPrefab = null;
        [SerializeField] AudioStorage _audioStorage = null;
        [SerializeField] BgmStorage _bgmStorage = null;
        [SerializeField] BgmPlayer _bgmPlayerPrefab = null;

        public SettingsConfigData SettingsConfig => _settingsConfig;
        public LocalizationTableData LocalizationTable => _localizationTable;
        public UIViewCatalogData ViewCatalog => _viewCatalog;
        public AudioStorage AudioStorage => _audioStorage;
        public BgmStorage BgmStorage => _bgmStorage;

        public ScreenTransitionView GetTransitionPrefab()
        {
            return _transitionPrefab;
        }

        public BgmPlayer GetBgmPlayerPrefab()
        {
            return _bgmPlayerPrefab;
        }
    }
}
