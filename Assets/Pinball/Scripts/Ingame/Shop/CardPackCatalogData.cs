using UnityEngine;

namespace Pinball.Ingame.Shop
{
    [CreateAssetMenu(fileName = "CardPackCatalogData", menuName = "Pinball/Ingame/CardPackCatalogData")]
    public class CardPackCatalogData : ScriptableObject
    {
        [SerializeField] CardPackData[] _packs = null;

        public int Count => null == _packs ? 0 : _packs.Length;

        public CardPackData GetPack(int index)
        {
            if (index < 0 || index >= Count)
                return null;

            return _packs[index];
        }
    }
}
