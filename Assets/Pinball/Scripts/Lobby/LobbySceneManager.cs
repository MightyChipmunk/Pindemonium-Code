using Cysharp.Threading.Tasks;
using Pinball.Audio;
using Pinball.Core;
using Pinball.Ingame;
using Pinball.Ingame.Reward;
using Pinball.Ingame.Stage;
using Pinball.Ingame.Tutorial;
using Pinball.Save;
using Pinball.UI;
using UnityEngine;
using GameCore.Localization;
using GameCore.UI;
using GameCore.Util;

namespace Pinball.Lobby
{
    public class LobbySceneManager : BehaviourBase
    {
        const int NO_SAVED_STAGE_NUMBER = 0;

        [SerializeField] GameBootStrapConfig _bootStrapConfig = null;
        [SerializeField] FlipperCatalogData _flipperCatalog = null;
        [SerializeField] ClearRewardCatalogData _rewardCatalog = null;
        [SerializeField] StageCatalogData _stageCatalog = null;
        [SerializeField] StageGimmickCatalogData _gimmickCatalog = null;
        [SerializeField] EndlessConfigData _endlessConfig = null;
        [SerializeField] LobbyUIRoot _uiRoot = null;

        LobbyRunSetupController _runSetupController = null;
        TutorialProgress _tutorialProgress = null;
        bool _isLoadingIngame = false;

        void Awake()
        {
            GameBootStrap.EnsureInit(_bootStrapConfig);
            _tutorialProgress = new TutorialProgress();
            BgmManager.Play(BgmStorage.EBgmType.Lobby);
            _runSetupController = new LobbyRunSetupController(new LobbyRunSetupController.InitParam(
                FlipperCatalog: _flipperCatalog,
                RewardCatalog: _rewardCatalog,
                StageCatalog: _stageCatalog,
                GimmickCatalog: _gimmickCatalog,
                EndlessConfig: _endlessConfig));
            _runSetupController.OnPlayRequested += OnPlayRequested;
            _runSetupController.OnContinueRequested += OnContinueConfirmed;
            _uiRoot.Init(OnContinueRequested, OnNewGameRequested, OnSettingsRequested, OnQuitRequested);
            _uiRoot.SetContinueInteractable(SaveManager.HasIngameSave);
            RefreshLobbyLabels();
            LocalizationManager.OnLanguageChanged += RefreshLobbyLabels;
            _runSetupController.ShowPendingClearReward();
        }

        void RefreshLobbyLabels()
        {
            if (_runSetupController.TryGetSavedStageDisplay(out int stageNumber, out bool isEndless))
                _uiRoot.SetContinueStage(stageNumber, isEndless);
            else
                _uiRoot.SetContinueStage(NO_SAVED_STAGE_NUMBER, false);
        }

        void OnDestroy()
        {
            LocalizationManager.OnLanguageChanged -= RefreshLobbyLabels;
            UIManager.HideImmediate(UIViewCatalogData.EViewType.Settings);

            if (null != _runSetupController)
            {
                _runSetupController.Dispose();
                _runSetupController = null;
            }

            if (null != _uiRoot)
                _uiRoot.Release();

            _tutorialProgress = null;
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

            if (!_tutorialProgress.IsCompleted(TutorialProgress.EBlock.PlayIntro))
            {
                OnPlayRequested(SceneFlow.DEFAULT_FLIPPER_INDEX);
                return;
            }

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

        void OnQuitRequested()
        {
            if (_isLoadingIngame)
                return;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif // UNITY_EDITOR
        }

#if UNITY_EDITOR
        protected override void OnBindSerializedField()
        {
            _uiRoot = GetComponentInChildren<LobbyUIRoot>(true);
        }
#endif // UNITY_EDITOR
    }
}
