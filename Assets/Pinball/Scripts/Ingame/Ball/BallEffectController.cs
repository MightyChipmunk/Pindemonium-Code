using System;
using System.Collections.Generic;
using Pinball.Ingame.Shop;
using Pinball.Ingame.Stat;
using UnityEngine;

namespace Pinball.Ingame
{
    public class BallEffectController
    {
        public delegate bool BallPositionGetter(out Vector2 position);

        public record InitParam(
            BallPositionGetter ActiveBallPositionGetter,
            Func<bool> IsPlayingGetter,
            Func<List<Vector2>, int> BumperPositionCollector,
            Func<float, float> EffectDamageEvaluator,
            Func<Collider2D, float, bool> ColliderHitApplier,
            Func<Collider2D, float, bool> BumperHitApplier,
            Action<Vector2, Vector2, float, float> BoxHitApplier);

        const int ORBITAL_COUNT = 2;
        const float ORBITAL_PHASE_STEP_DEGREE = 180f;
        const float MIN_MISSILE_INTERVAL = 0.5f;
        const float MISSILE_SPAWN_OFFSET = 0.45f;
        const float MISSILE_LIFETIME = 3.5f;
        const int HOLE_CAPACITY = 2;
        const float MIN_HOLE_DISTANCE = 0.35f;
        const float RIFT_DURATION_SECONDS = 0.4f;
        const float TARGET_MATCH_SQR_DISTANCE = 0.04f;
        const int NO_TARGET_COUNT = 0;

        static readonly Vector2[] MISSILE_LAUNCH_DIRECTIONS = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        BallPositionGetter _activeBallPositionGetter = null;
        Func<bool> _isPlayingGetter = null;
        Func<List<Vector2>, int> _bumperPositionCollector = null;
        Func<float, float> _effectDamageEvaluator = null;
        Func<Collider2D, float, bool> _colliderHitApplier = null;
        Func<Collider2D, float, bool> _bumperHitApplier = null;
        Action<Vector2, Vector2, float, float> _boxHitApplier = null;
        EffectBodySpawner _spawner = null;
        List<OrbitalBody> _orbitals = null;
        List<MissileBody> _missiles = null;
        List<float> _missileLifeRemains = null;
        List<Vector2> _bumperPositionBuffer = null;
        Vector2[] _holePositions = null;
        RiftBody _rift = null;
        float _riftRemaining = 0f;
        bool _isRiftHiding = false;
        float _missileElapsed = 0f;
        int _holeCount = 0;

        public BallEffectController(InitParam param, EffectBodySpawner spawner)
        {
            _activeBallPositionGetter = param.ActiveBallPositionGetter;
            _isPlayingGetter = param.IsPlayingGetter;
            _bumperPositionCollector = param.BumperPositionCollector;
            _effectDamageEvaluator = param.EffectDamageEvaluator;
            _colliderHitApplier = param.ColliderHitApplier;
            _bumperHitApplier = param.BumperHitApplier;
            _boxHitApplier = param.BoxHitApplier;
            _spawner = spawner;
            _orbitals = new List<OrbitalBody>();
            _missiles = new List<MissileBody>();
            _missileLifeRemains = new List<float>();
            _bumperPositionBuffer = new List<Vector2>();
            _holePositions = new Vector2[HOLE_CAPACITY];
        }

        public void Tick(float deltaTime)
        {
            TickOrbitals(deltaTime);
            TickMissiles(deltaTime);
            TickRift(deltaTime);
        }

        public void Reset()
        {
            DespawnOrbitals();
            DespawnAllMissiles();
            DespawnRift();
            ClearHoles();
            _missileElapsed = 0f;
        }

        public void NotifySurfaceHit(Vector2 point)
        {
            if (!IsPlaying())
                return;

            if (!StatProvider.HasEffect(ShopItemData.EEffectType.BallVoidRift))
                return;

            if (null != _rift)
                return;

            if (_holeCount > 0 && (point - _holePositions[_holeCount - 1]).sqrMagnitude < MIN_HOLE_DISTANCE * MIN_HOLE_DISTANCE)
                return;

            _holePositions[_holeCount] = point;
            _spawner.ShowHoleMarker(_holeCount, point);
            ++_holeCount;

            if (_holeCount < HOLE_CAPACITY)
                return;

            OpenRift();
        }

