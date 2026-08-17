using System.Collections.Generic;
using Pinball.Ingame;
using Pinball.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class ShopView : UIViewBase
    {
        public record Args(int Money, int RerollPrice) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.Shop;
        }

        public record OfferArgs(int SlotIndex, int ItemIndex) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.Shop;
        }

        public record OwnedResetArgs(int PassiveCount, int PassiveSlotCount, int UpgradeCount) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.Shop;
        }

        public record OwnedItemArgs(int ItemIndex, int Count) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.Shop;
        }

        const string MONEY_FORMAT = "$ {0}";
        const string REROLL_FORMAT = "REROLL ${0}";
        const string PRICE_FORMAT = "${0}";
        const string PASSIVE_HEADER_FORMAT = "PASSIVE  {0}/{1}";
        const string UPGRADE_HEADER_FORMAT = "UPGRADE  {0}";
        const string OWNED_COUNT_FORMAT = "x{0}";
        const int OWNED_SLOT_CAPACITY = 12;
        const int STACKED_COUNT_MIN = 2;

        [SerializeField] ShopCatalogData _catalog = null;
        [SerializeField] TMP_Text _moneyLabel = null;
        [SerializeField] ShopItemView _offerSlot0 = null;
        [SerializeField] ShopItemView _offerSlot1 = null;
        [SerializeField] ShopItemView _offerSlot2 = null;
        [SerializeField] TMP_Text _passiveHeaderLabel = null;
        [SerializeField] TMP_Text _upgradeHeaderLabel = null;
        [SerializeField] RectTransform _passiveContent = null;
        [SerializeField] RectTransform _upgradeContent = null;
        [SerializeField] ItemIconSlotView _ownedSlotOrigin = null;
        [SerializeField] ItemTooltipView _tooltip = null;
        [SerializeField] Button _rerollButton = null;
        [SerializeField] TMP_Text _rerollLabel = null;
        [SerializeField] Button _startButton = null;

        ObjectPool<ItemIconSlotView> _ownedSlotPool = null;
        List<ItemIconSlotView> _activeOwnedSlots = null;
        int _boundPassiveCount = 0;

        public override void OnGenerate()
        {
            _activeOwnedSlots = new List<ItemIconSlotView>();
            _ownedSlotPool = new ObjectPool<ItemIconSlotView>(_ownedSlotOrigin, CreateOwnedSlot, OWNED_SLOT_CAPACITY);

            _offerSlot0.Init(0, OnOfferClicked);
            _offerSlot1.Init(1, OnOfferClicked);
            _offerSlot2.Init(2, OnOfferClicked);
            _rerollButton.onClick.AddListener(OnRerollClicked);
            _startButton.onClick.AddListener(OnStartClicked);
            _tooltip.Hide();
        }

        public override void OnTerminate()
        {
            _offerSlot0.Release();
            _offerSlot1.Release();
            _offerSlot2.Release();
            _rerollButton.onClick.RemoveListener(OnRerollClicked);
            _startButton.onClick.RemoveListener(OnStartClicked);

            if (null != _ownedSlotPool)
            {
                _ownedSlotPool.Clear();
                _ownedSlotPool = null;
            }

            if (null != _activeOwnedSlots)
            {
                _activeOwnedSlots.Clear();
                _activeOwnedSlots = null;
            }
        }

        public override void OnReturn()
        {
            ClearOwnedSlots();
            _tooltip.Hide();
        }

        public override void Bind(UIViewArgs args)
        {
            Args mainArgs = args as Args;
            if (null != mainArgs)
            {
                BindMain(mainArgs);
                return;
            }

            OfferArgs offerArgs = args as OfferArgs;
            if (null != offerArgs)
            {
                BindOffer(offerArgs);
                return;
            }

            OwnedResetArgs ownedResetArgs = args as OwnedResetArgs;
            if (null != ownedResetArgs)
            {
                BindOwnedReset(ownedResetArgs);
                return;
            }

            OwnedItemArgs ownedItemArgs = args as OwnedItemArgs;
            if (null != ownedItemArgs)
                BindOwnedItem(ownedItemArgs);
        }

        void BindMain(Args args)
        {
            _moneyLabel.text = string.Format(MONEY_FORMAT, args.Money);
            _rerollLabel.text = string.Format(REROLL_FORMAT, args.RerollPrice);
        }

        void BindOffer(OfferArgs args)
        {
            ShopItemView slot = GetOfferSlot(args.SlotIndex);
            if (null == slot)
                return;

            ShopItemData item = GetItem(args.ItemIndex);
            if (null == item)
            {
                slot.BindSold();
                return;
            }

            slot.BindItem(new ShopItemView.BindParam(
                Kind: item.Kind,
                Title: item.GetDisplayName(),
                Icon: item.GetIcon(),
                Description: item.GetDescription(),
                PriceText: string.Format(PRICE_FORMAT, item.Price)));
        }

        void BindOwnedReset(OwnedResetArgs args)
        {
            _passiveHeaderLabel.text = string.Format(PASSIVE_HEADER_FORMAT, args.PassiveCount, args.PassiveSlotCount);
            _upgradeHeaderLabel.text = string.Format(UPGRADE_HEADER_FORMAT, args.UpgradeCount);
            ClearOwnedSlots();
            _tooltip.Hide();
            _boundPassiveCount = 0;

            int emptyCount = Mathf.Max(0, args.PassiveSlotCount - args.PassiveCount);
            for (int i = 0; i < emptyCount; ++i)
                BindEmptyPassiveSlot();
        }

        void BindEmptyPassiveSlot()
        {
            ItemIconSlotView slot = _ownedSlotPool.Get();
            if (null == slot)
                return;

            slot.transform.SetParent(_passiveContent, false);
            slot.transform.SetAsLastSibling();
            slot.BindEmpty();
            _activeOwnedSlots.Add(slot);
        }

        void BindOwnedItem(OwnedItemArgs args)
        {
            ShopItemData item = GetItem(args.ItemIndex);
            if (null == item)
                return;

            ItemIconSlotView slot = _ownedSlotPool.Get();
            if (null == slot)
                return;

            bool isPassive = item.Kind == ShopItemData.EItemKind.Passive;
            slot.transform.SetParent(isPassive ? _passiveContent : _upgradeContent, false);
            if (isPassive)
                slot.transform.SetSiblingIndex(_boundPassiveCount++);
            else
                slot.transform.SetAsLastSibling();

            slot.Bind(new ItemIconSlotView.BindParam(
                ItemIndex: args.ItemIndex,
                Icon: item.GetIcon(),
                CountText: GetCountText(args.Count),
                HoverCallback: OnTooltipHovered));
            _activeOwnedSlots.Add(slot);
        }

        void ClearOwnedSlots()
        {
            if (null == _activeOwnedSlots)
                return;

            for (int i = 0; i < _activeOwnedSlots.Count; ++i)
                _ownedSlotPool.Return(_activeOwnedSlots[i]);

            _activeOwnedSlots.Clear();
        }

        ItemIconSlotView CreateOwnedSlot(ItemIconSlotView origin)
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

            return string.Format(OWNED_COUNT_FORMAT, count);
        }

        ShopItemData GetItem(int itemIndex)
        {
            if (null == _catalog)
                return null;

            return _catalog.GetItem(itemIndex);
        }

        ShopItemView GetOfferSlot(int slotIndex)
        {
            switch (slotIndex)
            {
                case 0:
                    return _offerSlot0;
                case 1:
                    return _offerSlot1;
                case 2:
                    return _offerSlot2;
                default:
                    return null;
            }
        }

        void OnOfferClicked(int slotIndex)
        {
            UIManager.RaiseEvent(UIManager.EEvent.ShopBuyRequested, slotIndex);
        }

        void OnRerollClicked()
        {
            UIManager.RaiseEvent(UIManager.EEvent.ShopRerollRequested);
        }

        void OnStartClicked()
        {
            UIManager.Hide(UIViewCatalogData.EViewType.Shop);
            UIManager.RaiseEvent(UIManager.EEvent.ShopStartRequested);
        }
    }
}
