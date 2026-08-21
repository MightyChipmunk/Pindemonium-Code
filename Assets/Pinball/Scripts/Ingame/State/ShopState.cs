using System;
using System.Collections.Generic;
using Pinball.Audio;
using Pinball.Ingame.Shop;
using Pinball.Ingame.Stage;
using Pinball.UI;
using UnityEngine;

namespace Pinball.Ingame.State
{
    public class ShopState : IngameStateBase
    {
        public record InitParam(
            ShopConfigData Config,
            ShopCatalogData Catalog,
            CardPackCatalogData PackCatalog,
            VoucherCatalogData VoucherCatalog,
            StageCatalogData StageCatalog,
            Func<int> MoneyGetter,
            Func<int, bool> MoneySpender,
            Action<int> MoneyGranter,
            Func<int> StageNumberGetter,
            Func<ShopItemData, bool> IsItemUnlockedGetter,
            int OfferSlotCount,
            bool IsInterestDisabled);

        public record EnterParam(bool OpensImmediately);

        const int PASSIVE_OWNED_COUNT = 1;
        const int NOT_SELLABLE_PRICE = 0;
        const int MIN_SELL_PRICE = 1;

        ShopConfigData _config = null;
        ShopCatalogData _catalog = null;
        CardPackCatalogData _packCatalog = null;
        VoucherCatalogData _voucherCatalog = null;
        StageCatalogData _stageCatalog = null;
        Func<int> _moneyGetter = null;
        Func<int, bool> _moneySpender = null;
        Action<int> _moneyGranter = null;
        Func<int> _stageNumberGetter = null;
        PlayerLoadout _loadout = null;
        PlayerVouchers _vouchers = null;
        Func<ShopItemData, bool> _isItemUnlockedGetter = null;
        ShopSession _session = null;
        CardPackShop _packShop = null;
        int _offerSlotCount = ShopSession.DEFAULT_OFFER_SLOT_COUNT;
        bool _isInterestDisabled = false;

        public event Action OnLoadoutChanged;
        public event Action OnVoucherPurchased;

        public override EIngameState Key => EIngameState.Shop;

        public ShopState(InitParam param, PlayerLoadout loadout, PlayerVouchers vouchers)
        {
            _config = param.Config;
            _catalog = param.Catalog;
            _packCatalog = param.PackCatalog;
            _voucherCatalog = param.VoucherCatalog;
            _stageCatalog = param.StageCatalog;
            _moneyGetter = param.MoneyGetter;
            _moneySpender = param.MoneySpender;
            _moneyGranter = param.MoneyGranter;
            _stageNumberGetter = param.StageNumberGetter;
            _loadout = loadout;
            _vouchers = vouchers;
            _isItemUnlockedGetter = param.IsItemUnlockedGetter;
            _offerSlotCount = param.OfferSlotCount;
            _isInterestDisabled = param.IsInterestDisabled;
        }

        protected override void OnEnter(object param)
        {
            UIManager.OnEvent += HandleUIEvent;

            EnterParam enterParam = param as EnterParam;
            if (null != enterParam && enterParam.OpensImmediately)
                Open();
        }

        protected override void OnExit()
        {
            UIManager.OnEvent -= HandleUIEvent;
            _session = null;
            _packShop = null;
            UIManager.Hide(UIViewCatalogData.EViewType.CardPackPopup);
            UIManager.Hide(UIViewCatalogData.EViewType.Shop);
        }

        protected override void OnDispose()
        {
            OnLoadoutChanged = null;
            OnVoucherPurchased = null;
        }

        public int CalculateInterest()
        {
            if (_isInterestDisabled)
                return 0;

            if (null == _config)
                return 0;

            if (_config.InterestPerAmount <= 0)
                return 0;

            int cap = _config.InterestMax;
            if (_vouchers.Has(VoucherData.EEffect.InterestCapBoost))
                cap = Mathf.RoundToInt(cap * _vouchers.GetEffectValue(VoucherData.EEffect.InterestCapBoost, VoucherData.MAIN_VALUE_INDEX));

            int interest = Mathf.Min(_moneyGetter() / _config.InterestPerAmount, cap);
            if (_vouchers.Has(VoucherData.EEffect.InterestRateBoost))
                interest = Mathf.FloorToInt(interest * _vouchers.GetEffectValue(VoucherData.EEffect.InterestRateBoost, VoucherData.MAIN_VALUE_INDEX));

            return interest;
        }

