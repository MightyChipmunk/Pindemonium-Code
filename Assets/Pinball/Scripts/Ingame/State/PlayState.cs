using System;
using Pinball.FX;
using Pinball.Ingame.Map;
using Pinball.Ingame.Player;
using Pinball.Ingame.Presentation;
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
        public record InitParam(
            Func<int, MapRoot> MapLoader,
            Func<Collider2D, float, float> DamageEvaluator,
            Action<float> SurgeTicker,
            Action OnGimmickBroken,
            Action StageClearSettler,
            int MapIndex);

        InitParam _param = null;
        StageProgressTracker _stageTracker = null;
        BallFlowController _ballFlow = null;
        PlayerController _playerController = null;
        ScoreCounter _scoreCounter = null;
        IngameDirector _director = null;
        StageGimmickResolver _gimmickResolver = null;
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
            _ballFlow.Tick(deltaTime);
            _stageTracker.Tick(deltaTime);
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
                OnBallStruck: OnBallStruck,
                OnBumperDamaged: OnBumperDamaged,
                OnBumperBroken: OnBumperBroken,
                OnSpringChargeEvent: _director.PlaySpringCharge,
                FXPlayer: _director.PlayFX,
                LineFXPlayer: _director.PlayLineFX,
                OnGimmickBallHit: _ballFlow.CountGimmickHit,
                GimmickHitPresenter: _director.PlayGimmickHit,
                IsGimmickAbsentGetter: _gimmickResolver.IsGimmickAbsent,
                ScoreGainLimitGetter: _gimmickResolver.GetGimmickScoreGainLimit,
                IsBumperUnbreakable: _gimmickResolver.IsBumperUnbreakable()));
        }

        void OnStageCleared(int stageNumber, StageData stage)
        {
            StopPlay();
            _director.PlayStageClear();
            _param.StageClearSettler?.Invoke();
            UIManager.Show(new StageClearView.Args(stageNumber));
            ChangeState(EIngameState.Shop, new ShopState.EnterParam(OpensImmediately: false));
        }

        void OnAllStagesCleared()
        {
            StopPlay();
            _isGameClearPending = true;
            _director.PlayStageClear();
            UIManager.Show(new StageClearView.Args(_stageTracker.StageNumber));
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
        }

        void OnBallReady()
        {
            _isBallInPlay = false;
        }

        void OnBallStruck(Flipper.EHitGrade grade, Vector2 worldPoint)
        {
            _director.PlayFlipperHit(grade, worldPoint);
            _scoreCounter.HandleBallStruck(grade, worldPoint);
        }

        void OnBumperDamaged(BumperInteractable bumper, float damage)
        {
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

        void OnBumperBroken()
        {
            _director.PlayBumperBroken();
            _param.OnGimmickBroken?.Invoke();
        }
    }
}
