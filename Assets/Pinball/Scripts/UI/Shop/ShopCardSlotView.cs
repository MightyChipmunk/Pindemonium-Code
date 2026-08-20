using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class ShopCardSlotView : MonoBehaviour
    {
        public record BindParam(
            string Title,
            Sprite Icon,
            string Description,
            string PriceText,
            Sprite BandSprite,
            bool CanBuy);

        const float DISABLED_ALPHA = 0.45f;
        const float ENABLED_ALPHA = 1f;
        const float SOLD_ALPHA = 0.55f;

        [SerializeField] Button _button = null;
        [SerializeField] ShopItemHighlight _highlight = null;
        [SerializeField] CanvasGroup _canvasGroup = null;
        [SerializeField] Image _bandImage = null;
        [SerializeField] TMP_Text _titleLabel = null;
        [SerializeField] Image _iconImage = null;
        [SerializeField] TMP_Text _descriptionLabel = null;
        [SerializeField] GameObject _priceRoot = null;
        [SerializeField] TMP_Text _priceLabel = null;
        [SerializeField] GameObject _soldStamp = null;

        Action<int> _clickCallback = null;
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
            _highlight.Release();
            _button.onClick.RemoveListener(OnClicked);
            _clickCallback = null;
        }

        public void Bind(BindParam param)
        {
            gameObject.SetActive(true);
            _button.interactable = param.CanBuy;
            _highlight.SetInteractable(param.CanBuy);
            _canvasGroup.alpha = param.CanBuy ? ENABLED_ALPHA : DISABLED_ALPHA;
            _bandImage.sprite = param.BandSprite;
            _titleLabel.text = param.Title;
            _iconImage.sprite = param.Icon;
            _iconImage.enabled = null != param.Icon;
            _descriptionLabel.text = param.Description;
            _priceLabel.text = param.PriceText;
            _priceRoot.SetActive(true);
            _soldStamp.SetActive(false);
        }

        public void BindSold()
        {
            gameObject.SetActive(true);
            _button.interactable = false;
            _highlight.SetInteractable(false);
            _canvasGroup.alpha = SOLD_ALPHA;
            _titleLabel.text = string.Empty;
            _iconImage.sprite = null;
            _iconImage.enabled = false;
            _descriptionLabel.text = string.Empty;
            _priceLabel.text = string.Empty;
            _priceRoot.SetActive(false);
            _soldStamp.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void OnClicked()
        {
            _clickCallback?.Invoke(_slotIndex);
        }
    }
}
