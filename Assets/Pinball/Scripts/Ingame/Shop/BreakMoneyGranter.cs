using Pinball.Ingame.Stat;
using UnityEngine;

namespace Pinball.Ingame
{
    public class BreakMoneyGranter
    {
        MoneyWallet _wallet = null;
        StatProvider _stats = null;
        int _stageEarnedMoney = 0;

        public void Init(MoneyWallet wallet, StatProvider stats)
        {
            _wallet = wallet;
            _stats = stats;
            _stageEarnedMoney = 0;
        }

        public void Release()
        {
            _wallet = null;
            _stats = null;
            _stageEarnedMoney = 0;
        }

        public void ResetStage()
        {
            _stageEarnedMoney = 0;
        }

        public void HandleGimmickBroken()
        {
            if (null == _wallet || null == _stats)
                return;

            int amount = Mathf.RoundToInt(_stats.GetEffectSum(ShopItemData.EEffectType.GimmickBreakMoney));
            if (amount <= 0)
                return;

            int stageLimit = Mathf.RoundToInt(_stats.GetEffectValue(ShopItemData.EEffectType.GimmickBreakMoney, ShopItemData.SUB_VALUE_INDEX));
            if (stageLimit > 0)
            {
                if (_stageEarnedMoney >= stageLimit)
                    return;

                amount = Mathf.Min(amount, stageLimit - _stageEarnedMoney);
            }

            _stageEarnedMoney += amount;
            _wallet.Add(amount);
        }
    }
}
