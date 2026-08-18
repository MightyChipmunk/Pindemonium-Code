using System;
using System.Collections.Generic;
using Pinball.Ingame.Map;
using Pinball.Ingame.Stat;
using UnityEngine;

namespace Pinball.Ingame
{
    public class BallFlowController
    {
        const float CLONE_ALPHA = 0.6f;
        const float CLONE_SPAWN_OFFSET = 0.4f;
        const float MIN_CLONE_OFFSET_SQR_SPEED = 0.01f;

        public record InitParam(
            BallSpawner Spawner,
            BallDrainZone DrainZone,
            StatProvider Stats,
            Func<bool> IsControlBlockedGetter,
            Func<bool> IsPlayingGetter);

        BallSpawner _spawner = null;
        BallDrainZone _drainZone = null;
        BallStockCounter _ballStock = null;
        StatProvider _stats = null;
        Func<bool> _isControlBlockedGetter = null;
        Func<bool> _isPlayingGetter = null;
        MapRoot _map = null;
        Ball _activeBall = null;
        List<Ball> _cloneBalls = null;
        float _cloneSpawnElapsed = 0f;
        int _gimmickHitCount = 0;
        int _usedBallCount = 0;
        bool _isSurgeCharged = false;

        public int RemainingBalls => _ballStock.Remaining;
        public int UsedBallCount => _usedBallCount;

        public event Action<int> OnBallCountChanged;
        public event Action OnBallLost;
        public event Action OnBallReady;
        public event Action OnAllBallsLost;

