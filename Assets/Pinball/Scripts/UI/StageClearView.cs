using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.Localization;
using Pinball.Util;
using TMPro;
using UnityEngine;

namespace Pinball.UI
{
    public class StageClearView : UIViewBase
    {
        public record Args(int StageNumber) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.StageClear;
        }

        const string CLEAR_FORMAT = "STAGE {0} CLEAR!";
        const string CLEAR_KEY = "ui.stage_clear.title";
        const float SCALE_DURATION = 0.35f;
        const float HOLD_DURATION = 1.2f;

        [SerializeField] TMP_Text _label = null;
        [SerializeField] RectTransform _panel = null;

        CancellationTokenSource _cancellationTokenSource = null;

        public override bool IsTransient => true;

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            _label.text = LocalizationTable.Format(CLEAR_KEY, CLEAR_FORMAT, viewArgs.StageNumber);
            _panel.localScale = Vector3.zero;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            PlayAsync(_cancellationTokenSource.Token).Forget();
        }

        public override void OnReturn()
        {
            ReleasePlayHandle();
        }

        void OnDestroy()
        {
            ReleasePlayHandle();
        }

        void ReleasePlayHandle()
        {
            if (null == _cancellationTokenSource)
                return;

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        async UniTaskVoid PlayAsync(CancellationToken cancellationToken)
        {
            float time = 0f;
            while (time <= SCALE_DURATION)
            {
                float ratio = EaseUtil.Interpolate(EaseType.ElasticOut, time / SCALE_DURATION);
                _panel.localScale = Vector3.one * ratio;
                time += Time.deltaTime;
                await UniTask.Yield(cancellationToken);
            }

            _panel.localScale = Vector3.one;
            await UniTask.Delay(TimeSpan.FromSeconds(HOLD_DURATION), cancellationToken: cancellationToken);

            UIManager.RaiseEvent(UIManager.EEvent.StageClearFinished);

            if (null != OwnPool)
                OwnPool.Return(this);
        }
    }
}
