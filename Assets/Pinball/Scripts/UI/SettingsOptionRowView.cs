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

        Action<EOptionType, int> _onShifted = null;

        public EOptionType OptionType => _optionType;

        public void Init(Action<EOptionType, int> onShifted)
        {
            _onShifted = onShifted;
            _prevButton.onClick.AddListener(OnPrevClicked);
            _nextButton.onClick.AddListener(OnNextClicked);
        }

        public void Release()
        {
            _prevButton.onClick.RemoveListener(OnPrevClicked);
            _nextButton.onClick.RemoveListener(OnNextClicked);
            _onShifted = null;
        }

        public void SetValue(string value)
        {
            _valueLabel.text = value;
        }

        public void SetInteractable(bool isInteractable)
        {
            _prevButton.interactable = isInteractable;
            _nextButton.interactable = isInteractable;
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
    }
}
