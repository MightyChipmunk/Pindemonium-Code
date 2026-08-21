using System.Collections.Generic;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.Shop
{
    public class PlayerVouchers
    {
        public record RestoreParam(
            string[] OwnedIds,
            string[] OfferedIds,
            string OfferId,
            int OfferRoundIndex,
            int PurchasedRoundIndex,
            VoucherCatalogData Catalog);

        public const int PRICE = 10;
        const int NO_ROUND = -1;
        const int MIN_DISCOUNTED_PRICE = 1;

        List<VoucherData> _owned = null;
        List<VoucherData> _offered = null;
        VoucherData _offer = null;
        int _offerRoundIndex = NO_ROUND;
        int _lastPurchasedRoundIndex = NO_ROUND;

        public int OwnedCount => null == _owned ? 0 : _owned.Count;
        public int OfferRoundIndex => _offerRoundIndex;
        public int LastPurchasedRoundIndex => _lastPurchasedRoundIndex;

        public PlayerVouchers()
        {
            _owned = new List<VoucherData>();
            _offered = new List<VoucherData>();
        }

        public VoucherData GetOwned(int index)
        {
            if (index < 0 || index >= OwnedCount)
                return null;

            return _owned[index];
        }

        public VoucherData GetOffer(int roundIndex, VoucherCatalogData catalog)
        {
            if (_lastPurchasedRoundIndex == roundIndex)
                return null;

            if (_offerRoundIndex == roundIndex)
                return _offer;

            _offerRoundIndex = roundIndex;
            _offer = RollOffer(catalog);
            if (null != _offer)
                _offered.Add(_offer);

            return _offer;
        }

        public string GetOfferId()
        {
            return null == _offer ? string.Empty : _offer.GetId();
        }

        public void PurchaseOffer()
        {
            if (null == _offer)
                return;

            _owned.Add(_offer);
            _lastPurchasedRoundIndex = _offerRoundIndex;
            _offer = null;
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

        public bool TryRollChance(VoucherData.EEffect effect)
        {
            if (!Has(effect))
                return false;

            return Random.value < GetEffectValue(effect, VoucherData.MAIN_VALUE_INDEX);
        }

        public int GetDiscountedPrice(int price)
        {
            if (!Has(VoucherData.EEffect.ShopDiscount))
                return price;

            float discount = GetEffectValue(VoucherData.EEffect.ShopDiscount, VoucherData.MAIN_VALUE_INDEX);
            return Mathf.Max(MIN_DISCOUNTED_PRICE, Mathf.RoundToInt(price * (1f - discount)));
        }

        public int BoostInterestCap(int cap)
        {
            if (!Has(VoucherData.EEffect.InterestCapBoost))
                return cap;

            return Mathf.RoundToInt(cap * GetEffectValue(VoucherData.EEffect.InterestCapBoost, VoucherData.MAIN_VALUE_INDEX));
        }

        public int BoostInterest(int interest)
        {
            if (!Has(VoucherData.EEffect.InterestRateBoost))
                return interest;

            return Mathf.FloorToInt(interest * GetEffectValue(VoucherData.EEffect.InterestRateBoost, VoucherData.MAIN_VALUE_INDEX));
        }

        public void Restore(RestoreParam param)
        {
            _owned.Clear();
            _offered.Clear();
            _offer = null;
            _offerRoundIndex = param.OfferRoundIndex;
            _lastPurchasedRoundIndex = param.PurchasedRoundIndex;

            if (null == param.Catalog)
                return;

            Collect(param.OwnedIds, param.Catalog, _owned);
            Collect(param.OwnedIds, param.Catalog, _offered);
            Collect(param.OfferedIds, param.Catalog, _offered);
            _offer = param.Catalog.FindById(param.OfferId);
            if (null != _offer && !_offered.Contains(_offer))
                _offered.Add(_offer);
        }

        public string[] CollectIds()
        {
            return CollectIds(_owned);
        }

        public string[] CollectOfferedIds()
        {
            return CollectIds(_offered);
        }

        VoucherData RollOffer(VoucherCatalogData catalog)
        {
            if (null == catalog)
                return null;

            List<VoucherData> candidates = new List<VoucherData>();
            for (int i = 0; i < catalog.Count; ++i)
            {
                VoucherData voucher = catalog.GetVoucher(i);
                if (null == voucher || _offered.Contains(voucher))
                    continue;

                candidates.Add(voucher);
            }

            return RandomDraw.TakeOne(candidates);
        }

        static void Collect(string[] voucherIds, VoucherCatalogData catalog, List<VoucherData> destination)
        {
            if (null == voucherIds)
                return;

            for (int i = 0; i < voucherIds.Length; ++i)
            {
                VoucherData voucher = catalog.FindById(voucherIds[i]);
                if (null != voucher && !destination.Contains(voucher))
                    destination.Add(voucher);
            }
        }

        static string[] CollectIds(List<VoucherData> vouchers)
        {
            string[] ids = new string[vouchers.Count];
            for (int i = 0; i < ids.Length; ++i)
                ids[i] = vouchers[i].GetId();

            return ids;
        }
    }
}
