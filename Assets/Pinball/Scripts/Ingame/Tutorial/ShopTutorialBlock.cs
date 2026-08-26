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
        public record InitParam(int InterestPerAmount, int InterestMax);

        const float UI_FOCUS_PADDING = 12f;
        const string OFFERS_KEY = "ui.tutorial.shop.offers";
        const string PACKS_KEY = "ui.tutorial.shop.packs";
        const string VOUCHER_KEY = "ui.tutorial.shop.voucher";
        const string REROLL_KEY = "ui.tutorial.shop.reroll";
        const string INTEREST_KEY = "ui.tutorial.shop.interest";
        const string START_KEY = "ui.tutorial.shop.start";

        readonly InitParam _param;
        readonly TutorialDimmer _dimmer;
        readonly TutorialDialogView _dialog;
        readonly List<RectTransform> _anchorBuffer = new List<RectTransform>();

        public ShopTutorialBlock(InitParam param, TutorialDimmer dimmer, TutorialDialogView dialog)
        {
            _param = param;
            _dimmer = dimmer;
            _dialog = dialog;
        }

        public async UniTask<bool> RunAsync(CancellationToken cancellationToken)
        {
            ShopView view = UIManager.GetActive(UIViewCatalogData.EViewType.Shop) as ShopView;
            if (null == view)
                return false;

            _dimmer.FocusUI(view.GetOfferRowAnchor(), UI_FOCUS_PADDING);
            await _dialog.ShowAndWaitClickAsync(OFFERS_KEY, TutorialDialogView.EPlacement.Bottom, cancellationToken);

            _anchorBuffer.Clear();
            for (int i = 0, count = view.GetPackSlotCount(); i < count; ++i)
                _anchorBuffer.Add(view.GetPackSlotAnchor(i));

            _dimmer.FocusUITargets(_anchorBuffer, UI_FOCUS_PADDING);
            await _dialog.ShowAndWaitClickAsync(PACKS_KEY, TutorialDialogView.EPlacement.Top, cancellationToken);

            _dimmer.FocusUI(view.GetVoucherAnchor(), UI_FOCUS_PADDING);
            await _dialog.ShowAndWaitClickAsync(VOUCHER_KEY, TutorialDialogView.EPlacement.Top, cancellationToken);

            _dimmer.FocusUI(view.GetRerollAnchor(), UI_FOCUS_PADDING);
            await _dialog.ShowAndWaitClickAsync(REROLL_KEY, TutorialDialogView.EPlacement.Top, cancellationToken);

            _dimmer.FocusUI(view.GetMoneyAnchor(), UI_FOCUS_PADDING);
            LocalizedTextRef interestText = new LocalizedTextRef(INTEREST_KEY, new object[] { _param.InterestPerAmount, _param.InterestMax });
            await _dialog.ShowAndWaitClickAsync(interestText, TutorialDialogView.EPlacement.Bottom, cancellationToken);

            _dimmer.FocusUI(view.GetStartAnchor(), UI_FOCUS_PADDING);
            await _dialog.ShowAndWaitClickAsync(START_KEY, TutorialDialogView.EPlacement.Top, cancellationToken);

            _dimmer.Hide();
            _dialog.Hide();
            return true;
        }
    }
}
