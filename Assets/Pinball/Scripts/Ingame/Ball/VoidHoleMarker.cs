using DG.Tweening;
using UnityEngine;

namespace Pinball.Ingame
{
    public class VoidHoleMarker : MonoBehaviour
    {
        const float SHOW_DURATION = 0.14f;
        const float HIDE_DURATION = 0.16f;
        const float SHOW_OVERSHOOT = 2.4f;

        Tween _scaleTween = null;
        Vector3 _baseScale = Vector3.one;

        public void Init()
        {
            _baseScale = transform.localScale;
            HideImmediate();
        }

        public void Release()
        {
            HideImmediate();
        }

        public void Show(Vector2 position)
        {
            KillScaleTween();
            transform.position = position;
            transform.localScale = Vector3.zero;
            gameObject.SetActive(true);
            _scaleTween = transform.DOScale(_baseScale, SHOW_DURATION).SetEase(Ease.OutBack, SHOW_OVERSHOOT);
        }

        public void Hide()
        {
            if (!gameObject.activeSelf)
                return;

            KillScaleTween();
            _scaleTween = transform.DOScale(Vector3.zero, HIDE_DURATION).SetEase(Ease.InBack).OnComplete(HideImmediate);
        }

        public void HideImmediate()
        {
            KillScaleTween();
            transform.localScale = _baseScale;
            gameObject.SetActive(false);
        }

        void KillScaleTween()
        {
            if (null == _scaleTween)
                return;

            _scaleTween.Kill(false);
            _scaleTween = null;
        }
    }
}