        void OpenRift()
        {
            _rift = _spawner.SpawnRift();
            if (null == _rift)
            {
                ClearHoles();
                return;
            }

            if (!_rift.TryOpen(_holePositions[0], _holePositions[1]))
            {
                DespawnRift();
                ClearHoles();
                return;
            }

            _riftRemaining = RIFT_DURATION_SECONDS;
            _isRiftHiding = false;
            _boxHitApplier?.Invoke(
                _rift.transform.position,
                new Vector2(_rift.Length, RiftBody.RIFT_WIDTH),
                _rift.AngleDegree,
                EvaluateDamage(ShopItemData.EEffectType.BallVoidRift));
        }

        void TickOrbitals(float deltaTime)
        {
            if (!IsPlaying() || !StatProvider.HasEffect(ShopItemData.EEffectType.BallOrbital) || !TryGetBallPosition(out Vector2 ballPosition))
            {
                DespawnOrbitals();
                return;
            }

            if (_orbitals.Count == 0)
                SpawnOrbitals(ballPosition);

            for (int i = 0; i < _orbitals.Count; ++i)
                _orbitals[i].Tick(ballPosition, deltaTime);
        }

        void SpawnOrbitals(Vector2 ballPosition)
        {
            for (int i = 0; i < ORBITAL_COUNT; ++i)
            {
                OrbitalBody orbital = _spawner.SpawnOrbital();
                if (null == orbital)
                    continue;

                orbital.Setup(i * ORBITAL_PHASE_STEP_DEGREE, ballPosition);
                orbital.OnContact += HandleOrbitalContact;
                _orbitals.Add(orbital);
            }
        }

        void HandleOrbitalContact(Collider2D hitCollider)
        {
            _colliderHitApplier?.Invoke(hitCollider, EvaluateDamage(ShopItemData.EEffectType.BallOrbital));
        }

        void DespawnOrbitals()
        {
            for (int i = 0; i < _orbitals.Count; ++i)
            {
                _orbitals[i].OnContact -= HandleOrbitalContact;
                _spawner.Despawn(_orbitals[i]);
            }

            _orbitals.Clear();
        }

        void TickMissiles(float deltaTime)
        {
            int aliveTargetCount = _missiles.Count > 0 ? _bumperPositionCollector(_bumperPositionBuffer) : NO_TARGET_COUNT;

            for (int i = _missiles.Count - 1; i >= 0; --i)
            {
                MissileBody missile = _missiles[i];
                RetargetMissile(missile, aliveTargetCount);
                missile.Tick(deltaTime);
                _missileLifeRemains[i] -= deltaTime;

                if (!missile.IsExpired && _missileLifeRemains[i] > 0f)
                    continue;

                missile.OnContact -= HandleMissileContact;
                _spawner.Despawn(missile);
                _missiles.RemoveAt(i);
                _missileLifeRemains.RemoveAt(i);
            }

            if (!IsPlaying() || !StatProvider.HasEffect(ShopItemData.EEffectType.BallHomingMissile))
            {
                _missileElapsed = 0f;
                return;
            }

            if (!TryGetBallPosition(out Vector2 ballPosition))
                return;

            _missileElapsed += deltaTime;
            float interval = Mathf.Max(MIN_MISSILE_INTERVAL, StatProvider.GetEffectValue(ShopItemData.EEffectType.BallHomingMissile, ShopItemData.SUB_VALUE_INDEX));
            if (_missileElapsed < interval)
                return;

            _missileElapsed = 0f;
            FireMissiles(ballPosition);
        }

