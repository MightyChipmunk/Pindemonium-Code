using Pinball.Audio;
using Pinball.Ingame.UI;
using Pinball.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class ShopView : UIViewBase
    {
        public record OfferEntry(string TitleKey, Sprite Icon, string DescriptionKey, int Price, bool IsPassive);

        public record OwnedEntry(Sprite Icon, int Count, string TitleKey, string DescriptionKey, int SellPrice);

        public record PackEntry(string TitleKey, Sprite Icon, string DescriptionKey, int Price, Sprite BandSprite, bool CanBuy);

        public record VoucherEntry(string TitleKey, Sprite Icon, string DescriptionKey, int Price);

        public record PurchaseEntry(int SlotIndex, bool IsPassive, int OwnedIndex, int PreviousCount);

        public record UpgradeGrantEntry(int OwnedIndex, int PreviousCount);

        public record SellEntry(int RemovedIndex, UpgradeGrantEntry Grant);

        public record Args(
            int Money,
            int RerollPrice,
            OfferEntry[] Offers,
            PurchaseEntry Purchase,
            SellEntry Sell,
            int PassiveSlotCount,
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
        const string PASSIVE_HEADER_FORMAT = "PASSIVE  {0}/{1}";
        const string PASSIVE_HEADER_KEY = "ui.shop.passive_header";
        const string UPGRADE_HEADER_FORMAT = "UPGRADE  {0}";
        const string UPGRADE_HEADER_KEY = "ui.shop.upgrade_header";
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
        const string PASSIVE_SLOT_FULL_TEXT = "PASSIVE SLOTS FULL";
        const string PASSIVE_SLOT_FULL_KEY = "ui.shop.passive_slots_full";
        const float PASSIVE_SHAKE_STRENGTH = 18f;
        const float MONEY_SHAKE_STRENGTH = 24f;
        const float VIEWPORT_CENTER_RATIO = 0.5f;
        const string SELL_FORMAT = "SELL ${0}";
        const string SELL_KEY = "ui.shop.sell";
        const float SELL_BUTTON_GAP = 16f;
        const float SLOT_SHIFT_DURATION = 0.28f;
        const int NO_SLOT_INDEX = -1;

        [SerializeField] TMP_Text _moneyLabel = null;
        [SerializeField] ShopItemView[] _offerSlots = null;
        [SerializeField] HorizontalLayoutGroup _offerRow = null;
        [SerializeField] TMP_Text _passiveHeaderLabel = null;
        [SerializeField] TMP_Text _upgradeHeaderLabel = null;
        [SerializeField] ItemIconGridView _passiveGrid = null;
        [SerializeField] RectTransform _passiveSlotRoot = null;
        [SerializeField] RectTransform _upgradeSlotRoot = null;
        [SerializeField] ScrollRect _upgradeScroll = null;
        [SerializeField] ItemIconGridView _upgradeGrid = null;
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
        ShakeFeedback _passiveDenied = null;
        Args _animatedArgs = null;
        Args _boundArgs = null;
        int _selectedPassiveIndex = NO_SLOT_INDEX;
        int _sellingPassiveIndex = NO_SLOT_INDEX;
        Transform _lastRequestSource = null;

        public override void OnGenerate()
        {
            _passiveGrid.Init(OnPassiveSlotClicked);
            _upgradeGrid.Init(null);

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
            _passiveDenied = new ShakeFeedback(_passiveSlotRoot, PASSIVE_SHAKE_STRENGTH);

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
            _passiveDenied.Dispose();
            _passiveDenied = null;
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
            _passiveGrid.Release();
            _upgradeGrid.Release();
        }

        public override void OnReturn()
        {
            _offerReveal.Kill();
            _packReveal.Kill();
            _moneyDenied.Kill();
            _passiveDenied.Kill();
            _animatedArgs = null;
            _boundArgs = null;
            _selectedPassiveIndex = NO_SLOT_INDEX;
            _sellingPassiveIndex = NO_SLOT_INDEX;
            HideSellButton();
            _lastRequestSource = null;
            _passiveGrid.Clear();
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
            RectTransform slotRoot = isPassive ? _passiveSlotRoot : _upgradeSlotRoot;
            LayoutRebuilder.ForceRebuildLayoutImmediate(slotRoot);

            ItemIconGridView grid = isPassive ? _passiveGrid : _upgradeGrid;
            RectTransform iconRect = grid.GetSlotIconRect(ownedIndex);
            if (null == iconRect)
                return null;

            if (!isPassive)
                ScrollUpgradeSlotIntoView(iconRect);

            return iconRect;
        }

        void PlayOwnedSlotChange(bool isPassive, int ownedIndex, int previousCount, float delay)
        {
            ItemIconGridView grid = isPassive ? _passiveGrid : _upgradeGrid;
            if (previousCount > 0)
                grid.PlaySlotCountChange(ownedIndex, GetCountText(previousCount), delay);
            else
                grid.PlaySlotAppear(ownedIndex, delay);
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
                Title: LocalizationTable.GetOrKey(voucher.TitleKey),
                Icon: voucher.Icon,
                Description: LocalizationTable.GetOrKey(voucher.DescriptionKey),
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
                    Title: LocalizationTable.GetOrKey(pack.TitleKey),
                    Icon: pack.Icon,
                    Description: LocalizationTable.GetOrKey(pack.DescriptionKey),
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
                Title: LocalizationTable.GetOrKey(offer.TitleKey),
                Icon: offer.Icon,
                Description: LocalizationTable.GetOrKey(offer.DescriptionKey),
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

            int passiveCount = null == args.Passives ? 0 : args.Passives.Length;
            int upgradeCount = null == args.Upgrades ? 0 : args.Upgrades.Length;
            _passiveHeaderLabel.text = LocalizationTable.Format(PASSIVE_HEADER_KEY, PASSIVE_HEADER_FORMAT, passiveCount, args.PassiveSlotCount);
            _upgradeHeaderLabel.text = LocalizationTable.Format(UPGRADE_HEADER_KEY, UPGRADE_HEADER_FORMAT, upgradeCount);
            UITooltip.Hide();

            _passiveGrid.Bind(BuildGridEntries(args.Passives), Mathf.Max(0, args.PassiveSlotCount - passiveCount));
            _upgradeGrid.Bind(BuildGridEntries(args.Upgrades), 0);
            PlaySellShift(sell, passiveCount);
        }

        void PlaySellShift(SellEntry sell, int passiveCount)
        {
            if (null == sell)
                return;

            _passiveGrid.PlayIconShift(sell.RemovedIndex, passiveCount - 1, SLOT_SHIFT_DURATION);
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

        void OnPassiveSlotClicked(int slotIndex)
        {
            if (_sellingPassiveIndex >= 0)
                return;

            AudioManager.Play(AudioStorage.EAudioClipType.UIButtonClick);

            if (slotIndex == _selectedPassiveIndex)
            {
                ClearPassiveSelection();
                return;
            }

            SelectPassive(slotIndex);
        }

        void SelectPassive(int slotIndex)
        {
            int sellPrice = GetPassiveSellPrice(slotIndex);
            if (sellPrice <= 0)
            {
                ClearPassiveSelection();
                return;
            }

            _selectedPassiveIndex = slotIndex;
            _passiveGrid.SetSelectedSlot(slotIndex);
            _sellLabel.text = LocalizationTable.Format(SELL_KEY, SELL_FORMAT, sellPrice);
            _sellButtonRoot.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_passiveSlotRoot);
            PlaceSellButton(slotIndex);
        }

        int GetPassiveSellPrice(int slotIndex)
        {
            if (null == _boundArgs || null == _boundArgs.Passives)
                return 0;

            if (slotIndex < 0 || slotIndex >= _boundArgs.Passives.Length)
                return 0;

            OwnedEntry entry = _boundArgs.Passives[slotIndex];
            if (null == entry)
                return 0;

            return entry.SellPrice;
        }

        void PlaceSellButton(int slotIndex)
        {
            RectTransform slotRect = _passiveGrid.GetSlotRect(slotIndex);
            if (null == slotRect)
                return;

            Rect rect = slotRect.rect;
            _sellButtonRoot.position = slotRect.TransformPoint(new Vector3(rect.xMax, rect.center.y, 0f));
            _sellButtonRoot.anchoredPosition += new Vector2(SELL_BUTTON_GAP, 0f);
        }

        void ClearPassiveSelection()
        {
            _selectedPassiveIndex = NO_SLOT_INDEX;
            _passiveGrid.SetSelectedSlot(NO_SLOT_INDEX);
            HideSellButton();
        }

        void HideSellButton()
        {
            _sellButtonRoot.gameObject.SetActive(false);
        }

        void OnSellClicked()
        {
            if (_sellingPassiveIndex >= 0)
                return;

            if (_selectedPassiveIndex < 0)
                return;

            _sellingPassiveIndex = _selectedPassiveIndex;
            _selectedPassiveIndex = NO_SLOT_INDEX;
            HideSellButton();
            _passiveGrid.PlaySlotFadeOut(_sellingPassiveIndex, OnSellFadeCompleted);
        }

        void OnSellFadeCompleted()
        {
            int slotIndex = _sellingPassiveIndex;
            _sellingPassiveIndex = NO_SLOT_INDEX;
            if (slotIndex < 0)
                return;

            UIManager.RaiseEvent(UIManager.EEvent.ShopSellRequested, slotIndex);
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

                entries[i] = new ItemIconGridView.Entry(item.Icon, GetCountText(item.Count), item.TitleKey, item.DescriptionKey);
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
                    _messagePopup.PlayTextAt(LocalizationTable.Get(PASSIVE_SLOT_FULL_KEY, PASSIVE_SLOT_FULL_TEXT), GetRequestSourcePosition());
                    _passiveDenied.Play();
                    break;
            }
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
