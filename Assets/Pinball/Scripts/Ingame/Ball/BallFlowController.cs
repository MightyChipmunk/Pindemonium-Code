using System;
using Pinball.Ingame.Map;
using Pinball.Ingame.Stat;
using UnityEngine;

namespace Pinball.Ingame
{
    public class BallFlowController
    {
        static readonly Color DEFAULT_TRAIL_COLOR = Color.white;
        static readonly Color FIREBALL_TRAIL_COLOR = new Color(1f, 0.55f, 0.2f, 1f);
        static readonly Color LIGHTNING_BALL_TRAIL_COLOR = new Color(0.45f, 0.85f, 1f, 1f);

        BallSpawner _spawner = null;
        BallDrainZone _drainZone = null;
        BallStockCounter _ballStock = null;
        StatProvider _stats = null;
        Func<bool> _isControlBlockedGetter = null;
        Func<bool> _isPlayingGetter = null;
        MapRoot _map = null;
        Ball _activeBall = null;
        Vector3 _spawnPosition = Vector3.zero;
        float _ballMass = 0f;
        int _initialBallCount = 0;
        int _ballCollisionCount = 0;

        public int RemainingBalls => _ballStock.Remaining;

        public event Action<int> OnBallCountChanged;
        public event Action OnBallLost;
        public event Action OnBallReady;
        public event Action OnAllBallsLost;

        public void Init(BallSpawner spawner, BallDrainZone drainZone, StatProvider stats, int initialBallCount, float ballMass, Vector3 spawnPosition, Func<bool> isControlBlockedGetter, Func<bool> isPlayingGetter)
        {
            _spawner = spawner;
            _drainZone = drainZone;
            _stats = stats;
            _initialBallCount = initialBallCount;
            _ballMass = ballMass;
            _spawnPosition = spawnPosition;
            _isControlBlockedGetter = isControlBlockedGetter;
            _isPlayingGetter = isPlayingGetter;
            _ballStock = new BallStockCounter();
            _ballStock.Init(initialBallCount);
            _ballStock.OnChanged += HandleBallCountChanged;
            _drainZone.OnColliderEntered += HandleBallDrained;
        }

        public void Release()
        {
            if (null != _drainZone)
            {
                _drainZone.OnColliderEntered -= HandleBallDrained;
                _drainZone = null;
            }

            if (null != _ballStock)
            {
                _ballStock.Release();
                _ballStock = null;
            }

            OnBallCountChanged = null;
            OnBallLost = null;
            OnBallReady = null;
            OnAllBallsLost = null;
            _activeBall = null;
            _map = null;
            _stats = null;
            _spawner = null;
            _isControlBlockedGetter = null;
            _isPlayingGetter = null;
        }

        public void SetMap(MapRoot map)
        {
            _map = map;
        }

        public void LoadBallOnSpring()
        {
            if (null == _map)
                return;

            Ball ball = _spawner.Spawn(_spawnPosition);
            if (null == ball)
                return;

            ball.SetMass(_ballMass);
            ball.SetTrailColor(GetBallTrailColor());
            ball.SetCollisionCallback(CountBallCollision);
            _activeBall = ball;
            _ballCollisionCount = 0;
            _map.LoadBall(ball);
        }

        public void DespawnActiveBall()
        {
            if (null == _activeBall)
                return;

            _spawner.Despawn(_activeBall);
            _activeBall = null;
        }

        public void RefillStock()
        {
            _ballStock.Init(_initialBallCount);
            OnBallCountChanged?.Invoke(_ballStock.Remaining);
        }

        void HandleBallCountChanged(int remaining)
        {
            OnBallCountChanged?.Invoke(remaining);
        }

        void HandleBallDrained(Collider2D collider)
        {
            if (IsControlBlocked())
                return;

            if (!_spawner.TryResolve(collider, out Ball ball))
                return;

            _spawner.Despawn(ball);

            if (ball == _activeBall)
                _activeBall = null;

            HandleBallLost();
        }

        void HandleBallLost()
        {
            OnBallLost?.Invoke();

            if (IsControlBlocked())
                return;

            _ballStock.Consume();

            if (_ballStock.Remaining <= 0)
            {
                OnAllBallsLost?.Invoke();
                return;
            }

            LoadBallOnSpring();
            OnBallReady?.Invoke();
        }

        void CountBallCollision()
        {
            if (!IsPlaying())
                return;

            if (!_stats.HasEffect(ShopItemData.EEffectType.FragileBallScoreRatio))
                return;

            int collisionLimit = Mathf.RoundToInt(_stats.GetEffectSub(ShopItemData.EEffectType.FragileBallScoreRatio));
            if (collisionLimit <= 0)
                return;

            ++_ballCollisionCount;
            if (_ballCollisionCount < collisionLimit)
                return;

            DespawnActiveBall();
            HandleBallLost();
        }

        Color GetBallTrailColor()
        {
            if (_stats.HasEffect(ShopItemData.EEffectType.BumperBurn))
                return FIREBALL_TRAIL_COLOR;

            if (_stats.HasEffect(ShopItemData.EEffectType.BumperChainLightning))
                return LIGHTNING_BALL_TRAIL_COLOR;

            return DEFAULT_TRAIL_COLOR;
        }

        bool IsControlBlocked()
        {
            if (null == _isControlBlockedGetter)
                return false;

            return _isControlBlockedGetter();
        }

        bool IsPlaying()
        {
            if (null == _isPlayingGetter)
                return false;

            return _isPlayingGetter();
        }
    }
}