        public void RefreshOpenedView()
        {
            if (null == _session)
                return;

            RefreshShopView(false);
        }

        void HandleUIEvent(UIManager.EEvent uiEvent, int payload)
        {
            switch (uiEvent)
            {
                case UIManager.EEvent.StageClearFinished:
                    Open();
                    break;
                case UIManager.EEvent.ShopStartRequested:
                    RequestNextStage();
                    break;
                case UIManager.EEvent.ShopBuyRequested:
                    TryBuyOffer(payload);
                    break;
                case UIManager.EEvent.ShopRerollRequested:
                    TryReroll();
                    break;
                case UIManager.EEvent.ShopPackBuyRequested:
                    TryBuyPack(payload);
                    break;
                case UIManager.EEvent.ShopVoucherBuyRequested:
                    TryBuyVoucher();
                    break;
                case UIManager.EEvent.ShopSellRequested:
                    TrySellPassive(payload);
                    break;
                case UIManager.EEvent.CardPackPickRequested:
                    TryPickPackItem(payload);
                    break;
                case UIManager.EEvent.CardPackSkipRequested:
                    ClosePackOpening();
                    break;
            }
        }

        void Open()
        {
            _session = new ShopSession(_config, _catalog, CanOfferItem, _offerSlotCount, _vouchers.Has(VoucherData.EEffect.ShopFirstRerollFree));
            _packShop = new CardPackShop(_packCatalog, _catalog, CanOfferItem, CardPackShop.DEFAULT_PACK_SLOT_COUNT);
            RefreshShopView(false);
        }

        int GetRoundIndex()
        {
            return StageCatalogData.GetRoundIndex(_stageNumberGetter() - 1);
        }

        int GetOwnedUpgradeCount(ShopItemData item)
        {
            if (item.Kind == ShopItemData.EItemKind.Passive)
                return 0;

            int ownedIndex = _loadout.GetOwnedIndex(item);
            if (ownedIndex < 0)
                return 0;

            return _loadout.GetUpgradeCount(ownedIndex);
        }

        int GetSellPrice(ShopItemData item)
        {
            return Mathf.Max(MIN_SELL_PRICE, Mathf.RoundToInt(item.Price * _config.SellPriceRatio));
        }

        void TrySellPassive(int passiveIndex)
        {
            if (null == _session)
                return;

            if (_packShop.IsOpening)
                return;

            ShopItemData passive = _loadout.GetPassive(passiveIndex);
            if (null == passive)
                return;

            if (!_loadout.TryRemovePassiveAt(passiveIndex))
                return;

            _moneyGranter(GetSellPrice(passive));
            AudioManager.Play(AudioStorage.EAudioClipType.MoneyGain);
            OnLoadoutChanged?.Invoke();
            RefreshShopView(false);
        }

        void NotifyPassiveSlotFull()
        {
            AudioManager.Play(AudioStorage.EAudioClipType.ShopDenied);
            UIManager.RaiseEvent(UIManager.EEvent.ShopPassiveSlotFull);
        }

        bool TrySpendMoney(int price)
        {
            if (_moneySpender(price))
                return true;

            AudioManager.Play(AudioStorage.EAudioClipType.ShopDenied);
            UIManager.RaiseEvent(UIManager.EEvent.ShopPurchaseDenied);
            return false;
        }

        void RequestNextStage()
        {
            ChangeState(EIngameState.StageSelect, new StageSelectState.EnterParam(IsAfterShop: true));
        }

