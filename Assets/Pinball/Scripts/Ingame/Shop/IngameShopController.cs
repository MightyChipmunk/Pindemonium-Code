using System;
using Pinball.Audio;
using Pinball.UI;
using UnityEngine;

namespace Pinball.Ingame
{
    public class IngameShopController
    {
        const int PASSIVE_OWNED_COUNT = 1;

        public record InitParam(
            ShopConfigData Config,
            ShopCatalogData Catalog,
            MoneyWallet Wallet,
            PlayerLoadout Loadout,
            Func<ShopItemData, bool> IsItemUnlockedGetter,
            int OfferSlotCount,
            bool IsInterestDisabled);

        ShopConfigData _config = null;
        ShopCatalogData _catalog = null;
        MoneyWallet _wallet = null;
        PlayerLoadout _loadout = null;
        Func<ShopItemData, bool> _isItemUnlockedGetter = null;
        ShopSession _session = null;
        int _offerSlotCount = ShopSession.DEFAULT_OFFER_SLOT_COUNT;
        bool _isInterestDisabled = false;

        public event Action OnNextStageRequested;
        public event Action OnLoadoutChanged;

        public void Init(InitParam param)
        {
            _config = param.Config;
            _catalog = param.Catalog;
            _wallet = param.Wallet;
            _loadout = param.Loadout;
            _isItemUnlockedGetter = param.IsItemUnlockedGetter;
            _offerSlotCount = param.OfferSlotCount;
            _isInterestDisabled = param.IsInterestDisabled;
            UIManager.OnEvent += HandleUIEvent;
        }

        public void Release()
        {
            UIManager.OnEvent -= HandleUIEvent;
            OnNextStageRequested = null;
            OnLoadoutChanged = null;

            if (null != _session)
            {
                _session.Release();
                _session = null;
            }

            _isItemUnlockedGetter = null;
            _loadout = null;
            _wallet = null;
            _catalog = null;
            _config = null;
        }

        public int CalculateInterest()
        {
            if (_isInterestDisabled)
                return 0;

            if (null == _config)
                return 0;

            if (_config.InterestPerAmount <= 0)
                return 0;

            return Mathf.Min(_wallet.Current / _config.InterestPerAmount, _config.InterestMax);
        }

        public void RefreshOpenedView()
        {
            if (null == _session)
                return;

            RefreshShopView();
        }

        void HandleUIEvent(UIManager.EEvent uiEvent, int payload)
        {
            switch (uiEvent)
            {
                case UIManager.EEvent.StageClearFinished:
                    Open();
                    break;
                case UIManager.EEvent.ShopStartRequested:
                    CloseAndRequestNextStage();
                    break;
                case UIManager.EEvent.ShopBuyRequested:
                    TryBuyOffer(payload);
                    break;
                case UIManager.EEvent.ShopRerollRequested:
                    TryReroll();
                    break;
            }
        }

        public void Open()
        {
            if (null != _session)
                _session.Release();

            _session = new ShopSession();
            _session.Init(_config, _catalog, CanOfferItem, _offerSlotCount);
            RefreshShopView();
        }

        void CloseAndRequestNextStage()
        {
            if (null != _session)
            {
                _session.Release();
                _session = null;
            }

            OnNextStageRequested?.Invoke();
        }

        void RefreshShopView()
        {
            if (null == _session)
                return;

            UIManager.Show(new ShopView.Args(
                Money: _wallet.Current,
                RerollPrice: _session.CurrentRerollPrice,
                Offers: BuildOfferEntries(),
                PassiveSlotCount: _loadout.PassiveSlotCount,
                Passives: BuildPassiveEntries(),
                Upgrades: BuildUpgradeEntries()));
        }

        ShopView.OfferEntry[] BuildOfferEntries()
        {
            ShopView.OfferEntry[] offers = new ShopView.OfferEntry[_session.OfferSlotCount];
            for (int i = 0; i < offers.Length; ++i)
                offers[i] = BuildOfferEntry(_session.GetOffer(i));

            return offers;
        }

        static ShopView.OfferEntry BuildOfferEntry(ShopItemData item)
        {
            if (null == item)
                return null;

            return new ShopView.OfferEntry(
                Title: item.GetDisplayName(),
                Icon: item.GetIcon(),
                Description: item.GetDescription(),
                Price: item.Price,
                IsPassive: item.Kind == ShopItemData.EItemKind.Passive);
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

            if (!_wallet.TrySpend(item.Price))
                return;

            ApplyItem(item);
            AudioManager.Play(AudioStorage.EAudioClipType.ShopBuy);
            _session.MarkSold(slotIndex);
            OnLoadoutChanged?.Invoke();
            RefreshShopView();
        }

        void TryReroll()
        {
            if (null == _session)
                return;

            if (!_wallet.TrySpend(_session.CurrentRerollPrice))
                return;

            _session.Reroll();
            AudioManager.Play(AudioStorage.EAudioClipType.ShopReroll);
            RefreshShopView();
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
