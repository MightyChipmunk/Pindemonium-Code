using DG.Tweening;
using Pinball.Localization;
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

        Tween _playTween = null;

        public override bool IsTransient => true;

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            _label.text = LocalizationTable.Format(CLEAR_KEY, CLEAR_FORMAT, viewArgs.StageNumber);
            PlayShow();
        }

        public override void OnReturn()
        {
            KillPlay();
        }

        public override void OnTerminate()
        {
            KillPlay();
        }

        void PlayShow()
        {
            KillPlay();
            _panel.localScale = Vector3.zero;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(_panel.DOScale(Vector3.one, SCALE_DURATION).SetEase(Ease.OutElastic));
            sequence.AppendInterval(HOLD_DURATION);
            sequence.OnComplete(OnShowCompleted);
            sequence.SetUpdate(true);
            _playTween = sequence;
        }

        void OnShowCompleted()
        {
            _playTween = null;
            UIManager.RaiseEvent(UIManager.EEvent.StageClearFinished);

            if (null != OwnPool)
                OwnPool.Return(this);
        }

        void KillPlay()
        {
            if (null == _playTween)
                return;

            _playTween.Kill();
            _playTween = null;
        }
    }
}