        void RefreshShopView(bool playsRerollReveal, ShopView.PurchaseEntry purchase = null)
        {
            if (null == _session)
                return;

            int stageNumber = _stageNumberGetter();
            UIManager.Show(new ShopView.Args(
                Money: _moneyGetter(),
                RerollPrice: _session.CurrentRerollPrice,
                Offers: BuildOfferEntries(),
                Purchase: purchase,
                PassiveSlotCount: _loadout.PassiveSlotCount,
                Passives: BuildPassiveEntries(),
                Upgrades: BuildUpgradeEntries(),
                Packs: BuildPackEntries(),
                Voucher: BuildVoucherEntry(),
                StageNumber: stageNumber,
                TotalStageCount: _stageCatalog.Count,
                RoundNumber: GetRoundIndex() + 1,
                RoundCount: _stageCatalog.RoundCount,
                PlaysRerollReveal: playsRerollReveal));
        }

        ShopView.PackEntry[] BuildPackEntries()
        {
            ShopView.PackEntry[] entries = new ShopView.PackEntry[_packShop.PackSlotCount];
            for (int i = 0; i < entries.Length; ++i)
                entries[i] = BuildPackEntry(_packShop.GetPack(i));

            return entries;
        }

        ShopView.PackEntry BuildPackEntry(CardPackData pack)
        {
            if (null == pack)
                return null;

            return new ShopView.PackEntry(
                TitleKey: pack.GetNameKey(),
                Icon: pack.GetIcon(),
                DescriptionKey: pack.GetDescriptionKey(),
                Price: pack.Price,
                BandSprite: pack.BandSprite,
                CanBuy: _packShop.HasCandidates(pack));
        }

        ShopView.VoucherEntry BuildVoucherEntry()
        {
            VoucherData voucher = _vouchers.GetOffer(GetRoundIndex(), _voucherCatalog);
            if (null == voucher)
                return null;

            return new ShopView.VoucherEntry(
                TitleKey: voucher.GetNameKey(),
                Icon: voucher.GetIcon(),
                DescriptionKey: voucher.GetDescriptionKey(),
                Price: PlayerVouchers.PRICE);
        }

        void TryBuyPack(int slotIndex)
        {
            if (null == _packShop || _packShop.IsOpening)
                return;

            CardPackData pack = _packShop.GetPack(slotIndex);
            if (null == pack)
                return;

            if (!_packShop.HasCandidates(pack))
                return;

            if (!TrySpendMoney(pack.Price))
                return;

            _packShop.TryStartOpening(slotIndex);
            AudioManager.Play(AudioStorage.EAudioClipType.ShopBuy);
            RefreshShopView(false);
            ShowPackOpeningView(false, null);
        }

        void TryBuyVoucher()
        {
            if (null == _session)
                return;

            VoucherData voucher = _vouchers.GetOffer(GetRoundIndex(), _voucherCatalog);
            if (null == voucher)
                return;

            if (!TrySpendMoney(PlayerVouchers.PRICE))
                return;

            _vouchers.PurchaseOffer();
            ApplyVoucherImmediateEffect(voucher);
            AudioManager.Play(AudioStorage.EAudioClipType.ShopBuy);
            OnVoucherPurchased?.Invoke();
            RefreshShopView(false);
        }

        void ApplyVoucherImmediateEffect(VoucherData voucher)
        {
            if (voucher.Effect != VoucherData.EEffect.PassiveSlotAdd)
                return;

            _loadout.AddPassiveSlot(Mathf.RoundToInt(voucher.GetEffectValue(VoucherData.MAIN_VALUE_INDEX)));
        }

        void TryPickPackItem(int cardIndex)
        {
            if (null == _packShop || !_packShop.IsOpening)
                return;

            ShopItemData item = _packShop.GetRevealedItem(cardIndex);
            if (null == item)
                return;

            if (!CanApplyItem(item))
            {
                NotifyPassiveSlotFull();
                return;
            }

            if (null == _packShop.TakePick(cardIndex))
                return;

            int previousCount = GetOwnedUpgradeCount(item);
            ApplyItem(item);
            AudioManager.Play(AudioStorage.EAudioClipType.ShopBuy);
            OnLoadoutChanged?.Invoke();

            if (_packShop.IsOpeningFinished)
            {
                DismissPackOpening(new CardPackPopupView.PickEntry(
                    CardIndex: cardIndex,
                    IsPassive: item.Kind == ShopItemData.EItemKind.Passive,
                    OwnedIndex: _loadout.GetOwnedIndex(item),
                    PreviousCount: previousCount));
                return;
            }

            ShowPackOpeningView(false, null);
        }

