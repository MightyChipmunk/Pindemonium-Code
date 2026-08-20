using System.Collections.Generic;

namespace Pinball.Ingame
{
    public class PlayerVouchers
    {
        public const int PRICE = 10;
        public const int NO_PURCHASED_ROUND = -1;

        List<VoucherData> _owned = null;
        int _lastPurchasedRoundIndex = NO_PURCHASED_ROUND;

        public int OwnedCount => null == _owned ? 0 : _owned.Count;
        public int LastPurchasedRoundIndex => _lastPurchasedRoundIndex;

        public PlayerVouchers()
        {
            _owned = new List<VoucherData>();
        }

        public VoucherData GetOwned(int index)
        {
            if (index < 0 || index >= OwnedCount)
                return null;

            return _owned[index];
        }

        public bool Owns(VoucherData voucher)
        {
            if (null == voucher)
                return true;

            return _owned.Contains(voucher);
        }

        public bool CanPurchaseInRound(int roundIndex)
        {
            return _lastPurchasedRoundIndex != roundIndex;
        }

        public bool TryPurchase(VoucherData voucher, int roundIndex)
        {
            if (Owns(voucher))
                return false;

            if (!CanPurchaseInRound(roundIndex))
                return false;

            _owned.Add(voucher);
            _lastPurchasedRoundIndex = roundIndex;
            return true;
        }

        public bool Has(VoucherData.EEffect effect)
        {
            for (int i = 0; i < OwnedCount; ++i)
                if (_owned[i].Effect == effect)
                    return true;

            return false;
        }

        public float GetEffectValue(VoucherData.EEffect effect, int valueIndex)
        {
            for (int i = 0; i < OwnedCount; ++i)
                if (_owned[i].Effect == effect)
                    return _owned[i].GetEffectValue(valueIndex);

            return 0f;
        }

        public float GetEffectSum(VoucherData.EEffect effect)
        {
            float sum = 0f;
            for (int i = 0; i < OwnedCount; ++i)
                if (_owned[i].Effect == effect)
                    sum += _owned[i].GetEffectValue(VoucherData.MAIN_VALUE_INDEX);

            return sum;
        }

        public void Restore(string[] voucherIds, int lastPurchasedRoundIndex, VoucherCatalogData catalog)
        {
            _owned.Clear();
            _lastPurchasedRoundIndex = lastPurchasedRoundIndex;

            if (null == voucherIds || null == catalog)
                return;

            for (int i = 0; i < voucherIds.Length; ++i)
            {
                VoucherData voucher = catalog.FindById(voucherIds[i]);
                if (null != voucher && !_owned.Contains(voucher))
                    _owned.Add(voucher);
            }
        }

        public string[] CollectIds()
        {
            string[] ids = new string[OwnedCount];
            for (int i = 0; i < ids.Length; ++i)
                ids[i] = _owned[i].GetId();

            return ids;
        }
    }
}
