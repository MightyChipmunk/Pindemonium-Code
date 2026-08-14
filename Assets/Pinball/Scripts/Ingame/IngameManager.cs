using Cysharp.Threading.Tasks;
using Pinball.Core;
using Pinball.FX;
using Pinball.Ingame.InputModule;
using Pinball.Ingame.Map;
using Pinball.Ingame.Presentation;
using Pinball.Ingame.Stage;
using Pinball.Ingame.Stat;
using Pinball.Ingame.UI;
using Pinball.Save;
using Pinball.Settings;
using Pinball.UI;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame
{
    public class IngameManager : BehaviourBase
    {
        public enum EGameState
        {
            Ready = 0,
            Playing = 1,
            GameOver = 2,
            Shopping = 3,
        }

        const int DEFAULT_MAP_INDEX = 0;
        const int STAGE_CLEAR_BASE_MONEY = 3;
        const int FIRST_STAGE_NUMBER = 1;
        const int DEFAULT_PASSIVE_SLOT_COUNT = 3;

        [SerializeField] IngameInputRouter _inputRouter = null;
        [SerializeField] BallSpawner _ballSpawner = null;
        [SerializeField] BallDrainZone _drainZone = null;
        [SerializeField] MapLoader _mapLoader = null;
        [SerializeField] IngameUIRoot _uiRoot = null;
        [SerializeField] CameraController _cameraController = null;
        [SerializeField] PinballStatData _statData = null;
        [SerializeField] PresentationConfigData _presentationConfig = null;
        [SerializeField] FXPoolData _fxPoolData = null;
        [SerializeField] StageCatalogData _stageCatalog = null;
        [SerializeField] UIViewCatalogData _viewCatalog = null;
        [SerializeField] SettingsConfigData _settingsConfig = null;
        [SerializeField] ComboConfigData _comboConfig = null;
        [SerializeField] ShopConfigData _shopConfig = null;
        [SerializeField] ShopCatalogData _shopCatalog = null;

        IngameDirector _director = null;
        PlayerController _playerController = null;
        BallFlowController _ballFlow = null;
        ScoreCounter _scoreCounter = null;
        DamageCalculator _damageCalculator = null;
        StageProgressTracker _stageTracker = null;
        MoneyWallet _wallet = null;
        PlayerLoadout _loadout = null;
        StatProvider _statProvider = null;
        BreakMoneyGranter _breakMoneyGranter = null;
        IngameShopController _shopController = null;
        IngameSaveHandler _saveHandler = null;
        MapRoot _currentMap = null;
        EGameState _gameState = EGameState.Ready;
        bool _isExitingToLobby = false;

        bool IsControlBlocked => _gameState == EGameState.GameOver || _gameState == EGameState.Shopping;

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
            _saveHandler = new IngameSaveHandler();

            int startStageNumber = FIRST_STAGE_NUMBER;
            int startMoney = 0;
            int startScore = 0;
            if (_saveHandler.TryLoad(out int savedStageNumber, out int savedMoney, out int savedScore))
            {
                startStageNumber = savedStageNumber;
                startMoney = savedMoney;
                startScore = savedScore;
            }

            _wallet = new MoneyWallet();
            _wallet.Init(startMoney);
            _loadout = new PlayerLoadout();
            _loadout.Init(null == _shopConfig ? DEFAULT_PASSIVE_SLOT_COUNT : _shopConfig.PassiveSlotCount);
            _statProvider = new StatProvider();
            _statProvider.Init(_statData, _loadout, GetRemainingBallCount, GetComboCount);
            _breakMoneyGranter = new BreakMoneyGranter();
            _breakMoneyGranter.Init(_wallet, _statProvider);
            _damageCalculator = new DamageCalculator();
            _damageCalculator.Init(_statProvider.GetBallAttackPower);
            _scoreCounter = new ScoreCounter();
            _scoreCounter.Init(startScore, _comboConfig, _statProvider, _uiRoot.PlayScorePopup);
            _scoreCounter.OnComboChanged += OnComboChanged;
            _scoreCounter.OnComboEnded += OnComboEnded;
            _shopController = new IngameShopController();
            _shopController.Init(_shopConfig, _shopCatalog, _wallet, _loadout);
            _shopController.OnNextStageRequested += RestartNextStage;

            _inputRouter.Init();
            _inputRouter.OnEscapePressed += OnEscapePressed;
            _ballSpawner.Init();
            _drainZone.Init();
            _mapLoader.Init();
            _uiRoot.Init();
            _cameraController.Init();
            _director = new IngameDirector();
            _director.Init(_presentationConfig, _fxPoolData, _cameraController.ShakeCam);
            _ballFlow = new BallFlowController();
            _ballFlow.Init(new BallFlowController.InitParam(
                Spawner: _ballSpawner,
                DrainZone: _drainZone,
                Stats: _statProvider,
                IsControlBlockedGetter: GetIsControlBlocked,
                IsPlayingGetter: GetIsPlaying));
            _ballFlow.OnBallLost += OnBallLost;
            _ballFlow.OnBallReady += OnBallReady;
            _ballFlow.OnAllBallsLost += EndGame;
            _playerController = new PlayerController();
            _playerController.Init(new PlayerController.InitParam(
                InputRouter: _inputRouter,
                IsControlBlockedGetter: GetIsControlBlocked,
                IsPlayingGetter: GetIsPlaying,
                NudgeImpulseApplier: _ballFlow.TryApplyImpulseToActiveBall,
                NudgePresenter: _director.PlayNudge));
            _playerController.OnLaunched += OnBallLaunched;

            LoadMap();

            _scoreCounter.OnChanged += _uiRoot.SetScore;
            _ballFlow.OnBallCountChanged += _uiRoot.SetBallCount;
            _uiRoot.SetScore(_scoreCounter.Current);
            _uiRoot.SetBallCount(_ballFlow.RemainingBalls);

            GameSettings.EnsureInit(_settingsConfig);
            UIManager.EnsureInit(_viewCatalog);
            UIManager.OnEvent += OnUIEvent;
            _uiRoot.OnPauseClicked += OnPauseButtonClicked;
            _stageTracker = new StageProgressTracker();
            _stageTracker.Init(_stageCatalog, startStageNumber, startScore);
            _stageTracker.OnStageStarted += OnStageStarted;
            _stageTracker.OnStageCleared += OnStageCleared;
            _stageTracker.OnStageScoreChanged += OnStageScoreChanged;
            _scoreCounter.OnChanged += _stageTracker.SetTotalScore;
            _stageTracker.StartFirstStage();

            _ballFlow.LoadBallOnSpring();
            _gameState = EGameState.Ready;
        }

        public void Release()
        {
            UIManager.OnEvent -= OnUIEvent;
            UIManager.HideAll();

            if (null != _stageTracker)
            {
                _stageTracker.Release();
                _stageTracker = null;
            }

            if (null != _ballFlow)
            {
                _ballFlow.Release();
                _ballFlow = null;
            }

            if (null != _playerController)
            {
                _playerController.Release();
                _playerController = null;
            }

            if (null != _shopController)
            {
                _shopController.Release();
                _shopController = null;
            }

            if (null != _scoreCounter)
            {
                _scoreCounter.Release();
                _scoreCounter = null;
            }

            if (null != _damageCalculator)
            {
                _damageCalculator.Release();
                _damageCalculator = null;
            }

            if (null != _breakMoneyGranter)
            {
                _breakMoneyGranter.Release();
                _breakMoneyGranter = null;
            }

            if (null != _statProvider)
            {
                _statProvider.Release();
                _statProvider = null;
            }

            if (null != _loadout)
            {
                _loadout.Release();
                _loadout = null;
            }

            if (null != _wallet)
            {
                _wallet.Release();
                _wallet = null;
            }

            if (null != _drainZone)
                _drainZone.Release();

            if (null != _inputRouter)
                _inputRouter.Release();

            if (null != _uiRoot)
                _uiRoot.Release();

            if (null != _mapLoader)
                _mapLoader.Release();

            _currentMap = null;

            if (null != _director)
            {
                _director.Release();
                _director = null;
            }

            if (null != _cameraController)
                _cameraController.Release();

            if (null != _ballSpawner)
                _ballSpawner.Release();

            _saveHandler = null;
        }

        void Update()
        {
            if (null == _scoreCounter)
                return;

            _scoreCounter.Tick(Time.deltaTime);
            _playerController.Tick(Time.deltaTime);
        }

        void OnBallStruck(Flipper.EHitGrade grade, Vector2 worldPoint)
        {
            _director.PlayFlipperHit(grade, worldPoint);
            _scoreCounter.HandleBallStruck(grade, worldPoint);
        }

        void OnBumperDamaged(BumperInteractable bumper, float damage)
        {
            if (_statProvider.HasEffect(ShopItemData.EEffectType.BumperBurn))
            {
                float burnDamage = damage * _statProvider.GetEffectSum(ShopItemData.EEffectType.BumperBurn);
                bumper.Ignite(burnDamage, _statProvider.GetEffectSub(ShopItemData.EEffectType.BumperBurn), PoolableFX.EFXType.Fireball);
            }

            if (_statProvider.HasEffect(ShopItemData.EEffectType.BumperFreeze))
                bumper.ApplyFrostHit(_statProvider.GetEffectSum(ShopItemData.EEffectType.BumperFreeze), PoolableFX.EFXType.Frostbite, PoolableFX.EFXType.FrostShatter);

            if (!_statProvider.HasEffect(ShopItemData.EEffectType.BumperChainLightning))
                return;

            float chainRatio = _statProvider.GetEffectSum(ShopItemData.EEffectType.BumperChainLightning);
            int maxChainCount = Mathf.RoundToInt(_statProvider.GetEffectSub(ShopItemData.EEffectType.BumperChainLightning));
            _currentMap.ApplyChainDamage(bumper, damage, chainRatio, maxChainCount);
        }

        void OnBumperBroken()
        {
            _breakMoneyGranter.HandleGimmickBroken();
        }

        void OnComboChanged(int comboCount, float multiplier, int pendingBonus)
        {
            UIManager.Show(new ComboView.Args(comboCount, multiplier, pendingBonus));
        }

        void OnComboEnded(int comboCount, int bonusScore)
        {
            if (bonusScore <= 0)
            {
                UIManager.Hide(UIViewCatalogData.EViewType.Combo);
                return;
            }

            UIManager.Show(new ComboView.BonusArgs(bonusScore));
        }

        void OnStageStarted(int stageNumber, StageData stage)
        {
            _saveHandler.UpdateBestStage(stageNumber);
            SaveProgress();

            if (null == stage)
                return;

            UIManager.Show(new StageInfoView.Args(stageNumber, stage.TargetScore, 0));
        }

        void OnStageCleared(int stageNumber, StageData stage)
        {
            _gameState = EGameState.Shopping;
            _playerController.ResetControlStates();
            _scoreCounter.EndCombo();
            _ballFlow.DespawnActiveBall();

            int remainingBalls = _ballFlow.RemainingBalls;
            int resupplyMoney = Mathf.RoundToInt(remainingBalls * _statProvider.GetEffectSum(ShopItemData.EEffectType.BallResupplyMoney));
            _wallet.Add(remainingBalls + STAGE_CLEAR_BASE_MONEY + _shopController.CalculateInterest() + resupplyMoney);
            SaveProgress();

            if (!UIManager.IsInitialized)
            {
                RestartNextStage();
                return;
            }

            UIManager.Show(new StageClearView.Args(stageNumber));
        }

        void OnStageScoreChanged(int stageScore)
        {
            StageData stage = _stageTracker.GetCurrentStage();
            if (null == stage)
                return;

            UIManager.Show(new StageInfoView.Args(_stageTracker.StageNumber, stage.TargetScore, stageScore));
        }

        void OnBallLaunched()
        {
            _gameState = EGameState.Playing;
            _playerController.ResetNudgeCooldown();
        }

        void OnBallLost()
        {
            _scoreCounter.EndCombo();
        }

        void OnBallReady()
        {
            _gameState = EGameState.Ready;
        }

        bool GetIsControlBlocked()
        {
            return IsControlBlocked;
        }

        bool GetIsPlaying()
        {
            return _gameState == EGameState.Playing;
        }

        int GetRemainingBallCount()
        {
            if (null == _ballFlow)
                return 0;

            return _ballFlow.RemainingBalls;
        }

        int GetComboCount()
        {
            if (null == _scoreCounter)
                return 0;

            return _scoreCounter.ComboCount;
        }

        void LoadMap()
        {
            _currentMap = _mapLoader.Load(DEFAULT_MAP_INDEX);
            _playerController.SetMap(_currentMap);
            _ballFlow.SetMap(_currentMap);
            if (null == _currentMap)
                return;

            _currentMap.Init(new MapRoot.InitParam(
                OnScoreGained: _scoreCounter.HandleMapScoreGained,
                DamageEvaluator: _damageCalculator.Calculate,
                FlipPowerGetter: _statProvider.GetFlipPower,
                PerfectTimingRelaxGetter: _statProvider.GetPerfectTimingRelax,
                OnBallStruck: OnBallStruck,
                OnBumperDamaged: OnBumperDamaged,
                OnBumperBroken: OnBumperBroken,
                OnSpringChargeEvent: _director.PlaySpringCharge,
                FXPlayer: _director.PlayFX,
                LineFXPlayer: _director.PlayLineFX,
                OnGimmickBallHit: _ballFlow.CountGimmickHit));
        }

        void EndGame()
        {
            _gameState = EGameState.GameOver;
            _playerController.ResetControlStates();
            _saveHandler.ClearOnGameOver();
            Debug.Log("[IngameManager] Game Over");
        }

        void SaveProgress()
        {
            _saveHandler.Save(_stageTracker.StageNumber, _wallet.Current, _scoreCounter.Current);
        }

        void OnUIEvent(UIManager.EEvent uiEvent, int payload)
        {
            if (uiEvent == UIManager.EEvent.ExitToLobbyRequested)
                OnLobbyExitRequested();
        }

        void RestartNextStage()
        {
            _breakMoneyGranter.ResetStage();
            _ballFlow.RefillStock();
            _stageTracker.AdvanceToNextStage();
            LoadMap();
            _ballFlow.LoadBallOnSpring();
            _gameState = EGameState.Ready;
        }

        void OnEscapePressed()
        {
            TogglePausePopup();
        }

        void OnPauseButtonClicked()
        {
            TogglePausePopup();
        }

        void TogglePausePopup()
        {
            if (_gameState == EGameState.Shopping)
                return;

            if (UIManager.IsOpen(UIViewCatalogData.EViewType.Settings))
                return;

            if (UIManager.IsOpen(UIViewCatalogData.EViewType.PausePopup))
            {
                UIManager.Hide(UIViewCatalogData.EViewType.PausePopup);
                return;
            }

            UIManager.Show(new PausePopupView.Args());
        }

        void OnLobbyExitRequested()
        {
            if (_isExitingToLobby)
                return;

            _isExitingToLobby = true;
            SceneFlow.LoadLobbyAsync().Forget();
        }
    }
}
