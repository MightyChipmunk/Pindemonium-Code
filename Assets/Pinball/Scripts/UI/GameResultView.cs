using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameCore.Localization;
using GameCore.UI;

namespace Pinball.UI
{
    public class GameResultView : UIViewBase
    {
        public enum EResult
        {
            Clear = 0,
            GameOver = 1,
        }

        public record Args(EResult Result, ResultStatsView.Args Stats) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.GameResult;
        }

        const string CLEAR_TITLE = "ALL STAGES CLEAR!";
        const string CLEAR_TITLE_KEY = "ui.game_result.clear_title";
        const string GAME_OVER_TITLE = "GAME OVER";
        const string GAME_OVER_TITLE_KEY = "ui.game_result.game_over_title";
        const string SCORE_FORMAT = "N0";
        const string STAGE_FORMAT = "STAGE {0}";
        const string STAGE_KEY = "ui.common.stage_number";
        const string ENDLESS_STAGE_FORMAT = "ENDLESS STAGE {0}";
        const string ENDLESS_STAGE_KEY = "ui.common.endless_stage";
        const float APPEAR_DURATION = 0.35f;
        const float APPEAR_SLIDE_OFFSET = 120f;

        [SerializeField] CanvasGroup _canvasGroup = null;
        [SerializeField] RectTransform _panel = null;
        [SerializeField] Image _panelImage = null;
        [SerializeField] Image _bannerImage = null;
        [SerializeField] TMP_Text _titleLabel = null;
        [SerializeField] TMP_Text _scoreLabel = null;
        [SerializeField] TMP_Text _stageLabel = null;
        [SerializeField] GameObject _clearDecorRoot = null;
        [SerializeField] Button _statsButton = null;
        [SerializeField] Button _lobbyButton = null;
        [SerializeField] Button _endlessButton = null;
        [SerializeField] Image _statsButtonImage = null;
        [SerializeField] Sprite _clearPanelSprite = null;
        [SerializeField] Sprite _clearBannerSprite = null;
        [SerializeField] Sprite _clearStatsButtonSprite = null;
        [SerializeField] Sprite _gameOverPanelSprite = null;
        [SerializeField] Sprite _gameOverBannerSprite = null;
        [SerializeField] Sprite _gameOverStatsButtonSprite = null;

        Args _args = null;
        Vector2 _panelBasePosition = Vector2.zero;
        Tween _appearTween = null;
        bool _isOpened = false;

        public override void OnGenerate()
        {
            _panelBasePosition = _panel.anchoredPosition;
            _statsButton.onClick.AddListener(OnStatsClicked);
            _lobbyButton.onClick.AddListener(OnLobbyClicked);
            _endlessButton.onClick.AddListener(OnEndlessClicked);
        }

        public override void OnTerminate()
        {
            KillAppear();
            _statsButton.onClick.RemoveListener(OnStatsClicked);
            _lobbyButton.onClick.RemoveListener(OnLobbyClicked);
            _endlessButton.onClick.RemoveListener(OnEndlessClicked);
        }

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            _args = viewArgs;
            ApplyResult(viewArgs);

            if (_isOpened)
                return;

            _isOpened = true;
            PlayAppear();
        }

        public override void OnReturn()
        {
            KillAppear();
            _isOpened = false;
            _args = null;
        }

        void ApplyResult(Args args)
        {
            bool isClear = args.Result == EResult.Clear;
            _titleLabel.text = isClear ? LocalizationTable.Get(CLEAR_TITLE_KEY, CLEAR_TITLE) : LocalizationTable.Get(GAME_OVER_TITLE_KEY, GAME_OVER_TITLE);
            _panelImage.sprite = isClear ? _clearPanelSprite : _gameOverPanelSprite;
            _bannerImage.sprite = isClear ? _clearBannerSprite : _gameOverBannerSprite;
            _statsButtonImage.sprite = isClear ? _clearStatsButtonSprite : _gameOverStatsButtonSprite;
            _clearDecorRoot.SetActive(isClear);
            _endlessButton.gameObject.SetActive(isClear);

            if (null == args.Stats)
                return;

            _scoreLabel.text = args.Stats.Score.ToString(SCORE_FORMAT, CultureInfo.InvariantCulture);
            _stageLabel.text = args.Stats.IsEndless
                ? LocalizationTable.Format(ENDLESS_STAGE_KEY, ENDLESS_STAGE_FORMAT, args.Stats.StageNumber)
                : LocalizationTable.Format(STAGE_KEY, STAGE_FORMAT, args.Stats.StageNumber);
        }

        void PlayAppear()
        {
            KillAppear();
            _canvasGroup.interactable = false;
            _canvasGroup.alpha = 0f;
            _panel.anchoredPosition = _panelBasePosition + Vector2.down * APPEAR_SLIDE_OFFSET;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(_canvasGroup.DOFade(1f, APPEAR_DURATION).SetEase(Ease.OutQuad));
            sequence.Join(_panel.DOAnchorPos(_panelBasePosition, APPEAR_DURATION).SetEase(Ease.OutQuad));
            sequence.OnComplete(OnAppearCompleted);
            sequence.SetUpdate(true);
            _appearTween = sequence;
        }

        void OnAppearCompleted()
        {
            _appearTween = null;
            _canvasGroup.interactable = true;
        }

        void KillAppear()
        {
            if (null == _appearTween)
                return;

            _appearTween.Kill();
            _appearTween = null;
        }

        void OnStatsClicked()
        {
            if (null == _args)
                return;

            if (null == _args.Stats)
                return;

            UIManager.Show(_args.Stats);
        }

        void OnLobbyClicked()
        {
            UIManager.RaiseEvent(UIManager.EEvent.ExitToLobbyRequested);
        }

        void OnEndlessClicked()
        {
            UIManager.RaiseEvent(UIManager.EEvent.EndlessStartRequested);
        }
    }
}
