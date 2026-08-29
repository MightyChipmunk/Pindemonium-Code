using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.Localization;
using Pinball.UI;

namespace Pinball.Ingame.Tutorial
{
    public class ShopTutorialBlock
    {
        public record InitParam(int InterestPerAmount, int InterestMax, int PassiveSlotCount);

        readonly InitParam _param;
        readonly TutorialStepCatalogData _catalog;
        readonly TutorialDialogView _dialog;
        readonly TutorialHighlighter _highlighter;

        public ShopTutorialBlock(InitParam param, TutorialStepCatalogData catalog, TutorialDialogView dialog, TutorialHighlighter highlighter)
        {
            _param = param;
            _catalog = catalog;
            _dialog = dialog;
            _highlighter = highlighter;
        }

        public async UniTask<bool> RunAsync(CancellationToken cancellationToken)
        {
            await ScreenTransition.WaitUntilIdleAsync(cancellationToken);

            ShopView view = UIManager.GetActive(UIViewCatalogData.EViewType.Shop) as ShopView;
            if (null == view)
                return false;

            await UniTask.WaitWhile(view.IsTransitionPlaying, cancellationToken: cancellationToken);

            await ShowStepAsync(TutorialStepCatalogData.EStep.ShopOffers, null, cancellationToken);
            await ShowStepAsync(TutorialStepCatalogData.EStep.ShopPassiveLimit, new object[] { _param.PassiveSlotCount }, cancellationToken);
            await ShowStepAsync(TutorialStepCatalogData.EStep.ShopPacks, null, cancellationToken);
            await ShowStepAsync(TutorialStepCatalogData.EStep.ShopVoucher, null, cancellationToken);
            await ShowStepAsync(TutorialStepCatalogData.EStep.ShopReroll, null, cancellationToken);
            await ShowStepAsync(TutorialStepCatalogData.EStep.ShopInterest, new object[] { _param.InterestPerAmount, _param.InterestMax }, cancellationToken);
            await ShowStepAsync(TutorialStepCatalogData.EStep.ShopStart, null, cancellationToken);

            _highlighter.Clear();
            _dialog.Hide();
            return true;
        }

        async UniTask ShowStepAsync(TutorialStepCatalogData.EStep id, object[] argArr, CancellationToken cancellationToken)
        {
            TutorialStepCatalogData.Step step = _catalog.GetStep(id);
            _highlighter.Apply(step.Highlight);
            LocalizedTextRef body = new LocalizedTextRef(step.Key, argArr);
            await _dialog.ShowAndWaitClickAsync(body, step.Placement, step.Mascot, cancellationToken);
        }
    }
}