        void ShowPackOpeningView(bool isDismissing, CardPackPopupView.PickEntry pick)
        {
            CardPackPopupView.CardArgs[] cards = new CardPackPopupView.CardArgs[_packShop.RevealedItemCount];
            for (int i = 0; i < cards.Length; ++i)
            {
                ShopItemData item = _packShop.GetRevealedItem(i);
                cards[i] = new CardPackPopupView.CardArgs(
                    TitleKey: item.GetNameKey(),
                    Icon: item.GetIcon(),
                    DescriptionKey: item.GetDescriptionKey(),
                    IsPassive: item.Kind == ShopItemData.EItemKind.Passive,
                    IsTaken: _packShop.IsTaken(i));
            }

            UIManager.Show(new CardPackPopupView.Args(
                PackTitleKey: _packShop.GetOpeningPackNameKey(),
                RemainingPicks: _packShop.RemainingPicks,
                Cards: cards,
                IsDismissing: isDismissing,
                Pick: pick));
        }

        void ClosePackOpening()
        {
            if (null == _packShop || !_packShop.IsOpening)
                return;

            DismissPackOpening(null);
        }

        void DismissPackOpening(CardPackPopupView.PickEntry pick)
        {
            RefreshShopView(false);
            ShowPackOpeningView(true, pick);
            _packShop.CloseOpening();
        }

        ShopView.OfferEntry[] BuildOfferEntries()
        {
            ShopView.OfferEntry[] offers = new ShopView.OfferEntry[_session.OfferSlotCount];
            for (int i = 0; i < offers.Length; ++i)
                offers[i] = BuildOfferEntry(_session.GetOffer(i));

            return offers;
        }

        ShopView.OfferEntry BuildOfferEntry(ShopItemData item)
        {
            if (null == item)
                return null;

            return new ShopView.OfferEntry(
                TitleKey: item.GetNameKey(),
                Icon: item.GetIcon(),
                DescriptionKey: item.GetDescriptionKey(),
                Price: GetOfferPrice(item),
                IsPassive: item.Kind == ShopItemData.EItemKind.Passive);
        }

        int GetOfferPrice(ShopItemData item)
        {
            if (!_vouchers.Has(VoucherData.EEffect.ShopDiscount))
                return item.Price;

            float discount = _vouchers.GetEffectValue(VoucherData.EEffect.ShopDiscount, VoucherData.MAIN_VALUE_INDEX);
            return Mathf.Max(1, Mathf.RoundToInt(item.Price * (1f - discount)));
        }

        ShopView.OwnedEntry[] BuildPassiveEntries()
        {
            ShopView.OwnedEntry[] entries = new ShopView.OwnedEntry[_loadout.PassiveCount];
            for (int i = 0; i < entries.Length; ++i)
                entries[i] = BuildPassiveEntry(_loadout.GetPassive(i));

            return entries;
        }

        ShopView.OwnedEntry BuildPassiveEntry(ShopItemData item)
        {
            if (null == item)
                return null;

            return BuildOwnedEntry(item, PASSIVE_OWNED_COUNT, GetSellPrice(item));
        }

        ShopView.OwnedEntry[] BuildUpgradeEntries()
        {
            ShopView.OwnedEntry[] entries = new ShopView.OwnedEntry[_loadout.UpgradeKindCount];
            for (int i = 0; i < entries.Length; ++i)
                entries[i] = BuildOwnedEntry(_loadout.GetUpgrade(i), _loadout.GetUpgradeCount(i), NOT_SELLABLE_PRICE);

            return entries;
        }

