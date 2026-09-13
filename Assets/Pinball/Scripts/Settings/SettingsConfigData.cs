using GameCore.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pinball.Settings
{
    [CreateAssetMenu(fileName = "SettingsConfigData", menuName = "Pinball/Settings/SettingsConfigData")]
    public class SettingsConfigData : SettingsConfigBase
    {
        const string KEY_CONFIG_MISSING_MESSAGE = "[SettingsConfigData] KeyConfigData가 지정되지 않아 기본 키를 제공할 수 없습니다.";

        [SerializeField] KeyConfigData _defaultKeyConfig = null;

        public Key GetDefaultKey(EKeyAction action)
        {
            if (null == _defaultKeyConfig)
            {
                Debug.LogError(KEY_CONFIG_MISSING_MESSAGE, this);
                return Key.None;
            }

            return _defaultKeyConfig.GetKey(action);
        }
    }
}
