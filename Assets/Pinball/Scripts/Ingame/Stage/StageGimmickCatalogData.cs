using UnityEngine;

namespace Pinball.Ingame.Stage
{
    [CreateAssetMenu(fileName = "StageGimmickCatalogData", menuName = "Pinball/Ingame/StageGimmickCatalogData")]
    public class StageGimmickCatalogData : ScriptableObject
    {
        public const int INVALID_INDEX = -1;

        [SerializeField] StageGimmickData[] _pool = null;
        [SerializeField] int _rerollBasePrice = 2;
        [SerializeField] int _rerollPriceIncrement = 1;

        public int Count => null == _pool ? 0 : _pool.Length;
        public int RerollBasePrice => _rerollBasePrice;
        public int RerollPriceIncrement => _rerollPriceIncrement;

        public StageGimmickData GetGimmick(int index)
        {
            if (index < 0 || index >= Count)
                return null;

            return _pool[index];
        }

        public int IndexOf(StageGimmickData gimmick)
        {
            if (null == gimmick)
                return INVALID_INDEX;

            for (int i = 0; i < Count; ++i)
            {
                if (_pool[i] == gimmick)
                    return i;
            }

            return INVALID_INDEX;
        }
    }
}
