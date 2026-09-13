using DG.Tweening;
using TMPro;
using UnityEngine;
using GameCore.Localization;
using GameCore.UI;

namespace Pinball.UI
{
    public class RoundClearView : UIViewBase
    {
        public record Args(int RoundNumber, bool IsEndless) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.RoundClear;
        }

        const string TITLE_FORMAT = "ROUND {0} CLEAR!";
        const string TITLE_KEY = "ui.round_clear.title";
        const string ENDLESS_TITLE_FORMAT = "ENDLESS ROUND {0} CLEAR!";
        const string ENDLESS_TITLE_KEY = "ui.round_clear.endless_title";
        const string REWARD_TEXT = "BONUS CARD PACK!";
        const string REWARD_KEY = "ui.round_clear.reward";
        const float INTRO_DELAY = 0.25f;
        const float BACKDROP_ALPHA = 0.72f;
        const float BACKDROP_HIDDEN_ALPHA = 0f;
        const float BACKDROP_DURATION = 0.2f;
        const float PANEL_POP_DURATION = 0.4f;
        const float REWARD_POP_DELAY = 0.2f;
        const float REWARD_POP_DURATION = 0.3f;
        const float HOLD_DURATION = 1.3f;
        const float EXIT_DURATION = 0.18f;

        [SerializeField] CanvasGroup _backdropGroup = null;
        [SerializeField] RectTransform _panel = null;
        [SerializeField] RectTransform _rewardRoot = null;
        [SerializeField] TMP_Text _titleLabel = null;
        [SerializeField] TMP_Text _rewardLabel = null;

        Tween _playTween = null;

        public override bool IsTransient => true;

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            _titleLabel.text = viewArgs.IsEndless
                ? LocalizationTable.Format(ENDLESS_TITLE_KEY, ENDLESS_TITLE_FORMAT, viewArgs.RoundNumber)
                : LocalizationTable.Format(TITLE_KEY, TITLE_FORMAT, viewArgs.RoundNumber);
            _rewardLabel.text = LocalizationTable.Get(REWARD_KEY, REWARD_TEXT);
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
            _backdropGroup.alpha = BACKDROP_HIDDEN_ALPHA;
            _panel.localScale = Vector3.zero;
            _rewardRoot.localScale = Vector3.zero;

            Sequence sequence = DOTween.Sequence();
            sequence.AppendInterval(INTRO_DELAY);
            sequence.Append(_backdropGroup.DOFade(BACKDROP_ALPHA, BACKDROP_DURATION).SetEase(Ease.OutQuad));
            sequence.Append(_panel.DOScale(Vector3.one, PANEL_POP_DURATION).SetEase(Ease.OutBack));
            sequence.Insert(INTRO_DELAY + BACKDROP_DURATION + REWARD_POP_DELAY, _rewardRoot.DOScale(Vector3.one, REWARD_POP_DURATION).SetEase(Ease.OutBack));
            sequence.AppendInterval(HOLD_DURATION);
            sequence.AppendCallback(OnHoldFinished);
            sequence.Append(_panel.DOScale(Vector3.zero, EXIT_DURATION).SetEase(Ease.InBack));
            sequence.Join(_rewardRoot.DOScale(Vector3.zero, EXIT_DURATION).SetEase(Ease.InBack));
            sequence.Join(_backdropGroup.DOFade(BACKDROP_HIDDEN_ALPHA, EXIT_DURATION).SetEase(Ease.InQuad));
            sequence.OnComplete(OnShowCompleted);
            sequence.SetUpdate(true);
            _playTween = sequence;
        }

        static void OnHoldFinished()
        {
            UIManager.RaiseEvent(UIManager.EEvent.RoundClearFinished);
        }

        void OnShowCompleted()
        {
            _playTween = null;

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
