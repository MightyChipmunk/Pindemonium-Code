using Cysharp.Threading.Tasks;
using Pinball.Audio;
using Pinball.Core;
using Pinball.Ingame;
using Pinball.Ingame.Reward;
using Pinball.Ingame.Stage;
using Pinball.Localization;
using Pinball.Save;
using Pinball.UI;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Lobby
{
    public class LobbySceneManager : BehaviourBase
    {
        [SerializeField] GameBootStrapConfig _bootStrapConfig = null;
        [SerializeField] FlipperCatalogData _flipperCatalog = null;
        [SerializeField] ClearRewardCatalogData _rewardCatalog = null;
        [SerializeField] StageCatalogData _stageCatalog = null;
        [SerializeField] StageGimmickCatalogData _gimmickCatalog = null;
        [SerializeField] LobbyUIRoot _uiRoot = null;

        LobbyRunSetupController _runSetupController = null;
        bool _isLoadingIngame = false;

        void Awake()
        {
            GameBootStrap.EnsureInit(_bootStrapConfig);
            BgmManager.Play(BgmStorage.EBgmType.Lobby);
            _runSetupController = new LobbyRunSetupController(new LobbyRunSetupController.InitParam(
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
            _runSetupController.ShowPendingClearReward();
        }

        void RefreshLobbyLabels()
        {
            _uiRoot.SetContinueStage(GetSavedStageNumber());
        }

        static int GetSavedStageNumber()
        {
            IngamePlayData saved = SaveManager.GetIngameData();
            return null == saved ? 0 : saved.StageNumber;
        }

        void OnDestroy()
        {
            LocalizationManager.OnLanguageChanged -= RefreshLobbyLabels;
            UIManager.Hide(UIViewCatalogData.EViewType.Settings);

            if (null != _runSetupController)
            {
                _runSetupController.Dispose();
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
