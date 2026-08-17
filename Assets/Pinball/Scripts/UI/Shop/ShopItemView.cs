using System;
using Pinball.Ingame;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class ShopItemView : MonoBehaviour
    {
        public record BindParam(
            ShopItemData.EItemKind Kind,
            string Title,
            Sprite Icon,
            string Description,
            string PriceText);

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
        [SerializeField] Sprite _passiveBandSprite = null;
        [SerializeField] Sprite _upgradeBandSprite = null;

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

        public void BindItem(BindParam param)
        {
            _button.interactable = true;
            _highlight.SetInteractable(true);
            _canvasGroup.alpha = 1f;
            _bandImage.sprite = param.Kind == ShopItemData.EItemKind.Passive ? _passiveBandSprite : _upgradeBandSprite;
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

        void OnClicked()
        {
            _clickCallback?.Invoke(_slotIndex);
        }
    }
}
