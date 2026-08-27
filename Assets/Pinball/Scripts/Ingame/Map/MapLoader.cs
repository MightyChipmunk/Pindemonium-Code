using System.Collections.Generic;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class MapLoader : MonoBehaviour
    {
        [SerializeField] MapCatalogData _catalog = null;

        MapRoot _currentMap = null;

        public void Init()
        {
            _currentMap = null;
        }

        public void Release()
        {
            Unload();
        }

        public MapRoot Load(int index)
        {
            Unload();

            MapRoot prefab = _catalog.GetPrefab(index);
            if (null == prefab)
                return null;

            _currentMap = Instantiate(prefab, transform);
            return _currentMap;
        }

        public void Unload()
        {
            if (null == _currentMap)
                return;

            _currentMap.Release();
            Destroy(_currentMap.gameObject);
            _currentMap = null;
        }

        public int CollectAliveBumperPositions(List<Vector2> buffer)
        {
            if (null == _currentMap)
            {
                buffer.Clear();
                return 0;
            }

            return _currentMap.CollectAliveBumperPositions(buffer);
        }

        public bool ApplyColliderHit(Collider2D hitCollider, float damage)
        {
            if (null == _currentMap)
                return false;

            return _currentMap.ApplyColliderHit(hitCollider, damage);
        }

        public void ApplyBoxHit(Vector2 center, Vector2 size, float angleDegree, float damage)
        {
            if (null == _currentMap)
                return;

            _currentMap.ApplyBoxHit(center, size, angleDegree, damage);
        }
    }
}
