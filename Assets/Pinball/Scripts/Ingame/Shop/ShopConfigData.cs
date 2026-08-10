using UnityEngine;

namespace Pinball.Ingame
{
    [CreateAssetMenu(fileName = "ShopConfigData", menuName = "Pinball/Ingame/ShopConfigData")]
    public class ShopConfigData : ScriptableObject
    {
        [SerializeField] int _passiveSlotCount = 3;
        [SerializeField] int _rerollBasePrice = 1;
        [SerializeField] int _rerollPriceIncrement = 1;
        [SerializeField] int _interestPerAmount = 5;
        [SerializeField] int _interestMax = 5;

        public int PassiveSlotCount => _passiveSlotCount;
        public int RerollBasePrice => _rerollBasePrice;
        public int RerollPriceIncrement => _rerollPriceIncrement;
        public int InterestPerAmount => _interestPerAmount;
        public int InterestMax => _interestMax;
    }
}
