using System.Collections.Generic;
using Pinball.Ingame.Map;

namespace Pinball.Ingame
{
    public class PlayerLoadout
    {
        List<ShopItemData> _passives = null;
        Dictionary<ShopItemData.EEffectType, float> _upgradeSums = null;
        Dictionary<MapInteractable.EGimmickType, float> _gimmickScoreSums = null;
        int _passiveSlotCount = 0;

        public int PassiveSlotCount => _passiveSlotCount;

        public void Init(int passiveSlotCount)
        {
            _passiveSlotCount = passiveSlotCount;
            _passives = new List<ShopItemData>();
            _upgradeSums = new Dictionary<ShopItemData.EEffectType, float>();
            _gimmickScoreSums = new Dictionary<MapInteractable.EGimmickType, float>();
        }

        public void Release()
        {
            if (null != _passives)
            {
                _passives.Clear();
                _passives = null;
            }

            if (null != _upgradeSums)
            {
                _upgradeSums.Clear();
                _upgradeSums = null;
            }

            if (null != _gimmickScoreSums)
            {
                _gimmickScoreSums.Clear();
                _gimmickScoreSums = null;
            }
        }

        public ShopItemData GetPassive(int index)
        {
            if (null == _passives)
                return null;

            if (index < 0 || index >= _passives.Count)
                return null;

            return _passives[index];
        }

        public bool CanAddPassive(ShopItemData item)
        {
            if (null == _passives)
                return false;

            if (null == item)
                return false;

            if (_passives.Count >= _passiveSlotCount)
                return false;

            return !_passives.Contains(item);
        }

        public bool TryAddPassive(ShopItemData item)
        {
            if (!CanAddPassive(item))
                return false;

            _passives.Add(item);
            return true;
        }

        public void AddUpgrade(ShopItemData item)
        {
            if (null == _upgradeSums)
                return;

            if (null == item)
                return;

            if (item.EffectType == ShopItemData.EEffectType.GimmickScoreRatioAdd)
            {
                _gimmickScoreSums.TryGetValue(item.GimmickType, out float gimmickSum);
                _gimmickScoreSums[item.GimmickType] = gimmickSum + item.EffectValue;
                return;
            }

            _upgradeSums.TryGetValue(item.EffectType, out float sum);
            _upgradeSums[item.EffectType] = sum + item.EffectValue;
        }

        public float GetEffectSum(ShopItemData.EEffectType effectType)
        {
            float sum = 0f;

            if (null != _upgradeSums && _upgradeSums.TryGetValue(effectType, out float upgradeSum))
                sum += upgradeSum;

            if (null == _passives)
                return sum;

            for (int i = 0; i < _passives.Count; ++i)
                if (_passives[i].EffectType == effectType)
                    sum += _passives[i].EffectValue;

            return sum;
        }

        public float GetEffectSub(ShopItemData.EEffectType effectType)
        {
            if (null == _passives)
                return 0f;

            for (int i = 0; i < _passives.Count; ++i)
                if (_passives[i].EffectType == effectType)
                    return _passives[i].EffectValueSub;

            return 0f;
        }

        public bool HasEffect(ShopItemData.EEffectType effectType)
        {
            if (null == _passives)
                return false;

            for (int i = 0; i < _passives.Count; ++i)
                if (_passives[i].EffectType == effectType)
                    return true;

            return false;
        }

        public float GetGimmickScoreRatio(MapInteractable.EGimmickType gimmickType)
        {
            if (null == _gimmickScoreSums)
                return 0f;

            _gimmickScoreSums.TryGetValue(gimmickType, out float sum);
            return sum;
        }
    }
}
