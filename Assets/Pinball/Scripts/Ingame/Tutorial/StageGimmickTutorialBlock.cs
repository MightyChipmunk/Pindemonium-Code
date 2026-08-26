using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.UI;

namespace Pinball.Ingame.Tutorial
{
    public class StageGimmickTutorialBlock
    {
        const float UI_FOCUS_PADDING = 12f;
        const string GIMMICK_KEY = "ui.tutorial.stage_select.gimmick";
        const string REROLL_KEY = "ui.tutorial.stage_select.reroll";

        readonly TutorialDimmer _dimmer;
        readonly TutorialDialogView _dialog;

        public StageGimmickTutorialBlock(TutorialDimmer dimmer, TutorialDialogView dialog)
        {
            _dimmer = dimmer;
            _dialog = dialog;
        }

        public async UniTask<bool> RunAsync(CancellationToken cancellationToken)
        {
            StageSelectView view = UIManager.GetActive(UIViewCatalogData.EViewType.StageSelect) as StageSelectView;
            if (null == view)
                return false;

            _dimmer.FocusUI(view.GetCurrentGimmickAnchor(), UI_FOCUS_PADDING);
            await _dialog.ShowAndWaitClickAsync(GIMMICK_KEY, TutorialDialogView.EPlacement.Bottom, cancellationToken);

            _dimmer.FocusUI(view.GetCurrentRerollAnchor(), UI_FOCUS_PADDING);
            await _dialog.ShowAndWaitClickAsync(REROLL_KEY, TutorialDialogView.EPlacement.Bottom, cancellationToken);

            _dimmer.Hide();
            _dialog.Hide();
            return true;
        }
    }
}
