using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public abstract class ShopSlotViewBase : MonoBehaviour
    {
        protected const float SOLD_ALPHA = 0.55f;
        protected const float FULL_ALPHA = 1f;

        [SerializeField] protected Button _button = null;
        [SerializeField] protected ShopItemHighlight _highlight = null;
        [SerializeField] protected CanvasGroup _canvasGroup = null;
        [SerializeField] protected Image _bandImage = null;
        [SerializeField] protected TMP_Text _titleLabel = null;
        [SerializeField] protected Image _iconImage = null;
        [SerializeField] protected TMP_Text _descriptionLabel = null;
        [SerializeField] protected GameObject _priceRoot = null;
        [SerializeField] protected TMP_Text _priceLabel = null;
        [SerializeField] protected GameObject _soldStamp = null;

        Action<int> _clickCallback = null;
        int _slotIndex = 0;

        public void Init(int slotIndex, Action<int> clickCallback)
        {
            _slotIndex = slotIndex;
            _clickCallback = clickCallback;
            _button.onClick.AddListener(OnClicked);
            _highlight.Init();
        }

        public virtual void Release()
        {
            _highlight.Release();
            _button.onClick.RemoveListener(OnClicked);
            _clickCallback = null;
        }

        public virtual void BindSold()
        {
            ApplySoldVisual();
            SetIcon(null);
            _canvasGroup.alpha = SOLD_ALPHA;
        }

        protected void SetInteractable(bool isInteractable)
        {
            _button.interactable = isInteractable;
            _highlight.SetInteractable(isInteractable);
        }

        protected void ApplySoldVisual()
        {
            SetInteractable(false);
            _titleLabel.text = string.Empty;
            _descriptionLabel.text = string.Empty;
            _priceLabel.text = string.Empty;
            _priceRoot.SetActive(false);
            _soldStamp.SetActive(true);
        }

        protected void SetIcon(Sprite icon)
        {
            _iconImage.sprite = icon;
            _iconImage.enabled = null != icon;
        }

        void OnClicked()
        {
            _clickCallback?.Invoke(_slotIndex);
        }
    }
}
