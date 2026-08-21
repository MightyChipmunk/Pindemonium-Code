using System;
using Pinball.Ingame.Stat;
using UnityEngine;

namespace Pinball.Ingame.Shop
{
    public class BreakMoneyGranter
    {
        MoneyWallet _wallet = null;
        Action _moneyGrantedCallback = null;
        int _stageEarnedMoney = 0;

        public BreakMoneyGranter(MoneyWallet wallet, Action moneyGrantedCallback)
        {
            _wallet = wallet;
            _moneyGrantedCallback = moneyGrantedCallback;
            _stageEarnedMoney = 0;
        }

        public void ResetStage()
        {
            _stageEarnedMoney = 0;
        }

        public void HandleGimmickBroken()
        {
            if (null == _wallet)
                return;

            int amount = Mathf.RoundToInt(StatProvider.GetEffectSum(ShopItemData.EEffectType.GimmickBreakMoney));
            if (amount <= 0)
                return;

            int stageLimit = Mathf.RoundToInt(StatProvider.GetEffectValue(ShopItemData.EEffectType.GimmickBreakMoney, ShopItemData.SUB_VALUE_INDEX));
            if (stageLimit > 0)
            {
                if (_stageEarnedMoney >= stageLimit)
                    return;

                amount = Mathf.Min(amount, stageLimit - _stageEarnedMoney);
            }

            _stageEarnedMoney += amount;
            _wallet.Add(amount);
            _moneyGrantedCallback?.Invoke();
        }
    }
}
