using DG.Tweening;
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

        public record OwnedEntry(Sprite Icon, int Count, string TitleKey, string DescriptionKey);

        public record PackEntry(string TitleKey, Sprite Icon, string DescriptionKey, int Price, Sprite BandSprite, bool CanBuy);

        public record VoucherEntry(string TitleKey, Sprite Icon, string DescriptionKey, int Price, bool CanBuy);

        public record Args(
            int Money,
            int RerollPrice,
            OfferEntry[] Offers,
            int PassiveSlotCount,
            OwnedEntry[] Passives,
            OwnedEntry[] Upgrades,
            PackEntry[] Packs,
            VoucherEntry Voucher,
            int StageNumber,
            int TotalStageCount,
            int RoundNumber,
            int RoundCount,
            bool PlaysOfferReveal) : UIViewArgs
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
        const float MONEY_SHAKE_DURATION = 0.42f;
        const float MONEY_SHAKE_STRENGTH = 24f;
        const int MONEY_SHAKE_VIBRATO = 16;
        const float MONEY_SHAKE_RANDOMNESS = 0f;
        const float MONEY_COLOR_DURATION = 0.21f;
        const int MONEY_COLOR_LOOPS = 2;
        const float OFFER_HIDE_DURATION = 0.14f;
        const float OFFER_HIDE_STAGGER = 0.05f;
        const float OFFER_SHOW_DURATION = 0.26f;
        const float OFFER_SHOW_STAGGER = 0.07f;

        [SerializeField] TMP_Text _moneyLabel = null;
        [SerializeField] ShopItemView[] _offerSlots = null;
        [SerializeField] HorizontalLayoutGroup _offerRow = null;
        [SerializeField] TMP_Text _passiveHeaderLabel = null;
        [SerializeField] TMP_Text _upgradeHeaderLabel = null;
        [SerializeField] ItemIconGridView _passiveGrid = null;
        [SerializeField] ItemIconGridView _upgradeGrid = null;
        [SerializeField] ItemTooltipView _tooltip = null;
        [SerializeField] Button _rerollButton = null;
        [SerializeField] TMP_Text _rerollLabel = null;
        [SerializeField] Button _startButton = null;
        [SerializeField] ShopCardSlotView[] _packSlots = null;
        [SerializeField] ShopCardSlotView _voucherSlot = null;
        [SerializeField] Sprite _voucherBandSprite = null;
        [SerializeField] Button _settingsButton = null;
        [SerializeField] TMP_Text _roundLabel = null;
        [SerializeField] TMP_Text _stageLabel = null;
        [SerializeField] RectTransform _moneyPlaque = null;
        [SerializeField] Image _moneyPlaqueImage = null;
        [SerializeField] Color _moneyDeniedColor = new Color(1f, 0.32f, 0.26f, 1f);
        [SerializeField] ScorePopupSpawner _messagePopup = null;

        Tween _offerRevealTween = null;
        Tween _moneyDeniedTween = null;
        OfferEntry[] _pendingOffers = null;
        Args _revealedArgs = null;
        Transform _lastRequestSource = null;
        Vector2 _moneyPlaqueBasePosition = Vector2.zero;
        Color _moneyPlaqueBaseColor = Color.white;

        public override void OnGenerate()
        {
            _passiveGrid.Init(_tooltip);
            _upgradeGrid.Init(_tooltip);

            for (int i = 0; i < _offerSlots.Length; ++i)
                _offerSlots[i].Init(i, OnOfferClicked);
            for (int i = 0; i < _packSlots.Length; ++i)
                _packSlots[i].Init(i, OnPackClicked);
            _voucherSlot.Init(0, OnVoucherClicked);
            _rerollButton.onClick.AddListener(OnRerollClicked);
            _startButton.onClick.AddListener(OnStartClicked);
            _settingsButton.onClick.AddListener(OnSettingsClicked);
            _messagePopup.Init();
            _moneyPlaqueBasePosition = _moneyPlaque.anchoredPosition;
            _moneyPlaqueBaseColor = _moneyPlaqueImage.color;
            UIManager.OnEvent += HandleUIEvent;
            _tooltip.Hide();
        }

        public override void OnTerminate()
        {
            UIManager.OnEvent -= HandleUIEvent;
            KillOfferReveal();
            KillMoneyDenied();
            _messagePopup.Release();
            for (int i = 0; i < _offerSlots.Length; ++i)
                _offerSlots[i].Release();
            for (int i = 0; i < _packSlots.Length; ++i)
                _packSlots[i].Release();
            _voucherSlot.Release();
            _rerollButton.onClick.RemoveListener(OnRerollClicked);
            _startButton.onClick.RemoveListener(OnStartClicked);
            _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            _passiveGrid.Release();
            _upgradeGrid.Release();
        }

        public override void OnReturn()
        {
            KillOfferReveal();
            KillMoneyDenied();
            _revealedArgs = null;
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

            _moneyLabel.text = string.Format(MONEY_FORMAT, viewArgs.Money);
            _rerollLabel.text = LocalizationTable.Format(REROLL_KEY, REROLL_FORMAT, viewArgs.RerollPrice);
            _roundLabel.text = LocalizationTable.Format(ROUND_KEY, ROUND_FORMAT, viewArgs.RoundNumber, viewArgs.RoundCount);
            _stageLabel.text = LocalizationTable.Format(STAGE_KEY, STAGE_FORMAT, viewArgs.StageNumber, viewArgs.TotalStageCount);
            BindOfferSection(viewArgs);
            BindPacks(viewArgs.Packs);
            BindVoucher(viewArgs.Voucher);
            BindOwned(viewArgs);
        }

        void BindVoucher(VoucherEntry voucher)
        {
            if (null == voucher)
            {
                _voucherSlot.Hide();
                return;
            }

            _voucherSlot.Bind(new ShopCardSlotView.BindParam(
                Title: LocalizationTable.GetOrKey(voucher.TitleKey),
                Icon: voucher.Icon,
                Description: LocalizationTable.GetOrKey(voucher.DescriptionKey),
                PriceText: string.Format(PRICE_FORMAT, voucher.Price),
                BandSprite: _voucherBandSprite,
                CanBuy: voucher.CanBuy));
        }

        void BindPacks(PackEntry[] packs)
        {
            int packCount = null == packs ? 0 : packs.Length;
            for (int i = 0; i < _packSlots.Length; ++i)
            {
                PackEntry pack = i < packCount ? packs[i] : null;
                if (null == pack)
                {
                    _packSlots[i].Hide();
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

        void BindOfferSection(Args args)
        {
            if (args.PlaysOfferReveal && !ReferenceEquals(_revealedArgs, args))
            {
                _revealedArgs = args;
                PlayOfferReveal(args.Offers);
                return;
            }

            KillOfferReveal();
            BindOffers(args.Offers);
        }

        void PlayOfferReveal(OfferEntry[] offers)
        {
            KillOfferReveal();
            _pendingOffers = offers;

            Sequence sequence = DOTween.Sequence();
            float hideEndTime = 0f;
            for (int i = 0; i < _offerSlots.Length; ++i)
            {
                if (!_offerSlots[i].gameObject.activeSelf)
                    continue;

                float hideStartTime = i * OFFER_HIDE_STAGGER;
                sequence.Insert(hideStartTime, _offerSlots[i].transform.DOScale(Vector3.zero, OFFER_HIDE_DURATION).SetEase(Ease.InBack));
                hideEndTime = Mathf.Max(hideEndTime, hideStartTime + OFFER_HIDE_DURATION);
            }

            sequence.InsertCallback(hideEndTime, BindPendingOffers);

            for (int i = 0; i < _offerSlots.Length; ++i)
            {
                Tween showTween = _offerSlots[i].transform.DOScale(Vector3.one, OFFER_SHOW_DURATION).From(Vector3.zero, false).SetEase(Ease.OutBack);
                sequence.Insert(hideEndTime + i * OFFER_SHOW_STAGGER, showTween);
            }

            sequence.OnComplete(OnOfferRevealCompleted);
            sequence.SetUpdate(true);
            _offerRevealTween = sequence;
        }

        void BindPendingOffers()
        {
            if (null == _pendingOffers)
                return;

            BindOffers(_pendingOffers);
            _pendingOffers = null;
        }

        void OnOfferRevealCompleted()
        {
            _offerRevealTween = null;
        }

        void KillOfferReveal()
        {
            _pendingOffers = null;
            if (null == _offerRevealTween)
                return;

            _offerRevealTween.Kill();
            _offerRevealTween = null;
            for (int i = 0; i < _offerSlots.Length; ++i)
                _offerSlots[i].transform.localScale = Vector3.one;
        }

        void BindOffers(OfferEntry[] offers)
        {
            int offerCount = null == offers ? 0 : offers.Length;
            ApplyOfferLayout(offerCount);

            for (int i = 0; i < offerCount; ++i)
                BindOffer(i, offers[i]);
        }

        void BindOffer(int slotIndex, OfferEntry offer)
        {
            ShopItemView slot = GetOfferSlot(slotIndex);
            if (null == slot)
                return;

            if (null == offer)
            {
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

        void BindOwned(Args args)
        {
            int passiveCount = null == args.Passives ? 0 : args.Passives.Length;
            int upgradeCount = null == args.Upgrades ? 0 : args.Upgrades.Length;
            _passiveHeaderLabel.text = LocalizationTable.Format(PASSIVE_HEADER_KEY, PASSIVE_HEADER_FORMAT, passiveCount, args.PassiveSlotCount);
            _upgradeHeaderLabel.text = LocalizationTable.Format(UPGRADE_HEADER_KEY, UPGRADE_HEADER_FORMAT, upgradeCount);
            _tooltip.Hide();

            _passiveGrid.Bind(BuildGridEntries(args.Passives), Mathf.Max(0, args.PassiveSlotCount - passiveCount));
            _upgradeGrid.Bind(BuildGridEntries(args.Upgrades), 0);
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
            if (uiEvent != UIManager.EEvent.ShopPurchaseDenied)
                return;

            _messagePopup.PlayTextAt(LocalizationTable.Get(NOT_ENOUGH_MONEY_KEY, NOT_ENOUGH_MONEY_TEXT), GetRequestSourcePosition());
            PlayMoneyDenied();
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

        void OnSettingsClicked()
        {
            UIManager.Show(new SettingsView.Args());
        }
    }
}