        public void Init(InitParam param)
        {
            _spawner = param.Spawner;
            _drainZone = param.DrainZone;
            _stats = param.Stats;
            _isControlBlockedGetter = param.IsControlBlockedGetter;
            _isPlayingGetter = param.IsPlayingGetter;
            _usedBallCount = 0;
            _cloneBalls = new List<Ball>();
            _cloneSpawnElapsed = 0f;
            _isSurgeCharged = false;
            _ballStock = new BallStockCounter();
            _ballStock.Init(_stats.GetInitialBallCount());
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

            if (null != _cloneBalls)
            {
                _cloneBalls.Clear();
                _cloneBalls = null;
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

        public void Tick(float deltaTime)
        {
            TickCloneSpawn(deltaTime);
        }

        public void LoadBallOnSpring()
        {
            if (null == _map)
                return;

            Ball ball = _spawner.Spawn();
            if (null == ball)
                return;

            ball.SetMass(_stats.GetBallMass());
            ApplyPassiveAura(ball);
            ball.SetAura(BallAura.EAuraType.Surge, _isSurgeCharged);
            _map.SetFlipperCollisionIgnored(ball, false);
            _activeBall = ball;
            _gimmickHitCount = 0;
            _cloneSpawnElapsed = 0f;
            _map.LoadBall(ball);
        }

        public void DespawnBalls()
        {
            DespawnActiveBall();
            DespawnCloneBalls();
        }

        public bool TryApplyImpulseToActiveBall(Vector2 impulse)
        {
            if (null == _activeBall)
                return false;

            _activeBall.AddImpulse(impulse);
            return true;
        }

        public void RefillStock()
        {
            _ballStock.Init(_stats.GetInitialBallCount());
            OnBallCountChanged?.Invoke(_ballStock.Remaining);
        }

        public void SetSurgeCharged(bool isCharged)
        {
            _isSurgeCharged = isCharged;

            if (null == _activeBall)
                return;

            _activeBall.SetAura(BallAura.EAuraType.Surge, isCharged);
        }

        public bool IsCloneBall(Collider2D ballCollider)
        {
            if (null == _cloneBalls)
                return false;

            if (!_spawner.TryResolve(ballCollider, out Ball ball))
                return false;

            return _cloneBalls.Contains(ball);
        }

        void HandleBallCountChanged(int remaining)
        {
            OnBallCountChanged?.Invoke(remaining);
        }

        void HandleBallDrained(Collider2D collider)
        {
            if (!_spawner.TryResolve(collider, out Ball ball))
                return;

            if (TryDespawnCloneBall(ball))
                return;

            if (IsControlBlocked())
                return;

            _spawner.Despawn(ball);

            if (ball == _activeBall)
                _activeBall = null;

            HandleBallLost();
        }

        void HandleBallLost()
        {
            DespawnCloneBalls();
            OnBallLost?.Invoke();

            if (IsControlBlocked())
                return;

            _ballStock.Consume();
            ++_usedBallCount;

            if (_ballStock.Remaining <= 0)
            {
                OnAllBallsLost?.Invoke();
                return;
            }

            LoadBallOnSpring();
            OnBallReady?.Invoke();
        }

        public void CountGimmickHit(Collider2D ballCollider)
        {
            if (!IsPlaying())
                return;

            if (!IsActiveBall(ballCollider))
                return;

            if (!_stats.HasEffect(ShopItemData.EEffectType.FragileBallScoreRatio))
                return;

            int hitLimit = Mathf.RoundToInt(_stats.GetEffectValue(ShopItemData.EEffectType.FragileBallScoreRatio, ShopItemData.SUB_VALUE_INDEX));
            if (hitLimit <= 0)
                return;

            ++_gimmickHitCount;
            if (_gimmickHitCount < hitLimit)
                return;

            DespawnActiveBall();
            HandleBallLost();
        }

        void DespawnActiveBall()
        {
            if (null == _activeBall)
                return;

            _spawner.Despawn(_activeBall);
            _activeBall = null;
        }

        void DespawnCloneBalls()
        {
            if (null == _cloneBalls)
                return;

            for (int i = 0; i < _cloneBalls.Count; ++i)
                _spawner.Despawn(_cloneBalls[i]);

            _cloneBalls.Clear();
        }

        bool TryDespawnCloneBall(Ball ball)
        {
            if (null == _cloneBalls)
                return false;

            if (!_cloneBalls.Remove(ball))
                return false;

            _spawner.Despawn(ball);
            return true;
        }

        void TickCloneSpawn(float deltaTime)
        {
            if (!IsPlaying())
                return;

            if (null == _activeBall || null == _map)
                return;

            float spawnInterval = _stats.GetEffectValue(ShopItemData.EEffectType.BallCloneSpawn, ShopItemData.SUB_VALUE_INDEX);
            if (spawnInterval <= 0f)
                return;

            _cloneSpawnElapsed += deltaTime;
            if (_cloneSpawnElapsed < spawnInterval)
                return;

            _cloneSpawnElapsed = 0f;
            SpawnCloneBall();
        }

        void SpawnCloneBall()
        {
            Ball clone = _spawner.Spawn();
            if (null == clone)
                return;

            _cloneBalls.Add(clone);
            clone.SetMass(_stats.GetBallMass());
            clone.MoveTo(GetCloneSpawnPosition());
            clone.SetKinematic(false);
            clone.SetAlpha(CLONE_ALPHA);
            ApplyPassiveAura(clone);
            _map.SetFlipperCollisionIgnored(clone, true);
        }

        Vector3 GetCloneSpawnPosition()
        {
            Vector2 velocity = _activeBall.GetVelocity();
            Vector2 offsetDirection = velocity.sqrMagnitude > MIN_CLONE_OFFSET_SQR_SPEED ? -velocity.normalized : Vector2.down;
            return _activeBall.transform.position + (Vector3)(offsetDirection * CLONE_SPAWN_OFFSET);
        }

        bool IsActiveBall(Collider2D ballCollider)
        {
            if (!_spawner.TryResolve(ballCollider, out Ball ball))
                return false;

            return ball == _activeBall;
        }

        void ApplyPassiveAura(Ball ball)
        {
            ball.SetAura(BallAura.EAuraType.Fire, _stats.HasEffect(ShopItemData.EEffectType.BumperBurn));
            ball.SetAura(BallAura.EAuraType.Lightning, _stats.HasEffect(ShopItemData.EEffectType.BumperChainLightning));
            ball.SetAura(BallAura.EAuraType.Frost, _stats.HasEffect(ShopItemData.EEffectType.BumperFreeze));
            ball.SetAura(BallAura.EAuraType.Gold, _stats.HasEffect(ShopItemData.EEffectType.GimmickBreakMoney));
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
