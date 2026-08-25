using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Pinball.Audio;
using Pinball.Core;
using Pinball.FX;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Pinball.Ingame.Cheat;
#endif
using Pinball.Ingame.Input;
using Pinball.Ingame.Map;
using Pinball.Ingame.Player;
using Pinball.Ingame.Presentation;
using Pinball.Ingame.Reward;
using Pinball.Ingame.Run;
using Pinball.Ingame.Save;
using Pinball.Ingame.Score;
using Pinball.Ingame.Shop;
using Pinball.Ingame.Stage;
using Pinball.Ingame.Stat;
using Pinball.Ingame.State;
using Pinball.Ingame.UI;
using Pinball.Save;
using Pinball.UI;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame
{
    public class IngameManager : BehaviourBase
    {
        const int DEFAULT_MAP_INDEX = 0;
        const int STAGE_CLEAR_BASE_MONEY = 3;
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
        [SerializeField] StageGimmickCatalogData _gimmickCatalog = null;
        [SerializeField] GameBootStrapConfig _bootStrapConfig = null;
        [SerializeField] ComboConfigData _comboConfig = null;
        [SerializeField] ShopConfigData _shopConfig = null;
        [SerializeField] ShopCatalogData _shopCatalog = null;
        [SerializeField] CardPackCatalogData _cardPackCatalog = null;
        [SerializeField] VoucherCatalogData _voucherCatalog = null;
        [SerializeField] FlipperCatalogData _flipperCatalog = null;
        [SerializeField] ClearRewardCatalogData _rewardCatalog = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] IngameCheatHandler _cheatHandler = null;
#endif

        readonly List<Action> _releaseActions = new List<Action>();

        IngameStateMachine _stateMachine = null;
        StageSelectState _stageSelectState = null;
        ShopState _shopState = null;
        PlayState _playState = null;
        ResultState _resultState = null;
        IngameDirector _director = null;
        IngameResultPresenter _resultPresenter = null;
        PlayerController _playerController = null;
        BallFlowController _ballFlow = null;
        ScoreCounter _scoreCounter = null;
        SlingshotScoreGrowth _slingshotGrowth = null;
        GimmickScoreTracker _gimmickScoreTracker = null;
        DamageCalculator _damageCalculator = null;
        StageProgressTracker _stageTracker = null;
        StageGimmickResolver _gimmickResolver = null;
        StageGimmickRoller _gimmickRoller = null;
        MoneyWallet _wallet = null;
        PlayerLoadout _loadout = null;
        PlayerVouchers _vouchers = null;
        PowerSurgeCharger _powerSurgeCharger = null;
        BreakMoneyGranter _breakMoneyGranter = null;
        IngameSaveHandler _saveHandler = null;
        UnlockProgress _unlockProgress = null;
        FlipperData _selectedFlipper = null;
        FlipperData.RunSetup _runSetup = null;
        int _stageClearMoney = STAGE_CLEAR_BASE_MONEY;
        bool _isRoundRewardPending = false;
        bool _isExitingToLobby = false;

        void Awake()
        {
            GameBootStrap.EnsureInit(_bootStrapConfig);
            _saveHandler = new IngameSaveHandler();
            _unlockProgress = new UnlockProgress(_rewardCatalog);

            IngameRunSetupResolver.RunStart start = ResolveRunStart();
            _selectedFlipper = start.Flipper;
            _runSetup = start.Setup;
            _stageClearMoney = start.StageClearMoney;
            _isRoundRewardPending = start.IsRoundRewardPending;

            InitRunModel(start);
            InitStageFlow(start);
            InitSceneComponents();
            InitPlay();
            InitPresentation();
            InitStates();

            UIManager.OnEvent += OnUIEvent;
            _scoreCounter.OnChanged += _stageTracker.SetTotalScore;

            if (start.IsShopPending)
                _stateMachine.ChangeState(EIngameState.Shop, new ShopState.EnterParam(OpensImmediately: true));
            else
                _stateMachine.ChangeState(EIngameState.StageSelect, new StageSelectState.EnterParam(IsAfterShop: false));
        }

        void OnDestroy()
        {
            UIManager.OnEvent -= OnUIEvent;
            UIManager.HideAll();

            for (int i = _releaseActions.Count - 1; i >= 0; --i)
                _releaseActions[i]();

            _releaseActions.Clear();
            _selectedFlipper = null;
            _runSetup = null;
            _saveHandler = null;
        }

        void Update()
        {
            if (null == _stateMachine)
                return;

            _stateMachine.Tick(Time.deltaTime);
        }

        void Track(Action release)
        {
            _releaseActions.Add(release);
        }

        IngameRunSetupResolver.RunStart ResolveRunStart()
        {
            bool isContinueRequested = SceneFlow.ConsumeContinueRequested();
            int requestedFlipperIndex = SceneFlow.ConsumeRequestedFlipperIndex();
            _saveHandler.TryLoad(isContinueRequested, out IngamePlayData savedData);
            bool hasConfig = null != _shopConfig;
            return IngameRunSetupResolver.Resolve(new IngameRunSetupResolver.ResolveParam(
                SavedData: savedData,
                RequestedFlipperIndex: requestedFlipperIndex,
                FlipperCatalog: _flipperCatalog,
                IsFlipperUnlockedGetter: _unlockProgress.IsFlipperUnlocked,
                BaseBallPassiveSlotCount: hasConfig ? _shopConfig.BallPassiveSlotCount : DEFAULT_PASSIVE_SLOT_COUNT,
                BaseMapPassiveSlotCount: hasConfig ? _shopConfig.MapPassiveSlotCount : DEFAULT_PASSIVE_SLOT_COUNT,
                BaseCommonPassiveSlotCount: hasConfig ? _shopConfig.CommonPassiveSlotCount : DEFAULT_PASSIVE_SLOT_COUNT,
                BaseStageClearMoney: STAGE_CLEAR_BASE_MONEY));
        }

        void InitRunModel(IngameRunSetupResolver.RunStart start)
        {
            _wallet = new MoneyWallet(start.Money);
            _loadout = new PlayerLoadout(start.BallPassiveSlotCount, start.MapPassiveSlotCount, start.CommonPassiveSlotCount);
            _vouchers = new PlayerVouchers(_runSetup.IsInterestDisabled);
            if (start.IsNewRun)
            {
                _loadout.GrantRandomPassives(_shopCatalog, _unlockProgress.IsItemUnlocked, _runSetup.StartPassiveCount);
            }
            else
            {
                _loadout.Restore(start.SavedData, _shopCatalog);
                RestoreVouchers(start.SavedData);
            }

            StatProvider.Init(_statData, _loadout, _vouchers, _selectedFlipper, GetRemainingBallCount, GetComboCount);
            Track(StatProvider.Release);

            _powerSurgeCharger = new PowerSurgeCharger(GetIsPlaying);
            _breakMoneyGranter = new BreakMoneyGranter(_wallet, PlayMoneyGainSound);
        }

        static void PlayMoneyGainSound()
        {
            AudioManager.Play(AudioStorage.EAudioClipType.MoneyGain);
        }

        void RestoreVouchers(IngamePlayData savedData)
        {
            if (null == savedData)
                return;

            _vouchers.Restore(new PlayerVouchers.RestoreParam(
                OwnedIds: savedData.GetVoucherIds(),
                OfferedIds: savedData.GetVoucherOfferedIds(),
                OfferId: savedData.VoucherOfferId,
                OfferRoundIndex: savedData.VoucherOfferRound,
                PurchasedRoundIndex: savedData.VoucherPurchasedRound,
                Catalog: _voucherCatalog));
            for (int i = 0; i < _vouchers.OwnedCount; ++i)
            {
                VoucherData voucher = _vouchers.GetOwned(i);
                if (voucher.Effect == VoucherData.EEffect.PassiveSlotAdd)
                    _loadout.AddCommonPassiveSlot(Mathf.RoundToInt(voucher.GetEffectValue(VoucherData.MAIN_VALUE_INDEX)));
            }
        }

        void OnVoucherPurchased()
        {
            _gimmickRoller.SetFirstRerollFree(_vouchers.Has(VoucherData.EEffect.StageFirstRerollFree));
        }

        void InitStageFlow(IngameRunSetupResolver.RunStart start)
        {
            _gimmickRoller = new StageGimmickRoller(_gimmickCatalog);
            _gimmickRoller.SetFirstRerollFree(_vouchers.Has(VoucherData.EEffect.StageFirstRerollFree));
            _gimmickResolver = new StageGimmickResolver(_gimmickRoller);
            _stageTracker = new StageProgressTracker(_stageCatalog, start.StageNumber, start.Score, _gimmickResolver);
            _stageTracker.OnStageStarted += OnStageStarted;
            _gimmickRoller.RestoreOrSetBase(start.GimmickIndices, _stageTracker.GetCurrentStage());

            _slingshotGrowth = new SlingshotScoreGrowth(start.SlingshotHitCount);
            _gimmickScoreTracker = new GimmickScoreTracker(start.GimmickScores);
            _scoreCounter = new ScoreCounter(start.Score, _comboConfig, _uiRoot.PlayScorePopup, _gimmickResolver.GetGimmickScoreMultiplier, _gimmickScoreTracker.Add, _slingshotGrowth);
            _scoreCounter.OnComboChanged += OnComboChanged;
            _scoreCounter.OnComboEnded += OnComboEnded;
        }

        void InitSceneComponents()
        {
            _inputRouter.Init();
            Track(_inputRouter.Release);
            _inputRouter.OnEscapePressed += OnPauseToggleRequested;

            _ballSpawner.Init();
            Track(_ballSpawner.Release);
            _drainZone.Init();
            Track(_drainZone.Release);
            _uiRoot.Init();
            Track(_uiRoot.Release);
            _uiRoot.OnPauseClicked += OnPauseToggleRequested;
            _cameraController.Init();
            Track(_cameraController.Release);

            _director = new IngameDirector(_presentationConfig, _fxPoolData, _cameraController.ShakeCam);
            Track(_director.Dispose);

            _mapLoader.Init();
            Track(_mapLoader.Release);
        }

        void InitPlay()
        {
            _ballFlow = new BallFlowController(new BallFlowController.InitParam(
                IsControlBlockedGetter: GetIsControlBlocked,
                IsPlayingGetter: GetIsPlaying,
                FXPlayer: _director.PlayFX), _ballSpawner);
            _drainZone.OnColliderEntered += _ballFlow.HandleBallDrained;

            _damageCalculator = new DamageCalculator(new DamageCalculator.InitParam(
                SurgeMultiplierConsumer: _powerSurgeCharger.ConsumeMultiplier,
                IsCloneBallGetter: _ballFlow.IsCloneBall));
            _powerSurgeCharger.OnChargedChanged += _ballFlow.SetSurgeCharged;
            _powerSurgeCharger.OnChargedChanged += OnSurgeChargedChanged;

            _playerController = new PlayerController(new PlayerController.InitParam(
                IsControlBlockedGetter: GetIsControlBlocked,
                IsPlayingGetter: GetIsPlaying,
                NudgeImpulseApplier: _ballFlow.TryApplyImpulseToActiveBall,
                NudgePresenter: _director.PlayNudge), _inputRouter);
            Track(_playerController.Dispose);
        }

        void InitPresentation()
        {
            _resultPresenter = new IngameResultPresenter(_loadout, _uiRoot.SetOwnedItems);

            _ballFlow.OnBallCountChanged += _uiRoot.SetBallCount;
            _stageTracker.OnStageScoreChanged += OnStageScoreChanged;
            _stageTracker.OnStageScoreChanged += _uiRoot.SetStageScore;
            _stageTracker.OnRemainingSecondsChanged += OnStageRemainingSecondsChanged;
            _uiRoot.SetStageScore(_stageTracker.StageScore);
            _uiRoot.SetBallCount(_ballFlow.RemainingBalls);
            RefreshOwnedItems();
        }

        void InitStates()
        {
            _stateMachine = new IngameStateMachine();

            _shopState = new ShopState(new ShopState.InitParam(
                Config: _shopConfig,
                Catalog: _shopCatalog,
                PackCatalog: _cardPackCatalog,
                VoucherCatalog: _voucherCatalog,
                StageCatalog: _stageCatalog,
                MoneyGetter: GetMoney,
                MoneySpender: _wallet.TrySpend,
                MoneyGranter: _wallet.Add,
                StageNumberGetter: GetStageNumber,
                IsItemUnlockedGetter: _unlockProgress.IsItemUnlocked,
                IsRoundRewardPendingGetter: GetIsRoundRewardPending,
                RoundRewardConsumer: ConsumeRoundReward,
                OfferSlotCount: ShopSession.DEFAULT_OFFER_SLOT_COUNT + _runSetup.ShopOfferSlotAdd,
                IsInterestDisabled: _runSetup.IsInterestDisabled), _loadout, _vouchers);
            _shopState.OnLoadoutChanged += OnShopLoadoutChanged;
            _shopState.OnVoucherPurchased += OnVoucherPurchased;

            _stageSelectState = new StageSelectState(new StageSelectState.InitParam(
                StageCatalog: _stageCatalog,
                StageNumberGetter: GetStageNumber,
                MoneyGetter: GetMoney,
                MoneySpender: _wallet.TrySpend,
                RemainingBallGetter: GetRemainingBallCount,
                NextStagePreparer: PrepareNextStage,
                ProgressSaver: SaveProgress,
                StageClearBaseMoney: _stageClearMoney), _gimmickRoller, _gimmickResolver);

            _playState = new PlayState(new PlayState.InitParam(
                MapLoader: _mapLoader.Load,
                DamageEvaluator: _damageCalculator.Calculate,
                SurgeTicker: _powerSurgeCharger.Tick,
                OnGimmickBroken: _breakMoneyGranter.HandleGimmickBroken,
                StageClearSettler: SettleStageClear,
                MapIndex: DEFAULT_MAP_INDEX), _stageTracker, _ballFlow, _playerController, _scoreCounter, _director, _gimmickResolver);

            _resultState = new ResultState(new ResultState.InitParam(
                SummaryGetter: BuildRunSummary,
                ResultAudioPlayer: _director.PlayGameResult,
                PlayDataClearer: _saveHandler.ClearPlayData,
                ClearRewardGranter: _unlockProgress.GrantClearReward), _resultPresenter);

            _stateMachine.Register(_stageSelectState);
            _stateMachine.Register(_shopState);
            _stateMachine.Register(_playState);
            _stateMachine.Register(_resultState);
            Track(_stateMachine.Dispose);
            InitCheatHandler();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        const int MIN_CHEAT_CLEAR_SCORE = 1;

        void InitCheatHandler()
        {
            if (null == _cheatHandler)
                return;

            _cheatHandler.Init(new IngameCheatHandler.InitParam(
                MoneyGetter: GetMoney,
                MoneyAdder: _wallet.Add,
                IsStageClearableGetter: GetIsStageClearable,
                StageClearer: ClearCurrentStage,
                RoundClearer: ClearCurrentRound,
                GameClearer: ClearAllStages,
                OnCheatApplied: OnCheatApplied), _loadout);
            Track(_cheatHandler.Release);
        }

        bool GetIsStageClearable()
        {
            return !GetIsControlBlocked();
        }

        void ClearCurrentStage()
        {
            int remainingScore = _stageTracker.TargetScore - _stageTracker.StageScore;
            _scoreCounter.AddCheatScore(Mathf.Max(remainingScore, MIN_CHEAT_CLEAR_SCORE));
        }

        void ClearCurrentRound()
        {
            int roundIndex = StageCatalogData.GetRoundIndex(_stageTracker.StageNumber - 1);
            SkipStagesByCheat(StageCatalogData.GetRoundStartStageIndex(roundIndex) + StageCatalogData.STAGES_PER_ROUND);
            ClearCurrentStage();
        }

        void ClearAllStages()
        {
            if (null == _stageCatalog)
                return;

            SkipStagesByCheat(_stageCatalog.Count);
            ClearCurrentStage();
        }

        void SkipStagesByCheat(int lastStageNumber)
        {
            while (_stageTracker.StageNumber < lastStageNumber)
            {
                _stageTracker.SetCheatCleared();
                SettleStageClear();
            }
        }

        void OnCheatApplied()
        {
            _shopState.RefreshOpenedView();
            _stageSelectState.RefreshOpenedView();
            RefreshOwnedItems();
        }
#else
        void InitCheatHandler()
        {
        }
#endif

        void PrepareNextStage()
        {
            _breakMoneyGranter.ResetStage();
            _ballFlow.RefillStock();
        }

        void SettleStageClear()
        {
            int remainingBalls = _ballFlow.RemainingBalls;
            int resupplyMoney = Mathf.RoundToInt(remainingBalls * StatProvider.GetBallResupplyMoney());
            _wallet.Add(remainingBalls + _stageClearMoney + _shopState.CalculateInterest() + resupplyMoney);
            _isRoundRewardPending = StageCatalogData.IsRoundLastStage(_stageTracker.StageNumber - 1);
            _stageTracker.AdvanceToNextStage();
            _gimmickRoller.SetBaseGimmicks(_stageTracker.GetCurrentStage());
            SaveProgress(true);
        }

        bool GetIsRoundRewardPending()
        {
            return _isRoundRewardPending;
        }

        void ConsumeRoundReward()
        {
            _isRoundRewardPending = false;
            SaveProgress(true);
        }

        void OnStageStarted(int stageNumber, StageData stage)
        {
            _saveHandler.UpdateBestStage(null == _selectedFlipper ? string.Empty : _selectedFlipper.GetId(), stageNumber);
            SaveProgress(false);
            ShowStageInfo();
        }

        void OnStageScoreChanged(int stageScore)
        {
            ShowStageInfo();
        }

        void OnStageRemainingSecondsChanged(int remainingSeconds)
        {
            ShowStageInfo();
        }

        void ShowStageInfo()
        {
            if (GetIsControlBlocked())
                return;

            if (null == _stageTracker.GetCurrentStage())
                return;

            int remainingSeconds = _stageTracker.HasTimeLimit ? _stageTracker.RemainingSeconds : StageInfoView.NO_TIME_LIMIT;
            _uiRoot.SetStageInfo(_stageTracker.StageNumber, _stageTracker.TargetScore, remainingSeconds);
        }

        void OnShopLoadoutChanged()
        {
            SaveProgress(true);
            RefreshOwnedItems();
        }

        void OnComboChanged(int comboCount, float multiplier, int pendingBonus)
        {
            _director.PlayComboUp();
            _uiRoot.SetCombo(comboCount, multiplier, pendingBonus, GetComboStepProgress(comboCount));
        }

        float GetComboStepProgress(int comboCount)
        {
            int countPerStep = _comboConfig.ComboCountPerStep;
            if (countPerStep <= 0)
                return 0f;

            return (comboCount % countPerStep) / (float)countPerStep;
        }

        void OnSurgeChargedChanged(bool isCharged)
        {
            if (!isCharged)
                return;

            _director.PlaySurgeCharged();
        }

        void OnComboEnded(int comboCount, float multiplier, int bonusScore)
        {
            _director.ResetComboPitch();

            if (bonusScore <= 0)
            {
                _uiRoot.HideCombo();
                return;
            }

            _uiRoot.ShowComboBonus(comboCount, multiplier, bonusScore);
        }

        void RefreshOwnedItems()
        {
            if (null == _resultPresenter)
                return;

            _resultPresenter.RefreshOwnedItems();
        }

        IngameResultPresenter.RunSummary BuildRunSummary()
        {
            return new IngameResultPresenter.RunSummary(
                StageNumber: _stageTracker.StageNumber,
                Score: _scoreCounter.Current,
                Money: _wallet.Current,
                UsedBallCount: _ballFlow.UsedBallCount,
                GimmickScores: _gimmickScoreTracker.CollectScores());
        }

        bool GetIsControlBlocked()
        {
            if (null == _stateMachine || !_stateMachine.IsCurrent(EIngameState.Play))
                return true;

            return _playState.IsControlBlocked;
        }

        bool GetIsPlaying()
        {
            if (null == _stateMachine || !_stateMachine.IsCurrent(EIngameState.Play))
                return false;

            return _playState.IsPlaying;
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

        int GetMoney()
        {
            if (null == _wallet)
                return 0;

            return _wallet.Current;
        }

        int GetStageNumber()
        {
            if (null == _stageTracker)
                return 0;

            return _stageTracker.StageNumber;
        }

        void SaveProgress(bool isShopPending)
        {
            _saveHandler.Save(new IngameSaveHandler.SaveParam(
                StageNumber: _stageTracker.StageNumber,
                IsShopPending: isShopPending,
                IsRoundRewardPending: _isRoundRewardPending,
                Money: _wallet.Current,
                TotalScore: _scoreCounter.Current,
                GimmickIndices: _gimmickRoller.GetCatalogIndices(),
                FlipperId: null == _selectedFlipper ? string.Empty : _selectedFlipper.GetId(),
                PassiveIds: _loadout.CollectPassiveIds(),
                Upgrades: _loadout.CollectUpgradeEntries(),
                VoucherIds: _vouchers.CollectIds(),
                VoucherOfferedIds: _vouchers.CollectOfferedIds(),
                VoucherOfferId: _vouchers.GetOfferId(),
                VoucherOfferRound: _vouchers.OfferRoundIndex,
                VoucherPurchasedRound: _vouchers.LastPurchasedRoundIndex,
                SlingshotHitCount: _slingshotGrowth.HitCount,
                GimmickScores: _gimmickScoreTracker.CollectScores()));
        }

        void OnUIEvent(UIManager.EEvent uiEvent, int payload)
        {
            switch (uiEvent)
            {
                case UIManager.EEvent.ExitToLobbyRequested:
                    OnLobbyExitRequested();
                    break;
                case UIManager.EEvent.PauseResumeRequested:
                    OnPauseResumeRequested();
                    break;
            }
        }

        void OnPauseToggleRequested()
        {
            if (!_stateMachine.IsCurrent(EIngameState.Play))
                return;

            _playState.TogglePause();
        }

        void OnPauseResumeRequested()
        {
            if (!_stateMachine.IsCurrent(EIngameState.Play))
                return;

            _playState.Resume();
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
