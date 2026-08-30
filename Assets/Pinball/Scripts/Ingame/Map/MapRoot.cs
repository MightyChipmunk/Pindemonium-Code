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
        const PoolableFX.EFXType SWEEP_FX = PoolableFX.EFXType.Blast;

        public record InitParam(
            Action<MapInteractable.EGimmickType, int, Vector2, MapInteractable.EScoreSource> OnScoreGained,
            Func<Collider2D, float, float> DamageEvaluator,
            Func<Collider2D, float> AttackPowerEvaluator,
            Action<Flipper.EHitGrade, Vector2> OnBallStruck,
            Action<BumperInteractable, float, Collider2D> OnBumperDamaged,
            Action OnBumperBroken,
            Action<Collider2D> OnSlingshotBallHit,
            Func<SlingshotInteractable.ESide, float> SlingshotHitScoreMultiplierConsumer,
            Func<int> SlingshotGrowthHitCountGetter,
            Action<Spring.EChargeEvent, Vector2> OnSpringChargeEvent,
            Action<PoolableFX.EFXType, Vector2> FXPlayer,
            Action<PoolableFX.EFXType, Vector2, Vector2> LineFXPlayer,
            Action<Collider2D> OnGimmickBallHit,
            Action GimmickHitPresenter,
            Func<MapInteractable.EGimmickType, bool> IsGimmickAbsentGetter,
            Func<MapInteractable.EGimmickType, int> ScoreGainLimitGetter,
            float BumperHealthMultiplier);

        [SerializeField] MapInteractable[] _interactables = null;
        [SerializeField] MapSlot[] _slots = null;
        [SerializeField] MapSlotReplacementData _slotReplacementData = null;
        [SerializeField] Flipper _leftFlipper = null;
        [SerializeField] Flipper _rightFlipper = null;
        [SerializeField] Spring _spring = null;
        [SerializeField] Renderer[] _leftFlipperRenderers = null;
        [SerializeField] Renderer[] _rightFlipperRenderers = null;
        [SerializeField] Renderer[] _springRenderers = null;
        [SerializeField] BallHoldZone _flipperApproachZone = null;

        Action<MapInteractable.EGimmickType, int, Vector2, MapInteractable.EScoreSource> _onScoreGained = null;
        Action<Flipper.EHitGrade, Vector2> _onBallStruck = null;
        Action<BumperInteractable, float, Collider2D> _onBumperDamaged = null;
        Action _onBumperBroken = null;
        Action<Collider2D> _onSlingshotBallHit = null;
        Func<int> _slingshotGrowthHitCountGetter = null;
        Action<PoolableFX.EFXType, Vector2> _fxPlayer = null;
        Action<PoolableFX.EFXType, Vector2, Vector2> _lineFXPlayer = null;
        List<MapInteractable> _activeInteractables = null;
        ObjectRegistry<int, IDamageable> _damageableRegistry = null;
        List<Collider2D> _damageColliderBuffer = null;
        List<Collider2D> _hitBuffer = null;
        ContactFilter2D _hitFilter = default;
        List<BumperInteractable> _bumpers = null;
        List<SlingshotInteractable> _slingshots = null;
        List<LampBankInteractable> _lampBanks = null;
        List<LaneInteractable> _lanes = null;
        List<BumperInteractable> _markCandidates = null;
        CancellationTokenSource _cancellationTokenSource = null;
        SlingshotInteractable.ESide _lastRallySide = SlingshotInteractable.ESide.Left;
        float _sniperMarkElapsed = 0f;
        bool _isSweepArmed = true;
        bool _hasRallySide = false;

        public void Init(InitParam param)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _bumpers = new List<BumperInteractable>();
            _slingshots = new List<SlingshotInteractable>();
            _lampBanks = new List<LampBankInteractable>();
            _lanes = new List<LaneInteractable>();
            _markCandidates = new List<BumperInteractable>();
            _sniperMarkElapsed = 0f;
            _isSweepArmed = true;
            _onScoreGained = param.OnScoreGained;
            _onBumperDamaged = param.OnBumperDamaged;
            _onBumperBroken = param.OnBumperBroken;
            _hasRallySide = false;
            _onSlingshotBallHit = param.OnSlingshotBallHit;
            _slingshotGrowthHitCountGetter = param.SlingshotGrowthHitCountGetter;
            _fxPlayer = param.FXPlayer;
            _lineFXPlayer = param.LineFXPlayer;

            _damageableRegistry = new ObjectRegistry<int, IDamageable>();
            _damageColliderBuffer = new List<Collider2D>();
            _hitBuffer = new List<Collider2D>();
            _hitFilter = new ContactFilter2D();
            _hitFilter.useTriggers = true;

            BuildActiveInteractables();
            for (int i = 0; i < _activeInteractables.Count; ++i)
            {
                MapInteractable interactable = _activeInteractables[i];
                int scoreGainLimit = interactable.IsDeactivatable ? param.ScoreGainLimitGetter(interactable.GimmickType) : MapInteractable.NO_SCORE_GAIN_LIMIT;
                BumperInteractable bumper = interactable as BumperInteractable;

                if (null != bumper)
                    bumper.SetHealthMultiplier(param.BumperHealthMultiplier);

                SlingshotInteractable slingshot = interactable as SlingshotInteractable;
                if (null != slingshot)
                    slingshot.SetHitScoreMultiplierConsumer(param.SlingshotHitScoreMultiplierConsumer);

                interactable.Init(new MapInteractable.InitParam(
                    OnScoreGained: param.OnScoreGained,
                    DamageEvaluator: param.DamageEvaluator,
                    AttackPowerEvaluator: param.AttackPowerEvaluator,
                    FXPlayer: param.FXPlayer,
                    OnBallHit: param.OnGimmickBallHit,
                    GimmickHitPresenter: param.GimmickHitPresenter,
                    ScoreGainLimit: scoreGainLimit));

                if (interactable.IsDeactivatable && param.IsGimmickAbsentGetter(interactable.GimmickType))
                    interactable.Deactivate();

                RegisterDamageable(interactable);

                if (null != bumper)
                {
                    _bumpers.Add(bumper);
                    bumper.OnBallDamaged += HandleBumperDamaged;
                    bumper.OnBroken += HandleBumperBroken;
                }

                if (null != slingshot)
                {
                    _slingshots.Add(slingshot);
                    slingshot.OnBallBounced += HandleSlingshotBallBounced;
                }

                LampBankInteractable lampBank = interactable as LampBankInteractable;
                if (null != lampBank)
                    _lampBanks.Add(lampBank);

                LaneInteractable lane = interactable as LaneInteractable;
                if (null != lane)
                    _lanes.Add(lane);
            }


            RefreshSlingshotGrowthTier();
            RefreshRallyIndicators();

            _onBallStruck = param.OnBallStruck;
            _leftFlipper.Init();
            _rightFlipper.Init();
            _leftFlipper.OnBallStruck += OnFlipperBallStruck;
            _rightFlipper.OnBallStruck += OnFlipperBallStruck;
            _spring.Init(param.OnSpringChargeEvent);

            if (null != _flipperApproachZone)
                _flipperApproachZone.SetHoldPredicate(CanFlipperStrikePoint);
        }

        void RegisterDamageable(MapInteractable interactable)
        {
            IDamageable damageable = interactable as IDamageable;
            if (null == damageable)
                return;

            _damageColliderBuffer.Clear();
            interactable.CollectDamageColliders(_damageColliderBuffer);
            for (int i = 0; i < _damageColliderBuffer.Count; ++i)
                _damageableRegistry.Register(_damageColliderBuffer[i].GetInstanceID(), damageable);
        }

        public bool ApplyColliderHit(Collider2D hitCollider, float damage)
        {
            if (!TryFindDamageable(hitCollider, out IDamageable damageable))
                return false;

            damageable.TakeHit(damage, hitCollider.bounds.center);
            return true;
        }

        public bool ApplyBumperHit(Collider2D hitCollider, float damage)
        {
            if (!TryFindDamageable(hitCollider, out IDamageable damageable))
                return false;

            BumperInteractable bumper = damageable as BumperInteractable;
            if (null == bumper)
                return false;

            damageable.TakeHit(damage, hitCollider.bounds.center);
            return true;
        }

        bool TryFindDamageable(Collider2D hitCollider, out IDamageable damageable)
        {
            damageable = null;
            if (null == _damageableRegistry || null == hitCollider)
                return false;

            return _damageableRegistry.Find(hitCollider.GetInstanceID(), out damageable);
        }

        public void ApplyBoxHit(Vector2 center, Vector2 size, float angleDegree, float damage)
        {
            if (null == _hitBuffer)
                return;

            ApplyHits(Physics2D.OverlapBox(center, size, angleDegree, _hitFilter, _hitBuffer), damage);
        }

        void ApplyHits(int hitCount, float damage)
        {
            for (int i = 0; i < hitCount; ++i)
            {
                Collider2D hitCollider = _hitBuffer[i];
                if (!_damageableRegistry.Find(hitCollider.GetInstanceID(), out IDamageable damageable))
                    continue;

                damageable.TakeHit(damage, hitCollider.bounds.center);
            }
        }

        void BuildActiveInteractables()
        {
            _activeInteractables = new List<MapInteractable>();
            if (null != _interactables)
                _activeInteractables.AddRange(_interactables);

            if (null == _slots)
                return;

            for (int i = 0; i < _slots.Length; ++i)
            {
                MapSlot slot = _slots[i];
                MapInteractable defaultInteractable = slot.GetDefaultInteractable();
                MapInteractable resolved = slot.Resolve(_slotReplacementData);
                if (resolved == defaultInteractable)
                    continue;

                int index = _activeInteractables.IndexOf(defaultInteractable);
                if (index < 0)
                    continue;

                _activeInteractables[index] = resolved;
            }
        }

        public void Suspend()
        {
            CancelChainDamage();
            _spring.SetCharging(false);
            ReleaseInteractables();
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
            _onScoreGained = null;

            if (null != _flipperApproachZone)
            {
                _flipperApproachZone.Disarm();
                _flipperApproachZone.SetHoldPredicate(null);
            }

            _onBumperDamaged = null;
            _onBumperBroken = null;
            _onSlingshotBallHit = null;
            _slingshotGrowthHitCountGetter = null;

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

            if (null != _slingshots)
            {
                for (int i = 0; i < _slingshots.Count; ++i)
                    _slingshots[i].OnBallBounced -= HandleSlingshotBallBounced;

                _slingshots.Clear();
                _slingshots = null;
            }

            if (null != _lampBanks)
            {
                _lampBanks.Clear();
                _lampBanks = null;
            }

            if (null != _lanes)
            {
                _lanes.Clear();
                _lanes = null;
            }

            if (null != _markCandidates)
            {
                _markCandidates.Clear();
                _markCandidates = null;
            }

            if (null != _damageableRegistry)
            {
                _damageableRegistry.Clear();
                _damageableRegistry = null;
            }

            if (null != _damageColliderBuffer)
            {
                _damageColliderBuffer.Clear();
                _damageColliderBuffer = null;
            }

            if (null != _hitBuffer)
            {
                _hitBuffer.Clear();
                _hitBuffer = null;
            }

            ReleaseInteractables();
            ClearSlots();
            _activeInteractables = null;
        }

        void ReleaseInteractables()
        {
            if (null != _activeInteractables)
            {
                for (int i = 0; i < _activeInteractables.Count; ++i)
                    _activeInteractables[i].Release();

                return;
            }

            if (null == _interactables)
                return;

            for (int i = 0; i < _interactables.Length; ++i)
                _interactables[i].Release();
        }

        void ClearSlots()
        {
            if (null == _slots)
                return;

            for (int i = 0; i < _slots.Length; ++i)
                _slots[i].Clear();
        }

        void CancelChainDamage()
        {
            if (null == _cancellationTokenSource)
                return;

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        public void Tick(float deltaTime)
        {
            TickLampWarmup(deltaTime);
            TickSniperMark(deltaTime);
            RefreshSweepArm();
        }

        void RefreshSweepArm()
        {
            if (_isSweepArmed)
                return;

            if (null == _bumpers)
                return;

            for (int i = 0; i < _bumpers.Count; ++i)
            {
                BumperInteractable bumper = _bumpers[i];
                if (bumper.IsDeactivated)
                    continue;

                if (bumper.IsBroken)
                    return;
            }

            _isSweepArmed = true;
        }

        void TickLampWarmup(float deltaTime)
        {
            if (null == _lampBanks)
                return;

            for (int i = 0; i < _lampBanks.Count; ++i)
                _lampBanks[i].TickWarmup(deltaTime);
        }

        void TickSniperMark(float deltaTime)
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
            AudioManager.Play(AudioStorage.EAudioClipType.EffectBlast);

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

        void HandleBumperDamaged(BumperInteractable bumper, float damage, Collider2D ballCollider)
        {
            _onBumperDamaged?.Invoke(bumper, damage, ballCollider);
        }

        void HandleBumperBroken()
        {
            _onBumperBroken?.Invoke();
            TryGainSweepBonus();
        }

        void TryGainSweepBonus()
        {
            if (!_isSweepArmed)
                return;

            float bonusScale = StatProvider.GetBumperSweepBonusScale();
            if (bonusScale <= 0f)
                return;

            if (null == _bumpers)
                return;

            int breakScoreSum = 0;
            int sweptCount = 0;
            Vector2 centerPoint = Vector2.zero;
            for (int i = 0; i < _bumpers.Count; ++i)
            {
                BumperInteractable bumper = _bumpers[i];
                if (bumper.IsDeactivated)
                    continue;

                if (!bumper.IsBroken)
                    return;

                breakScoreSum += bumper.GetBreakScore();
                centerPoint += (Vector2)bumper.transform.position;
                ++sweptCount;
            }

            if (sweptCount <= 0)
                return;

            int bonusScore = Mathf.RoundToInt(breakScoreSum * bonusScale);
            if (bonusScore <= 0)
                return;

            _isSweepArmed = false;
            centerPoint /= sweptCount;
            _fxPlayer?.Invoke(SWEEP_FX, centerPoint);
            AudioManager.Play(AudioStorage.EAudioClipType.GimmickBankCleared);
            _onScoreGained?.Invoke(MapInteractable.EGimmickType.Bumper, bonusScore, centerPoint, MapInteractable.EScoreSource.GimmickBreak);
            RespawnAllBumpers();
        }

        void RespawnAllBumpers()
        {
            for (int i = 0; i < _bumpers.Count; ++i)
            {
                BumperInteractable bumper = _bumpers[i];
                if (bumper.IsDeactivated)
                    continue;

                bumper.RespawnImmediately();
            }
        }

        public void ResetBumperGrades()
        {
            if (null == _bumpers)
                return;

            for (int i = 0; i < _bumpers.Count; ++i)
                _bumpers[i].ResetGrade();
        }

        void HandleSlingshotBallBounced(SlingshotInteractable.ESide side, Collider2D ballCollider)
        {
            _onSlingshotBallHit?.Invoke(ballCollider);
            _hasRallySide = true;
            _lastRallySide = side;
            RefreshRallyIndicators();
            RefreshSlingshotGrowthTier();
        }

        void RefreshSlingshotGrowthTier()
        {
            if (null == _slingshots || null == _slingshotGrowthHitCountGetter)
                return;

            int hitCount = _slingshotGrowthHitCountGetter();
            for (int i = 0; i < _slingshots.Count; ++i)
                _slingshots[i].SetGrowthHitCount(hitCount);
        }

        void RefreshRallyIndicators()
        {
            if (null == _slingshots)
                return;

            for (int i = 0; i < _slingshots.Count; ++i)
                _slingshots[i].SetRallyActive(!_hasRallySide || _slingshots[i].GetSide() != _lastRallySide);
        }

        public int CollectAliveBumperPositions(List<Vector2> buffer)
        {
            buffer.Clear();
            if (null == _bumpers)
                return 0;

            for (int i = 0; i < _bumpers.Count; ++i)
            {
                BumperInteractable bumper = _bumpers[i];
                if (bumper.IsBroken || bumper.IsDeactivated)
                    continue;

                buffer.Add(bumper.transform.position);
            }

            return buffer.Count;
        }

        public int GetInteractableCount()
        {
            if (null != _activeInteractables)
                return _activeInteractables.Count;

            return null == _interactables ? 0 : _interactables.Length;
        }

        public MapInteractable GetInteractable(int index)
        {
            if (null != _activeInteractables)
                return index < 0 || index >= _activeInteractables.Count ? null : _activeInteractables[index];

            if (null == _interactables || index < 0 || index >= _interactables.Length)
                return null;

            return _interactables[index];
        }

        public Vector2 GetSpringPosition()
        {
            return _spring.transform.position;
        }

        public bool TryGetLeftFlipperBounds(out Bounds worldBounds)
        {
            return TryUnionRendererBounds(_leftFlipperRenderers, out worldBounds);
        }

        public bool TryGetRightFlipperBounds(out Bounds worldBounds)
        {
            return TryUnionRendererBounds(_rightFlipperRenderers, out worldBounds);
        }

        public bool TryGetSpringBounds(out Bounds worldBounds)
        {
            return TryUnionRendererBounds(_springRenderers, out worldBounds);
        }

        public void CollectLeftFlipperSprites(List<MapInteractable.SpriteSnapshot> buffer)
        {
            CollectSprites(_leftFlipperRenderers, buffer);
        }

        public void CollectRightFlipperSprites(List<MapInteractable.SpriteSnapshot> buffer)
        {
            CollectSprites(_rightFlipperRenderers, buffer);
        }

        public void CollectSpringSprites(List<MapInteractable.SpriteSnapshot> buffer)
        {
            CollectSprites(_springRenderers, buffer);
        }

        static void CollectSprites(Renderer[] renderers, List<MapInteractable.SpriteSnapshot> buffer)
        {
            if (null == renderers)
                return;

            for (int i = 0; i < renderers.Length; ++i)
                if (MapInteractable.SpriteSnapshot.TryCreate(renderers[i], out MapInteractable.SpriteSnapshot snapshot))
                    buffer.Add(snapshot);
        }

        static bool TryUnionRendererBounds(Renderer[] renderers, out Bounds worldBounds)
        {
            worldBounds = default;
            if (null == renderers)
                return false;

            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; ++i)
            {
                Renderer boundsRenderer = renderers[i];
                if (null == boundsRenderer || !boundsRenderer.enabled || !boundsRenderer.gameObject.activeInHierarchy)
                    continue;

                if (hasBounds)
                {
                    worldBounds.Encapsulate(boundsRenderer.bounds);
                }
                else
                {
                    worldBounds = boundsRenderer.bounds;
                    hasBounds = true;
                }
            }

            return hasBounds;
        }

        public Vector2 GetLeftFlipperPosition()
        {
            return _leftFlipper.transform.position;
        }

        public Vector2 GetRightFlipperPosition()
        {
            return _rightFlipper.transform.position;
        }

        public void ArmFlipperApproachZone()
        {
            if (null == _flipperApproachZone)
                return;

            _flipperApproachZone.Arm();
        }

        public void DisarmFlipperApproachZone()
        {
            if (null == _flipperApproachZone)
                return;

            _flipperApproachZone.Disarm();
        }

        bool CanFlipperStrikePoint(Vector2 worldPoint)
        {
            return _leftFlipper.CanStrikePoint(worldPoint) || _rightFlipper.CanStrikePoint(worldPoint);
        }

        public bool IsHoldingBallOnFlipperApproach()
        {
            return null != _flipperApproachZone && _flipperApproachZone.IsHolding();
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

        public void UnloadBall(Ball ball)
        {
            _spring.UnloadBall(ball);
        }

        public void CancelBallTravel(int colliderInstanceId)
        {
            if (null == _lanes)
                return;

            for (int i = 0; i < _lanes.Count; ++i)
                _lanes[i].CancelBallTravel(colliderInstanceId);
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
            _slots = GetComponentsInChildren<MapSlot>(true);
            _spring = GetComponentInChildren<Spring>(true);
            _flipperApproachZone = GetComponentInChildren<BallHoldZone>(true);

            Flipper[] flippers = GetComponentsInChildren<Flipper>(true);
            for (int i = 0; i < flippers.Length; ++i)
            {
                if (flippers[i].Side == Flipper.ESide.Left)
                    _leftFlipper = flippers[i];
                else
                    _rightFlipper = flippers[i];
            }

            _leftFlipperRenderers = null == _leftFlipper ? null : _leftFlipper.GetComponentsInChildren<Renderer>(true);
            _rightFlipperRenderers = null == _rightFlipper ? null : _rightFlipper.GetComponentsInChildren<Renderer>(true);
            _springRenderers = null == _spring ? null : _spring.GetComponentsInChildren<Renderer>(true);

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
