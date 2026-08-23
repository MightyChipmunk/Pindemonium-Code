using System;
using System.Collections.Generic;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.Shop
{
    public class CardPackShop
    {
        public const int DEFAULT_PACK_SLOT_COUNT = 2;

        CardPackCatalogData _packCatalog = null;
        ShopCatalogData _itemCatalog = null;
        Func<ShopItemData, bool> _offerFilter = null;
        CardPackData[] _packs = null;
        CardPackData _openedPack = null;
        List<ShopItemData> _revealedItems = null;
        List<bool> _takenFlags = null;
        int _remainingPicks = 0;

        public int PackSlotCount => null == _packs ? 0 : _packs.Length;
        public bool IsOpening => null != _openedPack;
        public bool IsOpeningFinished => _remainingPicks <= 0;
        public int RemainingPicks => _remainingPicks;
        public int RevealedItemCount => null == _revealedItems ? 0 : _revealedItems.Count;

        public CardPackShop(CardPackCatalogData packCatalog, ShopCatalogData itemCatalog, Func<ShopItemData, bool> offerFilter, int packSlotCount)
        {
            _packCatalog = packCatalog;
            _itemCatalog = itemCatalog;
            _offerFilter = offerFilter;
            _revealedItems = new List<ShopItemData>();
            _takenFlags = new List<bool>();
            _packs = new CardPackData[Mathf.Max(0, packSlotCount)];
            RollPacks();
        }

        public void Reroll()
        {
            if (IsOpening)
                return;

            RollPacks();
        }

        public CardPackData GetPack(int slotIndex)
        {
            if (null == _packs)
                return null;

            if (slotIndex < 0 || slotIndex >= _packs.Length)
                return null;

            return _packs[slotIndex];
        }

        public bool HasCandidates(CardPackData pack)
        {
            if (null == pack)
                return false;

            return CollectCandidates(pack).Count > 0;
        }

        public bool TryStartOpening(int slotIndex)
        {
            if (IsOpening)
                return false;

            if (!TryStartOpening(GetPack(slotIndex)))
                return false;

            _packs[slotIndex] = null;
            return true;
        }

        public bool TryOpenRandomPack()
        {
            if (IsOpening)
                return false;

            List<CardPackData> candidates = new List<CardPackData>();
            if (null != _packCatalog)
            {
                for (int i = 0; i < _packCatalog.Count; ++i)
                {
                    CardPackData pack = _packCatalog.GetPack(i);
                    if (HasCandidates(pack))
                        candidates.Add(pack);
                }
            }

            return TryStartOpening(RandomDraw.TakeOne(candidates));
        }

        bool TryStartOpening(CardPackData pack)
        {
            if (null == pack)
                return false;

            List<ShopItemData> candidates = CollectCandidates(pack);
            if (candidates.Count <= 0)
                return false;

            _revealedItems.Clear();
            _takenFlags.Clear();
            for (int i = 0; i < pack.RevealCount && candidates.Count > 0; ++i)
            {
                _revealedItems.Add(RandomDraw.TakeOne(candidates));
                _takenFlags.Add(false);
            }

            _openedPack = pack;
            _remainingPicks = Mathf.Min(pack.PickCount, _revealedItems.Count);
            return true;
        }

        public string GetOpeningPackNameKey()
        {
            return null == _openedPack ? string.Empty : _openedPack.GetNameKey();
        }

        public ShopItemData GetRevealedItem(int index)
        {
            if (index < 0 || index >= RevealedItemCount)
                return null;

            return _revealedItems[index];
        }

        public bool IsTaken(int index)
        {
            if (index < 0 || index >= RevealedItemCount)
                return true;

            return _takenFlags[index];
        }

        public ShopItemData TakePick(int index)
        {
            if (!IsOpening || IsOpeningFinished)
                return null;

            if (IsTaken(index))
                return null;

            _takenFlags[index] = true;
            --_remainingPicks;
            return _revealedItems[index];
        }

        public void CloseOpening()
        {
            _openedPack = null;
            _revealedItems.Clear();
            _takenFlags.Clear();
            _remainingPicks = 0;
        }

        void RollPacks()
        {
            List<CardPackData> candidates = new List<CardPackData>();
            if (null != _packCatalog)
            {
                for (int i = 0; i < _packCatalog.Count; ++i)
                {
                    CardPackData pack = _packCatalog.GetPack(i);
                    if (null != pack)
                        candidates.Add(pack);
                }
            }

            RandomDraw.Fill(candidates, _packs);
        }

        List<ShopItemData> CollectCandidates(CardPackData pack)
        {
            List<ShopItemData> candidates = new List<ShopItemData>();
            if (null == _itemCatalog)
                return candidates;

            for (int i = 0; i < _itemCatalog.Count; ++i)
            {
                ShopItemData item = _itemCatalog.GetItem(i);
                if (!pack.Accepts(item))
                    continue;

                if (null != _offerFilter && !_offerFilter(item))
                    continue;

                candidates.Add(item);
            }

            return candidates;
        }
    }
}
