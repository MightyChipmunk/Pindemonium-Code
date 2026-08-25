using System;
using System.Collections.Generic;
using Pinball.Audio;
using Pinball.FX;
using Pinball.Ingame.Map;
using Pinball.Ingame.Score;
using Pinball.Ingame.Shop;
using Pinball.Ingame.Stat;
using UnityEngine;

namespace Pinball.Ingame
{
    public class BallFlowController
    {
        const float CLONE_ALPHA = 0.6f;
        const float CLONE_SPAWN_OFFSET = 0.4f;
        const float MIN_CLONE_OFFSET_SQR_SPEED = 0.01f;
        const float CLONE_SPLIT_IMPULSE = 2f;
        const float FULL_CIRCLE_DEGREE = 360f;

        public record InitParam(
            Func<bool> IsControlBlockedGetter,
            Func<bool> IsPlayingGetter,
            Action<PoolableFX.EFXType, Vector2> FXPlayer);

        BallSpawner _spawner = null;
        BallStockCounter _ballStock = null;
        Func<bool> _isControlBlockedGetter = null;
        Func<bool> _isPlayingGetter = null;
        Action<PoolableFX.EFXType, Vector2> _fxPlayer = null;
        MapRoot _map = null;
        Ball _activeBall = null;
        List<Ball> _cloneBalls = null;
        List<float> _cloneLifeRemains = null;
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

        public BallFlowController(InitParam param, BallSpawner spawner)
        {
            _spawner = spawner;
            _isControlBlockedGetter = param.IsControlBlockedGetter;
            _isPlayingGetter = param.IsPlayingGetter;
            _fxPlayer = param.FXPlayer;
            _usedBallCount = 0;
            _cloneBalls = new List<Ball>();
            _cloneLifeRemains = new List<float>();
            _cloneSpawnElapsed = 0f;
            _isSurgeCharged = false;
            _ballStock = new BallStockCounter(StatProvider.GetInitialBallCount());
            _ballStock.OnChanged += HandleBallCountChanged;
        }

        public void SetMap(MapRoot map)
        {
            _map = map;
        }

        public void Tick(float deltaTime)
        {
            TickCloneLife(deltaTime);
            TickCloneSpawn(deltaTime);
        }

        public void LoadBallOnSpring()
        {
            if (null == _map)
                return;

            Ball ball = _spawner.Spawn();
            if (null == ball)
                return;

            ball.SetMass(StatProvider.GetBallMass());
            ball.SetHitFXPlayer(_fxPlayer);
            ApplyPassiveAura(ball);
            ball.SetAura(BallAura.EAuraType.Surge, _isSurgeCharged);
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

        public void SetNudgeAimActive(bool isActive)
        {
            if (null == _activeBall)
                return;

            _activeBall.SetNudgeAimActive(isActive);
        }

        public void SetNudgeAimDirection(Vector2 direction)
        {
            if (null == _activeBall)
                return;

            _activeBall.SetNudgeAimDirection(direction);
        }

        public bool TryApplyNudgeToActiveBall(Vector2 impulse)
        {
            if (null == _activeBall)
                return false;

            _activeBall.ScaleVelocity(StatProvider.GetNudgeVelocityKeepRatio());
            _activeBall.AddImpulse(impulse);
            return true;
        }

        public void RefillStock()
        {
            _ballStock.Reset(StatProvider.GetInitialBallCount());
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

        public void HandleBallDrained(Collider2D collider)
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
            AudioManager.Play(AudioStorage.EAudioClipType.BallLost);
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
            AudioManager.Play(AudioStorage.EAudioClipType.BallReady);
            OnBallReady?.Invoke();
        }

        public void CountGimmickHit(Collider2D ballCollider)
        {
            if (!IsPlaying())
                return;

            if (!IsActiveBall(ballCollider))
                return;

            if (!StatProvider.HasEffect(ShopItemData.EEffectType.FragileBallScoreRatio))
                return;

            int hitLimit = Mathf.RoundToInt(StatProvider.GetEffectValue(ShopItemData.EEffectType.FragileBallScoreRatio, ShopItemData.SUB_VALUE_INDEX));
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
            _cloneLifeRemains.Clear();
        }

        bool TryDespawnCloneBall(Ball ball)
        {
            if (null == _cloneBalls)
                return false;

            int index = _cloneBalls.IndexOf(ball);
            if (index < 0)
                return false;

            DespawnCloneBallAt(index);
            return true;
        }

        void DespawnCloneBallAt(int index)
        {
            _spawner.Despawn(_cloneBalls[index]);
            _cloneBalls.RemoveAt(index);
            _cloneLifeRemains.RemoveAt(index);
        }

        void TickCloneLife(float deltaTime)
        {
            if (null == _cloneBalls)
                return;

            for (int i = _cloneBalls.Count - 1; i >= 0; --i)
            {
                _cloneLifeRemains[i] -= deltaTime;
                if (_cloneLifeRemains[i] > 0f)
                    continue;

                DespawnCloneBallAt(i);
            }
        }

        void TickCloneSpawn(float deltaTime)
        {
            if (!IsPlaying())
                return;

            if (null == _activeBall || null == _map)
                return;

            float spawnInterval = StatProvider.GetEffectValue(ShopItemData.EEffectType.BallCloneSpawn, ShopItemData.SUB_VALUE_INDEX);
            if (spawnInterval <= 0f)
                return;

            _cloneSpawnElapsed += deltaTime;
            if (_cloneSpawnElapsed < spawnInterval)
                return;

            _cloneSpawnElapsed = 0f;
            SpawnCloneBall(spawnInterval);
        }

        void SpawnCloneBall(float lifeTime)
        {
            Ball clone = _spawner.Spawn();
            if (null == clone)
                return;

            _cloneBalls.Add(clone);
            _cloneLifeRemains.Add(lifeTime);
            clone.SetMass(StatProvider.GetBallMass());
            clone.MoveTo(GetCloneSpawnPosition());
            clone.SetKinematic(false);
            clone.SetAlpha(CLONE_ALPHA);
            clone.SetHitFXPlayer(_fxPlayer);
            ApplyPassiveAura(clone);
            clone.AddImpulse(GetRandomSplitImpulse());
            AudioManager.Play(AudioStorage.EAudioClipType.BallCloneSpawn);
        }

        Vector2 GetRandomSplitImpulse()
        {
            float angleRadian = UnityEngine.Random.Range(0f, FULL_CIRCLE_DEGREE) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angleRadian), Mathf.Sin(angleRadian)) * CLONE_SPLIT_IMPULSE;
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
            ball.SetAura(BallAura.EAuraType.Fire, StatProvider.HasEffect(ShopItemData.EEffectType.BumperBurn));
            ball.SetAura(BallAura.EAuraType.Lightning, StatProvider.HasEffect(ShopItemData.EEffectType.BumperChainLightning));
            ball.SetAura(BallAura.EAuraType.Frost, StatProvider.HasEffect(ShopItemData.EEffectType.BumperFreeze));
            ball.SetAura(BallAura.EAuraType.Gold, StatProvider.HasEffect(ShopItemData.EEffectType.GimmickBreakMoney));
            ball.SetAura(BallAura.EAuraType.Blast, StatProvider.HasEffect(ShopItemData.EEffectType.BumperBlast));
            ball.SetSpikesActive(StatProvider.HasEffect(ShopItemData.EEffectType.BumperHitScoreAdd));
            ball.SetScopeActive(StatProvider.HasEffect(ShopItemData.EEffectType.BumperSniperMark));
            ball.SetStoneActive(StatProvider.HasEffect(ShopItemData.EEffectType.BallDamageRatio));
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
