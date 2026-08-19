using UnityEngine;

namespace Pinball.Ingame
{
    [CreateAssetMenu(fileName = "FlipperCatalogData", menuName = "Pinball/Ingame/FlipperCatalogData")]
    public class FlipperCatalogData : ScriptableObject
    {
        public const int INVALID_INDEX = -1;
        public const int DEFAULT_INDEX = 0;

        [SerializeField] FlipperData[] _flippers = null;

        public int Count => null == _flippers ? 0 : _flippers.Length;

        public FlipperData GetFlipper(int index)
        {
            if (index < 0 || index >= Count)
                return null;

            return _flippers[index];
        }

        public int IndexOf(FlipperData flipper)
        {
            if (null == flipper)
                return INVALID_INDEX;

            for (int i = 0; i < Count; ++i)
                if (_flippers[i] == flipper)
                    return i;

            return INVALID_INDEX;
        }

        public int IndexOfId(string flipperId)
        {
            if (string.IsNullOrEmpty(flipperId))
                return INVALID_INDEX;

            for (int i = 0; i < Count; ++i)
                if (null != _flippers[i] && _flippers[i].GetId() == flipperId)
                    return i;

            return INVALID_INDEX;
        }

        public int IndexOfIdOrDefault(string flipperId)
        {
            int index = IndexOfId(flipperId);
            return index == INVALID_INDEX ? DEFAULT_INDEX : index;
        }
    }
}
