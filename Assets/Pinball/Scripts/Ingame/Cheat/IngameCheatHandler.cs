using System;
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
        }

        public record InitParam(
            MoneyWallet Wallet,
            PlayerLoadout Loadout,
            Action OnCheatApplied);

        const int MIN_APPLY_COUNT = 1;
        const int PASSIVE_OWNED_COUNT = 1;

        MoneyWallet _wallet = null;
        PlayerLoadout _loadout = null;
        Action _onCheatApplied = null;

        public void Init(InitParam param)
        {
            _wallet = param.Wallet;
            _loadout = param.Loadout;
            _onCheatApplied = param.OnCheatApplied;
        }

        public void Release()
        {
            _onCheatApplied = null;
            _loadout = null;
            _wallet = null;
        }

        public bool IsReady()
        {
            return null != _wallet && null != _loadout;
        }

        public int GetMoney()
        {
            if (null == _wallet)
                return 0;

            return _wallet.Current;
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

            _wallet.Add(amount);
            _onCheatApplied?.Invoke();
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

        bool HasPassive(ShopItemData item)
        {
            for (int i = 0; i < _loadout.PassiveCount; ++i)
                if (_loadout.GetPassive(i) == item)
                    return true;

            return false;
        }
    }
}
