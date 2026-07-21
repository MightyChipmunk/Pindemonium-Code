using Pinball.Ingame.InputModule;
using Pinball.Ingame.Map;
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
        [SerializeField] Flipper _leftFlipper = null;
        [SerializeField] Flipper _rightFlipper = null;
        [SerializeField] Spring _spring = null;
        [SerializeField] BallSpawner _ballSpawner = null;
        [SerializeField] BallDrainZone _drainZone = null;
        [SerializeField] MapLoader _mapLoader = null;
        [SerializeField] IngameUIRoot _uiRoot = null;
        [SerializeField] PinballStatData _statData = null;

        ScoreCounter _scoreCounter = null;
        BallStockCounter _ballStock = null;
        DamageCalculator _damageCalculator = null;
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
            _leftFlipper.Init(GetFlipPower);
            _rightFlipper.Init(GetFlipPower);
            _spring.Init();
            _ballSpawner.Init();
            _drainZone.Init();
            _mapLoader.Init();
            _uiRoot.Init();

            MapRoot mapRoot = _mapLoader.Load(DEFAULT_MAP_INDEX);
            if (null != mapRoot)
                mapRoot.Init(_scoreCounter.Add, _damageCalculator.Calculate);

            _scoreCounter.OnChanged += _uiRoot.SetScore;
            _ballStock.OnChanged += _uiRoot.SetBallCount;
            _uiRoot.SetScore(_scoreCounter.Current);
            _uiRoot.SetBallCount(_ballStock.Remaining);

            _inputRouter.OnFlipperLeftChanged += OnFlipperLeftChanged;
            _inputRouter.OnFlipperRightChanged += OnFlipperRightChanged;
            _inputRouter.OnLaunchPressed += OnLaunchPressed;
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

            if (null != _ballSpawner)
                _ballSpawner.Release();

            if (null != _spring)
                _spring.Release();

            if (null != _rightFlipper)
                _rightFlipper.Release();

            if (null != _leftFlipper)
                _leftFlipper.Release();

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
            if (_gameState == EGameState.GameOver)
                return;

            _leftFlipper.SetPressed(isPressed);
        }

        void OnFlipperRightChanged(bool isPressed)
        {
            if (_gameState == EGameState.GameOver)
                return;

            _rightFlipper.SetPressed(isPressed);
        }

        void OnLaunchPressed()
        {
            if (_gameState == EGameState.GameOver)
                return;

            if (_spring.TryLaunch())
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
            Ball ball = _ballSpawner.Spawn(transform.position);
            if (null == ball)
                return;

            ball.SetMass(_statData.BallMass);
            _spring.LoadBall(ball);
        }

        void EndGame()
        {
            _gameState = EGameState.GameOver;
            _leftFlipper.SetPressed(false);
            _rightFlipper.SetPressed(false);
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
