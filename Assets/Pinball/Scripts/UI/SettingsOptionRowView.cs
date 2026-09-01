using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class SettingsOptionRowView : MonoBehaviour
    {
        public enum EOptionType
        {
            Resolution,
            Fullscreen,
            RefreshRate,
            VSync,
            MasterVolume,
            SfxVolume,
            BgmVolume,
            Language,
        }

        const int PREV_DIRECTION = -1;
        const int NEXT_DIRECTION = 1;
        const float ENABLED_ALPHA = 1f;
        const float DISABLED_ALPHA = 0.35f;

        [SerializeField] EOptionType _optionType = EOptionType.Resolution;
        [SerializeField] CanvasGroup _canvasGroup = null;
        [SerializeField] Button _prevButton = null;
        [SerializeField] Button _nextButton = null;
        [SerializeField] TMP_Text _valueLabel = null;
        [SerializeField] Slider _slider = null;
        [SerializeField] GameObject _sliderBody = null;

        Action<EOptionType, int> _onShifted = null;
        Action<EOptionType, float> _onSliderChanged = null;

        public EOptionType OptionType => _optionType;

        public static bool HasSlider(EOptionType optionType)
        {
            return optionType == EOptionType.MasterVolume
                || optionType == EOptionType.SfxVolume
                || optionType == EOptionType.BgmVolume;
        }

        public void Init(Action<EOptionType, int> onShifted, Action<EOptionType, float> onSliderChanged)
        {
            _onShifted = onShifted;
            _onSliderChanged = onSliderChanged;
            _prevButton.onClick.AddListener(OnPrevClicked);
            _nextButton.onClick.AddListener(OnNextClicked);

            bool hasSlider = HasSlider(_optionType);
            _sliderBody.SetActive(hasSlider);
            _slider.enabled = hasSlider;
            if (hasSlider)
                _slider.onValueChanged.AddListener(OnSliderChanged);
        }

        public void Release()
        {
            _prevButton.onClick.RemoveListener(OnPrevClicked);
            _nextButton.onClick.RemoveListener(OnNextClicked);
            _slider.onValueChanged.RemoveListener(OnSliderChanged);
            _onShifted = null;
            _onSliderChanged = null;
        }

        public void SetValue(string value)
        {
            _valueLabel.text = value;
        }

        public void SetSliderValue(float normalizedValue)
        {
            _slider.SetValueWithoutNotify(normalizedValue);
        }

        public void SetInteractable(bool isInteractable)
        {
            _prevButton.interactable = isInteractable;
            _nextButton.interactable = isInteractable;
            _slider.interactable = isInteractable;
            _canvasGroup.alpha = isInteractable ? ENABLED_ALPHA : DISABLED_ALPHA;
        }

        void OnPrevClicked()
        {
            _onShifted?.Invoke(_optionType, PREV_DIRECTION);
        }

        void OnNextClicked()
        {
            _onShifted?.Invoke(_optionType, NEXT_DIRECTION);
        }

        void OnSliderChanged(float normalizedValue)
        {
            _onSliderChanged?.Invoke(_optionType, normalizedValue);
        }
    }
}
