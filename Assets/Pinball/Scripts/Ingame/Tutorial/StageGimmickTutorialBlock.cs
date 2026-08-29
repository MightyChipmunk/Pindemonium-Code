using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.UI;

namespace Pinball.Ingame.Tutorial
{
    public class StageGimmickTutorialBlock
    {
        readonly TutorialStepCatalogData _catalog;
        readonly TutorialDialogView _dialog;
        readonly TutorialHighlighter _highlighter;

        public StageGimmickTutorialBlock(TutorialStepCatalogData catalog, TutorialDialogView dialog, TutorialHighlighter highlighter)
        {
            _catalog = catalog;
            _dialog = dialog;
            _highlighter = highlighter;
        }

        public async UniTask<bool> RunAsync(CancellationToken cancellationToken)
        {
            await ScreenTransition.WaitUntilIdleAsync(cancellationToken);

            StageSelectView view = UIManager.GetActive(UIViewCatalogData.EViewType.StageSelect) as StageSelectView;
            if (null == view)
                return false;

            await UniTask.WaitWhile(view.IsTransitionPlaying, cancellationToken: cancellationToken);

            await ShowStepAsync(TutorialStepCatalogData.EStep.StageSelectGimmick, cancellationToken);
            await ShowStepAsync(TutorialStepCatalogData.EStep.StageSelectReroll, cancellationToken);

            _highlighter.Clear();
            _dialog.Hide();
            return true;
        }

        async UniTask ShowStepAsync(TutorialStepCatalogData.EStep id, CancellationToken cancellationToken)
        {
            TutorialStepCatalogData.Step step = _catalog.GetStep(id);
            _highlighter.Apply(step.Highlight);
            await _dialog.ShowAndWaitClickAsync(step.Key, step.Placement, step.Mascot, cancellationToken);
        }
    }
}
