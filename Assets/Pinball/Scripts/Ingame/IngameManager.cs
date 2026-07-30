using Pinball.Ingame.InputModule;
using Pinball.Ingame.Map;
using Pinball.Ingame.Presentation;
using Pinball.Ingame.Stat;
using Pinball.Ingame.UI;
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
        }

        const int DEFAULT_MAP_INDEX = 0;

        [SerializeField] IngameInputRouter _inputRouter = null;
        [SerializeField] BallSpawner _ballSpawner = null;
        [SerializeField] BallDrainZone _drainZone = null;
        [SerializeField] MapLoader _mapLoader = null;
        [SerializeField] IngameUIRoot _uiRoot = null;
        [SerializeField] IngameDirector _director = null;
        [SerializeField] CameraController _cameraController = null;
        [SerializeField] PinballStatData _statData = null;

        ScoreCounter _scoreCounter = null;
        BallStockCounter _ballStock = null;
        DamageCalculator _damageCalculator = null;
        MapRoot _currentMap = null;
        EGameState _gameState = EGameState.Ready;

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
            _scoreCounter = new ScoreCounter();
            _scoreCounter.Init();
            _ballStock = new BallStockCounter();
            _ballStock.Init(_statData.InitialBallCount);
            _damageCalculator = new DamageCalculator();
            _damageCalculator.Init(GetBallAttackPower);

            _inputRouter.Init();
            _ballSpawner.Init();
            _drainZone.Init();
            _mapLoader.Init();
            _uiRoot.Init();
            _cameraController.Init();
            _director.Init(_cameraController.ShakeCam);

            _currentMap = _mapLoader.Load(DEFAULT_MAP_INDEX);
            if (null != _currentMap)
                _currentMap.Init(_scoreCounter.Add, _damageCalculator.Calculate, GetFlipPower, _director.PlayFlipperHit, _director.PlaySpringCharge, _director.PlayFX);

            _scoreCounter.OnChanged += _uiRoot.SetScore;
            _ballStock.OnChanged += _uiRoot.SetBallCount;
            _uiRoot.SetScore(_scoreCounter.Current);
            _uiRoot.SetBallCount(_ballStock.Remaining);

            _inputRouter.OnFlipperLeftChanged += OnFlipperLeftChanged;
            _inputRouter.OnFlipperRightChanged += OnFlipperRightChanged;
            _inputRouter.OnLaunchChanged += OnLaunchChanged;
            _drainZone.OnColliderEntered += OnBallDrained;

            LoadBallOnSpring();
            _gameState = EGameState.Ready;
        }

        public void Release()
        {
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
                _director.Release();

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
            if (_gameState == EGameState.GameOver)
                return;

            if (null == _currentMap)
                return;

            _currentMap.SetFlipperPressed(side, isPressed);
        }

        void OnLaunchChanged(bool isPressed)
        {
            if (_gameState == EGameState.GameOver)
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
            if (_gameState == EGameState.GameOver)
                return;

            if (!_ballSpawner.TryResolve(collider, out Ball ball))
                return;

            _ballSpawner.Despawn(ball);
            _ballStock.Consume();

            if (_ballStock.Remaining <= 0)
            {
                EndGame();
                return;
            }

            LoadBallOnSpring();
            _gameState = EGameState.Ready;
        }

        void LoadBallOnSpring()
        {
            if (null == _currentMap)
                return;

            Ball ball = _ballSpawner.Spawn(transform.position);
            if (null == ball)
                return;

            ball.SetMass(_statData.BallMass);
            _currentMap.LoadBall(ball);
        }

        void EndGame()
        {
            _gameState = EGameState.GameOver;

            if (null != _currentMap)
            {
                _currentMap.SetFlipperPressed(Flipper.ESide.Left, false);
                _currentMap.SetFlipperPressed(Flipper.ESide.Right, false);
                _currentMap.SetSpringCharging(false);
            }

            Debug.Log("[IngameManager] Game Over");
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
