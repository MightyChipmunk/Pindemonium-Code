using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Pinball.Core;
using Pinball.FX;
using Pinball.Ingame.Cheat;
using Pinball.Ingame.InputModule;
using Pinball.Ingame.Map;
using Pinball.Ingame.Presentation;
using Pinball.Ingame.Reward;
using Pinball.Ingame.Stage;
using Pinball.Ingame.Stat;
using Pinball.Ingame.UI;
using Pinball.Save;
using Pinball.Localization;
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
            Cleared = 4,
            StageSelecting = 5,
        }

        const int DEFAULT_MAP_INDEX = 0;
        const int STAGE_CLEAR_BASE_MONEY = 3;
        const int FIRST_STAGE_NUMBER = 1;
        const int DEFAULT_PASSIVE_SLOT_COUNT = 4;

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
        [SerializeField] StageGimmickCatalogData _gimmickCatalog = null;
        [SerializeField] UIViewCatalogData _viewCatalog = null;
        [SerializeField] SettingsConfigData _settingsConfig = null;
        [SerializeField] LocalizationTableData _localizationTable = null;
        [SerializeField] ComboConfigData _comboConfig = null;
        [SerializeField] ShopConfigData _shopConfig = null;
        [SerializeField] ShopCatalogData _shopCatalog = null;
        [SerializeField] FlipperCatalogData _flipperCatalog = null;
        [SerializeField] ClearRewardCatalogData _rewardCatalog = null;
        [SerializeField] IngameCheatHandler _cheatHandler = null;
        [SerializeField] BlockerView _blockerPrefab = null;

        IngameDirector _director = null;
        PlayerController _playerController = null;
        BallFlowController _ballFlow = null;
        ScoreCounter _scoreCounter = null;
        DamageCalculator _damageCalculator = null;
        StageProgressTracker _stageTracker = null;
        StageGimmickResolver _gimmickResolver = null;
        StageGimmickRoller _gimmickRoller = null;
        IngameStageSelectController _stageSelectController = null;
        MoneyWallet _wallet = null;
        PlayerLoadout _loadout = null;
        StatProvider _statProvider = null;
        PowerSurgeCharger _powerSurgeCharger = null;
        BreakMoneyGranter _breakMoneyGranter = null;
        IngameShopController _shopController = null;
        IngameSaveHandler _saveHandler = null;
        UnlockProgress _unlockProgress = null;
        FlipperData _selectedFlipper = null;
        FlipperData.RunSetup _runSetup = null;
        int _stageClearMoney = STAGE_CLEAR_BASE_MONEY;
        MapRoot _currentMap = null;
        EGameState _gameState = EGameState.Ready;
        bool _isExitingToLobby = false;

        bool IsControlBlocked => _gameState == EGameState.GameOver || _gameState == EGameState.Shopping || _gameState == EGameState.Cleared || _gameState == EGameState.StageSelecting;

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
            _unlockProgress = new UnlockProgress();
            _unlockProgress.Init(_rewardCatalog);

            int startStageNumber = FIRST_STAGE_NUMBER;
            int startMoney = 0;
            int startScore = 0;
            int[] startGimmickIndices = null;
            int flipperIndex = SceneFlow.RequestedFlipperIndex;
            bool isContinuedRun = false;
            if (_saveHandler.TryLoad(out IngamePlayData savedData))
            {
                isContinuedRun = true;
                startStageNumber = savedData.StageNumber;
                startMoney = savedData.Money;
                startScore = savedData.TotalScore;
                startGimmickIndices = savedData.GimmickIndices;
                flipperIndex = ResolveSavedFlipperIndex(savedData.FlipperId);
            }

            _selectedFlipper = ResolveFlipper(flipperIndex);
            _runSetup = null == _selectedFlipper ? FlipperData.DEFAULT_RUN_SETUP : _selectedFlipper.GetRunSetup();
            _stageClearMoney = Mathf.Max(0, STAGE_CLEAR_BASE_MONEY + _runSetup.StageClearMoneyAdd);

            bool isNewRun = !isContinuedRun;
            if (isNewRun)
                startMoney = Mathf.Max(0, startMoney + _runSetup.StartMoneyAdd);

            _wallet = new MoneyWallet();
            _wallet.Init(startMoney);
            _loadout = new PlayerLoadout();
            int baseSlotCount = null == _shopConfig ? DEFAULT_PASSIVE_SLOT_COUNT : _shopConfig.PassiveSlotCount;
            _loadout.Init(Mathf.Max(1, baseSlotCount + _runSetup.PassiveSlotAdd));
            if (isNewRun)
                GrantStartPassives(_runSetup.StartPassiveCount);
            else
                RestoreLoadout(savedData);
            _statProvider = new StatProvider();
            _statProvider.Init(_statData, _loadout, _selectedFlipper, GetRemainingBallCount, GetComboCount);
            _powerSurgeCharger = new PowerSurgeCharger();
            _powerSurgeCharger.Init(_statProvider, GetIsPlaying);
            _breakMoneyGranter = new BreakMoneyGranter();
            _breakMoneyGranter.Init(_wallet, _statProvider);
            _gimmickRoller = new StageGimmickRoller();
            _gimmickRoller.Init(_gimmickCatalog);
            _gimmickResolver = new StageGimmickResolver();
            _gimmickResolver.Init(_gimmickRoller);
            _stageTracker = new StageProgressTracker();
            _stageTracker.Init(_stageCatalog, startStageNumber, startScore, _gimmickResolver);
            RestoreCurrentStageGimmicks(startGimmickIndices);
            _stageTracker.OnStageStarted += OnStageStarted;
            _stageTracker.OnStageCleared += OnStageCleared;
            _stageTracker.OnAllStagesCleared += OnAllStagesCleared;
            _stageTracker.OnStageScoreChanged += OnStageScoreChanged;
            _stageTracker.OnRemainingSecondsChanged += OnStageRemainingSecondsChanged;
            _stageTracker.OnTimeExpired += OnStageTimeExpired;
            _scoreCounter = new ScoreCounter();
            _scoreCounter.Init(startScore, _comboConfig, _statProvider, _uiRoot.PlayScorePopup, _gimmickResolver.GetGimmickScoreMultiplier);
            _scoreCounter.OnComboChanged += OnComboChanged;
            _scoreCounter.OnComboEnded += OnComboEnded;
            _shopController = new IngameShopController();
            _shopController.Init(new IngameShopController.InitParam(
                Config: _shopConfig,
                Catalog: _shopCatalog,
                Wallet: _wallet,
                Loadout: _loadout,
                IsItemUnlockedGetter: _unlockProgress.IsItemUnlocked,
                OfferSlotCount: ShopSession.DEFAULT_OFFER_SLOT_COUNT + _runSetup.ShopOfferSlotAdd,
                IsInterestDisabled: _runSetup.IsInterestDisabled));
            _shopController.OnNextStageRequested += OpenNextStageSelect;
            _shopController.OnLoadoutChanged += SaveProgress;
            _stageSelectController = new IngameStageSelectController();
            _stageSelectController.Init(new IngameStageSelectController.InitParam(
                StageCatalog: _stageCatalog,
                Tracker: _stageTracker,
                Roller: _gimmickRoller,
                Resolver: _gimmickResolver,
                Wallet: _wallet,
                RemainingBallGetter: GetRemainingBallCount,
                StageClearBaseMoney: _stageClearMoney));
            _stageSelectController.OnStageStartRequested += StartCurrentStage;
            _stageSelectController.OnGimmickRerolled += SaveProgress;

            if (null != _cheatHandler)
                _cheatHandler.Init(new IngameCheatHandler.InitParam(
                    Catalog: _shopCatalog,
                    Wallet: _wallet,
                    Loadout: _loadout,
                    OnCheatApplied: OnCheatApplied));

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
            _damageCalculator = new DamageCalculator();
            _damageCalculator.Init(new DamageCalculator.InitParam(
                AttackPowerGetter: _statProvider.GetBallAttackPower,
                FlatDamageGetter: _statProvider.GetHitFlatDamage,
                SurgeMultiplierConsumer: _powerSurgeCharger.ConsumeMultiplier,
                IsCloneBallGetter: _ballFlow.IsCloneBall,
                CloneDamageRatioGetter: _statProvider.GetCloneDamageRatio));
            _powerSurgeCharger.OnChargedChanged += _ballFlow.SetSurgeCharged;
            _playerController = new PlayerController();
            _playerController.Init(new PlayerController.InitParam(
                InputRouter: _inputRouter,
                IsControlBlockedGetter: GetIsControlBlocked,
                IsPlayingGetter: GetIsPlaying,
                NudgeImpulseApplier: _ballFlow.TryApplyImpulseToActiveBall,
                NudgePresenter: _director.PlayNudge));
            _playerController.OnLaunched += OnBallLaunched;

            _scoreCounter.OnChanged += _uiRoot.SetScore;
            _ballFlow.OnBallCountChanged += _uiRoot.SetBallCount;
            _uiRoot.SetScore(_scoreCounter.Current);
            _uiRoot.SetBallCount(_ballFlow.RemainingBalls);

            GameSettings.EnsureInit(_settingsConfig);
            LocalizationManager.EnsureInit();
            LocalizationTable.EnsureInit(_localizationTable);
            UIManager.EnsureInit(_viewCatalog);
            Blocker.EnsureInit(_blockerPrefab);
            UIManager.OnEvent += OnUIEvent;
            _uiRoot.OnPauseClicked += OnPauseButtonClicked;
            _scoreCounter.OnChanged += _stageTracker.SetTotalScore;

            OpenStageSelect();
        }

        public void Release()
        {
            UIManager.OnEvent -= OnUIEvent;
            UIManager.HideAll();

            if (null != _cheatHandler)
                _cheatHandler.Release();

            if (null != _stageSelectController)
            {
                _stageSelectController.Release();
                _stageSelectController = null;
            }

            if (null != _stageTracker)
            {
                _stageTracker.Release();
                _stageTracker = null;
            }

            if (null != _gimmickResolver)
            {
                _gimmickResolver.Release();
                _gimmickResolver = null;
            }

            if (null != _gimmickRoller)
            {
                _gimmickRoller.Release();
                _gimmickRoller = null;
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

            if (null != _powerSurgeCharger)
            {
                _powerSurgeCharger.Release();
                _powerSurgeCharger = null;
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

            if (null != _unlockProgress)
            {
                _unlockProgress.Release();
                _unlockProgress = null;
            }

            _selectedFlipper = null;
            _runSetup = null;

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
            _powerSurgeCharger.Tick(Time.deltaTime);
            _ballFlow.Tick(Time.deltaTime);

            if (!IsControlBlocked)
                _stageTracker.Tick(Time.deltaTime);
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
                bumper.Ignite(burnDamage, _statProvider.GetEffectValue(ShopItemData.EEffectType.BumperBurn, ShopItemData.SUB_VALUE_INDEX), PoolableFX.EFXType.Fireball);
            }

            if (_statProvider.HasEffect(ShopItemData.EEffectType.BumperFreeze))
                bumper.ApplyFrostHit(_statProvider.GetEffectSum(ShopItemData.EEffectType.BumperFreeze), PoolableFX.EFXType.Frostbite, PoolableFX.EFXType.FrostShatter);

            if (!_statProvider.HasEffect(ShopItemData.EEffectType.BumperChainLightning))
                return;

            float chainRatio = _statProvider.GetEffectSum(ShopItemData.EEffectType.BumperChainLightning);
            int maxChainCount = Mathf.RoundToInt(_statProvider.GetEffectValue(ShopItemData.EEffectType.BumperChainLightning, ShopItemData.SUB_VALUE_INDEX));
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
            ShowStageInfo();
        }

        void OnStageCleared(int stageNumber, StageData stage)
        {
            _gameState = EGameState.Shopping;
            _playerController.ResetControlStates();
            _scoreCounter.EndCombo();
            _ballFlow.DespawnBalls();

            int remainingBalls = _ballFlow.RemainingBalls;
            int resupplyMoney = Mathf.RoundToInt(remainingBalls * _statProvider.GetBallResupplyMoney());
            _wallet.Add(remainingBalls + _stageClearMoney + _shopController.CalculateInterest() + resupplyMoney);
            SaveProgress();

            if (!UIManager.IsInitialized)
            {
                OpenNextStageSelect();
                StartCurrentStage();
                return;
            }

            UIManager.Show(new StageClearView.Args(stageNumber));
        }

        void OnAllStagesCleared()
        {
            _gameState = EGameState.Cleared;
            _playerController.ResetControlStates();
            _scoreCounter.EndCombo();
            _ballFlow.DespawnBalls();
            _saveHandler.ClearPlayData();
            ClearRewardData reward = _unlockProgress.GrantClearReward();
            ShowGameResult(GameResultView.EResult.Clear);
            ShowRewardUnlock(reward);
        }

        void ShowRewardUnlock(ClearRewardData reward)
        {
            if (null == reward)
                return;

            int entryCount = reward.FlipperCount + reward.ItemCount;
            if (entryCount <= 0)
                return;

            RewardUnlockView.Entry[] entries = new RewardUnlockView.Entry[entryCount];
            for (int i = 0; i < reward.FlipperCount; ++i)
                entries[i] = new RewardUnlockView.Entry(RewardUnlockView.ERewardKind.Flipper, GetFlipperCatalogIndex(reward.GetFlipper(i)));

            for (int i = 0; i < reward.ItemCount; ++i)
                entries[reward.FlipperCount + i] = new RewardUnlockView.Entry(RewardUnlockView.ERewardKind.ShopItem, GetCatalogIndex(reward.GetItem(i)));

            UIManager.Show(new RewardUnlockView.Args(entries));
        }

        string[] BuildPassiveIds()
        {
            string[] passiveIds = new string[_loadout.PassiveCount];
            for (int i = 0; i < passiveIds.Length; ++i)
                passiveIds[i] = _loadout.GetPassive(i).GetId();

            return passiveIds;
        }

        IngamePlayData.UpgradeEntry[] BuildUpgradeEntries()
        {
            IngamePlayData.UpgradeEntry[] entries = new IngamePlayData.UpgradeEntry[_loadout.UpgradeKindCount];
            for (int i = 0; i < entries.Length; ++i)
            {
                entries[i] = new IngamePlayData.UpgradeEntry();
                entries[i].Id = _loadout.GetUpgrade(i).GetId();
                entries[i].Count = _loadout.GetUpgradeCount(i);
            }

            return entries;
        }

        void RestoreLoadout(IngamePlayData savedData)
        {
            if (null == savedData || null == _shopCatalog)
                return;

            if (null != savedData.PassiveIds)
            {
                for (int i = 0; i < savedData.PassiveIds.Length; ++i)
                    _loadout.TryAddPassive(_shopCatalog.FindById(savedData.PassiveIds[i]));
            }

            if (null == savedData.Upgrades)
                return;

            for (int i = 0; i < savedData.Upgrades.Length; ++i)
            {
                IngamePlayData.UpgradeEntry entry = savedData.Upgrades[i];
                ShopItemData item = _shopCatalog.FindById(entry.Id);
                if (null == item)
                    continue;

                for (int k = 0; k < entry.Count; ++k)
                    _loadout.AddUpgrade(item);
            }
        }

        void GrantStartPassives(int passiveCount)
        {
            if (passiveCount <= 0 || null == _shopCatalog)
                return;

            List<ShopItemData> candidates = new List<ShopItemData>();
            for (int i = 0; i < _shopCatalog.Count; ++i)
            {
                ShopItemData item = _shopCatalog.GetItem(i);
                if (null == item || item.Kind != ShopItemData.EItemKind.Passive)
                    continue;

                if (!_unlockProgress.IsItemUnlocked(item))
                    continue;

                if (!_loadout.CanAddPassive(item))
                    continue;

                candidates.Add(item);
            }

            for (int i = 0; i < passiveCount && candidates.Count > 0; ++i)
            {
                int pickIndex = UnityEngine.Random.Range(0, candidates.Count);
                _loadout.TryAddPassive(candidates[pickIndex]);
                candidates.RemoveAt(pickIndex);
            }
        }

        int GetFlipperCatalogIndex(FlipperData flipper)
        {
            if (null == _flipperCatalog)
                return FlipperCatalogData.INVALID_INDEX;

            return _flipperCatalog.IndexOf(flipper);
        }

        int ResolveSavedFlipperIndex(string flipperId)
        {
            if (null == _flipperCatalog)
                return FlipperCatalogData.DEFAULT_INDEX;

            int index = _flipperCatalog.IndexOfId(flipperId);
            return index == FlipperCatalogData.INVALID_INDEX ? FlipperCatalogData.DEFAULT_INDEX : index;
        }

        FlipperData ResolveFlipper(int flipperIndex)
        {
            if (null == _flipperCatalog)
                return null;

            FlipperData flipper = _flipperCatalog.GetFlipper(flipperIndex);
            if (null != flipper && _unlockProgress.IsFlipperUnlocked(flipper))
                return flipper;

            return _flipperCatalog.GetFlipper(FlipperCatalogData.DEFAULT_INDEX);
        }

        void OnStageScoreChanged(int stageScore)
        {
            ShowStageInfo();
        }

        void OnStageRemainingSecondsChanged(int remainingSeconds)
        {
            ShowStageInfo();
        }

        void OnStageTimeExpired()
        {
            _scoreCounter.EndCombo();
            _ballFlow.DespawnBalls();
            EndGame();
        }

        void ShowStageInfo()
        {
            if (null == _stageTracker.GetCurrentStage())
                return;

            int remainingSeconds = _stageTracker.HasTimeLimit ? _stageTracker.RemainingSeconds : StageInfoView.NO_TIME_LIMIT;
            UIManager.Show(new StageInfoView.Args(_stageTracker.StageNumber, _stageTracker.TargetScore, _stageTracker.StageScore, remainingSeconds));
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
                FlipperParam: new Flipper.InitParam(
                    FlipPowerGetter: _statProvider.GetFlipPower,
                    PerfectTimingRelaxGetter: _statProvider.GetPerfectTimingRelax,
                    PerfectImpulseScaleGetter: _statProvider.GetPerfectImpulseScale,
                    LengthScaleGetter: _statProvider.GetFlipperLengthScale,
                    RotateSpeedScaleGetter: _statProvider.GetFlipperRotateSpeedScale),
                OnBallStruck: OnBallStruck,
                OnBumperDamaged: OnBumperDamaged,
                OnBumperBroken: OnBumperBroken,
                OnSpringChargeEvent: _director.PlaySpringCharge,
                FXPlayer: _director.PlayFX,
                LineFXPlayer: _director.PlayLineFX,
                OnGimmickBallHit: _ballFlow.CountGimmickHit,
                IsGimmickAbsentGetter: _gimmickResolver.IsGimmickAbsent,
                ScoreGainLimitGetter: _gimmickResolver.GetGimmickScoreGainLimit,
                IsBumperUnbreakable: _gimmickResolver.IsBumperUnbreakable()));
        }

        void EndGame()
        {
            _gameState = EGameState.GameOver;
            _playerController.ResetControlStates();
            _saveHandler.ClearPlayData();
            ShowGameResult(GameResultView.EResult.GameOver);
        }

        void ShowGameResult(GameResultView.EResult result)
        {
            UIManager.Show(new GameResultView.Args(result, BuildResultStats()));
        }

        ResultStatsView.Args BuildResultStats()
        {
            int[] passiveIndices = new int[_loadout.PassiveCount];
            for (int i = 0; i < passiveIndices.Length; ++i)
                passiveIndices[i] = GetCatalogIndex(_loadout.GetPassive(i));

            ResultStatsView.ItemCountEntry[] upgrades = new ResultStatsView.ItemCountEntry[_loadout.UpgradeKindCount];
            for (int i = 0; i < upgrades.Length; ++i)
                upgrades[i] = new ResultStatsView.ItemCountEntry(GetCatalogIndex(_loadout.GetUpgrade(i)), _loadout.GetUpgradeCount(i));

            return new ResultStatsView.Args(
                StageNumber: _stageTracker.StageNumber,
                Score: _scoreCounter.Current,
                Money: _wallet.Current,
                UsedBallCount: _ballFlow.UsedBallCount,
                PassiveIndices: passiveIndices,
                Upgrades: upgrades);
        }

        int GetCatalogIndex(ShopItemData item)
        {
            if (null == _shopCatalog)
                return ShopCatalogData.INVALID_INDEX;

            return _shopCatalog.IndexOf(item);
        }

        void SaveProgress()
        {
            _saveHandler.Save(new IngameSaveHandler.SaveParam(
                StageNumber: _stageTracker.StageNumber,
                Money: _wallet.Current,
                TotalScore: _scoreCounter.Current,
                GimmickIndices: _gimmickRoller.GetCatalogIndices(),
                FlipperId: null == _selectedFlipper ? string.Empty : _selectedFlipper.GetId(),
                PassiveIds: BuildPassiveIds(),
                Upgrades: BuildUpgradeEntries()));
        }

        void RestoreCurrentStageGimmicks(int[] savedGimmickIndices)
        {
            if (null == savedGimmickIndices || savedGimmickIndices.Length <= 0)
            {
                _gimmickRoller.SetBaseGimmicks(_stageTracker.GetCurrentStage());
                return;
            }

            _gimmickRoller.RestoreGimmicks(savedGimmickIndices);
        }

        void OnCheatApplied()
        {
            _shopController.RefreshOpenedView();
            _stageSelectController.RefreshOpenedView();
        }

        void OnUIEvent(UIManager.EEvent uiEvent, int payload)
        {
            if (uiEvent == UIManager.EEvent.ExitToLobbyRequested)
                OnLobbyExitRequested();
        }

        void OpenNextStageSelect()
        {
            _breakMoneyGranter.ResetStage();
            _ballFlow.RefillStock();
            _stageTracker.AdvanceToNextStage();
            _gimmickRoller.SetBaseGimmicks(_stageTracker.GetCurrentStage());
            OpenStageSelect();
        }

        void OpenStageSelect()
        {
            _gameState = EGameState.StageSelecting;
            _playerController.ResetControlStates();
            _stageSelectController.Open();
        }

        void StartCurrentStage()
        {
            _stageTracker.StartCurrentStage();
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
            if (IsControlBlocked)
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
