using System.Collections.Generic;
using Pinball.Ingame.Shop;
using Pinball.Localization;
using Pinball.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.Ingame.UI
{
    public class OwnedItemsView : MonoBehaviour
    {
        public record Entry(Sprite Icon, int Count, LocalizedTextRef Title, LocalizedTextRef Description, ShopItemData.EPassiveCategory Category);

        public record Args(List<Entry> Passives, List<Entry> Upgrades, int BallSlotCount, int MapSlotCount, int CommonSlotCount);

        const string COUNT_FORMAT = "x{0}";
        const int STACKED_COUNT_MIN = 2;
        const int PASSIVE_CATEGORY_COUNT = 3;
        const float PASSIVE_SLOT_HEIGHT = 112f;
        const float PASSIVE_SLOT_SPACING = 8f;
        const float PASSIVE_COLUMN_MAX_HEIGHT = 352f;
        const float PASSIVE_COLUMN_MAX_SCALE = 1f;

        [SerializeField] RectTransform _root = null;
        [SerializeField] ItemIconGridView[] _passiveGrids = null;
        [SerializeField] GameObject _upgradeSection = null;
        [SerializeField] ItemIconGridView _upgradeGrid = null;

        List<ItemIconGridView.Entry>[] _categoryEntries = null;

        public void Init()
        {
            _categoryEntries = new List<ItemIconGridView.Entry>[PASSIVE_CATEGORY_COUNT];
            for (int i = 0; i < PASSIVE_CATEGORY_COUNT; ++i)
                _categoryEntries[i] = new List<ItemIconGridView.Entry>();

            for (int i = 0; i < _passiveGrids.Length; ++i)
                _passiveGrids[i].Init(null);

            _upgradeGrid.Init(null);
        }

        public void Release()
        {
            for (int i = 0; i < _passiveGrids.Length; ++i)
                _passiveGrids[i].Release();

            _upgradeGrid.Release();
            _categoryEntries = null;
        }

        public void SetItems(Args args)
        {
            if (null == _categoryEntries || null == args)
                return;

            GroupPassives(args.Passives);

            int maxSlotCount = 0;
            for (int i = 0; i < PASSIVE_CATEGORY_COUNT; ++i)
                maxSlotCount = Mathf.Max(maxSlotCount, GetColumnSlotCount(args, i));

            for (int i = 0; i < PASSIVE_CATEGORY_COUNT; ++i)
                BindCategoryColumn(args, i, maxSlotCount);

            BindUpgrades(args.Upgrades);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
        }

        void GroupPassives(List<Entry> passives)
        {
            for (int i = 0; i < PASSIVE_CATEGORY_COUNT; ++i)
                _categoryEntries[i].Clear();

            if (null == passives)
                return;

            for (int i = 0; i < passives.Count; ++i)
            {
                Entry entry = passives[i];
                if (null == entry)
                    continue;

                _categoryEntries[(int)entry.Category].Add(
                    new ItemIconGridView.Entry(entry.Icon, GetCountText(entry.Count), entry.Title, entry.Description));
            }
        }

        int GetColumnSlotCount(Args args, int category)
        {
            return Mathf.Max(GetBaseSlotCount(args, category), _categoryEntries[category].Count);
        }

        static int GetBaseSlotCount(Args args, int category)
        {
            switch ((ShopItemData.EPassiveCategory)category)
            {
                case ShopItemData.EPassiveCategory.Ball:
                    return args.BallSlotCount;
                case ShopItemData.EPassiveCategory.Map:
                    return args.MapSlotCount;
                default:
                    return args.CommonSlotCount;
            }
        }

        void BindCategoryColumn(Args args, int category, int maxSlotCount)
        {
            List<ItemIconGridView.Entry> entries = _categoryEntries[category];
            int slotCount = GetColumnSlotCount(args, category);
            _passiveGrids[category].Bind(entries.ToArray(), Mathf.Max(0, slotCount - entries.Count));
            SlotRowLayout.FitHeight(
                _passiveGrids[category].GetContentRect(),
                maxSlotCount,
                PASSIVE_SLOT_HEIGHT,
                PASSIVE_SLOT_SPACING,
                PASSIVE_COLUMN_MAX_HEIGHT,
                PASSIVE_COLUMN_MAX_SCALE);
        }

        void BindUpgrades(List<Entry> upgrades)
        {
            bool hasUpgrade = null != upgrades && upgrades.Count > 0;
            _upgradeSection.SetActive(hasUpgrade);
            if (!hasUpgrade)
            {
                _upgradeGrid.Clear();
                return;
            }

            ItemIconGridView.Entry[] entries = new ItemIconGridView.Entry[upgrades.Count];
            for (int i = 0; i < upgrades.Count; ++i)
            {
                Entry entry = upgrades[i];
                if (null == entry)
                    continue;

                entries[i] = new ItemIconGridView.Entry(entry.Icon, GetCountText(entry.Count), entry.Title, entry.Description);
            }

            _upgradeGrid.Bind(entries, 0);
        }

        static string GetCountText(int count)
        {
            if (count < STACKED_COUNT_MIN)
                return string.Empty;

            return string.Format(COUNT_FORMAT, count);
        }
    }
}
