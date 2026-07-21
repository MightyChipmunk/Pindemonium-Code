using UnityEngine;

namespace Pinball.Ingame.Map
{
    [CreateAssetMenu(fileName = "MapCatalogData", menuName = "Pinball/Ingame/MapCatalogData")]
    public class MapCatalogData : ScriptableObject
    {
        [SerializeField] MapRoot[] _mapPrefabs = null;

        public int Count => null == _mapPrefabs ? 0 : _mapPrefabs.Length;

        public MapRoot GetPrefab(int index)
        {
            if (index < 0 || index >= Count)
                return null;

            return _mapPrefabs[index];
        }
    }
}
