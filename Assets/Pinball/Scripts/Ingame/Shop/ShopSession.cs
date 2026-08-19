using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pinball.Ingame
{
    public class ShopSession
    {
        public const int DEFAULT_OFFER_SLOT_COUNT = 3;
        public const int MAX_OFFER_SLOT_COUNT = 4;

        ShopConfigData _config = null;
        ShopCatalogData _catalog = null;
        Func<ShopItemData, bool> _offerFilter = null;
        ShopItemData[] _offers = null;
        int _rerollCount = 0;

        public int OfferSlotCount => null == _offers ? 0 : _offers.Length;
        public int CurrentRerollPrice => null == _config ? 0 : _config.RerollBasePrice + _config.RerollPriceIncrement * _rerollCount;

        public ShopSession(ShopConfigData config, ShopCatalogData catalog, Func<ShopItemData, bool> offerFilter, int offerSlotCount)
        {
            _config = config;
            _catalog = catalog;
            _offerFilter = offerFilter;
            _offers = new ShopItemData[Mathf.Clamp(offerSlotCount, 1, MAX_OFFER_SLOT_COUNT)];
            _rerollCount = 0;
            RollOffers();
        }

        public ShopItemData GetOffer(int slotIndex)
        {
            if (null == _offers)
                return null;

            if (slotIndex < 0 || slotIndex >= _offers.Length)
                return null;

            return _offers[slotIndex];
        }

        public void MarkSold(int slotIndex)
        {
            if (null == _offers)
                return;

            if (slotIndex < 0 || slotIndex >= _offers.Length)
                return;

            _offers[slotIndex] = null;
        }

        public void Reroll()
        {
            ++_rerollCount;
            RollOffers();
        }

        void RollOffers()
        {
            if (null == _offers)
                return;

            List<ShopItemData> candidates = CollectCandidates();
            for (int i = 0; i < _offers.Length; ++i)
            {
                if (candidates.Count <= 0)
                {
                    _offers[i] = null;
                    continue;
                }

                int pickIndex = UnityEngine.Random.Range(0, candidates.Count);
                _offers[i] = candidates[pickIndex];
                candidates.RemoveAt(pickIndex);
            }
        }

        List<ShopItemData> CollectCandidates()
        {
            List<ShopItemData> candidates = new List<ShopItemData>();
            if (null == _catalog)
                return candidates;

            for (int i = 0; i < _catalog.Count; ++i)
            {
                ShopItemData item = _catalog.GetItem(i);
                if (null == item)
                    continue;

                if (null != _offerFilter && !_offerFilter(item))
                    continue;

                candidates.Add(item);
            }

            return candidates;
        }
    }
}
