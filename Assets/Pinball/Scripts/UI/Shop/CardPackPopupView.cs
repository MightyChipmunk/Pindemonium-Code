using DG.Tweening;
using Pinball.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class CardPackPopupView : UIViewBase
    {
        public record CardArgs(string Title, Sprite Icon, string Description, bool IsTaken);

        public record Args(string PackTitle, int RemainingPicks, CardArgs[] Cards) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.CardPackPopup;
        }

        const string HINT_FORMAT = "CHOOSE {0}";
        const string HINT_KEY = "ui.card_pack.hint";
        const string SKIP_FORMAT = "SKIP";
        const string SKIP_KEY = "ui.card_pack.skip";
        const float CARD_ROW_WIDTH = 1360f;
        const float CARD_SLOT_WIDTH = 380f;
        const float CARD_SLOT_SPACING = 30f;
        const float BACKDROP_ALPHA = 0.72f;
        const float BACKDROP_DURATION = 0.18f;
        const float CARD_POP_DURATION = 0.26f;
        const float CARD_POP_STAGGER = 0.07f;
        const float CARD_POP_START_SCALE = 0.55f;

        [SerializeField] CanvasGroup _backdropGroup = null;
        [SerializeField] TMP_Text _titleLabel = null;
        [SerializeField] TMP_Text _hintLabel = null;
        [SerializeField] HorizontalLayoutGroup _cardRow = null;
        [SerializeField] ShopItemView[] _cards = null;
        [SerializeField] Button _skipButton = null;
        [SerializeField] TMP_Text _skipLabel = null;

        Tween _appearTween = null;
        bool _isOpened = false;

        public override void OnGenerate()
        {
            for (int i = 0; i < _cards.Length; ++i)
                _cards[i].Init(i, OnCardClicked);
            _skipButton.onClick.AddListener(OnSkipClicked);
        }

        public override void OnTerminate()
        {
            KillAppear();
            for (int i = 0; i < _cards.Length; ++i)
                _cards[i].Release();
            _skipButton.onClick.RemoveListener(OnSkipClicked);
        }

        public override void OnReturn()
        {
            KillAppear();
            _isOpened = false;
        }

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            _titleLabel.text = viewArgs.PackTitle;
            _hintLabel.text = LocalizationTable.Format(HINT_KEY, HINT_FORMAT, viewArgs.RemainingPicks);
            _skipLabel.text = LocalizationTable.Get(SKIP_KEY, SKIP_FORMAT);
            BindCards(viewArgs.Cards);

            if (_isOpened)
                return;

            _isOpened = true;
            PlayAppear();
        }

        void BindCards(CardArgs[] cards)
        {
            int cardCount = null == cards ? 0 : cards.Length;
            ApplyCardLayout(cardCount);

            for (int i = 0; i < _cards.Length; ++i)
            {
                if (i >= cardCount || null == cards[i])
                    continue;

                BindCard(_cards[i], cards[i]);
            }
        }

        static void BindCard(ShopItemView card, CardArgs args)
        {
            if (args.IsTaken)
            {
                card.BindSold();
                return;
            }

            card.BindItem(new ShopItemView.BindParam(
                IsPassive: false,
                Title: args.Title,
                Icon: args.Icon,
                Description: args.Description,
                PriceText: string.Empty,
                ShowPrice: false));
        }

        void ApplyCardLayout(int visibleCount)
        {
            int shownCount = Mathf.Clamp(visibleCount, 0, _cards.Length);
            for (int i = 0; i < _cards.Length; ++i)
                _cards[i].gameObject.SetActive(i < shownCount);

            if (shownCount <= 0)
                return;

            _cardRow.spacing = CARD_SLOT_SPACING;

            float requiredWidth = CARD_SLOT_WIDTH * shownCount + CARD_SLOT_SPACING * (shownCount - 1);
            RectTransform rowRect = _cardRow.transform as RectTransform;
            rowRect.sizeDelta = new Vector2(requiredWidth, rowRect.sizeDelta.y);

            float rowScale = Mathf.Min(1f, CARD_ROW_WIDTH / requiredWidth);
            rowRect.localScale = new Vector3(rowScale, rowScale, 1f);
        }

        void PlayAppear()
        {
            KillAppear();
            _backdropGroup.alpha = 0f;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(_backdropGroup.DOFade(BACKDROP_ALPHA, BACKDROP_DURATION).SetEase(Ease.OutQuad));

            for (int i = 0; i < _cards.Length; ++i)
            {
                if (!_cards[i].gameObject.activeSelf)
                    continue;

                Transform card = _cards[i].transform;
                card.localScale = Vector3.one * CARD_POP_START_SCALE;
                sequence.Insert(BACKDROP_DURATION + i * CARD_POP_STAGGER, card.DOScale(Vector3.one, CARD_POP_DURATION).SetEase(Ease.OutBack));
            }

            sequence.OnComplete(OnAppearCompleted);
            sequence.SetUpdate(true);
            _appearTween = sequence;
        }

        void OnAppearCompleted()
        {
            _appearTween = null;
        }

        void KillAppear()
        {
            if (null == _appearTween)
                return;

            _appearTween.Kill();
            _appearTween = null;
            _backdropGroup.alpha = BACKDROP_ALPHA;
            for (int i = 0; i < _cards.Length; ++i)
                _cards[i].transform.localScale = Vector3.one;
        }

        void OnCardClicked(int cardIndex)
        {
            UIManager.RaiseEvent(UIManager.EEvent.CardPackPickRequested, cardIndex);
        }

        void OnSkipClicked()
        {
            UIManager.RaiseEvent(UIManager.EEvent.CardPackSkipRequested);
        }
    }
}
