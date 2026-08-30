using System.Threading;
using Cysharp.Threading.Tasks;
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
        readonly TutorialStepRunner _runner;

        public ShopTutorialBlock(InitParam param, TutorialStepCatalogData catalog, TutorialDialogView dialog, TutorialHighlighter highlighter)
        {
            _param = param;
            _catalog = catalog;
            _dialog = dialog;
            _highlighter = highlighter;
            _runner = new TutorialStepRunner(dialog, highlighter);
        }

        public async UniTask<bool> RunAsync(CancellationToken cancellationToken)
        {
            await ScreenTransition.WaitUntilIdleAsync(cancellationToken);

            ShopView view = UIManager.GetActive(UIViewCatalogData.EViewType.Shop) as ShopView;
            if (null == view)
                return false;

            await UniTask.WaitWhile(view.IsTransitionPlaying, cancellationToken: cancellationToken);

            for (int i = 0, count = _catalog.GetFlowStepCount(TutorialStepCatalogData.EFlow.Shop); i < count; ++i)
            {
                TutorialStepCatalogData.Step step = _catalog.GetFlowStep(TutorialStepCatalogData.EFlow.Shop, i);
                await _runner.ShowAsync(step, GetStepArgs(step.Id), cancellationToken);
            }

            _highlighter.Clear();
            _dialog.Hide();
            return true;
        }

        object[] GetStepArgs(TutorialStepCatalogData.EStep id)
        {
            switch (id)
            {
                case TutorialStepCatalogData.EStep.ShopPassiveLimit:
                    return new object[] { _param.PassiveSlotCount };
                case TutorialStepCatalogData.EStep.ShopInterest:
                    return new object[] { _param.InterestPerAmount, _param.InterestMax };
                default:
                    return null;
            }
        }
    }
}
