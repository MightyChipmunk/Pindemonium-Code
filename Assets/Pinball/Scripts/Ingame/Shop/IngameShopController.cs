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

        void Open()
        {
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

            UIManager.Show(new ShopView.Args(_wallet.Current, _session.CurrentRerollPrice, _session.OfferSlotCount));

            for (int i = 0; i < _session.OfferSlotCount; ++i)
                UIManager.Show(new ShopView.OfferArgs(i, GetOfferItemIndex(i)));

            RefreshOwnedView();
        }

        void RefreshOwnedView()
        {
            UIManager.Show(new ShopView.OwnedResetArgs(_loadout.PassiveCount, _loadout.PassiveSlotCount, _loadout.UpgradeKindCount));

            for (int i = 0; i < _loadout.PassiveCount; ++i)
                UIManager.Show(new ShopView.OwnedItemArgs(GetPassiveItemIndex(i), PASSIVE_OWNED_COUNT));

            for (int i = 0; i < _loadout.UpgradeKindCount; ++i)
                UIManager.Show(new ShopView.OwnedItemArgs(GetUpgradeItemIndex(i), _loadout.GetUpgradeCount(i)));
        }

        int GetOfferItemIndex(int slotIndex)
        {
            if (null == _catalog || null == _session)
                return ShopCatalogData.INVALID_INDEX;

            return _catalog.IndexOf(_session.GetOffer(slotIndex));
        }

        int GetPassiveItemIndex(int passiveIndex)
        {
            if (null == _catalog || null == _loadout)
                return ShopCatalogData.INVALID_INDEX;

            return _catalog.IndexOf(_loadout.GetPassive(passiveIndex));
        }

        int GetUpgradeItemIndex(int upgradeIndex)
        {
            if (null == _catalog || null == _loadout)
                return ShopCatalogData.INVALID_INDEX;

            return _catalog.IndexOf(_loadout.GetUpgrade(upgradeIndex));
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
