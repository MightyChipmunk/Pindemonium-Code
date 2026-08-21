using System.Collections.Generic;
using Pinball.UI;
using Pinball.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.Ingame.UI
{
    public class OwnedItemsView : MonoBehaviour
    {
        public record Entry(Sprite Icon, int Count);

        const string COUNT_FORMAT = "x{0}";
        const int STACKED_COUNT_MIN = 2;
        const int SLOT_CAPACITY = 16;
        const int NO_ITEM_INDEX = -1;

        [SerializeField] RectTransform _root = null;
        [SerializeField] GameObject _passiveSection = null;
        [SerializeField] RectTransform _passiveContent = null;
        [SerializeField] GameObject _upgradeSection = null;
        [SerializeField] RectTransform _upgradeContent = null;
        [SerializeField] ItemIconSlotView _slotOrigin = null;

        List<ItemIconSlotView> _activeSlots = null;
        ObjectPool<ItemIconSlotView> _slotPool = null;

        public void Init()
        {
            _activeSlots = new List<ItemIconSlotView>();
            _slotPool = new ObjectPool<ItemIconSlotView>(_slotOrigin, CreateSlot, SLOT_CAPACITY);
            _slotOrigin.gameObject.SetActive(false);
        }

        public void Release()
        {
            if (null != _activeSlots)
            {
                _activeSlots.Clear();
                _activeSlots = null;
            }

            if (null != _slotPool)
            {
                _slotPool.Clear();
                _slotPool = null;
            }
        }

        public void SetItems(List<Entry> passives, List<Entry> upgrades)
        {
            if (null == _slotPool)
                return;

            ReturnActiveSlots();

            bool hasPassive = HasEntry(passives);
            bool hasUpgrade = HasEntry(upgrades);
            _root.gameObject.SetActive(hasPassive || hasUpgrade);

            if (!hasPassive && !hasUpgrade)
                return;

            FillSection(_passiveSection, _passiveContent, passives, hasPassive);
            FillSection(_upgradeSection, _upgradeContent, upgrades, hasUpgrade);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
        }

        static bool HasEntry(List<Entry> entries)
        {
            return null != entries && entries.Count > 0;
        }

        void FillSection(GameObject section, RectTransform content, List<Entry> entries, bool hasEntry)
        {
            section.SetActive(hasEntry);
            if (!hasEntry)
                return;

            for (int i = 0; i < entries.Count; ++i)
            {
                ItemIconSlotView slot = _slotPool.Get();
                slot.transform.SetParent(content, false);
                slot.transform.SetAsLastSibling();
                slot.Bind(new ItemIconSlotView.BindParam(
                    ItemIndex: NO_ITEM_INDEX,
                    Icon: entries[i].Icon,
                    CountText: GetCountText(entries[i].Count),
                    HoverCallback: null,
                    ClickCallback: null));
                _activeSlots.Add(slot);
            }
        }

        static string GetCountText(int count)
        {
            if (count < STACKED_COUNT_MIN)
                return string.Empty;

            return string.Format(COUNT_FORMAT, count);
        }

        void ReturnActiveSlots()
        {
            for (int i = 0; i < _activeSlots.Count; ++i)
                _slotPool.Return(_activeSlots[i]);

            _activeSlots.Clear();
        }

        ItemIconSlotView CreateSlot(ItemIconSlotView origin)
        {
            return Instantiate(origin, origin.transform.parent);
        }
    }
}
