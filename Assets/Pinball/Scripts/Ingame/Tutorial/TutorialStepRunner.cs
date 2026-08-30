using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.Localization;

namespace Pinball.Ingame.Tutorial
{
    public class TutorialStepRunner
    {
        readonly TutorialDialogView _dialog;
        readonly TutorialHighlighter _highlighter;

        public TutorialStepRunner(TutorialDialogView dialog, TutorialHighlighter highlighter)
        {
            _dialog = dialog;
            _highlighter = highlighter;
        }

        // Returns false when the highlight target is absent, so an optional step is
        // skipped without disturbing the overlay that is already showing.
        public async UniTask<bool> ShowAsync(TutorialStepCatalogData.Step step, object[] argArr, CancellationToken cancellationToken)
        {
            if (!_highlighter.Apply(step.Highlight))
                return false;

            LocalizedTextRef body = new LocalizedTextRef(step.Key, argArr);
            await _dialog.ShowAndWaitClickAsync(body, step.Placement, step.Mascot, cancellationToken);

            string secondPageKey = TutorialStepCatalogData.GetSecondPageKey(step.Key);
            if (string.IsNullOrEmpty(LocalizationTable.Get(secondPageKey, string.Empty)))
                return true;

            LocalizedTextRef secondBody = new LocalizedTextRef(secondPageKey, argArr);
            await _dialog.ShowAndWaitClickAsync(secondBody, step.Placement, step.Mascot, cancellationToken);
            return true;
        }
    }
}
