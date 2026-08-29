using System.Collections.Generic;
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
        const int SEPARATION_PASS_COUNT = 4;
        const float SEPARATION_SQR_LIMIT = 1f;
        const float MIN_SQR_DISTANCE = 0.0001f;
        const float FALLBACK_ANGLE_STEP = 137.5f;

        [SerializeField] RectTransform _rectTransform = null;
        [SerializeField] ScorePopup _popupPrefab = null;
        [SerializeField] int _poolCapacity = 8;
        [SerializeField] float _worldHeightOffset = 0.45f;
        [SerializeField] Vector2 _separationRange = new Vector2(70f, 34f);

        ObjectPool<ScorePopup> _pool = null;
        List<ScorePopup> _activePopups = null;
        int _spawnCount = 0;

        public void Init()
        {
            _pool = new ObjectPool<ScorePopup>(_popupPrefab, CreatePopup, _poolCapacity);
            _activePopups = new List<ScorePopup>(_poolCapacity);
            _spawnCount = 0;
        }

        public void Release()
        {
            DOTween.Kill(this);

            if (null != _activePopups)
            {
                _activePopups.Clear();
                _activePopups = null;
            }

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
            popup.Prepare(ResolveOverlap(anchoredPosition), RISE_DISTANCE);
            _activePopups.Add(popup);
            ++_spawnCount;

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

        Vector2 ResolveOverlap(Vector2 anchoredPosition)
        {
            if (_separationRange.x <= 0f || _separationRange.y <= 0f)
                return anchoredPosition;

            for (int pass = 0; pass < SEPARATION_PASS_COUNT; ++pass)
            {
                bool pushed = false;
                for (int i = 0; i < _activePopups.Count; ++i)
                {
                    Vector2 otherPosition = _activePopups[i].GetAnchoredPosition();
                    Vector2 delta = anchoredPosition - otherPosition;
                    Vector2 scaledDelta = new Vector2(delta.x / _separationRange.x, delta.y / _separationRange.y);
                    float sqrDistance = scaledDelta.sqrMagnitude;
                    if (sqrDistance >= SEPARATION_SQR_LIMIT)
                        continue;

                    Vector2 pushDirection = sqrDistance > MIN_SQR_DISTANCE ? scaledDelta / Mathf.Sqrt(sqrDistance) : GetFallbackDirection(i);
                    anchoredPosition = otherPosition + new Vector2(pushDirection.x * _separationRange.x, pushDirection.y * _separationRange.y);
                    pushed = true;
                }

                if (!pushed)
                    break;
            }

            return anchoredPosition;
        }

        Vector2 GetFallbackDirection(int index)
        {
            float angle = (_spawnCount + index) * FALLBACK_ANGLE_STEP * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        void ReturnPopup(ScorePopup popup)
        {
            if (null == _pool)
                return;

            _activePopups.Remove(popup);
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
