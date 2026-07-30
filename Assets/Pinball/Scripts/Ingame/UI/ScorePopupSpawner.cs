using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.UI
{
    public class ScorePopupSpawner : MonoBehaviour
    {
        const float SCALE_DURATION = 0.3f;
        const float FADE_DURATION = 0.35f;
        const float RISE_DISTANCE = 60f;

        [SerializeField] RectTransform _rectTransform = null;
        [SerializeField] ScorePopup _popupPrefab = null;
        [SerializeField] int _poolCapacity = 8;
        [SerializeField] float _worldHeightOffset = 0.45f;

        ObjectPool<ScorePopup> _pool = null;
        CancellationTokenSource _cancellationTokenSource = null;

        public void Init()
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _pool = new ObjectPool<ScorePopup>(_popupPrefab, CreatePopup, _poolCapacity);
        }

        public void Release()
        {
            if (null != _cancellationTokenSource)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            if (null != _pool)
            {
                _pool.Clear();
                _pool = null;
            }
        }

        public void Play(int score, Vector2 worldPoint)
        {
            if (null == _pool)
                return;

            ScorePopup popup = _pool.Get();
            if (null == popup)
                return;

            popup.SetValue(score);
            popup.SetBasePosition(GetAnchoredPosition(worldPoint));
            popup.SetProgress(0f, 0f, 1f);
            PlayPopupAsync(popup, _cancellationTokenSource.Token).Forget();
        }

        ScorePopup CreatePopup(ScorePopup origin)
        {
            return Instantiate(origin, _rectTransform);
        }

        Vector2 GetAnchoredPosition(Vector2 worldPoint)
        {
            Vector3 worldPosition = new Vector3(worldPoint.x, worldPoint.y + _worldHeightOffset, 0f);
            Vector3 screenPosition = CameraController.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, screenPosition, null, out Vector2 localPoint);
            return localPoint;
        }

        async UniTaskVoid PlayPopupAsync(ScorePopup popup, CancellationToken cancellationToken)
        {
            float scaleTime = 0f;
            while (scaleTime <= SCALE_DURATION)
            {
                float ratio = EaseUtil.Interpolate(EaseType.ElasticOut, scaleTime / SCALE_DURATION);
                popup.SetProgress(ratio, 0f, 1f);

                scaleTime += Time.deltaTime;
                await UniTask.Yield(cancellationToken);
            }

            float fadeTime = 0f;
            while (fadeTime <= FADE_DURATION)
            {
                float ratio = EaseUtil.Interpolate(EaseType.SineIn, fadeTime / FADE_DURATION);
                popup.SetProgress(1f, RISE_DISTANCE * ratio, 1f - ratio);

                fadeTime += Time.deltaTime;
                await UniTask.Yield(cancellationToken);
            }

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
