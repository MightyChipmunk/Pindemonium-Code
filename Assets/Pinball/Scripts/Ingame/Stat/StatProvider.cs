using Pinball.Ingame.Map;

namespace Pinball.Ingame.Stat
{
    public class StatProvider
    {
        PinballStatData _statData = null;
        PlayerLoadout _loadout = null;

        public void Init(PinballStatData statData, PlayerLoadout loadout)
        {
            _statData = statData;
            _loadout = loadout;
        }

        public void Release()
        {
            _statData = null;
            _loadout = null;
        }

        public float GetFlipPower()
        {
            if (null == _statData)
                return 0f;

            return _statData.FlipPower + GetEffectSum(ShopItemData.EEffectType.FlipPowerAdd);
        }

        public float GetBallAttackPower()
        {
            if (null == _statData)
                return 0f;

            return _statData.BallAttackPower + GetEffectSum(ShopItemData.EEffectType.BallAttackPowerAdd);
        }

        public float GetPerfectTimingRelax()
        {
            return GetEffectSum(ShopItemData.EEffectType.PerfectTimingRelax);
        }

        public float GetComboMultiplierBonus()
        {
            return GetEffectSum(ShopItemData.EEffectType.ComboMultiplierAdd);
        }

        public float GetComboTimeoutBonus()
        {
            return GetEffectSum(ShopItemData.EEffectType.ComboTimeoutAdd);
        }

        public float GetGimmickScoreRatio(MapInteractable.EGimmickType gimmickType)
        {
            if (null == _loadout)
                return 0f;

            return _loadout.GetGimmickScoreRatio(gimmickType);
        }

        public float GetEffectSum(ShopItemData.EEffectType effectType)
        {
            if (null == _loadout)
                return 0f;

            return _loadout.GetEffectSum(effectType);
        }

        public float GetEffectSub(ShopItemData.EEffectType effectType)
        {
            if (null == _loadout)
                return 0f;

            return _loadout.GetEffectSub(effectType);
        }

        public bool HasEffect(ShopItemData.EEffectType effectType)
        {
            if (null == _loadout)
                return false;

            return _loadout.HasEffect(effectType);
        }
    }
}
