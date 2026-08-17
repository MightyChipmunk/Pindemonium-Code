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
        const PoolableFX.EFXType CHAIN_FX = PoolableFX.EFXType.LightningBall;
        const PoolableFX.EFXType CHAIN_LINK_FX = PoolableFX.EFXType.LightningChainLink;

        public record InitParam(
            Action<MapInteractable.EGimmickType, int, Vector2, MapInteractable.EScoreSource> OnScoreGained,
            Func<Collider2D, float, float> DamageEvaluator,
            Func<float> FlipPowerGetter,
            Func<float> PerfectTimingRelaxGetter,
            Action<Flipper.EHitGrade, Vector2> OnBallStruck,
            Action<BumperInteractable, float> OnBumperDamaged,
            Action OnBumperBroken,
            Action<Spring.EChargeEvent, Vector2> OnSpringChargeEvent,
            Action<PoolableFX.EFXType, Vector2> FXPlayer,
            Action<PoolableFX.EFXType, Vector2, Vector2> LineFXPlayer,
            Action<Collider2D> OnGimmickBallHit,
            Func<MapInteractable.EGimmickType, bool> IsGimmickAbsentGetter,
            Func<MapInteractable.EGimmickType, int> ScoreGainLimitGetter,
            bool IsBumperUnbreakable);

        [SerializeField] MapInteractable[] _interactables = null;
        [SerializeField] Flipper _leftFlipper = null;
        [SerializeField] Flipper _rightFlipper = null;
        [SerializeField] Spring _spring = null;

        Action<Flipper.EHitGrade, Vector2> _onBallStruck = null;
        Action<BumperInteractable, float> _onBumperDamaged = null;
        Action _onBumperBroken = null;
        Action<PoolableFX.EFXType, Vector2, Vector2> _lineFXPlayer = null;
        List<BumperInteractable> _bumpers = null;
        CancellationTokenSource _cancellationTokenSource = null;

        public void Init(InitParam param)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _bumpers = new List<BumperInteractable>();
            _onBumperDamaged = param.OnBumperDamaged;
            _onBumperBroken = param.OnBumperBroken;
            _lineFXPlayer = param.LineFXPlayer;

            if (null != _interactables)
            {
                for (int i = 0; i < _interactables.Length; ++i)
                {
                    MapInteractable interactable = _interactables[i];
                    int scoreGainLimit = interactable.IsDeactivatable ? param.ScoreGainLimitGetter(interactable.GimmickType) : MapInteractable.NO_SCORE_GAIN_LIMIT;
                    interactable.Init(param.OnScoreGained, param.DamageEvaluator, param.FXPlayer, param.OnGimmickBallHit, scoreGainLimit);

                    if (interactable.IsDeactivatable && param.IsGimmickAbsentGetter(interactable.GimmickType))
                        interactable.Deactivate();

                    BumperInteractable bumper = interactable as BumperInteractable;
                    if (null == bumper)
                        continue;

                    bumper.SetUnbreakable(param.IsBumperUnbreakable);
                    _bumpers.Add(bumper);
                    bumper.OnBallDamaged += HandleBumperDamaged;
                    bumper.OnBroken += HandleBumperBroken;
                }
            }

            _onBallStruck = param.OnBallStruck;
            _leftFlipper.Init(param.FlipPowerGetter, param.PerfectTimingRelaxGetter);
            _rightFlipper.Init(param.FlipPowerGetter, param.PerfectTimingRelaxGetter);
            _leftFlipper.OnBallStruck += OnFlipperBallStruck;
            _rightFlipper.OnBallStruck += OnFlipperBallStruck;
            _spring.Init(param.OnSpringChargeEvent);
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
            _onBumperBroken = null;

            if (null != _bumpers)
            {
                for (int i = 0; i < _bumpers.Count; ++i)
                {
                    _bumpers[i].OnBallDamaged -= HandleBumperDamaged;
                    _bumpers[i].OnBroken -= HandleBumperBroken;
                }

                _bumpers.Clear();
                _bumpers = null;
            }

            if (null == _interactables)
                return;

            for (int i = 0; i < _interactables.Length; ++i)
                _interactables[i].Release();
        }

        public void ApplyChainDamage(BumperInteractable source, float baseDamage, float chainRatio, int maxChainCount)
        {
            if (null == _bumpers || null == _cancellationTokenSource)
                return;

            if (maxChainCount <= 0 || chainRatio <= 0f)
                return;

            ChainDamageAsync(source, baseDamage, chainRatio, maxChainCount, _cancellationTokenSource.Token).Forget();
        }

        async UniTaskVoid ChainDamageAsync(BumperInteractable source, float baseDamage, float chainRatio, int maxChainCount, CancellationToken cancellationToken)
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
                _lineFXPlayer?.Invoke(CHAIN_LINK_FX, originPoint, targetPoint);
                originPoint = targetPoint;
                target.ApplyDamage(chainDamage, CHAIN_FX);
            }
        }

        BumperInteractable FindNearestChainTarget(Vector2 originPoint, List<BumperInteractable> chainedBumpers)
        {
            BumperInteractable nearest = null;
            float nearestSqrDistance = float.MaxValue;

            for (int i = 0; i < _bumpers.Count; ++i)
            {
                BumperInteractable bumper = _bumpers[i];
                if (bumper.IsBroken || bumper.IsDeactivated)
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

        void HandleBumperBroken()
        {
            _onBumperBroken?.Invoke();
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

        public void SetFlipperCollisionIgnored(Ball ball, bool isIgnored)
        {
            _leftFlipper.SetBallCollisionIgnored(ball, isIgnored);
            _rightFlipper.SetBallCollisionIgnored(ball, isIgnored);
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
