using Pinball.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class ShopView : UIViewBase
    {
        public record OfferEntry(string Title, Sprite Icon, string Description, int Price, bool IsPassive);

        public record OwnedEntry(Sprite Icon, int Count, string Title, string Description);

        public record Args(
            int Money,
            int RerollPrice,
            OfferEntry[] Offers,
            int PassiveSlotCount,
            OwnedEntry[] Passives,
            OwnedEntry[] Upgrades) : UIViewArgs
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
        const float OFFER_ROW_WIDTH = 1260f;
        const float OFFER_SLOT_WIDTH = 380f;
        const float OFFER_SLOT_WIDE_SPACING = 60f;
        const float OFFER_SLOT_TIGHT_SPACING = 30f;
        const int OFFER_SLOT_WIDE_COUNT = 3;
        const int STACKED_COUNT_MIN = 2;

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

        public override void OnGenerate()
        {
            _passiveGrid.Init(_tooltip);
            _upgradeGrid.Init(_tooltip);

            for (int i = 0; i < _offerSlots.Length; ++i)
                _offerSlots[i].Init(i, OnOfferClicked);
            _rerollButton.onClick.AddListener(OnRerollClicked);
            _startButton.onClick.AddListener(OnStartClicked);
            _tooltip.Hide();
        }

        public override void OnTerminate()
        {
            for (int i = 0; i < _offerSlots.Length; ++i)
                _offerSlots[i].Release();
            _rerollButton.onClick.RemoveListener(OnRerollClicked);
            _startButton.onClick.RemoveListener(OnStartClicked);
            _passiveGrid.Release();
            _upgradeGrid.Release();
        }

        public override void OnReturn()
        {
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
            BindOffers(viewArgs.Offers);
            BindOwned(viewArgs);
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
                Title: offer.Title,
                Icon: offer.Icon,
                Description: offer.Description,
                PriceText: string.Format(PRICE_FORMAT, offer.Price)));
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
            _offerRow.spacing = spacing;

            float requiredWidth = OFFER_SLOT_WIDTH * shownCount + spacing * (shownCount - 1);
            RectTransform rowRect = _offerRow.transform as RectTransform;
            rowRect.sizeDelta = new Vector2(requiredWidth, rowRect.sizeDelta.y);

            float rowScale = Mathf.Min(1f, OFFER_ROW_WIDTH / requiredWidth);
            rowRect.localScale = new Vector3(rowScale, rowScale, 1f);
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
