using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.Localization;
using Pinball.UI;
using UnityEngine;

namespace Pinball.Ingame.Tutorial
{
    public class ShopTutorialBlock
    {
        public record InitParam(int InterestPerAmount, int InterestMax, int PassiveSlotCount);

        const string OFFERS_KEY = "ui.tutorial.shop.offers";
        const string PASSIVE_LIMIT_KEY = "ui.tutorial.shop.passive_limit";
        const string PACKS_KEY = "ui.tutorial.shop.packs";
        const string VOUCHER_KEY = "ui.tutorial.shop.voucher";
        const string REROLL_KEY = "ui.tutorial.shop.reroll";
        const string INTEREST_KEY = "ui.tutorial.shop.interest";
        const string START_KEY = "ui.tutorial.shop.start";

        readonly InitParam _param;
        readonly TutorialDimmer _dimmer;
        readonly TutorialDialogView _dialog;
        readonly TutorialHighlightLayer _highlightLayer;
        readonly List<RectTransform> _anchorBuffer = new List<RectTransform>();

        public ShopTutorialBlock(InitParam param, TutorialDimmer dimmer, TutorialDialogView dialog, TutorialHighlightLayer highlightLayer)
        {
            _param = param;
            _dimmer = dimmer;
            _dialog = dialog;
            _highlightLayer = highlightLayer;
        }

        public async UniTask<bool> RunAsync(CancellationToken cancellationToken)
        {
            await ScreenTransition.WaitUntilIdleAsync(cancellationToken);

            ShopView view = UIManager.GetActive(UIViewCatalogData.EViewType.Shop) as ShopView;
            if (null == view)
                return false;

            await UniTask.WaitWhile(view.IsTransitionPlaying, cancellationToken: cancellationToken);

            FocusUI(view.GetOfferRowAnchor());
            await _dialog.ShowAndWaitClickAsync(OFFERS_KEY, TutorialDialogView.EPlacement.Bottom, cancellationToken);

            LocalizedTextRef passiveLimitText = new LocalizedTextRef(PASSIVE_LIMIT_KEY, new object[] { _param.PassiveSlotCount });
            await _dialog.ShowAndWaitClickAsync(passiveLimitText, TutorialDialogView.EPlacement.Bottom, cancellationToken);

            _anchorBuffer.Clear();
            for (int i = 0, count = view.GetPackSlotCount(); i < count; ++i)
                _anchorBuffer.Add(view.GetPackSlotAnchor(i));

            _dimmer.ShowFull();
            _highlightLayer.ShowUITargets(_anchorBuffer);
            await _dialog.ShowAndWaitClickAsync(PACKS_KEY, TutorialDialogView.EPlacement.Top, cancellationToken);

            FocusUI(view.GetVoucherAnchor());
            await _dialog.ShowAndWaitClickAsync(VOUCHER_KEY, TutorialDialogView.EPlacement.Top, cancellationToken);

            FocusUI(view.GetRerollAnchor());
            await _dialog.ShowAndWaitClickAsync(REROLL_KEY, TutorialDialogView.EPlacement.Top, cancellationToken);

            FocusUI(view.GetMoneyAnchor());
            LocalizedTextRef interestText = new LocalizedTextRef(INTEREST_KEY, new object[] { _param.InterestPerAmount, _param.InterestMax });
            await _dialog.ShowAndWaitClickAsync(interestText, TutorialDialogView.EPlacement.Bottom, cancellationToken);

            FocusUI(view.GetStartAnchor());
            await _dialog.ShowAndWaitClickAsync(START_KEY, TutorialDialogView.EPlacement.Top, cancellationToken);

            _highlightLayer.Clear();
            _dimmer.Hide();
            _dialog.Hide();
            return true;
        }

        void FocusUI(RectTransform target)
        {
            _dimmer.ShowFull();
            _highlightLayer.ShowUITarget(target);
        }
    }
}
