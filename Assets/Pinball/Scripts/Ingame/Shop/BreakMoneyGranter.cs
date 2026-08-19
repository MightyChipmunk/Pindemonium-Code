using Pinball.Audio;
using Pinball.Ingame.Stat;
using UnityEngine;

namespace Pinball.Ingame
{
    public class BreakMoneyGranter
    {
        MoneyWallet _wallet = null;
        int _stageEarnedMoney = 0;

        public BreakMoneyGranter(MoneyWallet wallet)
        {
            _wallet = wallet;
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
            AudioManager.Play(AudioStorage.EAudioClipType.MoneyGain);
        }
    }
}
