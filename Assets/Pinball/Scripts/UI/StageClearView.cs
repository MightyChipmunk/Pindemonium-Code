using DG.Tweening;
using Pinball.Audio;
using Pinball.Ingame.Reward;
using Pinball.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pinball.UI
{
    public class StageClearView : UIViewBase
    {
        public record Args(int StageNumber, bool IsEndless, StageClearReward Reward) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.StageClear;
        }

        const string CLEAR_FORMAT = "STAGE {0} CLEAR!";
        const string CLEAR_KEY = "ui.stage_clear.title";
        const string ENDLESS_CLEAR_FORMAT = "ENDLESS {0} CLEAR!";
        const string ENDLESS_CLEAR_KEY = "ui.stage_clear.endless_title";
        const string REWARD_CLEAR_FORMAT = "STAGE CLEAR   +${0}";
        const string REWARD_CLEAR_KEY = "ui.stage_clear.reward_clear";
        const string REWARD_BALL_FORMAT = "BALLS LEFT {0}   +${1}";
        const string REWARD_BALL_KEY = "ui.stage_clear.reward_ball";
        const string REWARD_INTEREST_FORMAT = "INTEREST   +${0}   (MAX ${1})";
        const string REWARD_INTEREST_KEY = "ui.stage_clear.reward_interest";
        const string REWARD_SOURCE_FORMAT = "   ({0})";
        const string REWARD_SOURCE_KEY = "ui.stage_clear.reward_source";
        const string CONTINUE_HINT_TEXT = "SPACE OR CLICK TO CONTINUE";
        const string CONTINUE_HINT_KEY = "ui.common.continue_hint";
        const Key CONTINUE_KEY = Key.Space;
        const float SCALE_DURATION = 0.35f;
        const float REWARD_START_DELAY = 0.12f;
        const float REWARD_LINE_DURATION = 0.28f;
        const float REWARD_LINE_INTERVAL = 0.18f;
        const float HINT_SHOW_DELAY = 0.2f;
        const float HINT_PULSE_DURATION = 0.65f;
        const float HINT_MIN_ALPHA = 0.35f;
        const float HINT_MAX_ALPHA = 1f;
        const float HINT_REWARD_POSITION_Y = -215f;
        const float HINT_PLAIN_POSITION_Y = -20f;

        [SerializeField] TMP_Text _label = null;
        [SerializeField] RectTransform _panel = null;
        [SerializeField] GameObject _rewardRoot = null;
        [SerializeField] StageClearRewardLine _clearRewardLine = null;
        [SerializeField] StageClearRewardLine _ballRewardLine = null;
        [SerializeField] StageClearRewardLine _interestRewardLine = null;
        [SerializeField] RectTransform _continueHintRoot = null;
        [SerializeField] CanvasGroup _continueHintGroup = null;
        [SerializeField] TMP_Text _continueHintLabel = null;

        Tween _playTween = null;
        Tween _hintTween = null;
        bool _isRevealing = false;
        bool _isWaitingInput = false;

        public override bool IsTransient => true;

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            _label.text = viewArgs.IsEndless
                ? LocalizationTable.Format(ENDLESS_CLEAR_KEY, ENDLESS_CLEAR_FORMAT, viewArgs.StageNumber)
                : LocalizationTable.Format(CLEAR_KEY, CLEAR_FORMAT, viewArgs.StageNumber);
            BindReward(viewArgs.Reward);
            BindContinueHint(null != viewArgs.Reward);
            PlayShow(null != viewArgs.Reward);
        }

        public override void OnReturn()
        {
            KillPlay();
        }

        public override void OnTerminate()
        {
            KillPlay();
        }

        void Update()
        {
            if (!_isRevealing && !_isWaitingInput)
                return;

            if (!IsContinuePressed())
                return;

            if (_isRevealing)
            {
                SkipReveal();
                return;
            }

            Continue();
        }

        void BindReward(StageClearReward reward)
        {
            if (null != _rewardRoot)
                _rewardRoot.SetActive(null != reward);

            if (null == reward)
                return;

            SetRewardLine(_clearRewardLine, LocalizationTable.Format(REWARD_CLEAR_KEY, REWARD_CLEAR_FORMAT, reward.ClearMoney), reward.ClearMoneySourceNames);
            SetRewardLine(_ballRewardLine, LocalizationTable.Format(REWARD_BALL_KEY, REWARD_BALL_FORMAT, reward.RemainingBallCount, reward.RemainingBallMoney), reward.RemainingBallSourceNames);
            SetRewardLine(_interestRewardLine, LocalizationTable.Format(REWARD_INTEREST_KEY, REWARD_INTEREST_FORMAT, reward.InterestMoney, reward.InterestMaxMoney), reward.InterestSourceNames);
        }

        static void SetRewardLine(StageClearRewardLine line, string text, string sourceNames)
        {
            if (null == line)
                return;

            line.SetText(string.IsNullOrEmpty(sourceNames)
                ? text
                : text + LocalizationTable.Format(REWARD_SOURCE_KEY, REWARD_SOURCE_FORMAT, sourceNames));
            line.SetHidden();
        }

        void BindContinueHint(bool hasReward)
        {
            HideContinueHint();
            SetHintPositionY(hasReward ? HINT_REWARD_POSITION_Y : HINT_PLAIN_POSITION_Y);

            if (null == _continueHintLabel)
                return;

            _continueHintLabel.text = LocalizationTable.Get(CONTINUE_HINT_KEY, CONTINUE_HINT_TEXT);
        }

        void SetHintPositionY(float positionY)
        {
            if (null == _continueHintRoot)
                return;

            Vector2 position = _continueHintRoot.anchoredPosition;
            position.y = positionY;
            _continueHintRoot.anchoredPosition = position;
        }

        void PlayShow(bool hasReward)
        {
            KillPlay();
            _panel.localScale = Vector3.zero;
            _isRevealing = true;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(_panel.DOScale(Vector3.one, SCALE_DURATION).SetEase(Ease.OutElastic));

            if (hasReward)
            {
                float lineTime = SCALE_DURATION + REWARD_START_DELAY;
                lineTime = InsertRewardLine(sequence, _clearRewardLine, lineTime);
                lineTime = InsertRewardLine(sequence, _ballRewardLine, lineTime);
                InsertRewardLine(sequence, _interestRewardLine, lineTime);
            }

            sequence.AppendInterval(HINT_SHOW_DELAY);
            sequence.OnComplete(OnRevealCompleted);
            sequence.SetUpdate(true);
            _playTween = sequence;
        }

        static float InsertRewardLine(Sequence sequence, StageClearRewardLine line, float startTime)
        {
            if (null == line)
                return startTime;

            Tween showTween = line.CreateShowTween(REWARD_LINE_DURATION);
            if (null == showTween)
                return startTime;

            sequence.Insert(startTime, showTween);
            sequence.InsertCallback(startTime, PlayRewardLineSound);
            return startTime + REWARD_LINE_INTERVAL;
        }

        static void PlayRewardLineSound()
        {
            AudioManager.Play(AudioStorage.EAudioClipType.MoneyGain);
        }

        void SkipReveal()
        {
            if (null == _playTween)
            {
                OnRevealCompleted();
                return;
            }

            _playTween.Complete();
        }

        void OnRevealCompleted()
        {
            _playTween = null;
            _isRevealing = false;

            if (_isWaitingInput)
                return;

            _isWaitingInput = true;
            ShowContinueHint();
        }

        void ShowContinueHint()
        {
            if (null == _continueHintRoot || null == _continueHintGroup)
                return;

            KillHint();
            _continueHintRoot.gameObject.SetActive(true);
            _continueHintGroup.alpha = HINT_MAX_ALPHA;
            _hintTween = _continueHintGroup.DOFade(HINT_MIN_ALPHA, HINT_PULSE_DURATION)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        void HideContinueHint()
        {
            KillHint();

            if (null == _continueHintRoot)
                return;

            _continueHintRoot.gameObject.SetActive(false);
        }

        static bool IsContinuePressed()
        {
            Keyboard keyboard = Keyboard.current;
            if (null != keyboard && keyboard[CONTINUE_KEY].wasPressedThisFrame)
                return true;

            Mouse mouse = Mouse.current;
            if (null != mouse && mouse.leftButton.wasPressedThisFrame)
                return true;

            Touchscreen touchscreen = Touchscreen.current;
            return null != touchscreen && touchscreen.primaryTouch.press.wasPressedThisFrame;
        }

        void Continue()
        {
            _isWaitingInput = false;
            HideContinueHint();
            AudioManager.Play(AudioStorage.EAudioClipType.UIButtonClick);
            UIManager.RaiseEvent(UIManager.EEvent.StageClearFinished);

            if (null != OwnPool)
                OwnPool.Return(this);
        }

        void KillPlay()
        {
            _isRevealing = false;
            _isWaitingInput = false;
            HideContinueHint();

            if (null == _playTween)
                return;

            _playTween.Kill();
            _playTween = null;
        }

        void KillHint()
        {
            if (null == _hintTween)
                return;

            _hintTween.Kill();
            _hintTween = null;
        }
    }
}
