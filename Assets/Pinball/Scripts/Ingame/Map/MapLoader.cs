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
    }
}
