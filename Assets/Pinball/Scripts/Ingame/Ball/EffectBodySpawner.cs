using UnityEngine;
using GameCore.Util;

namespace Pinball.Ingame
{
    public class EffectBodySpawner : MonoBehaviour
    {
        const int ORBITAL_POOL_CAPACITY = 2;
        const int MISSILE_POOL_CAPACITY = 8;
        const int RIFT_POOL_CAPACITY = 1;

        [SerializeField] OrbitalBody _orbitalPrefab = null;
        [SerializeField] MissileBody _missilePrefab = null;
        [SerializeField] RiftBody _riftPrefab = null;
        [SerializeField] VoidHoleMarker[] _holeMarkers = null;

        ObjectPool<OrbitalBody> _orbitalPool = null;
        ObjectPool<MissileBody> _missilePool = null;
        ObjectPool<RiftBody> _riftPool = null;

        public void Init()
        {
            _orbitalPool = new ObjectPool<OrbitalBody>(_orbitalPrefab, CreateBody, ORBITAL_POOL_CAPACITY);
            _missilePool = new ObjectPool<MissileBody>(_missilePrefab, CreateBody, MISSILE_POOL_CAPACITY);
            _riftPool = new ObjectPool<RiftBody>(_riftPrefab, CreateBody, RIFT_POOL_CAPACITY);
            InitHoleMarkers();
        }

        void InitHoleMarkers()
        {
            if (null == _holeMarkers)
                return;

            for (int i = 0; i < _holeMarkers.Length; ++i)
                if (null != _holeMarkers[i])
                    _holeMarkers[i].Init();
        }

        void ReleaseHoleMarkers()
        {
            if (null == _holeMarkers)
                return;

            for (int i = 0; i < _holeMarkers.Length; ++i)
                if (null != _holeMarkers[i])
                    _holeMarkers[i].Release();
        }

        public void Release()
        {
            ReleaseHoleMarkers();

            if (null != _riftPool)
            {
                _riftPool.Clear();
                _riftPool = null;
            }

            if (null != _missilePool)
            {
                _missilePool.Clear();
                _missilePool = null;
            }

            if (null != _orbitalPool)
            {
                _orbitalPool.Clear();
                _orbitalPool = null;
            }
        }

        T CreateBody<T>(T origin) where T : EffectBodyBase
        {
            return Instantiate(origin, transform);
        }

        public OrbitalBody SpawnOrbital()
        {
            return null == _orbitalPool ? null : _orbitalPool.Get();
        }

        public MissileBody SpawnMissile()
        {
            return null == _missilePool ? null : _missilePool.Get();
        }

        public RiftBody SpawnRift()
        {
            return null == _riftPool ? null : _riftPool.Get();
        }

        public void Despawn(EffectBodyBase body)
        {
            if (null == body)
                return;

            body.OwnPool.Return(body);
        }

        public void ShowHoleMarker(int index, Vector2 position)
        {
            if (null == _holeMarkers || index < 0 || index >= _holeMarkers.Length)
                return;

            VoidHoleMarker marker = _holeMarkers[index];
            if (null == marker)
                return;

            marker.Show(position);
        }

        public void HideHoleMarkers()
        {
            if (null == _holeMarkers)
                return;

            for (int i = 0; i < _holeMarkers.Length; ++i)
            {
                VoidHoleMarker marker = _holeMarkers[i];
                if (null == marker)
                    continue;

                marker.Hide();
            }
        }
    }
}
