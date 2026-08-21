using DG.Tweening;
using Pinball.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class CardPackPopupView : UIViewBase
    {
        public record CardArgs(string TitleKey, Sprite Icon, string DescriptionKey, bool IsPassive, bool IsTaken);

        public record PickEntry(int CardIndex, bool IsPassive, int OwnedIndex, int PreviousCount);

        public delegate bool PickFlyStarter(bool isPassive, int ownedIndex, int previousCount, out Vector3 position, out float width);

        public record Args(string PackTitleKey, int RemainingPicks, CardArgs[] Cards, bool IsDismissing, PickEntry Pick, PickFlyStarter FlyStarter) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.CardPackPopup;
        }

        const int NO_KEPT_CARD_INDEX = -1;

        const string HINT_FORMAT = "CHOOSE {0}";
        const string HINT_KEY = "ui.card_pack.hint";
        const string SKIP_FORMAT = "SKIP";
        const string SKIP_KEY = "ui.card_pack.skip";
        const float CARD_ROW_WIDTH = 1360f;
        const float CARD_ROW_MAX_SCALE = 1f;
        const float CARD_SLOT_WIDTH = 380f;
        const float CARD_SLOT_SPACING = 30f;
        const float BACKDROP_ALPHA = 0.72f;
        const float BACKDROP_DURATION = 0.18f;
        const float CARD_POP_DURATION = 0.26f;
        const float CARD_POP_STAGGER = 0.07f;
        const float CARD_DISMISS_DURATION = 0.14f;
        const float CARD_DISMISS_STAGGER = 0.05f;
        const float POPUP_EXIT_DURATION = 0.16f;
        const float POPUP_FULL_ALPHA = 1f;
        const float POPUP_HIDDEN_ALPHA = 0f;

        [SerializeField] CanvasGroup _rootGroup = null;
        [SerializeField] CanvasGroup _backdropGroup = null;
        [SerializeField] TMP_Text _titleLabel = null;
        [SerializeField] TMP_Text _hintLabel = null;
        [SerializeField] HorizontalLayoutGroup _cardRow = null;
        [SerializeField] ShopItemView[] _cards = null;
        [SerializeField] Button _skipButton = null;
        [SerializeField] TMP_Text _skipLabel = null;

        Tween _appearTween = null;
        Tween _dismissTween = null;
        bool _isOpened = false;
        bool _isDismissing = false;

        public override void OnGenerate()
        {
            for (int i = 0; i < _cards.Length; ++i)
                _cards[i].Init(i, OnCardClicked);
            _skipButton.onClick.AddListener(OnSkipClicked);
        }

        public override void OnTerminate()
        {
            KillAppear();
            KillDismiss();
            for (int i = 0; i < _cards.Length; ++i)
                _cards[i].Release();
            _skipButton.onClick.RemoveListener(OnSkipClicked);
        }

        public override void OnReturn()
        {
            KillAppear();
            KillDismiss();
            ResetVisuals();
            _isOpened = false;
            _isDismissing = false;
        }

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            if (viewArgs.IsDismissing)
            {
                PlayDismiss(viewArgs.Pick, viewArgs.FlyStarter);
                return;
            }

            _titleLabel.text = LocalizationTable.GetOrKey(viewArgs.PackTitleKey);
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
                IsPassive: args.IsPassive,
                Title: LocalizationTable.GetOrKey(args.TitleKey),
                Icon: args.Icon,
                Description: LocalizationTable.GetOrKey(args.DescriptionKey),
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

            SlotRowLayout.Fit(_cardRow, shownCount, CARD_SLOT_WIDTH, CARD_SLOT_SPACING, CARD_ROW_WIDTH, CARD_ROW_MAX_SCALE);
        }

        void PlayAppear()
        {
            KillAppear();
            _rootGroup.alpha = POPUP_FULL_ALPHA;
            _backdropGroup.alpha = 0f;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(_backdropGroup.DOFade(BACKDROP_ALPHA, BACKDROP_DURATION).SetEase(Ease.OutQuad));

            for (int i = 0; i < _cards.Length; ++i)
            {
                if (!_cards[i].gameObject.activeSelf)
                    continue;

                Transform card = _cards[i].transform;
                card.localScale = Vector3.zero;
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
            ResetVisuals();
        }

        void PlayDismiss(PickEntry pick, PickFlyStarter flyStarter)
        {
            if (_isDismissing)
                return;

            _isDismissing = true;
            KillAppear();

            int keptCardIndex = null == pick ? NO_KEPT_CARD_INDEX : pick.CardIndex;
            float flyEndTime = TryPlayPickFly(pick, flyStarter) ? ShopItemView.PURCHASE_FLY_DURATION : 0f;

            Sequence sequence = DOTween.Sequence();
            float dismissEndTime = 0f;
            for (int i = 0; i < _cards.Length; ++i)
            {
                if (i == keptCardIndex || !_cards[i].gameObject.activeSelf)
                    continue;

                float startTime = i * CARD_DISMISS_STAGGER;
                sequence.Insert(startTime, _cards[i].transform.DOScale(Vector3.zero, CARD_DISMISS_DURATION).SetEase(Ease.InBack));
                dismissEndTime = Mathf.Max(dismissEndTime, startTime + CARD_DISMISS_DURATION);
            }

            sequence.Insert(dismissEndTime, _rootGroup.DOFade(POPUP_HIDDEN_ALPHA, POPUP_EXIT_DURATION).SetEase(Ease.InQuad));
            if (flyEndTime <= 0f && keptCardIndex >= 0 && keptCardIndex < _cards.Length)
                sequence.Insert(dismissEndTime, _cards[keptCardIndex].transform.DOScale(Vector3.zero, POPUP_EXIT_DURATION).SetEase(Ease.InBack));

            sequence.AppendInterval(Mathf.Max(0f, flyEndTime - dismissEndTime - POPUP_EXIT_DURATION));
            sequence.OnComplete(OnDismissCompleted);
            sequence.SetUpdate(true);
            _dismissTween = sequence;
        }

        bool TryPlayPickFly(PickEntry pick, PickFlyStarter flyStarter)
        {
            if (null == pick || null == flyStarter)
                return false;

            if (pick.CardIndex < 0 || pick.CardIndex >= _cards.Length)
                return false;

            if (!flyStarter(pick.IsPassive, pick.OwnedIndex, pick.PreviousCount, out Vector3 position, out float width))
                return false;

            _cards[pick.CardIndex].PlayPurchase(position, width, showsSoldStamp: false);
            return true;
        }

        void OnDismissCompleted()
        {
            _dismissTween = null;
            UIManager.Hide(UIViewCatalogData.EViewType.CardPackPopup);
        }

        void KillDismiss()
        {
            if (null == _dismissTween)
                return;

            _dismissTween.Kill();
            _dismissTween = null;
        }

        void ResetVisuals()
        {
            _rootGroup.alpha = POPUP_FULL_ALPHA;
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
