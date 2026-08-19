using System;
using System.Collections.Generic;
using Pinball.Ingame.Map;
using Pinball.Save;

namespace Pinball.Ingame
{
    public class PlayerLoadout
    {
        const int FIRST_UPGRADE_COUNT = 1;

        List<ShopItemData> _passives = null;
        List<ShopItemData> _upgrades = null;
        List<int> _upgradeCounts = null;
        Dictionary<ShopItemData.EEffectType, float> _upgradeSums = null;
        Dictionary<MapInteractable.EGimmickType, float> _gimmickScoreSums = null;
        int _passiveSlotCount = 0;

        public int PassiveSlotCount => _passiveSlotCount;
        public int PassiveCount => null == _passives ? 0 : _passives.Count;
        public int UpgradeKindCount => null == _upgrades ? 0 : _upgrades.Count;

        public PlayerLoadout(int passiveSlotCount)
        {
            _passiveSlotCount = passiveSlotCount;
            _passives = new List<ShopItemData>();
            _upgrades = new List<ShopItemData>();
            _upgradeCounts = new List<int>();
            _upgradeSums = new Dictionary<ShopItemData.EEffectType, float>();
            _gimmickScoreSums = new Dictionary<MapInteractable.EGimmickType, float>();
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

        public ShopItemData GetUpgrade(int index)
        {
            if (null == _upgrades)
                return null;

            if (index < 0 || index >= _upgrades.Count)
                return null;

            return _upgrades[index];
        }

        public int GetUpgradeCount(int index)
        {
            if (null == _upgradeCounts)
                return 0;

            if (index < 0 || index >= _upgradeCounts.Count)
                return 0;

            return _upgradeCounts[index];
        }

        public void AddUpgrade(ShopItemData item)
        {
            if (null == _upgradeSums)
                return;

            if (null == item)
                return;

            RecordUpgrade(item);

            if (item.EffectType == ShopItemData.EEffectType.GimmickScoreRatioAdd)
            {
                _gimmickScoreSums.TryGetValue(item.GimmickType, out float gimmickSum);
                _gimmickScoreSums[item.GimmickType] = gimmickSum + item.GetEffectValue(ShopItemData.MAIN_VALUE_INDEX);
                return;
            }

            _upgradeSums.TryGetValue(item.EffectType, out float sum);
            _upgradeSums[item.EffectType] = sum + item.GetEffectValue(ShopItemData.MAIN_VALUE_INDEX);
        }

        void RecordUpgrade(ShopItemData item)
        {
            int index = _upgrades.IndexOf(item);
            if (index < 0)
            {
                _upgrades.Add(item);
                _upgradeCounts.Add(FIRST_UPGRADE_COUNT);
                return;
            }

            ++_upgradeCounts[index];
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
                    sum += _passives[i].GetEffectValue(ShopItemData.MAIN_VALUE_INDEX);

            return sum;
        }

        public float GetEffectValue(ShopItemData.EEffectType effectType, int valueIndex)
        {
            if (null == _passives)
                return 0f;

            for (int i = 0; i < _passives.Count; ++i)
                if (_passives[i].EffectType == effectType)
                    return _passives[i].GetEffectValue(valueIndex);

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

        public void GrantRandomPassives(ShopCatalogData catalog, Func<ShopItemData, bool> isUnlocked, int passiveCount)
        {
            if (passiveCount <= 0 || null == catalog)
                return;

            List<ShopItemData> candidates = new List<ShopItemData>();
            for (int i = 0; i < catalog.Count; ++i)
            {
                ShopItemData item = catalog.GetItem(i);
                if (null == item || item.Kind != ShopItemData.EItemKind.Passive)
                    continue;

                if (null != isUnlocked && !isUnlocked(item))
                    continue;

                if (!CanAddPassive(item))
                    continue;

                candidates.Add(item);
            }

            for (int i = 0; i < passiveCount && candidates.Count > 0; ++i)
            {
                int pickIndex = UnityEngine.Random.Range(0, candidates.Count);
                TryAddPassive(candidates[pickIndex]);
                candidates.RemoveAt(pickIndex);
            }
        }

        public void Restore(IngamePlayData savedData, ShopCatalogData catalog)
        {
            if (null == savedData || null == catalog)
                return;

            string[] passiveIds = savedData.GetPassiveIds();
            if (null != passiveIds)
            {
                for (int i = 0; i < passiveIds.Length; ++i)
                    TryAddPassive(catalog.FindById(passiveIds[i]));
            }

            IngamePlayData.UpgradeEntry[] upgrades = savedData.GetUpgrades();
            if (null == upgrades)
                return;

            for (int i = 0; i < upgrades.Length; ++i)
            {
                IngamePlayData.UpgradeEntry entry = upgrades[i];
                ShopItemData item = catalog.FindById(entry.Id);
                if (null == item)
                    continue;

                for (int k = 0; k < entry.Count; ++k)
                    AddUpgrade(item);
            }
        }

        public string[] CollectPassiveIds()
        {
            string[] passiveIds = new string[PassiveCount];
            for (int i = 0; i < passiveIds.Length; ++i)
                passiveIds[i] = _passives[i].GetId();

            return passiveIds;
        }

        public IngamePlayData.UpgradeEntry[] CollectUpgradeEntries()
        {
            IngamePlayData.UpgradeEntry[] entries = new IngamePlayData.UpgradeEntry[UpgradeKindCount];
            for (int i = 0; i < entries.Length; ++i)
                entries[i] = new IngamePlayData.UpgradeEntry(_upgrades[i].GetId(), _upgradeCounts[i]);

            return entries;
        }
    }
}
