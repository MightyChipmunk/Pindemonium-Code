using Pinball.Ingame.Shop;
using Pinball.Ingame.Stat;

namespace Pinball.Ingame.Score
{
    public class SlingshotScoreGrowth
    {
        int _hitCount = 0;

        public int HitCount => _hitCount;

        public SlingshotScoreGrowth(int hitCount)
        {
            _hitCount = hitCount;
        }

        public void CountHit()
        {
            if (!StatProvider.HasEffect(ShopItemData.EEffectType.SlingshotScoreGrowth))
                return;

            ++_hitCount;
        }

        public float GetScoreRatio()
        {
            return _hitCount * StatProvider.GetSlingshotScoreGrowth();
        }
    }
}
