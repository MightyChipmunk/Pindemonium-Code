using System;
using System.Collections.Generic;
using Pinball.Audio;
using Pinball.Ingame.Shop;
using Pinball.Ingame.Stage;
using Pinball.UI;
using Pinball.Util;
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
            Func<bool> IsRoundRewardPendingGetter,
            Action RoundRewardConsumer,
            int OfferSlotCount,
            bool IsInterestDisabled,
            string[] RestoredOfferIds,
            string[] RestoredPackIds,
            int RestoredRerollCount);

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
        Func<bool> _isRoundRewardPendingGetter = null;
        Action _roundRewardConsumer = null;
        ShopSession _session = null;
        CardPackShop _packShop = null;
        int _offerSlotCount = ShopSession.DEFAULT_OFFER_SLOT_COUNT;
        bool _isInterestDisabled = false;
        bool _isRoundRewardOpening = false;
        string[] _restoredOfferIds = null;
        string[] _restoredPackIds = null;
        int _restoredRerollCount = 0;

        public event Action OnLoadoutChanged;
        public event Action OnOffersChanged;
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
            _isRoundRewardPendingGetter = param.IsRoundRewardPendingGetter;
            _roundRewardConsumer = param.RoundRewardConsumer;
            _offerSlotCount = param.OfferSlotCount;
            _isInterestDisabled = param.IsInterestDisabled;
            _restoredOfferIds = param.RestoredOfferIds;
            _restoredPackIds = param.RestoredPackIds;
            _restoredRerollCount = param.RestoredRerollCount;
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
            OnOffersChanged = null;
        }

        public string[] CollectOfferIds()
        {
            return null == _session ? new string[0] : _session.CollectOfferIds();
        }

        public string[] CollectPackIds()
        {
            return null == _packShop ? new string[0] : _packShop.CollectPackIds();
        }

        public int GetRerollCount()
        {
            return null == _session ? 0 : _session.RerollCount;
        }

        public int CalculateInterest()
        {
            if (_isInterestDisabled)
                return 0;

            if (null == _config)
                return 0;

            if (_config.InterestPerAmount <= 0)
                return 0;

            int cap = _vouchers.BoostInterestCap(_config.InterestMax);
            int interest = _vouchers.BoostInterest(_moneyGetter() / _config.InterestPerAmount);
            return Mathf.Min(interest, cap);
        }

        public int GetInterestMax()
        {
            if (_isInterestDisabled)
                return 0;

            if (null == _config)
                return 0;

            return _vouchers.BoostInterestCap(_config.InterestMax);
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
                case UIManager.EEvent.RoundClearFinished:
                    GrantRoundReward();
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
            BgmManager.Play(BgmStorage.EBgmType.Shop);
            _isRoundRewardOpening = false;
            _session = new ShopSession(_config, _catalog, CanOfferItem, _offerSlotCount, Mathf.RoundToInt(_vouchers.GetEffectSum(VoucherData.EEffect.ShopFreeReroll)));
            _packShop = new CardPackShop(_packCatalog, _catalog, CanOfferPackItem, CardPackShop.DEFAULT_PACK_SLOT_COUNT);
            RestoreOffers();
            RefreshShopView(false);
            OnOffersChanged?.Invoke();
            TryShowRoundClear();
        }

        void RestoreOffers()
        {
            if (null == _restoredOfferIds || _restoredOfferIds.Length <= 0)
                return;

            _session.Restore(new ShopSession.RestoreParam(OfferIds: _restoredOfferIds, RerollCount: _restoredRerollCount));
            _packShop.Restore(_restoredPackIds);
            _restoredOfferIds = null;
            _restoredPackIds = null;
            _restoredRerollCount = 0;
        }

        bool IsRoundRewardPending()
        {
            return null != _isRoundRewardPendingGetter && _isRoundRewardPendingGetter();
        }

        void TryShowRoundClear()
        {
            if (!IsRoundRewardPending())
                return;

            AudioManager.Play(AudioStorage.EAudioClipType.RewardUnlock);
            int clearedRoundIndex = StageCatalogData.GetRoundIndex(_stageNumberGetter() - 2);
            bool isEndless = clearedRoundIndex >= _stageCatalog.RoundCount;
            UIManager.Show(new RoundClearView.Args(
                RoundNumber: isEndless ? clearedRoundIndex - _stageCatalog.RoundCount + 1 : clearedRoundIndex + 1,
                IsEndless: isEndless));
        }

        void GrantRoundReward()
        {
            if (null == _packShop || _packShop.IsOpening)
                return;

            if (!IsRoundRewardPending())
                return;

            if (!_packShop.TryOpenRandomPack())
            {
                _roundRewardConsumer?.Invoke();
                return;
            }

            _isRoundRewardOpening = true;
            ShowPackOpeningView(false, null);
        }

        void ConsumeRoundRewardOpening()
        {
            if (!_isRoundRewardOpening)
                return;

            _isRoundRewardOpening = false;
            _roundRewardConsumer?.Invoke();
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
            int sellPrice = Mathf.Max(MIN_SELL_PRICE, Mathf.RoundToInt(item.Price * _config.SellPriceRatio));
            return _vouchers.BoostSellPrice(sellPrice);
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

            ShopItemData.EPassiveCategory category = passive.PassiveCategory;
            int removedSlotIndex = CountCategoryPassivesBefore(passiveIndex, category);
            if (!_loadout.TryRemovePassiveAt(passiveIndex))
                return;

            _moneyGranter(GetSellPrice(passive));
            ShopView.UpgradeGrantEntry grant = GrantSellUpgrades();
            AudioManager.Play(AudioStorage.EAudioClipType.MoneyGain);
            OnLoadoutChanged?.Invoke();
            RefreshShopView(false, sell: new ShopView.SellEntry(Category: category, RemovedSlotIndex: removedSlotIndex, Grant: grant));
        }

        int CountCategoryPassivesBefore(int passiveIndex, ShopItemData.EPassiveCategory category)
        {
            int count = 0;
            for (int i = 0; i < passiveIndex; ++i)
            {
                ShopItemData passive = _loadout.GetPassive(i);
                if (null != passive && passive.PassiveCategory == category)
                    ++count;
            }

            return count;
        }

        ShopView.UpgradeGrantEntry GrantSellUpgrades()
        {
            int grantCount = Mathf.RoundToInt(_loadout.GetEffectSum(ShopItemData.EEffectType.UpgradeOnPassiveSell));
            ShopView.UpgradeGrantEntry firstGrant = null;
            for (int i = 0; i < grantCount; ++i)
            {
                ShopItemData upgrade = DrawRandomUpgrade();
                if (null == upgrade)
                    break;

                int previousCount = GetOwnedUpgradeCount(upgrade);
                ApplyItem(upgrade);
                if (null == firstGrant)
                    firstGrant = new ShopView.UpgradeGrantEntry(OwnedIndex: _loadout.GetOwnedIndex(upgrade), PreviousCount: previousCount);
            }

            return firstGrant;
        }

        void NotifyItemDenied(ShopItemData item)
        {
            if (item.Kind != ShopItemData.EItemKind.Passive)
            {
                AudioManager.Play(AudioStorage.EAudioClipType.ShopDenied);
                return;
            }

            NotifyPassiveDenied(item);
        }

        void NotifyPassiveDenied(ShopItemData item)
        {
            AudioManager.Play(AudioStorage.EAudioClipType.ShopDenied);
            UIManager.EEvent uiEvent = _loadout.HasConflictingPassive(item) ? UIManager.EEvent.ShopPassiveConflicted : UIManager.EEvent.ShopPassiveSlotFull;
            UIManager.RaiseEvent(uiEvent, (int)item.PassiveCategory);
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

        void RefreshShopView(bool playsRerollReveal, ShopView.PurchaseEntry purchase = null, ShopView.SellEntry sell = null)
        {
            if (null == _session)
                return;

            int stageNumber = _stageNumberGetter();
            bool isEndless = _stageCatalog.IsEndlessStageIndex(stageNumber - 1);
            UIManager.Show(new ShopView.Args(
                Money: _moneyGetter(),
                RerollPrice: GetRerollPrice(),
                Offers: BuildOfferEntries(),
                Purchase: purchase,
                Sell: sell,
                BallPassiveSlotCount: _loadout.GetPassiveSlotCount(ShopItemData.EPassiveCategory.Ball),
                MapPassiveSlotCount: _loadout.GetPassiveSlotCount(ShopItemData.EPassiveCategory.Map),
                CommonPassiveSlotCount: _loadout.GetPassiveSlotCount(ShopItemData.EPassiveCategory.Common),
                Passives: BuildPassiveEntries(),
                Upgrades: BuildUpgradeEntries(),
                Packs: BuildPackEntries(),
                Voucher: BuildVoucherEntry(),
                OwnedVouchers: BuildOwnedVoucherEntries(),
                StageNumber: isEndless ? _stageCatalog.GetEndlessStageNumber(stageNumber - 1) : stageNumber,
                TotalStageCount: _stageCatalog.Count,
                RoundNumber: isEndless ? GetRoundIndex() - _stageCatalog.RoundCount + 1 : GetRoundIndex() + 1,
                RoundCount: _stageCatalog.RoundCount,
                IsEndless: isEndless,
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
                Title: pack.GetNameKey(),
                Icon: pack.GetIcon(),
                Description: pack.GetDescriptionKey(),
                Price: _vouchers.GetDiscountedPrice(pack.Price),
                BandSprite: pack.BandSprite,
                CanBuy: _packShop.HasCandidates(pack));
        }

        ShopView.VoucherEntry BuildVoucherEntry()
        {
            VoucherData voucher = _vouchers.GetOffer(GetRoundIndex(), _voucherCatalog);
            if (null == voucher)
                return null;

            return new ShopView.VoucherEntry(
                Title: voucher.GetNameRef(),
                Icon: voucher.GetIcon(),
                Description: voucher.GetDescriptionRef(),
                Price: _vouchers.GetDiscountedPrice(PlayerVouchers.PRICE));
        }

        VoucherListPopupView.Entry[] BuildOwnedVoucherEntries()
        {
            VoucherListPopupView.Entry[] entries = new VoucherListPopupView.Entry[_vouchers.OwnedCount];
            for (int i = 0; i < entries.Length; ++i)
            {
                VoucherData voucher = _vouchers.GetOwned(i);
                if (null == voucher)
                    continue;

                entries[i] = new VoucherListPopupView.Entry(
                    Title: voucher.GetNameRef(),
                    Icon: voucher.GetIcon(),
                    Description: voucher.GetDescriptionRef());
            }

            return entries;
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

            if (!TrySpendMoney(_vouchers.GetDiscountedPrice(pack.Price)))
                return;

            _packShop.TryStartOpening(slotIndex);
            if (ApplyBonusUpgradeOnBuy())
                OnLoadoutChanged?.Invoke();

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

            if (!TrySpendMoney(_vouchers.GetDiscountedPrice(PlayerVouchers.PRICE)))
                return;

            _vouchers.PurchaseOffer();
            bool isLoadoutChanged = ApplyVoucherImmediateEffect(voucher);
            if (ApplyBonusUpgradeOnBuy() || isLoadoutChanged)
                OnLoadoutChanged?.Invoke();

            AudioManager.Play(AudioStorage.EAudioClipType.ShopBuy);
            OnVoucherPurchased?.Invoke();
            RefreshShopView(false);
        }

        bool ApplyVoucherImmediateEffect(VoucherData voucher)
        {
            switch (voucher.Effect)
            {
                case VoucherData.EEffect.PassiveSlotAdd:
                    _loadout.AddCommonPassiveSlot(Mathf.RoundToInt(voucher.GetEffectValue(VoucherData.MAIN_VALUE_INDEX)));
                    return true;
                case VoucherData.EEffect.ShopFreeReroll:
                    _session.AddFreeRerollCount(Mathf.RoundToInt(voucher.GetEffectValue(VoucherData.MAIN_VALUE_INDEX)));
                    return false;
            }

            return false;
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
                NotifyItemDenied(item);
                return;
            }

            if (null == _packShop.TakePick(cardIndex))
                return;

            int previousCount = GetOwnedUpgradeCount(item);
            ApplyItem(item);
            ConsumeRoundRewardOpening();
            AudioManager.Play(AudioStorage.EAudioClipType.ShopBuy);
            OnLoadoutChanged?.Invoke();

            CardPackPopupView.PickEntry pick = new CardPackPopupView.PickEntry(
                CardIndex: cardIndex,
                IsPassive: item.Kind == ShopItemData.EItemKind.Passive,
                OwnedIndex: _loadout.GetOwnedIndex(item),
                PreviousCount: previousCount);

            if (_packShop.IsOpeningFinished)
            {
                DismissPackOpening(pick);
                return;
            }

            RefreshShopView(false);
            ShowPackOpeningView(false, pick);
        }

        void ShowPackOpeningView(bool isDismissing, CardPackPopupView.PickEntry pick)
        {
            CardPackPopupView.CardArgs[] cards = new CardPackPopupView.CardArgs[_packShop.RevealedItemCount];
            for (int i = 0; i < cards.Length; ++i)
            {
                ShopItemData item = _packShop.GetRevealedItem(i);
                cards[i] = new CardPackPopupView.CardArgs(
                    Title: item.GetNameRef(),
                    Icon: item.GetIcon(),
                    Description: item.GetDescriptionRef(),
                    IsPassive: item.Kind == ShopItemData.EItemKind.Passive,
                    PassiveCategory: item.PassiveCategory,
                    IsTaken: _packShop.IsTaken(i));
            }

            UIManager.Show(new CardPackPopupView.Args(
                PackTitleKey: _packShop.GetOpeningPackNameKey(),
                RemainingPicks: _packShop.RemainingPicks,
                Cards: cards,
                IsDismissing: isDismissing,
                Pick: pick,
                FlyStarter: GetPickFlyStarter()));
        }

        static CardPackPopupView.PickFlyStarter GetPickFlyStarter()
        {
            ShopView shopView = UIManager.GetActive(UIViewCatalogData.EViewType.Shop) as ShopView;
            if (null == shopView)
                return null;

            return shopView.TryBeginPurchaseFly;
        }

        void ClosePackOpening()
        {
            if (null == _packShop || !_packShop.IsOpening)
                return;

            DismissPackOpening(null);
        }

        void DismissPackOpening(CardPackPopupView.PickEntry pick)
        {
            ConsumeRoundRewardOpening();
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
                Title: item.GetNameRef(),
                Icon: item.GetIcon(),
                Description: item.GetDescriptionRef(),
                Price: _vouchers.GetDiscountedPrice(item.Price),
                IsPassive: item.Kind == ShopItemData.EItemKind.Passive,
                PassiveCategory: item.PassiveCategory);
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

            return new ShopView.OwnedEntry(item.GetIcon(), count, item.GetNameRef(), item.GetDescriptionRef(), sellPrice, item.PassiveCategory);
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
                NotifyItemDenied(item);
                return;
            }

            bool isTopUpgrade = item.Kind == ShopItemData.EItemKind.Upgrade && _loadout.IsMostUpgraded(item);
            if (!TrySpendMoney(_vouchers.GetDiscountedPrice(item.Price)))
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

            if (IsPurchaseLimitReached(item))
                return;

            if (!_vouchers.TryRollChance(VoucherData.EEffect.TopUpgradeDoubleBuy))
                return;

            ApplyItem(item);
        }

        bool ApplyBonusUpgradeOnBuy()
        {
            if (!_vouchers.TryRollChance(VoucherData.EEffect.BonusUpgradeOnBuy))
                return false;

            ShopItemData bonus = DrawRandomUpgrade();
            if (null == bonus)
                return false;

            ApplyItem(bonus);
            return true;
        }

        ShopItemData DrawRandomUpgrade()
        {
            List<ShopItemData> candidates = new List<ShopItemData>();
            for (int i = 0; i < _catalog.Count; ++i)
            {
                ShopItemData item = _catalog.GetItem(i);
                if (null == item || item.Kind != ShopItemData.EItemKind.Upgrade)
                    continue;

                if (null != _isItemUnlockedGetter && !_isItemUnlockedGetter(item))
                    continue;

                if (IsPurchaseLimitReached(item))
                    continue;

                candidates.Add(item);
            }

            return RandomDraw.TakeOne(candidates);
        }

        void TryReroll()
        {
            if (null == _session)
                return;

            if (_packShop.IsOpening)
                return;

            if (!TrySpendMoney(GetRerollPrice()))
                return;

            _session.Reroll();
            _packShop.Reroll();
            AudioManager.Play(AudioStorage.EAudioClipType.ShopReroll);
            RefreshShopView(true);
            OnOffersChanged?.Invoke();
        }

        int GetRerollPrice()
        {
            int price = _session.CurrentRerollPrice;
            if (price <= 0)
                return price;

            return _vouchers.GetDiscountedPrice(price);
        }

        bool CanOfferPackItem(ShopItemData item)
        {
            if (!CanOfferItem(item))
                return false;

            if (item.Kind != ShopItemData.EItemKind.Passive)
                return true;

            return null == _session || !_session.IsOffered(item);
        }

        bool CanOfferItem(ShopItemData item)
        {
            if (null == item)
                return false;

            if (null != _isItemUnlockedGetter && !_isItemUnlockedGetter(item))
                return false;

            if (item.Kind != ShopItemData.EItemKind.Passive)
                return !IsPurchaseLimitReached(item);

            return _loadout.CanAddPassive(item);
        }

        bool IsPurchaseLimitReached(ShopItemData item)
        {
            if (item.MaxPurchaseCount <= ShopItemData.NO_PURCHASE_LIMIT)
                return false;

            return GetOwnedUpgradeCount(item) >= item.MaxPurchaseCount;
        }

        bool CanApplyItem(ShopItemData item)
        {
            if (item.Kind != ShopItemData.EItemKind.Passive)
                return !IsPurchaseLimitReached(item);

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
