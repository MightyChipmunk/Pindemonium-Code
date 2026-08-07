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

        IngameDirector _director = null;
        ScoreCounter _scoreCounter = null;
        BallStockCounter _ballStock = null;
        DamageCalculator _damageCalculator = null;
        StageProgressTracker _stageTracker = null;
        MoneyWallet _wallet = null;
        MapRoot _currentMap = null;
        Ball _activeBall = null;
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

            int startStageNumber = FIRST_STAGE_NUMBER;
            int startMoney = 0;
            int startScore = 0;
            IngamePlayData savedData = SceneFlow.IsContinueRequested ? SaveManager.GetIngameData() : null;
            if (null != savedData)
            {
                startStageNumber = savedData.StageNumber;
                startMoney = savedData.Money;
                startScore = savedData.TotalScore;
            }

            _scoreCounter = new ScoreCounter();
            _scoreCounter.Init(startScore);
            _ballStock = new BallStockCounter();
            _ballStock.Init(_statData.InitialBallCount);
            _damageCalculator = new DamageCalculator();
            _damageCalculator.Init(GetBallAttackPower);
            _wallet = new MoneyWallet();
            _wallet.Init(startMoney);

            _inputRouter.Init();
            _inputRouter.OnEscapePressed += OnEscapePressed;
            _ballSpawner.Init();
            _drainZone.Init();
            _mapLoader.Init();
            _uiRoot.Init();
            _cameraController.Init();
            _director = new IngameDirector();
            _director.Init(_presentationConfig, _fxPoolData, _cameraController.ShakeCam);

            LoadMap();

            _scoreCounter.OnChanged += _uiRoot.SetScore;
            _ballStock.OnChanged += _uiRoot.SetBallCount;
            _uiRoot.SetScore(_scoreCounter.Current);
            _uiRoot.SetBallCount(_ballStock.Remaining);

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

            _inputRouter.OnFlipperLeftChanged += OnFlipperLeftChanged;
            _inputRouter.OnFlipperRightChanged += OnFlipperRightChanged;
            _inputRouter.OnLaunchChanged += OnLaunchChanged;
            _drainZone.OnColliderEntered += OnBallDrained;

            LoadBallOnSpring();
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

            if (null != _wallet)
            {
                _wallet.Release();
                _wallet = null;
            }

            _activeBall = null;

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

            if (null != _damageCalculator)
            {
                _damageCalculator.Release();
                _damageCalculator = null;
            }

            if (null != _ballStock)
            {
                _ballStock.Release();
                _ballStock = null;
            }

            if (null != _scoreCounter)
            {
                _scoreCounter.Release();
                _scoreCounter = null;
            }
        }

        void OnMapScoreGained(int score, Vector2 worldPoint)
        {
            _scoreCounter.Add(score);
            _uiRoot.PlayScorePopup(score, worldPoint);
        }

        void OnStageStarted(int stageNumber, StageData stage)
        {
            UpdateBestStage(stageNumber);
            SaveProgress();

            if (null == stage)
                return;

            UIManager.Show(new StageInfoView.Args(stageNumber, stage.TargetScore, 0));
        }

        void OnStageCleared(int stageNumber, StageData stage)
        {
            _gameState = EGameState.Shopping;
            ResetMapControlStates();
            DespawnActiveBall();
            _wallet.Add(_ballStock.Remaining + STAGE_CLEAR_BASE_MONEY);
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


        void OnFlipperLeftChanged(bool isPressed)
        {
            SetFlipperPressed(Flipper.ESide.Left, isPressed);
        }

        void OnFlipperRightChanged(bool isPressed)
        {
            SetFlipperPressed(Flipper.ESide.Right, isPressed);
        }

        void SetFlipperPressed(Flipper.ESide side, bool isPressed)
        {
            if (IsControlBlocked)
                return;

            if (null == _currentMap)
                return;

            _currentMap.SetFlipperPressed(side, isPressed);
        }

        void OnLaunchChanged(bool isPressed)
        {
            if (IsControlBlocked)
                return;

            if (null == _currentMap)
                return;

            if (isPressed)
            {
                _currentMap.SetSpringCharging(true);
                return;
            }

            if (_currentMap.TryLaunch())
                _gameState = EGameState.Playing;
        }

        void OnBallDrained(Collider2D collider)
        {
            if (IsControlBlocked)
                return;

            if (!_ballSpawner.TryResolve(collider, out Ball ball))
                return;

            _ballSpawner.Despawn(ball);

            if (ball == _activeBall)
                _activeBall = null;

            _ballStock.Consume();

            if (_ballStock.Remaining <= 0)
            {
                EndGame();
                return;
            }

            LoadBallOnSpring();
            _gameState = EGameState.Ready;
        }

        void LoadMap()
        {
            _currentMap = _mapLoader.Load(DEFAULT_MAP_INDEX);
            if (null == _currentMap)
                return;

            _currentMap.Init(OnMapScoreGained, _damageCalculator.Calculate, GetFlipPower, _director.PlayFlipperHit, _director.PlaySpringCharge, _director.PlayFX);
        }

        void LoadBallOnSpring()
        {
            if (null == _currentMap)
                return;

            Ball ball = _ballSpawner.Spawn(transform.position);
            if (null == ball)
                return;

            ball.SetMass(_statData.BallMass);
            _activeBall = ball;
            _currentMap.LoadBall(ball);
        }

        void EndGame()
        {
            _gameState = EGameState.GameOver;
            ResetMapControlStates();
            SaveManager.ClearIngame();
            Debug.Log("[IngameManager] Game Over");
        }

        void SaveProgress()
        {
            IngamePlayData data = new IngamePlayData();
            data.StageNumber = _stageTracker.StageNumber;
            data.Money = _wallet.Current;
            data.TotalScore = _scoreCounter.Current;
            SaveManager.SaveIngame(data);
        }

        void UpdateBestStage(int stageNumber)
        {
            UserData userData = SaveManager.GetUserData();
            if (null == userData)
                return;

            if (stageNumber <= userData.BestStageNumber)
                return;

            userData.BestStageNumber = stageNumber;
            SaveManager.SaveUser();
        }

        void ResetMapControlStates()
        {
            if (null == _currentMap)
                return;

            _currentMap.SetFlipperPressed(Flipper.ESide.Left, false);
            _currentMap.SetFlipperPressed(Flipper.ESide.Right, false);
            _currentMap.SetSpringCharging(false);
        }

        void DespawnActiveBall()
        {
            if (null == _activeBall)
                return;

            _ballSpawner.Despawn(_activeBall);
            _activeBall = null;
        }

        void OnUIEvent(UIManager.EEvent uiEvent)
        {
            switch (uiEvent)
            {
                case UIManager.EEvent.StageClearFinished:
                    OpenShop();
                    break;
                case UIManager.EEvent.ShopStartRequested:
                    RestartNextStage();
                    break;
                case UIManager.EEvent.ExitToLobbyRequested:
                    OnLobbyExitRequested();
                    break;
            }
        }

        void OpenShop()
        {
            UIManager.Show(new ShopView.Args(_wallet.Current));
        }

        void RestartNextStage()
        {
            _ballStock.Init(_statData.InitialBallCount);
            _uiRoot.SetBallCount(_ballStock.Remaining);
            _stageTracker.AdvanceToNextStage();
            LoadMap();
            LoadBallOnSpring();
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

        float GetFlipPower()
        {
            if (null == _statData)
                return 0f;

            return _statData.FlipPower;
        }

        float GetBallAttackPower()
        {
            if (null == _statData)
                return 0f;

            return _statData.BallAttackPower;
        }
    }
}
