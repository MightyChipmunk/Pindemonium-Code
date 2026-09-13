using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.UI;
using GameCore.UI;

namespace Pinball.Ingame.Tutorial
{
    public class StageGimmickTutorialBlock
    {
        readonly TutorialStepCatalogData _catalog;
        readonly TutorialDialogView _dialog;
        readonly TutorialHighlighter _highlighter;
        readonly TutorialStepRunner _runner;

        public StageGimmickTutorialBlock(TutorialStepCatalogData catalog, TutorialDialogView dialog, TutorialHighlighter highlighter)
        {
            _catalog = catalog;
            _dialog = dialog;
            _highlighter = highlighter;
            _runner = new TutorialStepRunner(dialog, highlighter);
        }

        public async UniTask<bool> RunAsync(CancellationToken cancellationToken)
        {
            await ScreenTransition.WaitUntilIdleAsync(cancellationToken);

            StageSelectView view = UIManager.GetActive(UIViewCatalogData.EViewType.StageSelect) as StageSelectView;
            if (null == view)
                return false;

            await UniTask.WaitWhile(view.IsTransitionPlaying, cancellationToken: cancellationToken);

            for (int i = 0, count = _catalog.GetFlowStepCount(TutorialStepCatalogData.EFlow.StageGimmick); i < count; ++i)
            {
                TutorialStepCatalogData.Step step = _catalog.GetFlowStep(TutorialStepCatalogData.EFlow.StageGimmick, i);
                await _runner.ShowAsync(step, null, cancellationToken);
            }

            _highlighter.Clear();
            _dialog.Hide();
            return true;
        }
    }
}
