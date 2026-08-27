using Pinball.Ingame.Map;
using Pinball.Ingame.Shop;
using Pinball.Ingame.Stat;
using UnityEngine;

namespace Pinball.Ingame.Score
{
    public class SlingshotAlternateTracker
    {
        const float DEFAULT_MULTIPLIER = 1f;
        const float MAX_MULTIPLIER = 1000000000f;

        int _alternateCount = 0;
        bool _hasLastSide = false;
        SlingshotInteractable.ESide _lastSide = SlingshotInteractable.ESide.Left;

        public float ConsumeHit(SlingshotInteractable.ESide side)
        {
            if (!StatProvider.HasEffect(ShopItemData.EEffectType.SlingshotAlternateScore))
                return DEFAULT_MULTIPLIER;

            if (_hasLastSide)
                _alternateCount = side != _lastSide ? _alternateCount + 1 : 0;

            _hasLastSide = true;
            _lastSide = side;

            float baseMultiplier = Mathf.Max(DEFAULT_MULTIPLIER, StatProvider.GetEffectValue(ShopItemData.EEffectType.SlingshotAlternateScore, ShopItemData.MAIN_VALUE_INDEX));
            return Mathf.Min(Mathf.Pow(baseMultiplier, _alternateCount), MAX_MULTIPLIER);
        }
    }
}
