using System;
using Cysharp.Threading.Tasks;
using Pinball.Audio;
using Pinball.FX;
using Pinball.Ingame.Map;
using Pinball.Ingame.Player;
using Pinball.Ingame.Presentation;
using Pinball.Ingame.Reward;
using Pinball.Ingame.Score;
using Pinball.Ingame.Shop;
using Pinball.Ingame.Stage;
using Pinball.Ingame.Stat;
using Pinball.UI;
using UnityEngine;

namespace Pinball.Ingame.State
{
    public class PlayState : IngameStateBase
    {
        const float FULL_BLAST_RADIUS_RATIO = 1f;

        public record InitParam(
            Func<int, MapRoot> MapLoader,
            Func<Collider2D, float, float> DamageEvaluator,
            Func<Collider2D, float> AttackPowerEvaluator,
            Action<float> SurgeTicker,
            Action<float> BallEffectTicker,
            Action BallEffectResetter,
            Action OnGimmickBroken,
            Action OnSlingshotHit,
            Action OnFlipperBallStruck,
            Func<StageClearReward> StageClearSettler,
            Func<UniTask> StageClearInterceptorAsync,
            int MapIndex);

        InitParam _param = null;
        StageProgressTracker _stageTracker = null;
        BallFlowController _ballFlow = null;
        PlayerController _playerController = null;
        ScoreCounter _scoreCounter = null;
        IngameDirector _director = null;
        StageGimmickResolver _gimmickResolver = null;
        SlingshotAlternateTracker _slingshotAlternateTracker = null;
        MapRoot _currentMap = null;
        bool _isBallInPlay = false;
        bool _isPaused = false;
        bool _isPlayStopped = true;
        bool _isGameClearPending = false;

        public override EIngameState Key => EIngameState.Play;
        public bool IsControlBlocked => _isPaused || _isPlayStopped;
        public bool IsPlaying => _isBallInPlay && !IsControlBlocked;

        public PlayState(InitParam param, StageProgressTracker stageTracker, BallFlowController ballFlow, PlayerController playerController, ScoreCounter scoreCounter, IngameDirector director, StageGimmickResolver gimmickResolver)
        {
            _param = param;
            _stageTracker = stageTracker;
            _ballFlow = ballFlow;
            _playerController = playerController;
            _scoreCounter = scoreCounter;
            _director = director;
            _gimmickResolver = gimmickResolver;
            _isPlayStopped = true;
        }

        protected override void OnEnter(object param)
        {
            _stageTracker.OnStageCleared += OnStageCleared;
            _stageTracker.OnAllStagesCleared += OnAllStagesCleared;
            _stageTracker.OnTimeExpired += OnStageTimeExpired;
            _ballFlow.OnBallLost += OnBallLost;
            _ballFlow.OnBallReady += OnBallReady;
            _ballFlow.OnAllBallsLost += OnAllBallsLost;
            _playerController.OnLaunched += OnBallLaunched;
            UIManager.OnEvent += HandleUIEvent;

            _isGameClearPending = false;
            _isPlayStopped = false;
            _isBallInPlay = false;
            _isPaused = false;
            _slingshotAlternateTracker = new SlingshotAlternateTracker();
            BgmManager.Play(GetStageBgmType());
            _stageTracker.StartCurrentStage();
            LoadMap();
            _ballFlow.LoadBallOnSpring();
        }

        protected override void OnExit()
        {
            StopPlay();
            ClearPause();

            if (null != _stageTracker)
            {
                _stageTracker.OnStageCleared -= OnStageCleared;
                _stageTracker.OnAllStagesCleared -= OnAllStagesCleared;
                _stageTracker.OnTimeExpired -= OnStageTimeExpired;
            }

            if (null != _ballFlow)
            {
                _ballFlow.OnBallLost -= OnBallLost;
                _ballFlow.OnBallReady -= OnBallReady;
                _ballFlow.OnAllBallsLost -= OnAllBallsLost;
            }

            if (null != _playerController)
                _playerController.OnLaunched -= OnBallLaunched;

            UIManager.OnEvent -= HandleUIEvent;
            _isGameClearPending = false;
            _currentMap = null;
        }

        protected override void OnTick(float deltaTime)
        {
            if (IsControlBlocked)
                return;

            _scoreCounter.Tick(deltaTime);
            _playerController.Tick(deltaTime);
            _param.SurgeTicker?.Invoke(deltaTime);
            _param.BallEffectTicker?.Invoke(deltaTime);
            _ballFlow.Tick(deltaTime);
            _stageTracker.Tick(deltaTime);

            if (null != _currentMap)
                _currentMap.Tick(deltaTime);
        }

