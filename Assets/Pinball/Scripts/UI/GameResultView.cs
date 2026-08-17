using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        const string GAME_OVER_TITLE = "GAME OVER";
        const string SCORE_FORMAT = "N0";
        const string STAGE_FORMAT = "STAGE {0}";
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
        [SerializeField] Image _statsButtonImage = null;
        [SerializeField] Sprite _clearPanelSprite = null;
        [SerializeField] Sprite _clearBannerSprite = null;
        [SerializeField] Sprite _clearStatsButtonSprite = null;
        [SerializeField] Sprite _gameOverPanelSprite = null;
        [SerializeField] Sprite _gameOverBannerSprite = null;
        [SerializeField] Sprite _gameOverStatsButtonSprite = null;

        Args _args = null;
        Vector2 _panelBasePosition = Vector2.zero;
        bool _isPanelBaseCached = false;
        CancellationTokenSource _appearTokenSource = null;

        public override void OnGenerate()
        {
            _statsButton.onClick.AddListener(OnStatsClicked);
            _lobbyButton.onClick.AddListener(OnLobbyClicked);
        }

        public override void OnTerminate()
        {
            _statsButton.onClick.RemoveListener(OnStatsClicked);
            _lobbyButton.onClick.RemoveListener(OnLobbyClicked);
        }

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            _args = viewArgs;
            ApplyResult(viewArgs);
            CachePanelBasePosition();
            CancelAppear();
            _appearTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            AppearAsync(_appearTokenSource.Token).Forget();
        }

        public override void OnReturn()
        {
            CancelAppear();
            _args = null;
        }

        void OnDestroy()
        {
            CancelAppear();
        }

        void ApplyResult(Args args)
        {
            bool isClear = args.Result == EResult.Clear;
            _titleLabel.text = isClear ? CLEAR_TITLE : GAME_OVER_TITLE;
            _panelImage.sprite = isClear ? _clearPanelSprite : _gameOverPanelSprite;
            _bannerImage.sprite = isClear ? _clearBannerSprite : _gameOverBannerSprite;
            _statsButtonImage.sprite = isClear ? _clearStatsButtonSprite : _gameOverStatsButtonSprite;
            _clearDecorRoot.SetActive(isClear);

            if (null == args.Stats)
                return;

            _scoreLabel.text = args.Stats.Score.ToString(SCORE_FORMAT, CultureInfo.InvariantCulture);
            _stageLabel.text = string.Format(STAGE_FORMAT, args.Stats.StageNumber);
        }

        void CachePanelBasePosition()
        {
            if (_isPanelBaseCached)
                return;

            _panelBasePosition = _panel.anchoredPosition;
            _isPanelBaseCached = true;
        }

        void CancelAppear()
        {
            if (null == _appearTokenSource)
                return;

            _appearTokenSource.Cancel();
            _appearTokenSource.Dispose();
            _appearTokenSource = null;
        }

        async UniTaskVoid AppearAsync(CancellationToken cancellationToken)
        {
            _canvasGroup.interactable = false;
            float time = 0f;
            while (time <= APPEAR_DURATION)
            {
                float ratio = EaseUtil.Interpolate(EaseType.QuadOut, Mathf.Clamp01(time / APPEAR_DURATION));
                ApplyAppearRatio(ratio);
                time += Time.deltaTime;
                await UniTask.Yield(cancellationToken);
            }

            ApplyAppearRatio(1f);
            _canvasGroup.interactable = true;
        }

        void ApplyAppearRatio(float ratio)
        {
            _canvasGroup.alpha = ratio;
            _panel.anchoredPosition = _panelBasePosition + (Vector2.down * (APPEAR_SLIDE_OFFSET * (1f - ratio)));
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
    }
}
