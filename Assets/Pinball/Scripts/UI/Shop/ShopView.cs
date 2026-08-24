using System.Collections.Generic;
using Pinball.Audio;
using Pinball.Ingame.Shop;
using Pinball.Ingame.UI;
using Pinball.Localization;
using Pinball.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class ShopView : UIViewBase
    {
        public record OfferEntry(LocalizedTextRef Title, Sprite Icon, LocalizedTextRef Description, int Price, bool IsPassive, ShopItemData.EPassiveCategory PassiveCategory);

        public record OwnedEntry(Sprite Icon, int Count, LocalizedTextRef Title, LocalizedTextRef Description, int SellPrice, ShopItemData.EPassiveCategory Category);

        public record PackEntry(LocalizedTextRef Title, Sprite Icon, LocalizedTextRef Description, int Price, Sprite BandSprite, bool CanBuy);

        public record VoucherEntry(LocalizedTextRef Title, Sprite Icon, LocalizedTextRef Description, int Price);

        public record PurchaseEntry(int SlotIndex, bool IsPassive, int OwnedIndex, int PreviousCount);

        public record UpgradeGrantEntry(int OwnedIndex, int PreviousCount);

        public record SellEntry(ShopItemData.EPassiveCategory Category, int RemovedSlotIndex, UpgradeGrantEntry Grant);

        public record Args(
            int Money,
            int RerollPrice,
            OfferEntry[] Offers,
            PurchaseEntry Purchase,
            SellEntry Sell,
            int BallPassiveSlotCount,
            int MapPassiveSlotCount,
            int CommonPassiveSlotCount,
            OwnedEntry[] Passives,
            OwnedEntry[] Upgrades,
            PackEntry[] Packs,
            VoucherEntry Voucher,
            int StageNumber,
            int TotalStageCount,
            int RoundNumber,
            int RoundCount,
            bool PlaysRerollReveal) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.Shop;
        }

        const string MONEY_FORMAT = "$ {0}";
        const string REROLL_FORMAT = "REROLL ${0}";
        const string REROLL_KEY = "ui.shop.reroll";
        const string PRICE_FORMAT = "${0}";
        const string OWNED_COUNT_FORMAT = "x{0}";
        const string ROUND_FORMAT = "ROUND {0} / {1}";
        const string ROUND_KEY = "ui.shop.round";
        const string STAGE_FORMAT = "STAGE {0} / {1}";
        const string STAGE_KEY = "ui.shop.stage";
        const float OFFER_ROW_WIDTH = 1071f;
        const float OFFER_ROW_HEIGHT = 338f;
        const float OFFER_SLOT_WIDTH = 380f;
        const float OFFER_SLOT_HEIGHT = 540f;
        const float OFFER_SLOT_WIDE_SPACING = 60f;
        const float OFFER_SLOT_TIGHT_SPACING = 30f;
        const int OFFER_SLOT_WIDE_COUNT = 3;
        const int STACKED_COUNT_MIN = 2;
        const string NOT_ENOUGH_MONEY_TEXT = "NOT ENOUGH MONEY";
        const string NOT_ENOUGH_MONEY_KEY = "ui.shop.not_enough_money";
        const string PASSIVE_SLOT_FULL_FORMAT = "{0} PASSIVE SLOTS FULL";
        const string PASSIVE_SLOT_FULL_KEY = "ui.shop.passive_slots_full";
        const float PASSIVE_SHAKE_STRENGTH = 18f;
        const float MONEY_SHAKE_STRENGTH = 24f;
        const float VIEWPORT_CENTER_RATIO = 0.5f;
        const string SELL_FORMAT = "SELL ${0}";
        const string SELL_KEY = "ui.shop.sell";
        const float SELL_BUTTON_GAP = 16f;
        const float SLOT_SHIFT_DURATION = 0.28f;
        const int NO_SLOT_INDEX = -1;
        const int PASSIVE_CATEGORY_COUNT = 3;
        const float PASSIVE_SLOT_WIDTH = 82f;
        const float PASSIVE_SLOT_SPACING = 7f;
        const float PASSIVE_GROUP_MAX_WIDTH = 260f;
        const float PASSIVE_GROUP_MAX_SCALE = 1f;
        const int UPGRADE_LIST_ROW_CAPACITY = 8;

        [SerializeField] TMP_Text _moneyLabel = null;
        [SerializeField] ShopItemView[] _offerSlots = null;
        [SerializeField] HorizontalLayoutGroup _offerRow = null;
        [SerializeField] ItemIconGridView[] _passiveGrids = null;
        [SerializeField] RectTransform[] _passiveGroupRoots = null;
        [SerializeField] ScrollRect _upgradeScroll = null;
        [SerializeField] ItemIconGridView _upgradeGrid = null;
        [SerializeField] Button _upgradeListButton = null;
        [SerializeField] RectTransform _upgradeListPanel = null;
        [SerializeField] RectTransform _upgradeListContent = null;
        [SerializeField] UpgradeListRowView _upgradeListRowOrigin = null;
        [SerializeField] RectTransform _sellButtonRoot = null;
        [SerializeField] Button _sellButton = null;
        [SerializeField] TMP_Text _sellLabel = null;
        [SerializeField] Button _rerollButton = null;
        [SerializeField] TMP_Text _rerollLabel = null;
        [SerializeField] Button _startButton = null;
        [SerializeField] ShopCardSlotView[] _packSlots = null;
        [SerializeField] ShopCardSlotView _voucherSlot = null;
        [SerializeField] Sprite _voucherBandSprite = null;
        [SerializeField] Button _menuButton = null;
        [SerializeField] TMP_Text _roundLabel = null;
        [SerializeField] TMP_Text _stageLabel = null;
        [SerializeField] RectTransform _moneyPlaque = null;
        [SerializeField] Image _moneyPlaqueImage = null;
        [SerializeField] Color _moneyDeniedColor = new Color(1f, 0.32f, 0.26f, 1f);
        [SerializeField] ScorePopupSpawner _messagePopup = null;

        SlotRevealAnimator _offerReveal = null;
        SlotRevealAnimator _packReveal = null;
        ShakeFeedback _moneyDenied = null;
        ShakeFeedback[] _passiveDeniedArr = null;
        Args _animatedArgs = null;
        Args _boundArgs = null;
        List<int>[] _passiveOwnedIndices = null;
        ObjectPool<UpgradeListRowView> _upgradeListRowPool = null;
        List<UpgradeListRowView> _upgradeListRows = null;
        int _selectedPassiveCategory = NO_SLOT_INDEX;
        int _selectedPassiveSlot = NO_SLOT_INDEX;
        int _sellingPassiveCategory = NO_SLOT_INDEX;
        int _sellingPassiveSlot = NO_SLOT_INDEX;
        Transform _lastRequestSource = null;

        public override void OnGenerate()
        {
            _passiveOwnedIndices = new List<int>[PASSIVE_CATEGORY_COUNT];
            for (int i = 0; i < PASSIVE_CATEGORY_COUNT; ++i)
                _passiveOwnedIndices[i] = new List<int>();

            for (int i = 0; i < _passiveGrids.Length; ++i)
            {
                int category = i;
                _passiveGrids[i].Init(slotIndex => OnPassiveSlotClicked(category, slotIndex));
            }

            _upgradeGrid.Init(null);
            _upgradeListRows = new List<UpgradeListRowView>();
            _upgradeListRowPool = new ObjectPool<UpgradeListRowView>(_upgradeListRowOrigin, CreateUpgradeListRow, UPGRADE_LIST_ROW_CAPACITY);
            _upgradeListButton.onClick.AddListener(OnUpgradeListToggleClicked);
            HideUpgradeListPanel();

            Transform[] offerSlotRoots = new Transform[_offerSlots.Length];
            for (int i = 0; i < _offerSlots.Length; ++i)
            {
                _offerSlots[i].Init(i, OnOfferClicked);
                offerSlotRoots[i] = _offerSlots[i].transform;
            }

            Transform[] packSlotRoots = new Transform[_packSlots.Length];
            for (int i = 0; i < _packSlots.Length; ++i)
            {
                _packSlots[i].Init(i, OnPackClicked);
                packSlotRoots[i] = _packSlots[i].transform;
            }

            _offerReveal = new SlotRevealAnimator(offerSlotRoots);
            _packReveal = new SlotRevealAnimator(packSlotRoots);
            _moneyDenied = new ShakeFeedback(_moneyPlaque, MONEY_SHAKE_STRENGTH, _moneyPlaqueImage, _moneyDeniedColor);
            _passiveDeniedArr = new ShakeFeedback[PASSIVE_CATEGORY_COUNT];
            for (int i = 0; i < PASSIVE_CATEGORY_COUNT; ++i)
                _passiveDeniedArr[i] = new ShakeFeedback(_passiveGroupRoots[i], PASSIVE_SHAKE_STRENGTH);

            _voucherSlot.Init(0, OnVoucherClicked);
            _rerollButton.onClick.AddListener(OnRerollClicked);
            _startButton.onClick.AddListener(OnStartClicked);
            _menuButton.onClick.AddListener(OnMenuClicked);
            _sellButton.onClick.AddListener(OnSellClicked);
            HideSellButton();
            _messagePopup.Init();
            UIManager.OnEvent += HandleUIEvent;
            UITooltip.Hide();
        }

        public override void OnTerminate()
        {
            UIManager.OnEvent -= HandleUIEvent;
            _offerReveal.Dispose();
            _offerReveal = null;
            _packReveal.Dispose();
            _packReveal = null;
            _moneyDenied.Dispose();
            _moneyDenied = null;
            for (int i = 0; i < _passiveDeniedArr.Length; ++i)
                _passiveDeniedArr[i].Dispose();

            _passiveDeniedArr = null;
            _messagePopup.Release();
            for (int i = 0; i < _offerSlots.Length; ++i)
                _offerSlots[i].Release();
            for (int i = 0; i < _packSlots.Length; ++i)
                _packSlots[i].Release();
            _voucherSlot.Release();
            _rerollButton.onClick.RemoveListener(OnRerollClicked);
            _startButton.onClick.RemoveListener(OnStartClicked);
            _menuButton.onClick.RemoveListener(OnMenuClicked);
            _sellButton.onClick.RemoveListener(OnSellClicked);
            _upgradeListButton.onClick.RemoveListener(OnUpgradeListToggleClicked);
            _upgradeListRowPool.Clear();
            _upgradeListRowPool = null;
            _upgradeListRows = null;
            for (int i = 0; i < _passiveGrids.Length; ++i)
                _passiveGrids[i].Release();
            _upgradeGrid.Release();
            _passiveOwnedIndices = null;
        }

        public override void OnReturn()
        {
            _offerReveal.Kill();
            _packReveal.Kill();
            _moneyDenied.Kill();
            for (int i = 0; i < _passiveDeniedArr.Length; ++i)
                _passiveDeniedArr[i].Kill();

            _animatedArgs = null;
            _boundArgs = null;
            _selectedPassiveCategory = NO_SLOT_INDEX;
            _selectedPassiveSlot = NO_SLOT_INDEX;
            _sellingPassiveCategory = NO_SLOT_INDEX;
            _sellingPassiveSlot = NO_SLOT_INDEX;
            HideSellButton();
            HideUpgradeListPanel();
            _lastRequestSource = null;
            for (int i = 0; i < _passiveGrids.Length; ++i)
                _passiveGrids[i].Clear();
            _upgradeGrid.Clear();
            UITooltip.Hide();
        }

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            _boundArgs = viewArgs;
            bool isReplay = ReferenceEquals(_animatedArgs, viewArgs);
            bool playsReveal = viewArgs.PlaysRerollReveal && !isReplay;
            PurchaseEntry purchase = isReplay ? null : viewArgs.Purchase;
            SellEntry sell = isReplay ? null : viewArgs.Sell;
            if (playsReveal || null != purchase || null != sell)
                _animatedArgs = viewArgs;

            _moneyLabel.text = string.Format(MONEY_FORMAT, viewArgs.Money);
            _rerollLabel.text = LocalizationTable.Format(REROLL_KEY, REROLL_FORMAT, viewArgs.RerollPrice);
            _roundLabel.text = LocalizationTable.Format(ROUND_KEY, ROUND_FORMAT, viewArgs.RoundNumber, viewArgs.RoundCount);
            _stageLabel.text = LocalizationTable.Format(STAGE_KEY, STAGE_FORMAT, viewArgs.StageNumber, viewArgs.TotalStageCount);
            BindOwned(viewArgs, sell);
            BindOfferSection(viewArgs.Offers, playsReveal, purchase);
            BindPackSection(viewArgs.Packs, playsReveal);
            BindVoucher(viewArgs.Voucher);
        }

        public bool TryBeginPurchaseFly(bool isPassive, int ownedIndex, int previousCount, out Vector3 position, out float width)
        {
            position = Vector3.zero;
            width = 0f;

            RectTransform iconRect = PrepareOwnedSlot(isPassive, ownedIndex);
            if (null == iconRect)
                return false;

            position = iconRect.TransformPoint(iconRect.rect.center);
            width = iconRect.rect.width * iconRect.lossyScale.x;
            PlayOwnedSlotChange(isPassive, ownedIndex, previousCount, ShopItemView.PURCHASE_FLY_DURATION);
            return true;
        }

        RectTransform PrepareOwnedSlot(bool isPassive, int ownedIndex)
        {
            ItemIconGridView grid = GetOwnedGrid(isPassive, ownedIndex, out int slotIndex);
            if (null == grid)
                return null;

            LayoutRebuilder.ForceRebuildLayoutImmediate(grid.GetContentRect());
            RectTransform iconRect = grid.GetSlotIconRect(slotIndex);
            if (null == iconRect)
                return null;

            if (!isPassive)
                ScrollUpgradeSlotIntoView(iconRect);

            return iconRect;
        }

        void PlayOwnedSlotChange(bool isPassive, int ownedIndex, int previousCount, float delay)
        {
            ItemIconGridView grid = GetOwnedGrid(isPassive, ownedIndex, out int slotIndex);
            if (null == grid)
                return;

            if (previousCount > 0)
                grid.PlaySlotCountChange(slotIndex, GetCountText(previousCount), delay);
            else
                grid.PlaySlotAppear(slotIndex, delay);
        }

        ItemIconGridView GetOwnedGrid(bool isPassive, int ownedIndex, out int slotIndex)
        {
            if (!isPassive)
            {
                slotIndex = ownedIndex;
                return _upgradeGrid;
            }

            if (!TryGetPassiveGridPosition(ownedIndex, out int category, out slotIndex))
                return null;

            return _passiveGrids[category];
        }

        void ScrollUpgradeSlotIntoView(RectTransform iconRect)
        {
            if (null == _upgradeScroll)
                return;

            RectTransform content = _upgradeScroll.content;
            RectTransform viewport = _upgradeScroll.viewport;
            if (null == content || null == viewport)
                return;

            float viewportWidth = viewport.rect.width;
            float scrollableWidth = content.rect.width - viewportWidth;
            if (scrollableWidth <= 0f)
                return;

            float slotOffset = content.InverseTransformPoint(iconRect.TransformPoint(iconRect.rect.center)).x - content.rect.xMin;
            _upgradeScroll.horizontalNormalizedPosition = Mathf.Clamp01((slotOffset - viewportWidth * VIEWPORT_CENTER_RATIO) / scrollableWidth);
        }

        void BindVoucher(VoucherEntry voucher)
        {
            if (null == voucher)
            {
                _voucherSlot.BindSold();
                return;
            }

            _voucherSlot.Bind(new ShopCardSlotView.BindParam(
                Title: voucher.Title.Resolve(),
                Icon: voucher.Icon,
                Description: voucher.Description.Resolve(),
                PriceText: string.Format(PRICE_FORMAT, voucher.Price),
                BandSprite: _voucherBandSprite,
                CanBuy: true));
        }

        void BindPacks(PackEntry[] packs)
        {
            int packCount = null == packs ? 0 : packs.Length;
            for (int i = 0; i < _packSlots.Length; ++i)
            {
                if (i >= packCount)
                {
                    _packSlots[i].Hide();
                    continue;
                }

                PackEntry pack = packs[i];
                if (null == pack)
                {
                    _packSlots[i].BindSold();
                    continue;
                }

                _packSlots[i].Bind(new ShopCardSlotView.BindParam(
                    Title: pack.Title.Resolve(),
                    Icon: pack.Icon,
                    Description: pack.Description.Resolve(),
                    PriceText: string.Format(PRICE_FORMAT, pack.Price),
                    BandSprite: pack.BandSprite,
                    CanBuy: pack.CanBuy));
            }
        }

        void BindOfferSection(OfferEntry[] offers, bool playsReveal, PurchaseEntry purchase)
        {
            if (playsReveal)
            {
                _offerReveal.Play(() => BindOffers(offers, null));
                return;
            }

            _offerReveal.Kill();
            BindOffers(offers, purchase);
        }

        void BindPackSection(PackEntry[] packs, bool playsReveal)
        {
            if (playsReveal)
            {
                _packReveal.Play(() => BindPacks(packs));
                return;
            }

            _packReveal.Kill();
            BindPacks(packs);
        }

        void BindOffers(OfferEntry[] offers, PurchaseEntry purchase)
        {
            int offerCount = null == offers ? 0 : offers.Length;
            ApplyOfferLayout(offerCount);

            for (int i = 0; i < offerCount; ++i)
                BindOffer(i, offers[i], purchase);
        }

        void BindOffer(int slotIndex, OfferEntry offer, PurchaseEntry purchase)
        {
            ShopItemView slot = GetOfferSlot(slotIndex);
            if (null == slot)
                return;

            if (null == offer)
            {
                if (!TryPlayPurchase(slot, slotIndex, purchase))
                    slot.BindSold();

                return;
            }

            slot.BindItem(new ShopItemView.BindParam(
                IsPassive: offer.IsPassive,
                PassiveCategory: offer.PassiveCategory,
                Title: offer.Title.Resolve(),
                Icon: offer.Icon,
                Description: offer.Description.Resolve(),
                PriceText: string.Format(PRICE_FORMAT, offer.Price),
                ShowPrice: true));
        }

        bool TryPlayPurchase(ShopItemView slot, int slotIndex, PurchaseEntry purchase)
        {
            if (null == purchase || purchase.SlotIndex != slotIndex)
                return false;

            if (!TryBeginPurchaseFly(purchase.IsPassive, purchase.OwnedIndex, purchase.PreviousCount, out Vector3 position, out float width))
                return false;

            slot.PlayPurchase(position, width, showsSoldStamp: true);
            return true;
        }

        void BindOwned(Args args, SellEntry sell)
        {
            ClearPassiveSelection();
            UITooltip.Hide();

            BindPassiveGrids(args);
            _upgradeGrid.Bind(BuildGridEntries(args.Upgrades), 0);
            BindUpgradeListRows(args.Upgrades);
            PlaySellShift(sell);
        }

        void BindPassiveGrids(Args args)
        {
            for (int i = 0; i < _passiveOwnedIndices.Length; ++i)
                _passiveOwnedIndices[i].Clear();

            int passiveCount = null == args.Passives ? 0 : args.Passives.Length;
            for (int i = 0; i < passiveCount; ++i)
            {
                OwnedEntry entry = args.Passives[i];
                if (null == entry)
                    continue;

                _passiveOwnedIndices[(int)entry.Category].Add(i);
            }

            for (int category = 0; category < _passiveGrids.Length; ++category)
                BindPassiveGrid(category, args);
        }

        void BindPassiveGrid(int category, Args args)
        {
            List<int> ownedIndices = _passiveOwnedIndices[category];
            ItemIconGridView.Entry[] entries = new ItemIconGridView.Entry[ownedIndices.Count];
            for (int i = 0; i < ownedIndices.Count; ++i)
            {
                OwnedEntry item = args.Passives[ownedIndices[i]];
                entries[i] = new ItemIconGridView.Entry(item.Icon, GetCountText(item.Count), item.Title, item.Description);
            }

            int slotCount = GetPassiveSlotCount(args, category);
            _passiveGrids[category].Bind(entries, Mathf.Max(0, slotCount - entries.Length));
            SlotRowLayout.FitWidth(
                _passiveGrids[category].GetContentRect(),
                Mathf.Max(slotCount, entries.Length),
                PASSIVE_SLOT_WIDTH,
                PASSIVE_SLOT_SPACING,
                PASSIVE_GROUP_MAX_WIDTH,
                PASSIVE_GROUP_MAX_SCALE);
        }

        static int GetPassiveSlotCount(Args args, int category)
        {
            switch ((ShopItemData.EPassiveCategory)category)
            {
                case ShopItemData.EPassiveCategory.Ball:
                    return args.BallPassiveSlotCount;
                case ShopItemData.EPassiveCategory.Map:
                    return args.MapPassiveSlotCount;
                default:
                    return args.CommonPassiveSlotCount;
            }
        }

        void BindUpgradeListRows(OwnedEntry[] upgrades)
        {
            ReturnUpgradeListRows();

            int upgradeCount = null == upgrades ? 0 : upgrades.Length;
            for (int i = 0; i < upgradeCount; ++i)
            {
                OwnedEntry entry = upgrades[i];
                if (null == entry)
                    continue;

                UpgradeListRowView row = _upgradeListRowPool.Get();
                row.transform.SetParent(_upgradeListContent, false);
                row.transform.SetAsLastSibling();
                row.Bind(new UpgradeListRowView.BindParam(
                    Icon: entry.Icon,
                    Name: entry.Title.Resolve(),
                    CountText: string.Format(OWNED_COUNT_FORMAT, entry.Count)));
                _upgradeListRows.Add(row);
            }

            _upgradeListButton.gameObject.SetActive(upgradeCount > 0);
            if (upgradeCount <= 0)
                HideUpgradeListPanel();
        }

        void ReturnUpgradeListRows()
        {
            for (int i = 0; i < _upgradeListRows.Count; ++i)
                _upgradeListRowPool.Return(_upgradeListRows[i]);

            _upgradeListRows.Clear();
        }

        UpgradeListRowView CreateUpgradeListRow(UpgradeListRowView origin)
        {
            return Instantiate(origin, _upgradeListContent);
        }

        void OnUpgradeListToggleClicked()
        {
            AudioManager.Play(AudioStorage.EAudioClipType.UIButtonClick);
            if (_upgradeListPanel.gameObject.activeSelf)
            {
                HideUpgradeListPanel();
                return;
            }

            _upgradeListPanel.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_upgradeListPanel);
        }

        void HideUpgradeListPanel()
        {
            _upgradeListPanel.gameObject.SetActive(false);
        }

        void PlaySellShift(SellEntry sell)
        {
            if (null == sell)
                return;

            int category = (int)sell.Category;
            int categoryCount = _passiveOwnedIndices[category].Count;
            _passiveGrids[category].PlayIconShift(sell.RemovedSlotIndex, categoryCount - 1, SLOT_SHIFT_DURATION);
            PlaySellGrant(sell.Grant);
        }

        void PlaySellGrant(UpgradeGrantEntry grant)
        {
            if (null == grant)
                return;

            if (null == PrepareOwnedSlot(false, grant.OwnedIndex))
                return;

            PlayOwnedSlotChange(false, grant.OwnedIndex, grant.PreviousCount, SLOT_SHIFT_DURATION);
        }

        void OnPassiveSlotClicked(int category, int slotIndex)
        {
            if (_sellingPassiveSlot >= 0)
                return;

            AudioManager.Play(AudioStorage.EAudioClipType.UIButtonClick);

            if (category == _selectedPassiveCategory && slotIndex == _selectedPassiveSlot)
            {
                ClearPassiveSelection();
                return;
            }

            SelectPassive(category, slotIndex);
        }

        void SelectPassive(int category, int slotIndex)
        {
            int sellPrice = GetPassiveSellPrice(category, slotIndex);
            if (sellPrice <= 0)
            {
                ClearPassiveSelection();
                return;
            }

            _selectedPassiveCategory = category;
            _selectedPassiveSlot = slotIndex;
            for (int i = 0; i < _passiveGrids.Length; ++i)
                _passiveGrids[i].SetSelectedSlot(i == category ? slotIndex : NO_SLOT_INDEX);

            _sellLabel.text = LocalizationTable.Format(SELL_KEY, SELL_FORMAT, sellPrice);
            _sellButtonRoot.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_passiveGrids[category].GetContentRect());
            PlaceSellButton(category, slotIndex);
        }

        int GetPassiveOwnedIndex(int category, int slotIndex)
        {
            if (null == _passiveOwnedIndices)
                return NO_SLOT_INDEX;

            if (category < 0 || category >= _passiveOwnedIndices.Length)
                return NO_SLOT_INDEX;

            List<int> ownedIndices = _passiveOwnedIndices[category];
            if (slotIndex < 0 || slotIndex >= ownedIndices.Count)
                return NO_SLOT_INDEX;

            return ownedIndices[slotIndex];
        }

        bool TryGetPassiveGridPosition(int ownedIndex, out int category, out int slotIndex)
        {
            for (int i = 0; i < _passiveOwnedIndices.Length; ++i)
            {
                int foundIndex = _passiveOwnedIndices[i].IndexOf(ownedIndex);
                if (foundIndex < 0)
                    continue;

                category = i;
                slotIndex = foundIndex;
                return true;
            }

            category = NO_SLOT_INDEX;
            slotIndex = NO_SLOT_INDEX;
            return false;
        }

        int GetPassiveSellPrice(int category, int slotIndex)
        {
            int ownedIndex = GetPassiveOwnedIndex(category, slotIndex);
            if (ownedIndex < 0 || null == _boundArgs || null == _boundArgs.Passives)
                return 0;

            OwnedEntry entry = _boundArgs.Passives[ownedIndex];
            if (null == entry)
                return 0;

            return entry.SellPrice;
        }

        void PlaceSellButton(int category, int slotIndex)
        {
            RectTransform slotRect = _passiveGrids[category].GetSlotRect(slotIndex);
            if (null == slotRect)
                return;

            Rect rect = slotRect.rect;
            _sellButtonRoot.position = slotRect.TransformPoint(new Vector3(rect.xMax, rect.center.y, 0f));
            _sellButtonRoot.anchoredPosition += new Vector2(SELL_BUTTON_GAP, 0f);
        }

        void ClearPassiveSelection()
        {
            _selectedPassiveCategory = NO_SLOT_INDEX;
            _selectedPassiveSlot = NO_SLOT_INDEX;
            for (int i = 0; i < _passiveGrids.Length; ++i)
                _passiveGrids[i].SetSelectedSlot(NO_SLOT_INDEX);

            HideSellButton();
        }

        void HideSellButton()
        {
            _sellButtonRoot.gameObject.SetActive(false);
        }

        void OnSellClicked()
        {
            if (_sellingPassiveSlot >= 0)
                return;

            if (_selectedPassiveSlot < 0)
                return;

            _sellingPassiveCategory = _selectedPassiveCategory;
            _sellingPassiveSlot = _selectedPassiveSlot;
            _selectedPassiveCategory = NO_SLOT_INDEX;
            _selectedPassiveSlot = NO_SLOT_INDEX;
            HideSellButton();
            _passiveGrids[_sellingPassiveCategory].PlaySlotFadeOut(_sellingPassiveSlot, OnSellFadeCompleted);
        }

        void OnSellFadeCompleted()
        {
            int ownedIndex = GetPassiveOwnedIndex(_sellingPassiveCategory, _sellingPassiveSlot);
            _sellingPassiveCategory = NO_SLOT_INDEX;
            _sellingPassiveSlot = NO_SLOT_INDEX;
            if (ownedIndex < 0)
                return;

            UIManager.RaiseEvent(UIManager.EEvent.ShopSellRequested, ownedIndex);
        }

        static ItemIconGridView.Entry[] BuildGridEntries(OwnedEntry[] owned)
        {
            if (null == owned)
                return null;

            ItemIconGridView.Entry[] entries = new ItemIconGridView.Entry[owned.Length];
            for (int i = 0; i < owned.Length; ++i)
            {
                OwnedEntry item = owned[i];
                if (null == item)
                    continue;

                entries[i] = new ItemIconGridView.Entry(item.Icon, GetCountText(item.Count), item.Title, item.Description);
            }

            return entries;
        }

        static string GetCountText(int count)
        {
            if (count < STACKED_COUNT_MIN)
                return string.Empty;

            return string.Format(OWNED_COUNT_FORMAT, count);
        }

        ShopItemView GetOfferSlot(int slotIndex)
        {
            if (null == _offerSlots)
                return null;

            if (slotIndex < 0 || slotIndex >= _offerSlots.Length)
                return null;

            return _offerSlots[slotIndex];
        }

        void ApplyOfferLayout(int visibleCount)
        {
            int shownCount = Mathf.Clamp(visibleCount, 0, _offerSlots.Length);
            for (int i = 0; i < _offerSlots.Length; ++i)
                _offerSlots[i].gameObject.SetActive(i < shownCount);

            if (shownCount <= 0)
                return;

            float spacing = shownCount > OFFER_SLOT_WIDE_COUNT ? OFFER_SLOT_TIGHT_SPACING : OFFER_SLOT_WIDE_SPACING;
            SlotRowLayout.Fit(_offerRow, shownCount, OFFER_SLOT_WIDTH, spacing, OFFER_ROW_WIDTH, OFFER_ROW_HEIGHT / OFFER_SLOT_HEIGHT);
        }

        void HandleUIEvent(UIManager.EEvent uiEvent, int payload)
        {
            switch (uiEvent)
            {
                case UIManager.EEvent.ShopPurchaseDenied:
                    _messagePopup.PlayTextAt(LocalizationTable.Get(NOT_ENOUGH_MONEY_KEY, NOT_ENOUGH_MONEY_TEXT), GetRequestSourcePosition());
                    _moneyDenied.Play();
                    break;
                case UIManager.EEvent.ShopPassiveSlotFull:
                    _messagePopup.PlayTextAt(LocalizationTable.Format(PASSIVE_SLOT_FULL_KEY, PASSIVE_SLOT_FULL_FORMAT, PassiveCategoryLabel.Get((ShopItemData.EPassiveCategory)payload)), GetRequestSourcePosition());
                    PlayPassiveDenied(payload);
                    break;
            }
        }

        void PlayPassiveDenied(int categoryIndex)
        {
            if (categoryIndex < 0 || categoryIndex >= _passiveDeniedArr.Length)
                return;

            _passiveDeniedArr[categoryIndex].Play();
        }

        Vector3 GetRequestSourcePosition()
        {
            if (null == _lastRequestSource)
                return _moneyPlaque.position;

            return _lastRequestSource.position;
        }

        void OnOfferClicked(int slotIndex)
        {
            _lastRequestSource = _offerSlots[slotIndex].transform;
            UIManager.RaiseEvent(UIManager.EEvent.ShopBuyRequested, slotIndex);
        }

        void OnPackClicked(int slotIndex)
        {
            _lastRequestSource = _packSlots[slotIndex].transform;
            UIManager.RaiseEvent(UIManager.EEvent.ShopPackBuyRequested, slotIndex);
        }

        void OnVoucherClicked(int slotIndex)
        {
            _lastRequestSource = _voucherSlot.transform;
            UIManager.RaiseEvent(UIManager.EEvent.ShopVoucherBuyRequested);
        }

        void OnRerollClicked()
        {
            _lastRequestSource = _rerollButton.transform;
            UIManager.RaiseEvent(UIManager.EEvent.ShopRerollRequested);
        }

        void OnStartClicked()
        {
            UIManager.Hide(UIViewCatalogData.EViewType.Shop);
            UIManager.RaiseEvent(UIManager.EEvent.ShopStartRequested);
        }

        void OnMenuClicked()
        {
            UIManager.Show(new PausePopupView.Args());
        }
    }
}
