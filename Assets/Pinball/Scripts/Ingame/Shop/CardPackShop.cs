using System;
using System.Collections.Generic;
using Pinball.Save;
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
        List<ShopItemData>[] _slotItems = null;
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
            _slotItems = new List<ShopItemData>[_packs.Length];
            RollPacks();
        }

        public void Restore(IngamePlayData.CardPackEntry[] slotEntries, IngamePlayData.CardPackEntry openingEntry)
        {
            if (null == _packs || null == _packCatalog || null == _itemCatalog)
                return;

            for (int i = 0; i < _packs.Length; ++i)
            {
                _packs[i] = null;
                _slotItems[i] = new List<ShopItemData>();
                if (null != slotEntries && i < slotEntries.Length)
                    RestoreSlot(i, slotEntries[i]);
            }

            RestoreOpening(openingEntry);
        }

        public IngamePlayData.CardPackEntry[] CollectSlotEntries()
        {
            if (null == _packs)
                return new IngamePlayData.CardPackEntry[0];

            IngamePlayData.CardPackEntry[] entries = new IngamePlayData.CardPackEntry[_packs.Length];
            for (int i = 0; i < entries.Length; ++i)
                entries[i] = new IngamePlayData.CardPackEntry(
                    null == _packs[i] ? string.Empty : _packs[i].GetId(),
                    CollectItemIds(_slotItems[i]),
                    null);

            return entries;
        }

        public IngamePlayData.CardPackEntry CollectOpeningEntry()
        {
            if (!IsOpening)
                return default;

            return new IngamePlayData.CardPackEntry(_openedPack.GetId(), CollectItemIds(_revealedItems), _takenFlags.ToArray());
        }

        public void RefreshSlotItems()
        {
            if (null == _packs)
                return;

            for (int i = 0; i < _packs.Length; ++i)
                _slotItems[i] = RollItems(_packs[i]);
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

        public bool CanBuy(int slotIndex)
        {
            if (null == _slotItems)
                return false;

            if (slotIndex < 0 || slotIndex >= _slotItems.Length)
                return false;

            return null != GetPack(slotIndex) && _slotItems[slotIndex].Count > 0;
        }

        public bool TryStartOpening(int slotIndex)
        {
            if (IsOpening)
                return false;

            if (!CanBuy(slotIndex))
                return false;

            if (!BeginOpening(_packs[slotIndex], _slotItems[slotIndex]))
                return false;

            _packs[slotIndex] = null;
            _slotItems[slotIndex] = new List<ShopItemData>();
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

            CardPackData picked = RandomDraw.TakeOne(candidates);
            return BeginOpening(picked, RollItems(picked));
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

        bool BeginOpening(CardPackData pack, List<ShopItemData> items)
        {
            if (null == pack || null == items || items.Count <= 0)
                return false;

            _revealedItems.Clear();
            _takenFlags.Clear();
            for (int i = 0; i < items.Count; ++i)
            {
                _revealedItems.Add(items[i]);
                _takenFlags.Add(false);
            }

            _openedPack = pack;
            _remainingPicks = Mathf.Min(pack.PickCount, _revealedItems.Count);
            return true;
        }

        void RestoreSlot(int slotIndex, IngamePlayData.CardPackEntry entry)
        {
            CardPackData pack = _packCatalog.FindById(entry.PackId);
            if (null == pack)
                return;

            _packs[slotIndex] = pack;
            string[] cardIds = entry.GetCardIds();
            if (null == cardIds)
                return;

            for (int i = 0; i < cardIds.Length; ++i)
            {
                ShopItemData item = _itemCatalog.FindById(cardIds[i]);
                if (null != item)
                    _slotItems[slotIndex].Add(item);
            }
        }

        void RestoreOpening(IngamePlayData.CardPackEntry entry)
        {
            CloseOpening();
            CardPackData pack = _packCatalog.FindById(entry.PackId);
            string[] cardIds = entry.GetCardIds();
            if (null == pack || null == cardIds)
                return;

            bool[] takenFlags = entry.GetTakenFlags();
            List<ShopItemData> items = new List<ShopItemData>();
            List<bool> restoredFlags = new List<bool>();
            for (int i = 0; i < cardIds.Length; ++i)
            {
                ShopItemData item = _itemCatalog.FindById(cardIds[i]);
                if (null == item)
                    continue;

                items.Add(item);
                restoredFlags.Add(null != takenFlags && i < takenFlags.Length && takenFlags[i]);
            }

            if (!BeginOpening(pack, items))
                return;

            for (int i = 0; i < restoredFlags.Count; ++i)
            {
                if (!restoredFlags[i])
                    continue;

                _takenFlags[i] = true;
                --_remainingPicks;
            }

            if (IsOpeningFinished)
                CloseOpening();
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
            RefreshSlotItems();
        }

        List<ShopItemData> RollItems(CardPackData pack)
        {
            List<ShopItemData> items = new List<ShopItemData>();
            if (null == pack)
                return items;

            List<ShopItemData> candidates = CollectCandidates(pack);
            for (int i = 0; i < pack.RevealCount && candidates.Count > 0; ++i)
                items.Add(RandomDraw.TakeOne(candidates));

            return items;
        }

        static string[] CollectItemIds(List<ShopItemData> items)
        {
            if (null == items)
                return new string[0];

            string[] ids = new string[items.Count];
            for (int i = 0; i < ids.Length; ++i)
                ids[i] = items[i].GetId();

            return ids;
        }

        bool HasCandidates(CardPackData pack)
        {
            if (null == pack)
                return false;

            return CollectCandidates(pack).Count > 0;
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
