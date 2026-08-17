using System.Collections.Generic;
using System.Globalization;
using Pinball.Ingame;
using Pinball.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class ResultStatsView : UIViewBase
    {
        public record ItemCountEntry(int ItemIndex, int Count);

        public record Args(
            int StageNumber,
            int Score,
            int Money,
            int UsedBallCount,
            int[] PassiveIndices,
            ItemCountEntry[] Upgrades) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.ResultStats;
        }

        const string NUMBER_FORMAT = "N0";
        const string COUNT_FORMAT = "x{0}";
        const int SLOT_CAPACITY = 16;
        const int STACKED_COUNT_MIN = 2;
        const int PASSIVE_OWNED_COUNT = 1;

        [SerializeField] ShopCatalogData _catalog = null;
        [SerializeField] TMP_Text _stageLabel = null;
        [SerializeField] TMP_Text _scoreLabel = null;
        [SerializeField] TMP_Text _moneyLabel = null;
        [SerializeField] TMP_Text _ballLabel = null;
        [SerializeField] RectTransform _passiveContent = null;
        [SerializeField] RectTransform _upgradeContent = null;
        [SerializeField] ItemIconSlotView _slotOrigin = null;
        [SerializeField] ItemTooltipView _tooltip = null;
        [SerializeField] Button _closeButton = null;

        ObjectPool<ItemIconSlotView> _slotPool = null;
        List<ItemIconSlotView> _activeSlots = null;

        public override void OnGenerate()
        {
            _activeSlots = new List<ItemIconSlotView>();
            _slotPool = new ObjectPool<ItemIconSlotView>(_slotOrigin, CreateSlot, SLOT_CAPACITY);
            _closeButton.onClick.AddListener(OnCloseClicked);
            _tooltip.Hide();
        }

        public override void OnTerminate()
        {
            _closeButton.onClick.RemoveListener(OnCloseClicked);

            if (null != _slotPool)
            {
                _slotPool.Clear();
                _slotPool = null;
            }

            if (null != _activeSlots)
            {
                _activeSlots.Clear();
                _activeSlots = null;
            }
        }

        public override void OnReturn()
        {
            ClearSlots();
            _tooltip.Hide();
        }

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            _stageLabel.text = viewArgs.StageNumber.ToString(CultureInfo.InvariantCulture);
            _scoreLabel.text = viewArgs.Score.ToString(NUMBER_FORMAT, CultureInfo.InvariantCulture);
            _moneyLabel.text = viewArgs.Money.ToString(CultureInfo.InvariantCulture);
            _ballLabel.text = viewArgs.UsedBallCount.ToString(CultureInfo.InvariantCulture);

            ClearSlots();
            _tooltip.Hide();
            BindPassives(viewArgs.PassiveIndices);
            BindUpgrades(viewArgs.Upgrades);
        }

        void BindPassives(int[] passiveIndices)
        {
            int boundCount = 0;
            if (null != passiveIndices)
            {
                for (int i = 0; i < passiveIndices.Length; ++i)
                    if (BindSlot(_passiveContent, passiveIndices[i], PASSIVE_OWNED_COUNT))
                        ++boundCount;
            }

            if (boundCount <= 0)
                BindEmptySlot(_passiveContent);
        }

        void BindUpgrades(ItemCountEntry[] upgrades)
        {
            int boundCount = 0;
            if (null != upgrades)
            {
                for (int i = 0; i < upgrades.Length; ++i)
                    if (BindSlot(_upgradeContent, upgrades[i].ItemIndex, upgrades[i].Count))
                        ++boundCount;
            }

            if (boundCount <= 0)
                BindEmptySlot(_upgradeContent);
        }

        bool BindSlot(RectTransform parent, int itemIndex, int count)
        {
            ShopItemData item = GetItem(itemIndex);
            if (null == item)
                return false;

            ItemIconSlotView slot = GetSlot(parent);
            if (null == slot)
                return false;

            slot.Bind(new ItemIconSlotView.BindParam(
                ItemIndex: itemIndex,
                Icon: item.GetIcon(),
                CountText: GetCountText(count),
                HoverCallback: OnTooltipHovered));
            return true;
        }

        void BindEmptySlot(RectTransform parent)
        {
            ItemIconSlotView slot = GetSlot(parent);
            if (null == slot)
                return;

            slot.BindEmpty();
        }

        ItemIconSlotView GetSlot(RectTransform parent)
        {
            ItemIconSlotView slot = _slotPool.Get();
            if (null == slot)
                return null;

            slot.transform.SetParent(parent, false);
            slot.transform.SetAsLastSibling();
            _activeSlots.Add(slot);
            return slot;
        }

        void ClearSlots()
        {
            if (null == _activeSlots)
                return;

            for (int i = 0; i < _activeSlots.Count; ++i)
                _slotPool.Return(_activeSlots[i]);

            _activeSlots.Clear();
        }

        ItemIconSlotView CreateSlot(ItemIconSlotView origin)
        {
            return Instantiate(origin, _passiveContent);
        }

        void OnTooltipHovered(int itemIndex, Vector3 anchorPosition, bool isEntered)
        {
            ShopItemData item = isEntered ? GetItem(itemIndex) : null;
            if (null == item)
            {
                _tooltip.Hide();
                return;
            }

            _tooltip.Show(item.GetDisplayName(), item.GetDescription(), anchorPosition);
        }

        string GetCountText(int count)
        {
            if (count < STACKED_COUNT_MIN)
                return string.Empty;

            return string.Format(COUNT_FORMAT, count);
        }

        ShopItemData GetItem(int itemIndex)
        {
            if (null == _catalog)
                return null;

            return _catalog.GetItem(itemIndex);
        }

        void OnCloseClicked()
        {
            UIManager.Hide(UIViewCatalogData.EViewType.ResultStats);
        }
    }
}
