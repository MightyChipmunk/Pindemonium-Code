using Cysharp.Threading.Tasks;
using Pinball.Core;
using Pinball.Ingame;
using Pinball.Ingame.Reward;
using Pinball.Ingame.Stage;
using Pinball.Localization;
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
        [SerializeField] LocalizationTableData _localizationTable = null;
        [SerializeField] UIViewCatalogData _viewCatalog = null;
        [SerializeField] BlockerView _blockerPrefab = null;
        [SerializeField] FlipperCatalogData _flipperCatalog = null;
        [SerializeField] ClearRewardCatalogData _rewardCatalog = null;
        [SerializeField] StageCatalogData _stageCatalog = null;
        [SerializeField] StageGimmickCatalogData _gimmickCatalog = null;
        [SerializeField] LobbyUIRoot _uiRoot = null;

        LobbyRunSetupController _runSetupController = null;
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
            LocalizationManager.EnsureInit();
            LocalizationTable.EnsureInit(_localizationTable);
            UIManager.EnsureInit(_viewCatalog);
            Blocker.EnsureInit(_blockerPrefab);
            _runSetupController = new LobbyRunSetupController();
            _runSetupController.Init(new LobbyRunSetupController.InitParam(
                FlipperCatalog: _flipperCatalog,
                RewardCatalog: _rewardCatalog,
                StageCatalog: _stageCatalog,
                GimmickCatalog: _gimmickCatalog));
            _runSetupController.OnPlayRequested += OnPlayRequested;
            _runSetupController.OnContinueRequested += OnContinueConfirmed;
            _uiRoot.Init(OnContinueRequested, OnNewGameRequested, OnSettingsRequested);
            _uiRoot.SetContinueInteractable(SaveManager.HasIngameSave);
            RefreshLobbyLabels();
            LocalizationManager.OnLanguageChanged += RefreshLobbyLabels;
        }

        void RefreshLobbyLabels()
        {
            _uiRoot.SetContinueStage(GetSavedStageNumber());
            _uiRoot.SetBestStage(GetBestStageNumber());
        }

        static int GetSavedStageNumber()
        {
            IngamePlayData saved = SaveManager.GetIngameData();
            return null == saved ? 0 : saved.StageNumber;
        }

        static int GetBestStageNumber()
        {
            UserData userData = SaveManager.GetUserData();
            return null == userData ? 0 : userData.BestStageNumber;
        }

        public void Release()
        {
            LocalizationManager.OnLanguageChanged -= RefreshLobbyLabels;
            UIManager.Hide(UIViewCatalogData.EViewType.Settings);

            if (null != _runSetupController)
            {
                _runSetupController.Release();
                _runSetupController = null;
            }

            if (null != _uiRoot)
                _uiRoot.Release();
        }

        void OnContinueRequested()
        {
            if (_isLoadingIngame)
                return;

            if (!SaveManager.HasIngameSave)
                return;

            _runSetupController.OpenContinue();
        }

        void OnNewGameRequested()
        {
            if (_isLoadingIngame)
                return;

            _runSetupController.OpenFlipperSelect();
        }

        void OnPlayRequested(int flipperIndex)
        {
            if (_isLoadingIngame)
                return;

            _isLoadingIngame = true;
            SaveManager.ClearIngame();
            SceneFlow.LoadIngameAsync(false, flipperIndex).Forget();
        }

        void OnContinueConfirmed()
        {
            if (_isLoadingIngame)
                return;

            if (!SaveManager.HasIngameSave)
                return;

            _isLoadingIngame = true;
            SceneFlow.LoadIngameAsync(true, SceneFlow.DEFAULT_FLIPPER_INDEX).Forget();
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
