using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.UI;
using UnityEngine;

namespace Pinball.Ingame.Tutorial
{
    public class StageGimmickTutorialBlock
    {
        const string GIMMICK_KEY = "ui.tutorial.stage_select.gimmick";
        const string REROLL_KEY = "ui.tutorial.stage_select.reroll";

        readonly TutorialDimmer _dimmer;
        readonly TutorialDialogView _dialog;
        readonly TutorialHighlightLayer _highlightLayer;

        public StageGimmickTutorialBlock(TutorialDimmer dimmer, TutorialDialogView dialog, TutorialHighlightLayer highlightLayer)
        {
            _dimmer = dimmer;
            _dialog = dialog;
            _highlightLayer = highlightLayer;
        }

        public async UniTask<bool> RunAsync(CancellationToken cancellationToken)
        {
            await ScreenTransition.WaitUntilIdleAsync(cancellationToken);

            StageSelectView view = UIManager.GetActive(UIViewCatalogData.EViewType.StageSelect) as StageSelectView;
            if (null == view)
                return false;

            await UniTask.WaitWhile(view.IsTransitionPlaying, cancellationToken: cancellationToken);

            FocusUI(view.GetCurrentGimmickAnchor());
            await _dialog.ShowAndWaitClickAsync(GIMMICK_KEY, TutorialDialogView.EPlacement.Top, cancellationToken);

            FocusUI(view.GetCurrentRerollAnchor());
            await _dialog.ShowAndWaitClickAsync(REROLL_KEY, TutorialDialogView.EPlacement.Top, cancellationToken);

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
