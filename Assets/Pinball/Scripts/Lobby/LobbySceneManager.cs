using Cysharp.Threading.Tasks;
using Pinball.Core;
using Pinball.Save;
using Pinball.Settings;
using Pinball.UI;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Lobby
{
    public class LobbySceneManager : BehaviourBase
    {
        [SerializeField] SettingsConfigData _settingsConfig = null;
        [SerializeField] UIViewCatalogData _viewCatalog = null;
        [SerializeField] LobbyUIRoot _uiRoot = null;

        bool _isLoadingIngame = false;

        void Awake()
        {
            Init();
        }

        void OnDestroy()
        {
            Release();
        }

        public void Init()
        {
            SaveManager.EnsureInit();
            GameSettings.EnsureInit(_settingsConfig);
            UIManager.EnsureInit(_viewCatalog);
            _uiRoot.Init(OnContinueRequested, OnNewGameRequested, OnSettingsRequested);
            _uiRoot.SetContinueInteractable(SaveManager.HasIngameSave);
        }

        public void Release()
        {
            UIManager.Hide(UIViewCatalogData.EViewType.Settings);

            if (null != _uiRoot)
                _uiRoot.Release();
        }

        void OnContinueRequested()
        {
            if (_isLoadingIngame)
                return;

            if (!SaveManager.HasIngameSave)
                return;

            _isLoadingIngame = true;
            SceneFlow.LoadIngameAsync(true).Forget();
        }

        void OnNewGameRequested()
        {
            if (_isLoadingIngame)
                return;

            _isLoadingIngame = true;
            SaveManager.ClearIngame();
            SceneFlow.LoadIngameAsync(false).Forget();
        }

        void OnSettingsRequested()
        {
            UIManager.Show(new SettingsView.Args());
        }

#if UNITY_EDITOR
        protected override void OnBindSerializedField()
        {
            _uiRoot = GetComponentInChildren<LobbyUIRoot>(true);
        }
#endif // UNITY_EDITOR
    }
}
