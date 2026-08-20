using System;
using System.Collections.Generic;
using Pinball.Audio;
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
            Func<int> StageNumberGetter,
            Func<ShopItemData, bool> IsItemUnlockedGetter,
            int OfferSlotCount,
            bool IsInterestDisabled);

        public record EnterParam(bool OpensImmediately);

        const int PASSIVE_OWNED_COUNT = 1;

        ShopConfigData _config = null;
        ShopCatalogData _catalog = null;
        CardPackCatalogData _packCatalog = null;
        VoucherCatalogData _voucherCatalog = null;
        StageCatalogData _stageCatalog = null;
        Func<int> _moneyGetter = null;
        Func<int, bool> _moneySpender = null;
        Func<int> _stageNumberGetter = null;
        PlayerLoadout _loadout = null;
        PlayerVouchers _vouchers = null;
        Func<ShopItemData, bool> _isItemUnlockedGetter = null;
        ShopSession _session = null;
        CardPackShop _packShop = null;
        VoucherData _offeredVoucher = null;
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
            _offeredVoucher = null;
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
            _offeredVoucher = RollVoucherOffer();
            RefreshShopView(false);
        }

        VoucherData RollVoucherOffer()
        {
            if (!_vouchers.CanPurchaseInRound(GetRoundIndex()))
                return null;

            List<VoucherData> candidates = new List<VoucherData>();
            if (null == _voucherCatalog)
                return null;

            for (int i = 0; i < _voucherCatalog.Count; ++i)
            {
                VoucherData voucher = _voucherCatalog.GetVoucher(i);
                if (null == voucher || _vouchers.Owns(voucher))
                    continue;

                candidates.Add(voucher);
            }

            if (candidates.Count <= 0)
                return null;

            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        int GetRoundIndex()
        {
            return StageCatalogData.GetRoundIndex(_stageNumberGetter() - 1);
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

        void RefreshShopView(bool playsOfferReveal)
        {
            if (null == _session)
                return;

            int stageNumber = _stageNumberGetter();
            UIManager.Show(new ShopView.Args(
                Money: _moneyGetter(),
                RerollPrice: _session.CurrentRerollPrice,
                Offers: BuildOfferEntries(),
                PassiveSlotCount: _loadout.PassiveSlotCount,
                Passives: BuildPassiveEntries(),
                Upgrades: BuildUpgradeEntries(),
                Packs: BuildPackEntries(),
                Voucher: BuildVoucherEntry(),
                StageNumber: stageNumber,
                TotalStageCount: _stageCatalog.Count,
                RoundNumber: GetRoundIndex() + 1,
                RoundCount: _stageCatalog.RoundCount,
                PlaysOfferReveal: playsOfferReveal));
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
                Title: pack.GetDisplayName(),
                Icon: pack.GetIcon(),
                Description: pack.GetDescription(),
                Price: pack.Price,
                BandSprite: pack.BandSprite,
                CanBuy: _packShop.HasCandidates(pack));
        }

        ShopView.VoucherEntry BuildVoucherEntry()
        {
            if (null == _offeredVoucher)
                return null;

            return new ShopView.VoucherEntry(
                Title: _offeredVoucher.GetDisplayName(),
                Icon: _offeredVoucher.GetIcon(),
                Description: _offeredVoucher.GetDescription(),
                Price: PlayerVouchers.PRICE,
                CanBuy: _moneyGetter() >= PlayerVouchers.PRICE);
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
            ShowPackOpeningView();
        }

        void TryBuyVoucher()
        {
            if (null == _offeredVoucher)
                return;

            int roundIndex = GetRoundIndex();
            if (!_vouchers.CanPurchaseInRound(roundIndex))
                return;

            if (!TrySpendMoney(PlayerVouchers.PRICE))
                return;

            VoucherData voucher = _offeredVoucher;
            _offeredVoucher = null;
            _vouchers.TryPurchase(voucher, roundIndex);
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
                return;

            if (null == _packShop.TakePick(cardIndex))
                return;

            ApplyItem(item);
            AudioManager.Play(AudioStorage.EAudioClipType.ShopBuy);
            OnLoadoutChanged?.Invoke();

            if (_packShop.IsOpeningFinished)
            {
                ClosePackOpening();
                return;
            }

            ShowPackOpeningView();
        }

        void ShowPackOpeningView()
        {
            CardPackPopupView.CardArgs[] cards = new CardPackPopupView.CardArgs[_packShop.RevealedItemCount];
            for (int i = 0; i < cards.Length; ++i)
            {
                ShopItemData item = _packShop.GetRevealedItem(i);
                cards[i] = new CardPackPopupView.CardArgs(
                    Title: item.GetDisplayName(),
                    Icon: item.GetIcon(),
                    Description: item.GetDescription(),
                    IsTaken: _packShop.IsTaken(i));
            }

            UIManager.Show(new CardPackPopupView.Args(
                PackTitle: _packShop.GetOpeningPackName(),
                RemainingPicks: _packShop.RemainingPicks,
                Cards: cards));
        }

        void ClosePackOpening()
        {
            if (null == _packShop || !_packShop.IsOpening)
                return;

            _packShop.CloseOpening();
            UIManager.Hide(UIViewCatalogData.EViewType.CardPackPopup);
            RefreshShopView(false);
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
                Title: item.GetDisplayName(),
                Icon: item.GetIcon(),
                Description: item.GetDescription(),
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
                entries[i] = BuildOwnedEntry(_loadout.GetPassive(i), PASSIVE_OWNED_COUNT);

            return entries;
        }

        ShopView.OwnedEntry[] BuildUpgradeEntries()
        {
            ShopView.OwnedEntry[] entries = new ShopView.OwnedEntry[_loadout.UpgradeKindCount];
            for (int i = 0; i < entries.Length; ++i)
                entries[i] = BuildOwnedEntry(_loadout.GetUpgrade(i), _loadout.GetUpgradeCount(i));

            return entries;
        }

        static ShopView.OwnedEntry BuildOwnedEntry(ShopItemData item, int count)
        {
            if (null == item)
                return null;

            return new ShopView.OwnedEntry(item.GetIcon(), count, item.GetDisplayName(), item.GetDescription());
        }

        void TryBuyOffer(int slotIndex)
        {
            if (null == _session)
                return;

            ShopItemData item = _session.GetOffer(slotIndex);
            if (null == item)
                return;

            if (!CanApplyItem(item))
                return;

            bool isTopUpgrade = item.Kind == ShopItemData.EItemKind.Upgrade && _loadout.IsMostUpgraded(item);
            if (!TrySpendMoney(GetOfferPrice(item)))
                return;

            ApplyItem(item);
            ApplyTopUpgradeDoubleBuy(item, isTopUpgrade);
            ApplyBonusUpgradeOnBuy();
            AudioManager.Play(AudioStorage.EAudioClipType.ShopBuy);
            _session.MarkSold(slotIndex);
            OnLoadoutChanged?.Invoke();
            RefreshShopView(false);
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

            if (!TrySpendMoney(_session.CurrentRerollPrice))
                return;

            _session.Reroll();
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
