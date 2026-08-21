using DG.Tweening;
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

        public record Args(
            int Money,
            int RerollPrice,
            OfferEntry[] Offers,
            PurchaseEntry Purchase,
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
        const float PASSIVE_SHAKE_DURATION = 0.42f;
        const float PASSIVE_SHAKE_STRENGTH = 18f;
        const int PASSIVE_SHAKE_VIBRATO = 16;
        const float PASSIVE_SHAKE_RANDOMNESS = 0f;
        const float VIEWPORT_CENTER_RATIO = 0.5f;
        const float MONEY_SHAKE_DURATION = 0.42f;
        const float MONEY_SHAKE_STRENGTH = 24f;
        const int MONEY_SHAKE_VIBRATO = 16;
        const float MONEY_SHAKE_RANDOMNESS = 0f;
        const float MONEY_COLOR_DURATION = 0.21f;
        const int MONEY_COLOR_LOOPS = 2;
        const float REVEAL_HIDE_DURATION = 0.14f;
        const float REVEAL_HIDE_STAGGER = 0.05f;
        const float REVEAL_SHOW_DURATION = 0.26f;
        const float REVEAL_SHOW_STAGGER = 0.07f;
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
        [SerializeField] ItemTooltipView _tooltip = null;
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

        Tween _offerRevealTween = null;
        Tween _packRevealTween = null;
        Tween _moneyDeniedTween = null;
        Tween _passiveDeniedTween = null;
        OfferEntry[] _pendingOffers = null;
        PackEntry[] _pendingPacks = null;
        Transform[] _offerSlotRoots = null;
        Transform[] _packSlotRoots = null;
        Vector3[] _offerSlotBaseScales = null;
        Vector3[] _packSlotBaseScales = null;
        Args _animatedArgs = null;
        Args _boundArgs = null;
        int _selectedPassiveIndex = NO_SLOT_INDEX;
        int _sellingPassiveIndex = NO_SLOT_INDEX;
        int _pendingShiftIndex = NO_SLOT_INDEX;
        Transform _lastRequestSource = null;
        Vector2 _moneyPlaqueBasePosition = Vector2.zero;
        Vector2 _passiveSlotBasePosition = Vector2.zero;
        Color _moneyPlaqueBaseColor = Color.white;

        public override void OnGenerate()
        {
            _passiveGrid.Init(_tooltip, OnPassiveSlotClicked);
            _upgradeGrid.Init(_tooltip, null);

            _offerSlotRoots = new Transform[_offerSlots.Length];
            _packSlotRoots = new Transform[_packSlots.Length];
            _offerSlotBaseScales = new Vector3[_offerSlots.Length];
            _packSlotBaseScales = new Vector3[_packSlots.Length];
            for (int i = 0; i < _offerSlots.Length; ++i)
            {
                _offerSlots[i].Init(i, OnOfferClicked);
                _offerSlotRoots[i] = _offerSlots[i].transform;
                _offerSlotBaseScales[i] = _offerSlotRoots[i].localScale;
            }

            for (int i = 0; i < _packSlots.Length; ++i)
            {
                _packSlots[i].Init(i, OnPackClicked);
                _packSlotRoots[i] = _packSlots[i].transform;
                _packSlotBaseScales[i] = _packSlotRoots[i].localScale;
            }

            _voucherSlot.Init(0, OnVoucherClicked);
            _rerollButton.onClick.AddListener(OnRerollClicked);
            _startButton.onClick.AddListener(OnStartClicked);
            _menuButton.onClick.AddListener(OnMenuClicked);
            _sellButton.onClick.AddListener(OnSellClicked);
            HideSellButton();
            _messagePopup.Init();
            _moneyPlaqueBasePosition = _moneyPlaque.anchoredPosition;
            _passiveSlotBasePosition = _passiveSlotRoot.anchoredPosition;
            _moneyPlaqueBaseColor = _moneyPlaqueImage.color;
            UIManager.OnEvent += HandleUIEvent;
            _tooltip.Hide();
        }

        public override void OnTerminate()
        {
            UIManager.OnEvent -= HandleUIEvent;
            KillOfferReveal();
            KillPackReveal();
            KillMoneyDenied();
            KillPassiveDenied();
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
            _offerSlotRoots = null;
            _packSlotRoots = null;
            _offerSlotBaseScales = null;
            _packSlotBaseScales = null;
        }

        public override void OnReturn()
        {
            KillOfferReveal();
            KillPackReveal();
            KillMoneyDenied();
            KillPassiveDenied();
            _animatedArgs = null;
            _boundArgs = null;
            _selectedPassiveIndex = NO_SLOT_INDEX;
            _sellingPassiveIndex = NO_SLOT_INDEX;
            _pendingShiftIndex = NO_SLOT_INDEX;
            HideSellButton();
            _lastRequestSource = null;
            _passiveGrid.Clear();
            _upgradeGrid.Clear();
            _tooltip.Hide();
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
            if (playsReveal || null != purchase)
                _animatedArgs = viewArgs;

            _moneyLabel.text = string.Format(MONEY_FORMAT, viewArgs.Money);
            _rerollLabel.text = LocalizationTable.Format(REROLL_KEY, REROLL_FORMAT, viewArgs.RerollPrice);
            _roundLabel.text = LocalizationTable.Format(ROUND_KEY, ROUND_FORMAT, viewArgs.RoundNumber, viewArgs.RoundCount);
            _stageLabel.text = LocalizationTable.Format(STAGE_KEY, STAGE_FORMAT, viewArgs.StageNumber, viewArgs.TotalStageCount);
            BindOwned(viewArgs);
            BindOfferSection(viewArgs.Offers, playsReveal, purchase);
            BindPackSection(viewArgs.Packs, playsReveal);
            BindVoucher(viewArgs.Voucher);
        }

        public bool TryBeginPurchaseFly(bool isPassive, int ownedIndex, int previousCount, out Vector3 position, out float width)
        {
            position = Vector3.zero;
            width = 0f;

            RectTransform slotRoot = isPassive ? _passiveSlotRoot : _upgradeSlotRoot;
            LayoutRebuilder.ForceRebuildLayoutImmediate(slotRoot);

            ItemIconGridView grid = isPassive ? _passiveGrid : _upgradeGrid;
            RectTransform iconRect = grid.GetSlotIconRect(ownedIndex);
            if (null == iconRect)
                return false;

            if (!isPassive)
                ScrollUpgradeSlotIntoView(iconRect);

            position = iconRect.TransformPoint(iconRect.rect.center);
            width = iconRect.rect.width * iconRect.lossyScale.x;

            if (previousCount > 0)
                grid.PlaySlotCountChange(ownedIndex, GetCountText(previousCount), ShopItemView.PURCHASE_FLY_DURATION);
            else
                grid.PlaySlotAppear(ownedIndex, ShopItemView.PURCHASE_FLY_DURATION);

            return true;
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
                PlayOfferReveal(offers);
                return;
            }

            KillOfferReveal();
            BindOffers(offers, purchase);
        }

        void BindPackSection(PackEntry[] packs, bool playsReveal)
        {
            if (playsReveal)
            {
                PlayPackReveal(packs);
                return;
            }

            KillPackReveal();
            BindPacks(packs);
        }

        void PlayOfferReveal(OfferEntry[] offers)
        {
            KillOfferReveal();
            _pendingOffers = offers;

            Sequence sequence = BuildRevealSequence(_offerSlotRoots, _offerSlotBaseScales, BindPendingOffers);
            sequence.OnComplete(OnOfferRevealCompleted);
            _offerRevealTween = sequence;
        }

        void PlayPackReveal(PackEntry[] packs)
        {
            KillPackReveal();
            _pendingPacks = packs;

            Sequence sequence = BuildRevealSequence(_packSlotRoots, _packSlotBaseScales, BindPendingPacks);
            sequence.OnComplete(OnPackRevealCompleted);
            _packRevealTween = sequence;
        }

        static Sequence BuildRevealSequence(Transform[] slotRoots, Vector3[] baseScales, TweenCallback bindCallback)
        {
            Sequence sequence = DOTween.Sequence();
            float hideEndTime = 0f;
            for (int i = 0; i < slotRoots.Length; ++i)
            {
                if (!slotRoots[i].gameObject.activeSelf)
                    continue;

                float hideStartTime = i * REVEAL_HIDE_STAGGER;
                sequence.Insert(hideStartTime, slotRoots[i].DOScale(Vector3.zero, REVEAL_HIDE_DURATION).SetEase(Ease.InBack));
                hideEndTime = Mathf.Max(hideEndTime, hideStartTime + REVEAL_HIDE_DURATION);
            }

            sequence.InsertCallback(hideEndTime, bindCallback);

            for (int i = 0; i < slotRoots.Length; ++i)
            {
                Tween showTween = slotRoots[i].DOScale(baseScales[i], REVEAL_SHOW_DURATION).From(Vector3.zero, false).SetEase(Ease.OutBack);
                sequence.Insert(hideEndTime + i * REVEAL_SHOW_STAGGER, showTween);
            }

            sequence.SetUpdate(true);
            return sequence;
        }

        void BindPendingOffers()
        {
            if (null == _pendingOffers)
                return;

            BindOffers(_pendingOffers, null);
            _pendingOffers = null;
        }

        void BindPendingPacks()
        {
            if (null == _pendingPacks)
                return;

            BindPacks(_pendingPacks);
            _pendingPacks = null;
        }

        void OnOfferRevealCompleted()
        {
            _offerRevealTween = null;
        }

        void OnPackRevealCompleted()
        {
            _packRevealTween = null;
        }

        void KillOfferReveal()
        {
            _pendingOffers = null;
            if (null == _offerRevealTween)
                return;

            _offerRevealTween.Kill();
            _offerRevealTween = null;
            ResetSlotScales(_offerSlotRoots, _offerSlotBaseScales);
        }

        void KillPackReveal()
        {
            _pendingPacks = null;
            if (null == _packRevealTween)
                return;

            _packRevealTween.Kill();
            _packRevealTween = null;
            ResetSlotScales(_packSlotRoots, _packSlotBaseScales);
        }

        static void ResetSlotScales(Transform[] slotRoots, Vector3[] baseScales)
        {
            if (null == slotRoots)
                return;

            for (int i = 0; i < slotRoots.Length; ++i)
                slotRoots[i].localScale = baseScales[i];
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

        void BindOwned(Args args)
        {
            ClearPassiveSelection();

            int passiveCount = null == args.Passives ? 0 : args.Passives.Length;
            int upgradeCount = null == args.Upgrades ? 0 : args.Upgrades.Length;
            _passiveHeaderLabel.text = LocalizationTable.Format(PASSIVE_HEADER_KEY, PASSIVE_HEADER_FORMAT, passiveCount, args.PassiveSlotCount);
            _upgradeHeaderLabel.text = LocalizationTable.Format(UPGRADE_HEADER_KEY, UPGRADE_HEADER_FORMAT, upgradeCount);
            _tooltip.Hide();

            _passiveGrid.Bind(BuildGridEntries(args.Passives), Mathf.Max(0, args.PassiveSlotCount - passiveCount));
            _upgradeGrid.Bind(BuildGridEntries(args.Upgrades), 0);
            PlayPendingShift(passiveCount);
        }

        void PlayPendingShift(int passiveCount)
        {
            if (_pendingShiftIndex < 0)
                return;

            _passiveGrid.PlayShiftIn(_pendingShiftIndex, passiveCount - 1, SLOT_SHIFT_DURATION);
            _pendingShiftIndex = NO_SLOT_INDEX;
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

            _pendingShiftIndex = slotIndex;
            UIManager.RaiseEvent(UIManager.EEvent.ShopSellRequested, slotIndex);
            _pendingShiftIndex = NO_SLOT_INDEX;
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

                entries[i] = new ItemIconGridView.Entry(item.Icon, GetCountText(item.Count), LocalizationTable.GetOrKey(item.TitleKey), LocalizationTable.GetOrKey(item.DescriptionKey));
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
            _offerRow.spacing = spacing;

            float requiredWidth = OFFER_SLOT_WIDTH * shownCount + spacing * (shownCount - 1);
            RectTransform rowRect = _offerRow.transform as RectTransform;
            rowRect.sizeDelta = new Vector2(requiredWidth, rowRect.sizeDelta.y);

            float rowScale = Mathf.Min(OFFER_ROW_WIDTH / requiredWidth, OFFER_ROW_HEIGHT / OFFER_SLOT_HEIGHT);
            rowRect.localScale = new Vector3(rowScale, rowScale, 1f);
        }

        void HandleUIEvent(UIManager.EEvent uiEvent, int payload)
        {
            switch (uiEvent)
            {
                case UIManager.EEvent.ShopPurchaseDenied:
                    _messagePopup.PlayTextAt(LocalizationTable.Get(NOT_ENOUGH_MONEY_KEY, NOT_ENOUGH_MONEY_TEXT), GetRequestSourcePosition());
                    PlayMoneyDenied();
                    break;
                case UIManager.EEvent.ShopPassiveSlotFull:
                    _messagePopup.PlayTextAt(LocalizationTable.Get(PASSIVE_SLOT_FULL_KEY, PASSIVE_SLOT_FULL_TEXT), GetRequestSourcePosition());
                    PlayPassiveDenied();
                    break;
            }
        }

        Vector3 GetRequestSourcePosition()
        {
            if (null == _lastRequestSource)
                return _moneyPlaque.position;

            return _lastRequestSource.position;
        }

        void PlayMoneyDenied()
        {
            KillMoneyDenied();

            Sequence sequence = DOTween.Sequence();
            sequence.Insert(0f, _moneyPlaque.DOShakeAnchorPos(MONEY_SHAKE_DURATION, new Vector2(MONEY_SHAKE_STRENGTH, 0f), MONEY_SHAKE_VIBRATO, MONEY_SHAKE_RANDOMNESS));
            sequence.Insert(0f, _moneyPlaqueImage.DOColor(_moneyDeniedColor, MONEY_COLOR_DURATION).SetLoops(MONEY_COLOR_LOOPS, LoopType.Yoyo));
            sequence.OnComplete(OnMoneyDeniedCompleted);
            sequence.SetUpdate(true);
            _moneyDeniedTween = sequence;
        }

        void OnMoneyDeniedCompleted()
        {
            _moneyDeniedTween = null;
            _moneyPlaque.anchoredPosition = _moneyPlaqueBasePosition;
            _moneyPlaqueImage.color = _moneyPlaqueBaseColor;
        }

        void KillMoneyDenied()
        {
            if (null == _moneyDeniedTween)
                return;

            _moneyDeniedTween.Kill();
            OnMoneyDeniedCompleted();
        }

        void PlayPassiveDenied()
        {
            KillPassiveDenied();

            Tween shake = _passiveSlotRoot.DOShakeAnchorPos(PASSIVE_SHAKE_DURATION, new Vector2(PASSIVE_SHAKE_STRENGTH, 0f), PASSIVE_SHAKE_VIBRATO, PASSIVE_SHAKE_RANDOMNESS);
            shake.OnComplete(OnPassiveDeniedCompleted);
            shake.SetUpdate(true);
            _passiveDeniedTween = shake;
        }

        void OnPassiveDeniedCompleted()
        {
            _passiveDeniedTween = null;
            _passiveSlotRoot.anchoredPosition = _passiveSlotBasePosition;
        }

        void KillPassiveDenied()
        {
            if (null == _passiveDeniedTween)
                return;

            _passiveDeniedTween.Kill();
            OnPassiveDeniedCompleted();
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
