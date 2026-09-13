using TMPro;
using UnityEngine;
using GameCore.Util;

namespace Pinball.Ingame.UI
{
    public class ScorePopup : MonoBehaviour, IPoolObject
    {
        const float START_RATIO = 0f;

        [SerializeField] TMP_Text _text = null;
        [SerializeField] RectTransform _rectTransform = null;

        Color _originColor = Color.white;
        Vector2 _basePosition = Vector2.zero;
        float _riseDistance = 0f;
        float _scaleRatio = START_RATIO;
        float _fadeRatio = START_RATIO;

        public IPoolBase OwnPool { get; set; }

        public void OnGenerate()
        {
            _originColor = _text.color;
        }

        public void OnGet()
        {
            gameObject.SetActive(true);
        }

        public void OnReturn()
        {
            _text.color = _originColor;
            _rectTransform.localScale = Vector3.one;
        }

        public void OnTerminate() { }

        public void SetText(string text)
        {
            _text.text = text;
        }

        public void Prepare(Vector2 anchoredPosition, float riseDistance)
        {
            _basePosition = anchoredPosition;
            _riseDistance = riseDistance;
            SetScaleRatio(START_RATIO);
            SetFadeRatio(START_RATIO);
        }

        public Vector2 GetAnchoredPosition()
        {
            return _rectTransform.anchoredPosition;
        }

        public float GetScaleRatio()
        {
            return _scaleRatio;
        }

        public void SetScaleRatio(float ratio)
        {
            _scaleRatio = ratio;
            _rectTransform.localScale = Vector3.one * ratio;
        }

        public float GetFadeRatio()
        {
            return _fadeRatio;
        }

        public void SetFadeRatio(float ratio)
        {
            _fadeRatio = ratio;
            _rectTransform.anchoredPosition = _basePosition + Vector2.up * (_riseDistance * ratio);

            Color color = _originColor;
            color.a = 1f - ratio;
            _text.color = color;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (null == _rectTransform)
                _rectTransform = transform as RectTransform;
        }
#endif // UNITY_EDITOR
    }
}
