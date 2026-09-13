using System;
using System.Collections.Generic;
using UnityEngine;
using GameCore.Util;

namespace Pinball.Ingame.Shop
{
    public class ShopSession
    {
        public record RestoreParam(string[] OfferIds, int RerollCount);

        public const int DEFAULT_OFFER_SLOT_COUNT = 3;
        public const int MAX_OFFER_SLOT_COUNT = 4;

        ShopConfigData _config = null;
        ShopCatalogData _catalog = null;
        Func<ShopItemData, bool> _offerFilter = null;
        ShopItemData[] _offers = null;
        int _rerollCount = 0;
        int _freeRerollCount = 0;

        public int OfferSlotCount => null == _offers ? 0 : _offers.Length;
        public int RerollCount => _rerollCount;

        public int CurrentRerollPrice
        {
            get
            {
                if (null == _config)
                    return 0;

                if (_rerollCount < _freeRerollCount)
                    return 0;

                return _config.RerollBasePrice + _config.RerollPriceIncrement * _rerollCount;
            }
        }

        public ShopSession(ShopConfigData config, ShopCatalogData catalog, Func<ShopItemData, bool> offerFilter, int offerSlotCount, int freeRerollCount)
        {
            _config = config;
            _catalog = catalog;
            _offerFilter = offerFilter;
            _offers = new ShopItemData[Mathf.Clamp(offerSlotCount, 1, MAX_OFFER_SLOT_COUNT)];
            _rerollCount = 0;
            _freeRerollCount = freeRerollCount;
            RollOffers();
        }

        public void Restore(RestoreParam param)
        {
            if (null == _offers || null == _catalog || null == param || null == param.OfferIds)
                return;

            _rerollCount = Mathf.Max(0, param.RerollCount);
            int count = Mathf.Min(_offers.Length, param.OfferIds.Length);
            for (int i = 0; i < _offers.Length; ++i)
                _offers[i] = i < count ? _catalog.FindById(param.OfferIds[i]) : null;
        }

        public string[] CollectOfferIds()
        {
            if (null == _offers)
                return new string[0];

            string[] ids = new string[_offers.Length];
            for (int i = 0; i < ids.Length; ++i)
                ids[i] = null == _offers[i] ? string.Empty : _offers[i].GetId();

            return ids;
        }

        public ShopItemData GetOffer(int slotIndex)
        {
            if (null == _offers)
                return null;

            if (slotIndex < 0 || slotIndex >= _offers.Length)
                return null;

            return _offers[slotIndex];
        }

        public bool IsOffered(ShopItemData item)
        {
            if (null == _offers || null == item)
                return false;

            for (int i = 0; i < _offers.Length; ++i)
                if (_offers[i] == item)
                    return true;

            return false;
        }

        public void MarkSold(int slotIndex)
        {
            if (null == _offers)
                return;

            if (slotIndex < 0 || slotIndex >= _offers.Length)
                return;

            _offers[slotIndex] = null;
        }

        public void AddFreeRerollCount(int count)
        {
            _freeRerollCount += count;
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

            RandomDraw.Fill(CollectCandidates(), _offers);
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
