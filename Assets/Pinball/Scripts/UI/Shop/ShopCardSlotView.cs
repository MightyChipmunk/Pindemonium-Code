using UnityEngine;

namespace Pinball.UI
{
    public class ShopCardSlotView : ShopSlotViewBase
    {
        public record BindParam(
            string Title,
            Sprite Icon,
            string Description,
            string PriceText,
            Sprite BandSprite,
            bool CanBuy);

        const float DISABLED_ALPHA = 0.45f;

        public void Bind(BindParam param)
        {
            gameObject.SetActive(true);
            SetInteractable(param.CanBuy);
            _canvasGroup.alpha = param.CanBuy ? FULL_ALPHA : DISABLED_ALPHA;
            _bandImage.sprite = param.BandSprite;
            _titleLabel.text = param.Title;
            SetIcon(param.Icon);
            _descriptionLabel.text = param.Description;
            _priceLabel.text = param.PriceText;
            _priceRoot.SetActive(true);
            _soldStamp.SetActive(false);
        }

        public override void BindSold()
        {
            gameObject.SetActive(true);
            base.BindSold();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
