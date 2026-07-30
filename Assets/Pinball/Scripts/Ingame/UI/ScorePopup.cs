using Pinball.Util;
using TMPro;
using UnityEngine;

namespace Pinball.Ingame.UI
{
    public class ScorePopup : MonoBehaviour, IPoolObject
    {
        const string SCORE_FORMAT = "+{0}";

        [SerializeField] TMP_Text _text = null;
        [SerializeField] RectTransform _rectTransform = null;

        Color _originColor = Color.white;
        Vector2 _basePosition = Vector2.zero;

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

        public void SetValue(int score)
        {
            _text.text = string.Format(SCORE_FORMAT, score);
        }

        public void SetBasePosition(Vector2 anchoredPosition)
        {
            _basePosition = anchoredPosition;
            _rectTransform.anchoredPosition = anchoredPosition;
        }

        public void SetProgress(float scaleRatio, float riseDistance, float alpha)
        {
            _rectTransform.localScale = Vector3.one * scaleRatio;
            _rectTransform.anchoredPosition = _basePosition + Vector2.up * riseDistance;

            Color color = _originColor;
            color.a = alpha;
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
