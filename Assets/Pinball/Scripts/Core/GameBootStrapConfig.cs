using Pinball.Audio;
using Pinball.Localization;
using Pinball.Settings;
using Pinball.UI;
using UnityEngine;

namespace Pinball.Core
{
    [CreateAssetMenu(fileName = "GameBootStrapConfig", menuName = "Pinball/Core/GameBootStrapConfig")]
    public class GameBootStrapConfig : ScriptableObject
    {
        [SerializeField] SettingsConfigData _settingsConfig = null;
        [SerializeField] LocalizationTableData _localizationTable = null;
        [SerializeField] UIViewCatalogData _viewCatalog = null;
        [SerializeField] BlockerView _blockerPrefab = null;
        [SerializeField] AudioStorage _audioStorage = null;

        public SettingsConfigData SettingsConfig => _settingsConfig;
        public LocalizationTableData LocalizationTable => _localizationTable;
        public UIViewCatalogData ViewCatalog => _viewCatalog;
        public BlockerView BlockerPrefab => _blockerPrefab;
        public AudioStorage AudioStorage => _audioStorage;
    }
}
