using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.Localization;
using Pinball.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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

        public enum EIcon
        {
            Flipper = 0,
            Launch = 1,
            Nudge = 2,
            Combo = 3,
            BallLost = 4,
            Score = 5,
            Bumper = 6,
        }

        const string CONTINUE_HINT_TEXT = "SPACE OR CLICK TO CONTINUE";
        const string CONTINUE_HINT_KEY = "ui.tutorial.continue";
        const Key CONTINUE_KEY = Key.Space;
        const float REVEAL_CHARS_PER_SECOND = 40f;

        static readonly Vector2 BOTTOM_POSITION = new Vector2(0f, -340f);
        static readonly Vector2 TOP_POSITION = new Vector2(0f, 340f);
        static readonly Vector2 CENTER_POSITION = Vector2.zero;

        [SerializeField] Button _clickCatcher = null;
        [SerializeField] RectTransform _panel = null;
        [SerializeField] TMP_Text _bodyLabel = null;
        [SerializeField] TMP_Text _continueHintLabel = null;
        [SerializeField] Image _iconImage = null;
        [SerializeField] Sprite[] _iconSprites = null;

        LocalizedTextRef _currentBody = default;
        bool _isClicked = false;
        bool _waitsClick = false;
        bool _isRevealing = false;
        float _revealProgress = 0f;
        int _revealTotalCount = 0;

        public void Init()
        {
            _clickCatcher.onClick.AddListener(Advance);
            LocalizationManager.OnLanguageChanged += RefreshTexts;
        }

        public void Release()
        {
            LocalizationManager.OnLanguageChanged -= RefreshTexts;
            _clickCatcher.onClick.RemoveListener(Advance);
        }

        public bool IsShowing()
        {
            return gameObject.activeSelf;
        }

        public async UniTask ShowAndWaitClickAsync(LocalizedTextRef body, EPlacement placement, CancellationToken cancellationToken, EIcon icon = EIcon.Flipper)
        {
            await ScreenTransition.WaitUntilIdleAsync(cancellationToken);
            Show(body, placement, true, icon);
            await UniTask.WaitUntil(GetIsClicked, cancellationToken: cancellationToken);
        }

        public void ShowGuide(LocalizedTextRef body, EPlacement placement, EIcon icon = EIcon.Flipper)
        {
            Show(body, placement, false, icon);
        }

        public void Hide()
        {
            _isRevealing = false;
            gameObject.SetActive(false);
        }

        void Show(LocalizedTextRef body, EPlacement placement, bool waitsClick, EIcon icon)
        {
            gameObject.SetActive(true);
            _currentBody = body;
            _isClicked = false;
            _waitsClick = waitsClick;
            _panel.anchoredPosition = GetPlacementPosition(placement);
            _clickCatcher.gameObject.SetActive(waitsClick);
            ApplyIcon(icon);
            RefreshTexts();
        }

        void ApplyIcon(EIcon icon)
        {
            if (null == _iconImage || null == _iconSprites)
                return;

            int index = (int)icon;
            if (index < 0 || index >= _iconSprites.Length || null == _iconSprites[index])
                return;

            _iconImage.sprite = _iconSprites[index];
        }

        void Update()
        {
            CheckSkipInput();

            if (!_isRevealing)
                return;

            _revealProgress += Time.unscaledDeltaTime * REVEAL_CHARS_PER_SECOND;
            _bodyLabel.maxVisibleCharacters = Mathf.Min(_revealTotalCount, Mathf.FloorToInt(_revealProgress));
            if (_revealProgress >= _revealTotalCount)
                CompleteReveal();
        }

        void CheckSkipInput()
        {
            if (_isClicked)
                return;

            Keyboard keyboard = Keyboard.current;
            if (null != keyboard && keyboard[CONTINUE_KEY].wasPressedThisFrame)
            {
                Advance();
                return;
            }

            if (_waitsClick || !_isRevealing)
                return;

            if (IsPointerPressed())
                CompleteReveal();
        }

        static bool IsPointerPressed()
        {
            Mouse mouse = Mouse.current;
            if (null != mouse && mouse.leftButton.wasPressedThisFrame)
                return true;

            Touchscreen touchscreen = Touchscreen.current;
            return null != touchscreen && touchscreen.primaryTouch.press.wasPressedThisFrame;
        }

        bool GetIsClicked()
        {
            return _isClicked;
        }

        void Advance()
        {
            if (_isRevealing)
            {
                CompleteReveal();
                return;
            }

            if (!_waitsClick)
                return;

            _isClicked = true;
        }

        void RefreshTexts()
        {
            if (!gameObject.activeSelf)
                return;

            _bodyLabel.text = _currentBody.IsEmpty() ? string.Empty : _currentBody.Resolve();
            _continueHintLabel.text = LocalizationTable.Get(CONTINUE_HINT_KEY, CONTINUE_HINT_TEXT);
            BeginReveal();
        }

        void BeginReveal()
        {
            _bodyLabel.ForceMeshUpdate();
            _revealTotalCount = _bodyLabel.textInfo.characterCount;
            _revealProgress = 0f;
            _continueHintLabel.gameObject.SetActive(false);
            if (_revealTotalCount <= 0)
            {
                CompleteReveal();
                return;
            }

            _bodyLabel.maxVisibleCharacters = 0;
            _isRevealing = true;
        }

        void CompleteReveal()
        {
            _isRevealing = false;
            _bodyLabel.maxVisibleCharacters = int.MaxValue;
            _continueHintLabel.gameObject.SetActive(_waitsClick);
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
