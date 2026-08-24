using DG.Tweening;
using Pinball.Ingame.Shop;
using TMPro;
using UnityEngine;

namespace Pinball.UI
{
    public class ShopItemView : ShopSlotViewBase
    {
        public record BindParam(
            bool IsPassive,
            ShopItemData.EPassiveCategory PassiveCategory,
            string Title,
            Sprite Icon,
            string Description,
            string PriceText,
            bool ShowPrice);

        public const float PURCHASE_FLY_DURATION = 0.31f;

        const float PURCHASE_FADE_DURATION = 0.22f;

        [SerializeField] GameObject _passiveLabel = null;
        [SerializeField] TMP_Text _passiveCategoryLabel = null;
        [SerializeField] GameObject _upgradeLabel = null;
        [SerializeField] Sprite _passiveBandSprite = null;
        [SerializeField] Sprite _upgradeBandSprite = null;

        Tween _purchaseTween = null;
        Vector2 _iconBasePosition = Vector2.zero;
        Vector3 _iconBaseScale = Vector3.one;

        public override void Release()
        {
            KillPurchase();
            base.Release();
        }

        public void BindItem(BindParam param)
        {
            KillPurchase();
            SetInteractable(true);
            _canvasGroup.alpha = FULL_ALPHA;
            _bandImage.sprite = param.IsPassive ? _passiveBandSprite : _upgradeBandSprite;
            _titleLabel.text = param.Title;
            _passiveLabel.SetActive(param.IsPassive);
            _upgradeLabel.SetActive(!param.IsPassive);
            if (param.IsPassive)
                _passiveCategoryLabel.text = PassiveCategoryLabel.Get(param.PassiveCategory);

            SetIcon(param.Icon);
            _descriptionLabel.text = param.Description;
            _priceLabel.text = param.PriceText;
            _priceRoot.SetActive(param.ShowPrice);
            _soldStamp.SetActive(false);
        }

        public override void BindSold()
        {
            KillPurchase();
            base.BindSold();
            _passiveLabel.SetActive(false);
            _upgradeLabel.SetActive(false);
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
                SetInteractable(false);

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
            SetIcon(null);
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

        void ApplySold()
        {
            ApplySoldVisual();
            _passiveLabel.SetActive(false);
            _upgradeLabel.SetActive(false);
        }
    }
}
