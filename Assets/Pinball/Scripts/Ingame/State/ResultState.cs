using System;
using Pinball.Audio;
using Pinball.Ingame.Presentation;
using Pinball.UI;

namespace Pinball.Ingame.State
{
    public class ResultState : IngameStateBase
    {
        public record InitParam(
            Func<IngameResultPresenter.RunSummary> SummaryGetter,
            Action<bool> ResultAudioPlayer,
            Action PlayDataClearer,
            Action ClearRewardGranter);

        public record EnterParam(GameResultView.EResult Result);

        InitParam _param = null;
        IngameResultPresenter _presenter = null;

        public override EIngameState Key => EIngameState.Result;

        public ResultState(InitParam param, IngameResultPresenter presenter)
        {
            _param = param;
            _presenter = presenter;
        }

        protected override void OnEnter(object param)
        {
            EnterParam enterParam = param as EnterParam;
            if (null == enterParam)
                return;

            BgmManager.Stop();

            bool isClear = enterParam.Result == GameResultView.EResult.Clear;
            _param.PlayDataClearer?.Invoke();
            if (isClear)
                _param.ClearRewardGranter?.Invoke();

            _param.ResultAudioPlayer?.Invoke(isClear);
            _presenter.ShowGameResult(enterParam.Result, _param.SummaryGetter());
        }
    }
}
