using DG.Tweening;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.UI
{
    public class ScorePopupSpawner : MonoBehaviour
    {
        const string SCORE_FORMAT = "+{0}";
        const float SCALE_DURATION = 0.3f;
        const float FADE_DURATION = 0.35f;
        const float RISE_DISTANCE = 60f;
        const float END_RATIO = 1f;

        [SerializeField] RectTransform _rectTransform = null;
        [SerializeField] ScorePopup _popupPrefab = null;
        [SerializeField] int _poolCapacity = 8;
        [SerializeField] float _worldHeightOffset = 0.45f;

        ObjectPool<ScorePopup> _pool = null;

        public void Init()
        {
            _pool = new ObjectPool<ScorePopup>(_popupPrefab, CreatePopup, _poolCapacity);
        }

        public void Release()
        {
            DOTween.Kill(this);

            if (null != _pool)
            {
                _pool.Clear();
                _pool = null;
            }
        }

        public void Play(int score, Vector2 worldPoint)
        {
            PlayText(string.Format(SCORE_FORMAT, score), GetAnchoredPosition(worldPoint));
        }

        public void PlayTextAt(string text, Vector3 uiWorldPosition)
        {
            Vector3 localPoint = _rectTransform.InverseTransformPoint(uiWorldPosition);
            PlayText(text, new Vector2(localPoint.x, localPoint.y));
        }

        void PlayText(string text, Vector2 anchoredPosition)
        {
            if (null == _pool)
                return;

            ScorePopup popup = _pool.Get();
            if (null == popup)
                return;

            popup.SetText(text);
            popup.Prepare(anchoredPosition, RISE_DISTANCE);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(DOTween.To(popup.GetScaleRatio, popup.SetScaleRatio, END_RATIO, SCALE_DURATION).SetEase(Ease.OutElastic));
            sequence.Append(DOTween.To(popup.GetFadeRatio, popup.SetFadeRatio, END_RATIO, FADE_DURATION).SetEase(Ease.InSine));
            sequence.OnComplete(() => ReturnPopup(popup));
            sequence.SetId(this);
        }

        ScorePopup CreatePopup(ScorePopup origin)
        {
            return Instantiate(origin, _rectTransform);
        }

        Vector2 GetAnchoredPosition(Vector2 worldPoint)
        {
            Vector3 worldPosition = new Vector3(worldPoint.x, worldPoint.y + _worldHeightOffset, 0f);
            Vector3 screenPosition = CameraController.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, screenPosition, CameraController.GetMainCamera(), out Vector2 localPoint);
            return localPoint;
        }

        void ReturnPopup(ScorePopup popup)
        {
            if (null == _pool)
                return;

            _pool.Return(popup);
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