        public void TogglePause()
        {
            if (UIManager.IsOpen(UIViewCatalogData.EViewType.Settings))
                return;

            if (_isPaused)
            {
                Resume();
                return;
            }

            if (_isPlayStopped)
                return;

            _isPaused = true;
            _playerController.ResetControlStates();
            _director.SetPaused(true);
            UIManager.Show(new PausePopupView.Args());
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void FailByCheat()
        {
            EndGame();
        }
#endif

        public void Resume()
        {
            if (!_isPaused)
                return;

            ClearPause();
        }

        void ClearPause()
        {
            if (!_isPaused)
                return;

            _isPaused = false;
            _director?.SetPaused(false);
            UIManager.Hide(UIViewCatalogData.EViewType.PausePopup);
        }

        void StopPlay()
        {
            if (_isPlayStopped)
                return;

            _isPlayStopped = true;
            _isBallInPlay = false;
            _playerController.ResetControlStates();
            _scoreCounter.EndCombo();
            _param.BallEffectResetter?.Invoke();
            _ballFlow.DespawnBalls();

            if (null != _currentMap)
                _currentMap.Suspend();
        }

        void LoadMap()
        {
            _currentMap = _param.MapLoader(_param.MapIndex);
            _playerController.SetMap(_currentMap);
            _ballFlow.SetMap(_currentMap);
            if (null == _currentMap)
                return;

            _currentMap.Init(new MapRoot.InitParam(
                OnScoreGained: _scoreCounter.HandleMapScoreGained,
                DamageEvaluator: _param.DamageEvaluator,
                AttackPowerEvaluator: _param.AttackPowerEvaluator,
                OnBallStruck: OnBallStruck,
                OnBumperDamaged: OnBumperDamaged,
                OnBumperBroken: OnBumperBroken,
                OnSlingshotBallHit: OnSlingshotBallHit,
                SlingshotHitScoreMultiplierConsumer: _slingshotAlternateTracker.ConsumeHit,
                OnSpringChargeEvent: _director.PlaySpringCharge,
                FXPlayer: _director.PlayFX,
                LineFXPlayer: _director.PlayLineFX,
                OnGimmickBallHit: _ballFlow.CountGimmickHit,
                GimmickHitPresenter: _director.PlayGimmickHit,
                IsGimmickAbsentGetter: _gimmickResolver.IsGimmickAbsent,
                ScoreGainLimitGetter: _gimmickResolver.GetGimmickScoreGainLimit,
                BumperHealthMultiplier: _stageTracker.BumperHealthMultiplier));
        }

        BgmStorage.EBgmType GetStageBgmType()
        {
            int stageIndex = _stageTracker.StageNumber - 1;
            return StageCatalogData.IsRoundLastStage(stageIndex)
                ? BgmStorage.EBgmType.PlayLate
                : BgmStorage.EBgmType.PlayEarly;
        }

        void OnStageCleared(int stageNumber, StageData stage)
        {
            HandleStageClearedAsync(stageNumber).Forget();
        }

        async UniTaskVoid HandleStageClearedAsync(int stageNumber)
        {
            if (null != _param.StageClearInterceptorAsync)
                await _param.StageClearInterceptorAsync();

            bool isEndless = _stageTracker.IsEndless;
            int displayStageNumber = isEndless ? _stageTracker.EndlessStageNumber : stageNumber;
            StopPlay();
            _director.PlayStageClear();
            StageClearReward reward = null == _param.StageClearSettler ? null : _param.StageClearSettler();
            UIManager.Show(new StageClearView.Args(displayStageNumber, isEndless, reward));
            ChangeState(EIngameState.Shop, new ShopState.EnterParam(OpensImmediately: false));
        }

        void OnAllStagesCleared()
        {
            StopPlay();
            _isGameClearPending = true;
            _director.PlayStageClear();
            UIManager.Show(new StageClearView.Args(_stageTracker.StageNumber, IsEndless: false, Reward: null));
        }

        void HandleUIEvent(UIManager.EEvent uiEvent, int payload)
        {
            if (uiEvent != UIManager.EEvent.StageClearFinished)
                return;

            if (!_isGameClearPending)
                return;

            _isGameClearPending = false;
            ChangeState(EIngameState.Result, new ResultState.EnterParam(GameResultView.EResult.Clear));
        }

        void OnStageTimeExpired()
        {
            EndGame();
        }

        void OnAllBallsLost()
        {
            EndGame();
        }

        void EndGame()
        {
            StopPlay();
            ChangeState(EIngameState.Result, new ResultState.EnterParam(GameResultView.EResult.GameOver));
        }

        void OnBallLaunched(float launchPowerRatio)
        {
            _isBallInPlay = true;
            _playerController.ResetNudgeCooldown();
            _director.PlaySpringLaunch(launchPowerRatio);
        }

        void OnBallLost()
        {
            _scoreCounter.EndCombo();

            if (null != _currentMap)
                _currentMap.ResetBumperGrades();
        }

        void OnBallReady()
        {
            _isBallInPlay = false;
        }

        void OnBallStruck(Flipper.EHitGrade grade, Vector2 worldPoint)
        {
            _param.OnFlipperBallStruck?.Invoke();
            _director.PlayFlipperHit(grade, worldPoint);
            _scoreCounter.HandleBallStruck(grade, worldPoint);
        }

        void OnSlingshotBallHit(Collider2D ballCollider)
        {
            _param.OnSlingshotHit?.Invoke();

            if (StatProvider.HasEffect(ShopItemData.EEffectType.SlingshotNudgeReset))
                _playerController.ResetNudgeCooldown();

            if (StatProvider.HasEffect(ShopItemData.EEffectType.SlingshotAttackStack))
                _ballFlow.MultiplyDamageStack(ballCollider, StatProvider.GetSlingshotAttackStackScale());
        }

        void OnBumperDamaged(BumperInteractable bumper, float damage, Collider2D ballCollider)
        {
            if (StatProvider.HasEffect(ShopItemData.EEffectType.SlingshotAttackStack))
                _ballFlow.ResetDamageStack(ballCollider);

            bumper.GainHitScore(Mathf.RoundToInt(StatProvider.GetBumperHitScore()));
            ApplyBlast(bumper, damage);

            if (StatProvider.HasEffect(ShopItemData.EEffectType.BumperBurn))
            {
                float burnDamage = damage * StatProvider.GetEffectSum(ShopItemData.EEffectType.BumperBurn);
                float burnInterval = StatProvider.GetEffectValue(ShopItemData.EEffectType.BumperBurn, ShopItemData.SUB_VALUE_INDEX);
                float burnDuration = StatProvider.GetEffectValue(ShopItemData.EEffectType.BumperBurn, ShopItemData.EXTRA_VALUE_INDEX);
                bumper.Ignite(burnDamage, burnInterval, burnDuration, PoolableFX.EFXType.Fireball);
            }

            if (StatProvider.HasEffect(ShopItemData.EEffectType.BumperFreeze))
                bumper.ApplyFrostHit(StatProvider.GetEffectSum(ShopItemData.EEffectType.BumperFreeze), PoolableFX.EFXType.Frostbite, PoolableFX.EFXType.FrostShatter);

            if (!StatProvider.HasEffect(ShopItemData.EEffectType.BumperChainLightning))
                return;

            if (null == _currentMap)
                return;

            float chainRatio = StatProvider.GetEffectSum(ShopItemData.EEffectType.BumperChainLightning);
            int maxChainCount = Mathf.RoundToInt(StatProvider.GetEffectValue(ShopItemData.EEffectType.BumperChainLightning, ShopItemData.SUB_VALUE_INDEX));
            _currentMap.ApplyChainDamage(bumper, damage, chainRatio, maxChainCount);
        }

        void ApplyBlast(BumperInteractable bumper, float damage)
        {
            if (!StatProvider.HasEffect(ShopItemData.EEffectType.BumperBlast))
                return;

            if (null == _currentMap)
                return;

            float damageRatio = StatProvider.GetEffectSum(ShopItemData.EEffectType.BumperBlast);
            float maxRadiusDamage = StatProvider.GetEffectValue(ShopItemData.EEffectType.BumperBlast, ShopItemData.SUB_VALUE_INDEX);
            float maxRadiusScale = StatProvider.GetEffectValue(ShopItemData.EEffectType.BumperBlast, ShopItemData.EXTRA_VALUE_INDEX);
            float radiusRatio = maxRadiusDamage <= 0f ? FULL_BLAST_RADIUS_RATIO : Mathf.Clamp01(damage / maxRadiusDamage);
            float blastRadius = bumper.GetRadius() * maxRadiusScale * radiusRatio;
            _currentMap.ApplyBlastDamage(bumper, damage, blastRadius, damageRatio);
        }

        void OnBumperBroken()
        {
            _director.PlayBumperBroken();
            _param.OnGimmickBroken?.Invoke();
        }
    }
}
