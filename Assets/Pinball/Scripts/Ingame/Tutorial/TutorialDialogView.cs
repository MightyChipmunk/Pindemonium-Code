using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.Ingame.Tutorial
{
    public class TutorialDialogView : MonoBehaviour
    {
        public enum EPlacement
        {
            Bottom = 0,
            Top = 1,
            Center = 2,
        }

        const string CONTINUE_HINT_TEXT = "CLICK TO CONTINUE";
        const string CONTINUE_HINT_KEY = "ui.tutorial.continue";

        static readonly Vector2 BOTTOM_POSITION = new Vector2(0f, -340f);
        static readonly Vector2 TOP_POSITION = new Vector2(0f, 340f);
        static readonly Vector2 CENTER_POSITION = Vector2.zero;

        [SerializeField] Button _clickCatcher = null;
        [SerializeField] RectTransform _panel = null;
        [SerializeField] TMP_Text _bodyLabel = null;
        [SerializeField] TMP_Text _continueHintLabel = null;

        LocalizedTextRef _currentBody = default;
        bool _isClicked = false;

        public void Init()
        {
            _clickCatcher.onClick.AddListener(OnClicked);
            LocalizationManager.OnLanguageChanged += RefreshTexts;
        }

        public void Release()
        {
            LocalizationManager.OnLanguageChanged -= RefreshTexts;
            _clickCatcher.onClick.RemoveListener(OnClicked);
        }

        public bool IsShowing()
        {
            return gameObject.activeSelf;
        }

        public async UniTask ShowAndWaitClickAsync(LocalizedTextRef body, EPlacement placement, CancellationToken cancellationToken)
        {
            Show(body, placement, true);
            await UniTask.WaitUntil(GetIsClicked, cancellationToken: cancellationToken);
        }

        public void ShowGuide(LocalizedTextRef body, EPlacement placement)
        {
            Show(body, placement, false);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void Show(LocalizedTextRef body, EPlacement placement, bool waitsClick)
        {
            gameObject.SetActive(true);
            _currentBody = body;
            _isClicked = false;
            _panel.anchoredPosition = GetPlacementPosition(placement);
            _clickCatcher.gameObject.SetActive(waitsClick);
            _continueHintLabel.gameObject.SetActive(waitsClick);
            RefreshTexts();
        }

        bool GetIsClicked()
        {
            return _isClicked;
        }

        void OnClicked()
        {
            _isClicked = true;
        }

        void RefreshTexts()
        {
            if (!gameObject.activeSelf)
                return;

            _bodyLabel.text = _currentBody.IsEmpty() ? string.Empty : _currentBody.Resolve();
            _continueHintLabel.text = LocalizationTable.Get(CONTINUE_HINT_KEY, CONTINUE_HINT_TEXT);
        }

        static Vector2 GetPlacementPosition(EPlacement placement)
        {
            switch (placement)
            {
                case EPlacement.Top:
                    return TOP_POSITION;
                case EPlacement.Center:
                    return CENTER_POSITION;
                default:
                    return BOTTOM_POSITION;
            }
        }
    }
}
