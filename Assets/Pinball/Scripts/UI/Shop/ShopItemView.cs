using System;
using DG.Tweening;
using Pinball.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class ShopItemView : MonoBehaviour
    {
        public record BindParam(
            bool IsPassive,
            string Title,
            Sprite Icon,
            string Description,
            string PriceText,
            bool ShowPrice);

        const float SOLD_ALPHA = 0.55f;
        public const float PURCHASE_FLY_DURATION = 0.62f;

        const float PURCHASE_FADE_DURATION = 0.22f;
        const float FULL_ALPHA = 1f;
        const string KIND_PASSIVE_TEXT = "PASSIVE";
        const string KIND_PASSIVE_KEY = "ui.shop.kind_passive";
        const string KIND_UPGRADE_TEXT = "UPGRADE";
        const string KIND_UPGRADE_KEY = "ui.shop.kind_upgrade";

        [SerializeField] Button _button = null;
        [SerializeField] ShopItemHighlight _highlight = null;
        [SerializeField] CanvasGroup _canvasGroup = null;
        [SerializeField] Image _bandImage = null;
        [SerializeField] TMP_Text _titleLabel = null;
        [SerializeField] TMP_Text _kindLabel = null;
        [SerializeField] Image _iconImage = null;
        [SerializeField] TMP_Text _descriptionLabel = null;
        [SerializeField] GameObject _priceRoot = null;
        [SerializeField] TMP_Text _priceLabel = null;
        [SerializeField] GameObject _soldStamp = null;
        [SerializeField] Sprite _passiveBandSprite = null;
        [SerializeField] Sprite _upgradeBandSprite = null;

        Action<int> _clickCallback = null;
        Tween _purchaseTween = null;
        Vector2 _iconBasePosition = Vector2.zero;
        Vector3 _iconBaseScale = Vector3.one;
        int _slotIndex = 0;

        public void Init(int slotIndex, Action<int> clickCallback)
        {
            _slotIndex = slotIndex;
            _clickCallback = clickCallback;
            _button.onClick.AddListener(OnClicked);
            _highlight.Init();
        }

        public void Release()
        {
            KillPurchase();
            _highlight.Release();
            _button.onClick.RemoveListener(OnClicked);
            _clickCallback = null;
        }

        public void BindItem(BindParam param)
        {
            KillPurchase();
            _button.interactable = true;
            _highlight.SetInteractable(true);
            _canvasGroup.alpha = FULL_ALPHA;
            _bandImage.sprite = param.IsPassive ? _passiveBandSprite : _upgradeBandSprite;
            _titleLabel.text = param.Title;
            _kindLabel.text = GetKindText(param.IsPassive);
            _iconImage.sprite = param.Icon;
            _iconImage.enabled = null != param.Icon;
            _descriptionLabel.text = param.Description;
            _priceLabel.text = param.PriceText;
            _priceRoot.SetActive(param.ShowPrice);
            _soldStamp.SetActive(false);
        }

        public void BindSold()
        {
            KillPurchase();
            ApplySold();
            ClearIcon();
            _canvasGroup.alpha = SOLD_ALPHA;
        }

        public void PlayPurchase(Vector3 targetWorldPosition, float targetWorldWidth, bool showsSoldStamp)
        {
            KillPurchase();

            RectTransform iconRect = _iconImage.rectTransform;
            _iconBasePosition = iconRect.anchoredPosition;
            _iconBaseScale = iconRect.localScale;

            Vector3 endScale = GetTargetIconScale(iconRect, targetWorldWidth);
            Vector3 endPosition = targetWorldPosition - GetPivotToCenterOffset(iconRect, endScale);

            if (showsSoldStamp)
                ApplySold();
            else
                DisableInteraction();

            Sequence sequence = DOTween.Sequence();
            sequence.Insert(0f, _canvasGroup.DOFade(SOLD_ALPHA, PURCHASE_FADE_DURATION).SetEase(Ease.InQuad));
            sequence.Insert(0f, iconRect.DOMove(endPosition, PURCHASE_FLY_DURATION).SetEase(Ease.InOutSine));
            sequence.Insert(0f, iconRect.DOScale(endScale, PURCHASE_FLY_DURATION).SetEase(Ease.InOutSine));
            sequence.InsertCallback(PURCHASE_FLY_DURATION, CompletePurchase);
            sequence.OnComplete(OnPurchaseCompleted);
            sequence.SetUpdate(true);
            _purchaseTween = sequence;
        }

        static Vector3 GetPivotToCenterOffset(RectTransform iconRect, Vector3 scale)
        {
            Vector2 center = iconRect.rect.center;
            return iconRect.parent.TransformVector(new Vector3(center.x * scale.x, center.y * scale.y, 0f));
        }

        Vector3 GetTargetIconScale(RectTransform iconRect, float targetWorldWidth)
        {
            float currentWorldWidth = iconRect.rect.width * iconRect.lossyScale.x;
            if (currentWorldWidth <= 0f || targetWorldWidth <= 0f)
                return _iconBaseScale;

            return _iconBaseScale * (targetWorldWidth / currentWorldWidth);
        }

        void CompletePurchase()
        {
            RestoreIcon();
            ClearIcon();
            _canvasGroup.alpha = SOLD_ALPHA;
        }

        void OnPurchaseCompleted()
        {
            _purchaseTween = null;
        }

        void KillPurchase()
        {
            if (null == _purchaseTween)
                return;

            _purchaseTween.Kill();
            _purchaseTween = null;
            RestoreIcon();
        }

        void RestoreIcon()
        {
            RectTransform iconRect = _iconImage.rectTransform;
            iconRect.anchoredPosition = _iconBasePosition;
            iconRect.localScale = _iconBaseScale;
        }

        void DisableInteraction()
        {
            _button.interactable = false;
            _highlight.SetInteractable(false);
        }

        void ApplySold()
        {
            DisableInteraction();
            _titleLabel.text = string.Empty;
            _kindLabel.text = string.Empty;
            _descriptionLabel.text = string.Empty;
            _priceLabel.text = string.Empty;
            _priceRoot.SetActive(false);
            _soldStamp.SetActive(true);
        }

        void ClearIcon()
        {
            _iconImage.sprite = null;
            _iconImage.enabled = false;
        }

        static string GetKindText(bool isPassive)
        {
            if (isPassive)
                return LocalizationTable.Get(KIND_PASSIVE_KEY, KIND_PASSIVE_TEXT);

            return LocalizationTable.Get(KIND_UPGRADE_KEY, KIND_UPGRADE_TEXT);
        }

        void OnClicked()
        {
            _clickCallback?.Invoke(_slotIndex);
        }
    }
}
