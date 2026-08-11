using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.FX;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class MapRoot : BehaviourBase
    {
        const float CHAIN_INTERVAL = 0.1f;

        [SerializeField] MapInteractable[] _interactables = null;
        [SerializeField] Flipper _leftFlipper = null;
        [SerializeField] Flipper _rightFlipper = null;
        [SerializeField] Spring _spring = null;

        Action<Flipper.EHitGrade, Vector2> _onBallStruck = null;
        Action<BumperInteractable, float> _onBumperDamaged = null;
        Action<PoolableFX.EFXType, Vector2, Vector2> _lineFXPlayer = null;
        List<BumperInteractable> _bumpers = null;
        CancellationTokenSource _cancellationTokenSource = null;

        public void Init(Action<MapInteractable.EGimmickType, int, Vector2, MapInteractable.EScoreSource> onScoreGained, Func<float, float> damageEvaluator, Func<float> flipPowerGetter, Func<float> perfectTimingRelaxGetter, Action<Flipper.EHitGrade, Vector2> onBallStruck, Action<BumperInteractable, float> onBumperDamaged, Action<Spring.EChargeEvent, Vector2> onSpringChargeEvent, Action<PoolableFX.EFXType, Vector2> fxPlayer, Action<PoolableFX.EFXType, Vector2, Vector2> lineFXPlayer, Action onGimmickBallHit)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _bumpers = new List<BumperInteractable>();
            _onBumperDamaged = onBumperDamaged;
            _lineFXPlayer = lineFXPlayer;

            if (null != _interactables)
            {
                for (int i = 0; i < _interactables.Length; ++i)
                {
                    _interactables[i].Init(onScoreGained, damageEvaluator, fxPlayer, onGimmickBallHit);

                    BumperInteractable bumper = _interactables[i] as BumperInteractable;
                    if (null == bumper)
                        continue;

                    _bumpers.Add(bumper);
                    bumper.OnBallDamaged += HandleBumperDamaged;
                }
            }

            _onBallStruck = onBallStruck;
            _leftFlipper.Init(flipPowerGetter, perfectTimingRelaxGetter);
            _rightFlipper.Init(flipPowerGetter, perfectTimingRelaxGetter);
            _leftFlipper.OnBallStruck += OnFlipperBallStruck;
            _rightFlipper.OnBallStruck += OnFlipperBallStruck;
            _spring.Init(onSpringChargeEvent);
        }

        public void Release()
        {
            if (null != _cancellationTokenSource)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            _lineFXPlayer = null;
            _spring.Release();
            _rightFlipper.OnBallStruck -= OnFlipperBallStruck;
            _leftFlipper.OnBallStruck -= OnFlipperBallStruck;
            _rightFlipper.Release();
            _leftFlipper.Release();
            _onBallStruck = null;
            _onBumperDamaged = null;

            if (null != _bumpers)
            {
                for (int i = 0; i < _bumpers.Count; ++i)
                    _bumpers[i].OnBallDamaged -= HandleBumperDamaged;

                _bumpers.Clear();
                _bumpers = null;
            }

            if (null == _interactables)
                return;

            for (int i = 0; i < _interactables.Length; ++i)
                _interactables[i].Release();
        }

        public void ApplyChainDamage(BumperInteractable source, float baseDamage, float chainRatio, int maxChainCount, PoolableFX.EFXType chainFX, PoolableFX.EFXType chainLinkFX)
        {
            if (null == _bumpers || null == _cancellationTokenSource)
                return;

            if (maxChainCount <= 0 || chainRatio <= 0f)
                return;

            ChainDamageAsync(source, baseDamage, chainRatio, maxChainCount, chainFX, chainLinkFX, _cancellationTokenSource.Token).Forget();
        }

        async UniTaskVoid ChainDamageAsync(BumperInteractable source, float baseDamage, float chainRatio, int maxChainCount, PoolableFX.EFXType chainFX, PoolableFX.EFXType chainLinkFX, CancellationToken cancellationToken)
        {
            List<BumperInteractable> chainedBumpers = new List<BumperInteractable>();
            chainedBumpers.Add(source);

            Vector2 originPoint = source.transform.position;
            float chainDamage = baseDamage;

            for (int i = 0; i < maxChainCount; ++i)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(CHAIN_INTERVAL), cancellationToken: cancellationToken);

                chainDamage *= chainRatio;
                BumperInteractable target = FindNearestChainTarget(originPoint, chainedBumpers);
                if (null == target)
                    return;

                chainedBumpers.Add(target);
                Vector2 targetPoint = target.transform.position;
                _lineFXPlayer?.Invoke(chainLinkFX, originPoint, targetPoint);
                originPoint = targetPoint;
                target.ApplyDamage(chainDamage, chainFX);
            }
        }

        BumperInteractable FindNearestChainTarget(Vector2 originPoint, List<BumperInteractable> chainedBumpers)
        {
            BumperInteractable nearest = null;
            float nearestSqrDistance = float.MaxValue;

            for (int i = 0; i < _bumpers.Count; ++i)
            {
                BumperInteractable bumper = _bumpers[i];
                if (bumper.IsBroken)
                    continue;

                if (chainedBumpers.Contains(bumper))
                    continue;

                float sqrDistance = ((Vector2)bumper.transform.position - originPoint).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance)
                    continue;

                nearestSqrDistance = sqrDistance;
                nearest = bumper;
            }

            return nearest;
        }

        void HandleBumperDamaged(BumperInteractable bumper, float damage)
        {
            _onBumperDamaged?.Invoke(bumper, damage);
        }

        public void SetFlipperPressed(Flipper.ESide side, bool isPressed)
        {
            if (side == Flipper.ESide.Left)
                _leftFlipper.SetPressed(isPressed);
            else
                _rightFlipper.SetPressed(isPressed);
        }

        public void LoadBall(Ball ball)
        {
            _spring.LoadBall(ball);
        }

        public void SetSpringCharging(bool isCharging)
        {
            _spring.SetCharging(isCharging);
        }

        public bool TryLaunch()
        {
            return _spring.TryLaunch();
        }

        void OnFlipperBallStruck(Flipper.EHitGrade grade, Vector2 worldPoint)
        {
            _onBallStruck?.Invoke(grade, worldPoint);
        }

#if UNITY_EDITOR
        protected override void OnBindSerializedField()
        {
            _interactables = GetComponentsInChildren<MapInteractable>(true);
            _spring = GetComponentInChildren<Spring>(true);

            Flipper[] flippers = GetComponentsInChildren<Flipper>(true);
            for (int i = 0; i < flippers.Length; ++i)
            {
                if (flippers[i].Side == Flipper.ESide.Left)
                    _leftFlipper = flippers[i];
                else
                    _rightFlipper = flippers[i];
            }

            for (int i = 0; i < _interactables.Length; ++i)
            {
                LaneInteractable lane = _interactables[i] as LaneInteractable;
                if (null != lane)
                    lane.BakePath();
            }
        }
#endif // UNITY_EDITOR
    }
}