        void FireMissiles(Vector2 ballPosition)
        {
            int targetCount = _bumperPositionCollector(_bumperPositionBuffer);
            if (targetCount <= 0)
                return;

            for (int i = 0; i < MISSILE_LAUNCH_DIRECTIONS.Length; ++i)
            {
                MissileBody missile = _spawner.SpawnMissile();
                if (null == missile)
                    continue;

                Vector2 launchDirection = MISSILE_LAUNCH_DIRECTIONS[i];
                Vector2 targetPosition = _bumperPositionBuffer[UnityEngine.Random.Range(0, targetCount)];
                missile.Launch(ballPosition + launchDirection * MISSILE_SPAWN_OFFSET, launchDirection, targetPosition);
                missile.OnContact += HandleMissileContact;
                _missiles.Add(missile);
                _missileLifeRemains.Add(MISSILE_LIFETIME);
            }
        }

        void RetargetMissile(MissileBody missile, int aliveTargetCount)
        {
            if (aliveTargetCount <= NO_TARGET_COUNT || missile.IsExpired)
                return;

            if (HasAliveTarget(missile.GetTargetPosition(), aliveTargetCount))
                return;

            missile.SetTarget(FindNearestTarget(missile.transform.position, aliveTargetCount));
        }

        bool HasAliveTarget(Vector2 targetPosition, int aliveTargetCount)
        {
            for (int i = 0; i < aliveTargetCount; ++i)
                if ((_bumperPositionBuffer[i] - targetPosition).sqrMagnitude <= TARGET_MATCH_SQR_DISTANCE)
                    return true;

            return false;
        }

        Vector2 FindNearestTarget(Vector2 origin, int aliveTargetCount)
        {
            Vector2 nearest = _bumperPositionBuffer[0];
            float nearestSqrDistance = (nearest - origin).sqrMagnitude;

            for (int i = 1; i < aliveTargetCount; ++i)
            {
                float sqrDistance = (_bumperPositionBuffer[i] - origin).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance)
                    continue;

                nearest = _bumperPositionBuffer[i];
                nearestSqrDistance = sqrDistance;
            }

            return nearest;
        }

        void HandleMissileContact(MissileBody missile, Collider2D hitCollider)
        {
            if (null == _bumperHitApplier)
                return;

            if (!_bumperHitApplier(hitCollider, EvaluateDamage(ShopItemData.EEffectType.BallHomingMissile)))
                return;

            missile.Expire();
        }

        void DespawnAllMissiles()
        {
            for (int i = 0; i < _missiles.Count; ++i)
            {
                _missiles[i].OnContact -= HandleMissileContact;
                _spawner.Despawn(_missiles[i]);
            }

            _missiles.Clear();
            _missileLifeRemains.Clear();
        }

        void TickRift(float deltaTime)
        {
            if (null == _rift)
                return;

            _riftRemaining -= deltaTime;

            if (!_isRiftHiding && _riftRemaining <= RiftBody.HIDE_DURATION)
            {
                _isRiftHiding = true;
                _rift.PlayHide();
                _spawner.HideHoleMarkers();
            }

            if (_riftRemaining > 0f)
                return;

            DespawnRift();
            ClearHoles();
        }

        void DespawnRift()
        {
            if (null == _rift)
                return;

            _spawner.Despawn(_rift);
            _rift = null;
            _riftRemaining = 0f;
            _isRiftHiding = false;
        }

        void ClearHoles()
        {
            _holeCount = 0;
            _spawner.HideHoleMarkers();
        }

        float EvaluateDamage(ShopItemData.EEffectType effectType)
        {
            if (null == _effectDamageEvaluator)
                return 0f;

            return _effectDamageEvaluator(StatProvider.GetEffectValue(effectType, ShopItemData.MAIN_VALUE_INDEX));
        }

        bool IsPlaying()
        {
            if (null == _isPlayingGetter)
                return false;

            return _isPlayingGetter();
        }

        bool TryGetBallPosition(out Vector2 position)
        {
            position = default;
            if (null == _activeBallPositionGetter)
                return false;

            return _activeBallPositionGetter(out position);
        }
    }
}
