using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.Audio;
using Pinball.FX;
using Pinball.Ingame.Shop;
using Pinball.Ingame.Stat;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class MapRoot : BehaviourBase
    {
        const float CHAIN_INTERVAL = 0.1f;
        const PoolableFX.EFXType CHAIN_FX = PoolableFX.EFXType.LightningBall;
        const PoolableFX.EFXType CHAIN_LINK_FX = PoolableFX.EFXType.LightningChainLink;
        const PoolableFX.EFXType BLAST_FX = PoolableFX.EFXType.Blast;

        public record InitParam(
            Action<MapInteractable.EGimmickType, int, Vector2, MapInteractable.EScoreSource> OnScoreGained,
            Func<Collider2D, float, float> DamageEvaluator,
            Action<Flipper.EHitGrade, Vector2> OnBallStruck,
            Action<BumperInteractable, float> OnBumperDamaged,
            Action OnBumperBroken,
            Action<Spring.EChargeEvent, Vector2> OnSpringChargeEvent,
            Action<PoolableFX.EFXType, Vector2> FXPlayer,
            Action<PoolableFX.EFXType, Vector2, Vector2> LineFXPlayer,
            Action<Collider2D> OnGimmickBallHit,
            Action GimmickHitPresenter,
            Func<MapInteractable.EGimmickType, bool> IsGimmickAbsentGetter,
            Func<MapInteractable.EGimmickType, int> ScoreGainLimitGetter);

        [SerializeField] MapInteractable[] _interactables = null;
        [SerializeField] Flipper _leftFlipper = null;
        [SerializeField] Flipper _rightFlipper = null;
        [SerializeField] Spring _spring = null;

        Action<Flipper.EHitGrade, Vector2> _onBallStruck = null;
        Action<BumperInteractable, float> _onBumperDamaged = null;
        Action _onBumperBroken = null;
        Action<PoolableFX.EFXType, Vector2> _fxPlayer = null;
        Action<PoolableFX.EFXType, Vector2, Vector2> _lineFXPlayer = null;
        List<BumperInteractable> _bumpers = null;
        List<BumperInteractable> _markCandidates = null;
        CancellationTokenSource _cancellationTokenSource = null;
        float _sniperMarkElapsed = 0f;

        public void Init(InitParam param)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _bumpers = new List<BumperInteractable>();
            _markCandidates = new List<BumperInteractable>();
            _sniperMarkElapsed = 0f;
            _onBumperDamaged = param.OnBumperDamaged;
            _onBumperBroken = param.OnBumperBroken;
            _fxPlayer = param.FXPlayer;
            _lineFXPlayer = param.LineFXPlayer;

            if (null != _interactables)
            {
                for (int i = 0; i < _interactables.Length; ++i)
                {
                    MapInteractable interactable = _interactables[i];
                    int scoreGainLimit = interactable.IsDeactivatable ? param.ScoreGainLimitGetter(interactable.GimmickType) : MapInteractable.NO_SCORE_GAIN_LIMIT;
                    interactable.Init(new MapInteractable.InitParam(
                        OnScoreGained: param.OnScoreGained,
                        DamageEvaluator: param.DamageEvaluator,
                        FXPlayer: param.FXPlayer,
                        OnBallHit: param.OnGimmickBallHit,
                        GimmickHitPresenter: param.GimmickHitPresenter,
                        ScoreGainLimit: scoreGainLimit));

                    if (interactable.IsDeactivatable && param.IsGimmickAbsentGetter(interactable.GimmickType))
                        interactable.Deactivate();

                    BumperInteractable bumper = interactable as BumperInteractable;
                    if (null == bumper)
                        continue;

                    _bumpers.Add(bumper);
                    bumper.OnBallDamaged += HandleBumperDamaged;
                    bumper.OnBroken += HandleBumperBroken;
                }
            }

            _onBallStruck = param.OnBallStruck;
            _leftFlipper.Init();
            _rightFlipper.Init();
            _leftFlipper.OnBallStruck += OnFlipperBallStruck;
            _rightFlipper.OnBallStruck += OnFlipperBallStruck;
            _spring.Init(param.OnSpringChargeEvent);
        }

        public void Suspend()
        {
            CancelChainDamage();
            _spring.SetCharging(false);

            if (null == _interactables)
                return;

            for (int i = 0; i < _interactables.Length; ++i)
                _interactables[i].Release();
        }

        public void Release()
        {
            CancelChainDamage();
            _fxPlayer = null;
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

            if (null != _markCandidates)
            {
                _markCandidates.Clear();
                _markCandidates = null;
            }

            if (null == _interactables)
                return;

            for (int i = 0; i < _interactables.Length; ++i)
                _interactables[i].Release();
        }

        void CancelChainDamage()
        {
            if (null == _cancellationTokenSource)
                return;

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        public void TickSniperMark(float deltaTime)
        {
            if (null == _bumpers)
                return;

            for (int i = 0; i < _bumpers.Count; ++i)
                _bumpers[i].TickSniperMark(deltaTime);

            float markInterval = StatProvider.GetEffectValue(ShopItemData.EEffectType.BumperSniperMark, ShopItemData.SUB_VALUE_INDEX);
            if (markInterval <= 0f)
                return;

            _sniperMarkElapsed += deltaTime;
            if (_sniperMarkElapsed < markInterval)
                return;

            _sniperMarkElapsed = 0f;
            MarkRandomBumper(StatProvider.GetEffectValue(ShopItemData.EEffectType.BumperSniperMark, ShopItemData.EXTRA_VALUE_INDEX));
        }

        void MarkRandomBumper(float markDuration)
        {
            _markCandidates.Clear();
            for (int i = 0; i < _bumpers.Count; ++i)
                if (_bumpers[i].CanApplySniperMark())
                    _markCandidates.Add(_bumpers[i]);

            BumperInteractable target = RandomDraw.TakeOne(_markCandidates);
            if (null == target)
                return;

            target.ApplySniperMark(markDuration);
        }

        public void ApplyBlastDamage(BumperInteractable source, float baseDamage, float blastRadius, float damageRatio)
        {
            if (null == _bumpers)
                return;

            if (blastRadius <= 0f || damageRatio <= 0f)
                return;

            Vector2 originPoint = source.transform.position;
            _fxPlayer?.Invoke(BLAST_FX, originPoint);

            float sqrRadius = blastRadius * blastRadius;
            float blastDamage = baseDamage * damageRatio;
            for (int i = 0; i < _bumpers.Count; ++i)
            {
                BumperInteractable bumper = _bumpers[i];
                if (bumper == source || bumper.IsBroken || bumper.IsDeactivated)
                    continue;

                if (((Vector2)bumper.transform.position - originPoint).sqrMagnitude > sqrRadius)
                    continue;

                bumper.ApplyDamage(blastDamage, PoolableFX.EFXType.NonePooling);
            }
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
                AudioManager.Play(AudioStorage.EAudioClipType.EffectChainLightning);
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

        public void SetSpringCharging(bool isCharging)
        {
            _spring.SetCharging(isCharging);
        }

        public bool TryLaunch(out float launchPowerRatio)
        {
            return _spring.TryLaunch(out launchPowerRatio);
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
