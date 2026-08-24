#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Pinball.Ingame.Shop;
using UnityEngine;

namespace Pinball.Ingame.Cheat
{
    public class IngameCheatHandler : MonoBehaviour
    {
        public enum EApplyResult
        {
            Success = 0,
            NotReady = 1,
            InvalidInput = 2,
            PassiveSlotFull = 3,
            NotInStage = 4,
            NotOwned = 5,
        }

        public record InitParam(
            Func<int> MoneyGetter,
            Action<int> MoneyAdder,
            Func<bool> IsStageClearableGetter,
            Action StageClearer,
            Action RoundClearer,
            Action GameClearer,
            Action OnCheatApplied);

        const int MIN_APPLY_COUNT = 1;
        const int PASSIVE_OWNED_COUNT = 1;

        Func<int> _moneyGetter = null;
        Action<int> _moneyAdder = null;
        Func<bool> _isStageClearableGetter = null;
        Action _stageClearer = null;
        Action _roundClearer = null;
        Action _gameClearer = null;
        PlayerLoadout _loadout = null;
        Action _onCheatApplied = null;

        public void Init(InitParam param, PlayerLoadout loadout)
        {
            _moneyGetter = param.MoneyGetter;
            _moneyAdder = param.MoneyAdder;
            _isStageClearableGetter = param.IsStageClearableGetter;
            _stageClearer = param.StageClearer;
            _roundClearer = param.RoundClearer;
            _gameClearer = param.GameClearer;
            _loadout = loadout;
            _onCheatApplied = param.OnCheatApplied;
        }

        public void Release()
        {
            _onCheatApplied = null;
            _loadout = null;
            _gameClearer = null;
            _roundClearer = null;
            _stageClearer = null;
            _isStageClearableGetter = null;
            _moneyAdder = null;
            _moneyGetter = null;
        }

        public bool IsReady()
        {
            return null != _moneyGetter && null != _moneyAdder && null != _isStageClearableGetter
                && null != _stageClearer && null != _roundClearer && null != _gameClearer && null != _loadout;
        }

        public int GetMoney()
        {
            if (null == _moneyGetter)
                return 0;

            return _moneyGetter();
        }

        public int GetPassiveCount()
        {
            if (null == _loadout)
                return 0;

            return _loadout.PassiveCount;
        }

        public int GetPassiveSlotCount()
        {
            if (null == _loadout)
                return 0;

            return _loadout.PassiveSlotCount;
        }

        public int GetUpgradeKindCount()
        {
            if (null == _loadout)
                return 0;

            return _loadout.UpgradeKindCount;
        }

        public int GetOwnedCount(ShopItemData item)
        {
            if (null == _loadout || null == item)
                return 0;

            if (item.Kind == ShopItemData.EItemKind.Passive)
                return HasPassive(item) ? PASSIVE_OWNED_COUNT : 0;

            for (int i = 0; i < _loadout.UpgradeKindCount; ++i)
                if (_loadout.GetUpgrade(i) == item)
                    return _loadout.GetUpgradeCount(i);

            return 0;
        }

        public EApplyResult AddMoney(int amount)
        {
            if (!IsReady())
                return EApplyResult.NotReady;

            if (amount <= 0)
                return EApplyResult.InvalidInput;

            _moneyAdder(amount);
            _onCheatApplied?.Invoke();
            return EApplyResult.Success;
        }

        public EApplyResult ClearStage()
        {
            if (!IsReady())
                return EApplyResult.NotReady;

            if (!IsStageClearable())
                return EApplyResult.NotInStage;

            _stageClearer();
            return EApplyResult.Success;
        }

        public EApplyResult ClearRound()
        {
            if (!IsReady())
                return EApplyResult.NotReady;

            if (!IsStageClearable())
                return EApplyResult.NotInStage;

            _roundClearer();
            return EApplyResult.Success;
        }

        public EApplyResult ClearGame()
        {
            if (!IsReady())
                return EApplyResult.NotReady;

            if (!IsStageClearable())
                return EApplyResult.NotInStage;

            _gameClearer();
            return EApplyResult.Success;
        }

        public EApplyResult ApplyItem(ShopItemData item, int count)
        {
            if (!IsReady())
                return EApplyResult.NotReady;

            if (null == item)
                return EApplyResult.InvalidInput;

            if (item.Kind == ShopItemData.EItemKind.Passive)
                return ApplyPassive(item);

            return ApplyUpgrade(item, count);
        }

        public EApplyResult RemoveItem(ShopItemData item, int count)
        {
            if (!IsReady())
                return EApplyResult.NotReady;

            if (null == item)
                return EApplyResult.InvalidInput;

            if (item.Kind == ShopItemData.EItemKind.Passive)
                return RemovePassive(item);

            return RemoveUpgrade(item, count);
        }

        bool IsStageClearable()
        {
            if (null == _isStageClearableGetter)
                return false;

            return _isStageClearableGetter();
        }

        EApplyResult ApplyPassive(ShopItemData item)
        {
            if (!_loadout.TryAddPassive(item))
                return EApplyResult.PassiveSlotFull;

            _onCheatApplied?.Invoke();
            return EApplyResult.Success;
        }

        EApplyResult ApplyUpgrade(ShopItemData item, int count)
        {
            int applyCount = Mathf.Max(count, MIN_APPLY_COUNT);
            for (int i = 0; i < applyCount; ++i)
                _loadout.AddUpgrade(item);

            _onCheatApplied?.Invoke();
            return EApplyResult.Success;
        }

        EApplyResult RemovePassive(ShopItemData item)
        {
            if (!_loadout.TryRemovePassive(item))
                return EApplyResult.NotOwned;

            _onCheatApplied?.Invoke();
            return EApplyResult.Success;
        }

        EApplyResult RemoveUpgrade(ShopItemData item, int count)
        {
            int removeCount = Mathf.Max(count, MIN_APPLY_COUNT);
            int removedCount = 0;
            for (int i = 0; i < removeCount; ++i)
                if (_loadout.TryRemoveUpgrade(item))
                    ++removedCount;

            if (removedCount <= 0)
                return EApplyResult.NotOwned;

            _onCheatApplied?.Invoke();
            return EApplyResult.Success;
        }

        bool HasPassive(ShopItemData item)
        {
            for (int i = 0; i < _loadout.PassiveCount; ++i)
                if (_loadout.GetPassive(i) == item)
                    return true;

            return false;
        }
    }
}
#endif
