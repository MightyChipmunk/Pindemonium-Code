using System;
using UIViewManager = GameCore.UI.UIManager<Pinball.UI.UIViewCatalogData.EViewType>;
using GameCore.UI;

namespace Pinball.UI
{
    /// <summary>
    /// GameCore.UIManager에 게임 이벤트 버스(EEvent)를 얹은 파사드.
    /// </summary>
    public static class UIManager
    {
        public enum EEvent
        {
            StageClearFinished = 0,
            ShopStartRequested = 1,
            ExitToLobbyRequested = 2,
            ShopBuyRequested = 3,
            ShopRerollRequested = 4,
            StageSelectStartRequested = 5,
            StageSelectRerollRequested = 6,
            FlipperSelectPlayRequested = 7,
            ContinueRunRequested = 8,
            PauseResumeRequested = 9,
            ShopPackBuyRequested = 10,
            CardPackPickRequested = 11,
            CardPackSkipRequested = 12,
            ShopVoucherBuyRequested = 13,
            ShopPurchaseDenied = 14,
            ShopPassiveSlotFull = 15,
            ShopSellRequested = 16,
            RoundClearFinished = 17,
            ShopPassiveConflicted = 18,
            EndlessStartRequested = 19,
            EscapeUnhandled = 20,
        }

        public const int EVENT_PAYLOAD_NONE = 0;

        public static bool IsInitialized => UIViewManager.IsInitialized;

        public static event Action<EEvent, int> OnEvent = null;

        public static void EnsureInit(UIViewCatalogData catalog)
        {
            if (IsInitialized)
                return;

            UIViewManager.Init(catalog);
            UIViewManager.OnEscapeUnhandled -= OnEscapeUnhandled;
            UIViewManager.OnEscapeUnhandled += OnEscapeUnhandled;
        }

        public static void Release()
        {
            UIViewManager.OnEscapeUnhandled -= OnEscapeUnhandled;
            UIViewManager.Release();
            OnEvent = null;
        }

        public static void Show(UIViewArgs args)
        {
            UIViewManager.Show(args);
        }

        public static UIViewBase GetActive(UIViewCatalogData.EViewType viewType)
        {
            return UIViewManager.GetActive(viewType) as UIViewBase;
        }

        public static void Hide(UIViewCatalogData.EViewType viewType)
        {
            UIViewManager.Hide(viewType);
        }

        public static void HideImmediate(UIViewCatalogData.EViewType viewType)
        {
            UIViewManager.HideImmediate(viewType);
        }

        public static void HideAll()
        {
            UIViewManager.HideAll();
        }

        public static bool IsOpen(UIViewCatalogData.EViewType viewType)
        {
            return UIViewManager.IsOpen(viewType);
        }

        public static void RaiseEvent(EEvent uiEvent)
        {
            RaiseEvent(uiEvent, EVENT_PAYLOAD_NONE);
        }

        public static void RaiseEvent(EEvent uiEvent, int payload)
        {
            OnEvent?.Invoke(uiEvent, payload);
        }

        static void OnEscapeUnhandled()
        {
            RaiseEvent(EEvent.EscapeUnhandled);
        }
    }
}
