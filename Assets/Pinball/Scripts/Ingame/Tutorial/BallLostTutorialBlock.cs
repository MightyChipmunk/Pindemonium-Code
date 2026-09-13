using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.Settings;
using GameCore.Localization;
using GameCore.Settings;

namespace Pinball.Ingame.Tutorial
{
    public class BallLostTutorialBlock
    {
        public record InitParam(
            Action LaunchWaitPreparer,
            Func<bool> HasLaunchedGetter,
            Func<bool> IsPlayStateGetter,
            Func<bool> IsStageClearInterceptedGetter);

        readonly InitParam _param;
        readonly TutorialStepCatalogData _catalog;
        readonly TutorialDialogView _dialog;
        readonly TutorialHighlighter _highlighter;

        public BallLostTutorialBlock(InitParam param, TutorialStepCatalogData catalog, TutorialDialogView dialog, TutorialHighlighter highlighter)
        {
            _param = param;
            _catalog = catalog;
            _dialog = dialog;
            _highlighter = highlighter;
        }

        // The guide is hidden once the player has already seen it, but the wait still
        // runs so a caller stays paused until the next ball is back in play.
        public async UniTask<bool> RunAsync(bool isGuideVisible, CancellationToken cancellationToken)
        {
            _param.LaunchWaitPreparer();

            if (isGuideVisible)
            {
                TutorialStepCatalogData.Step step = _catalog.GetStep(TutorialStepCatalogData.EStep.PlayBallLost);
                _highlighter.Apply(step.Highlight);
                LocalizedTextRef lostText = new LocalizedTextRef(step.Key, new object[] { GetKeyName(EKeyAction.Launch) });
                _dialog.ShowGuide(lostText, step.Placement, step.Mascot);
            }

            await UniTask.WaitUntil(GetIsRelaunchWaitFinished, cancellationToken: cancellationToken);

            if (isGuideVisible)
            {
                _dialog.Hide();
                _highlighter.Clear();
            }

            return _param.HasLaunchedGetter();
        }

        bool GetIsRelaunchWaitFinished()
        {
            if (_param.HasLaunchedGetter() || _param.IsStageClearInterceptedGetter())
                return true;

            return !_param.IsPlayStateGetter();
        }

        static string GetKeyName(EKeyAction action)
        {
            return KeyLabel.Get(GameSettings.GetKey(action));
        }
    }
}
