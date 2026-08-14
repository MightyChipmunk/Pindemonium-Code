using System;
using Pinball.Ingame.Map;
using Pinball.Ingame.Stat;
using UnityEngine;

namespace Pinball.Ingame
{
    public class BallFlowController
    {
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
        int _gimmickHitCount = 0;

        public int RemainingBalls => _ballStock.Remaining;

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

            Ball ball = _spawner.Spawn();
            if (null == ball)
                return;

            ball.SetMass(_stats.GetBallMass());
            ApplyPassiveAura(ball);
            _activeBall = ball;
            _gimmickHitCount = 0;
            _map.LoadBall(ball);
        }

        public void DespawnActiveBall()
        {
            if (null == _activeBall)
                return;

            _spawner.Despawn(_activeBall);
            _activeBall = null;
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

        public void CountGimmickHit()
        {
            if (!IsPlaying())
                return;

            if (null == _activeBall)
                return;

            if (!_stats.HasEffect(ShopItemData.EEffectType.FragileBallScoreRatio))
                return;

            int hitLimit = Mathf.RoundToInt(_stats.GetEffectSub(ShopItemData.EEffectType.FragileBallScoreRatio));
            if (hitLimit <= 0)
                return;

            ++_gimmickHitCount;
            if (_gimmickHitCount < hitLimit)
                return;

            DespawnActiveBall();
            HandleBallLost();
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