        static ShopView.OwnedEntry BuildOwnedEntry(ShopItemData item, int count, int sellPrice)
        {
            if (null == item)
                return null;

            return new ShopView.OwnedEntry(item.GetIcon(), count, item.GetNameKey(), item.GetDescriptionKey(), sellPrice);
        }

        void TryBuyOffer(int slotIndex)
        {
            if (null == _session)
                return;

            ShopItemData item = _session.GetOffer(slotIndex);
            if (null == item)
                return;

            if (!CanApplyItem(item))
            {
                NotifyPassiveSlotFull();
                return;
            }

            bool isTopUpgrade = item.Kind == ShopItemData.EItemKind.Upgrade && _loadout.IsMostUpgraded(item);
            if (!TrySpendMoney(GetOfferPrice(item)))
                return;

            int previousCount = GetOwnedUpgradeCount(item);
            ApplyItem(item);
            ApplyTopUpgradeDoubleBuy(item, isTopUpgrade);
            ApplyBonusUpgradeOnBuy();
            AudioManager.Play(AudioStorage.EAudioClipType.ShopBuy);
            _session.MarkSold(slotIndex);
            OnLoadoutChanged?.Invoke();
            RefreshShopView(false, new ShopView.PurchaseEntry(
                SlotIndex: slotIndex,
                IsPassive: item.Kind == ShopItemData.EItemKind.Passive,
                OwnedIndex: _loadout.GetOwnedIndex(item),
                PreviousCount: previousCount));
        }

        void ApplyTopUpgradeDoubleBuy(ShopItemData item, bool isTopUpgrade)
        {
            if (!isTopUpgrade)
                return;

            if (!_vouchers.Has(VoucherData.EEffect.TopUpgradeDoubleBuy))
                return;

            float chance = _vouchers.GetEffectValue(VoucherData.EEffect.TopUpgradeDoubleBuy, VoucherData.MAIN_VALUE_INDEX);
            if (UnityEngine.Random.value >= chance)
                return;

            ApplyItem(item);
        }

        void ApplyBonusUpgradeOnBuy()
        {
            if (!_vouchers.Has(VoucherData.EEffect.BonusUpgradeOnBuy))
                return;

            float chance = _vouchers.GetEffectValue(VoucherData.EEffect.BonusUpgradeOnBuy, VoucherData.MAIN_VALUE_INDEX);
            if (UnityEngine.Random.value >= chance)
                return;

            List<ShopItemData> candidates = new List<ShopItemData>();
            for (int i = 0; i < _catalog.Count; ++i)
            {
                ShopItemData item = _catalog.GetItem(i);
                if (null == item || item.Kind != ShopItemData.EItemKind.Upgrade)
                    continue;

                if (null != _isItemUnlockedGetter && !_isItemUnlockedGetter(item))
                    continue;

                candidates.Add(item);
            }

            if (candidates.Count <= 0)
                return;

            ApplyItem(candidates[UnityEngine.Random.Range(0, candidates.Count)]);
        }

        void TryReroll()
        {
            if (null == _session)
                return;

            if (_packShop.IsOpening)
                return;

            if (!TrySpendMoney(_session.CurrentRerollPrice))
                return;

            _session.Reroll();
            _packShop.Reroll();
            AudioManager.Play(AudioStorage.EAudioClipType.ShopReroll);
            RefreshShopView(true);
        }

        bool CanOfferItem(ShopItemData item)
        {
            if (null == item)
                return false;

            if (null != _isItemUnlockedGetter && !_isItemUnlockedGetter(item))
                return false;

            if (item.Kind != ShopItemData.EItemKind.Passive)
                return true;

            return _loadout.CanAddPassive(item);
        }

        bool CanApplyItem(ShopItemData item)
        {
            if (item.Kind != ShopItemData.EItemKind.Passive)
                return true;

            return _loadout.CanAddPassive(item);
        }

        void ApplyItem(ShopItemData item)
        {
            if (item.Kind == ShopItemData.EItemKind.Passive)
            {
                _loadout.TryAddPassive(item);
                return;
            }

            _loadout.AddUpgrade(item);
        }
    }
}
