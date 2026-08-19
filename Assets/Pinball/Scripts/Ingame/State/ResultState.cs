using System;
using Pinball.Ingame.Presentation;
using Pinball.Ingame.Reward;
using Pinball.UI;

namespace Pinball.Ingame.State
{
    public class ResultState : IngameStateBase
    {
        public record InitParam(
            Func<IngameResultPresenter.RunSummary> SummaryGetter,
            Action<bool> ResultAudioPlayer,
            Action RewardAudioPlayer,
            Action PlayDataClearer,
            Func<ClearRewardData> ClearRewardGranter);

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

            bool isClear = enterParam.Result == GameResultView.EResult.Clear;
            _param.PlayDataClearer?.Invoke();
            ClearRewardData reward = isClear && null != _param.ClearRewardGranter ? _param.ClearRewardGranter() : null;

            _param.ResultAudioPlayer?.Invoke(isClear);
            _presenter.ShowGameResult(enterParam.Result, _param.SummaryGetter());

            if (_presenter.TryShowRewardUnlock(reward))
                _param.RewardAudioPlayer?.Invoke();
        }
    }
}
