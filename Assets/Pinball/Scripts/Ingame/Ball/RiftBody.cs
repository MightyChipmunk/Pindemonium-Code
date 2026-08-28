using DG.Tweening;
using UnityEngine;

namespace Pinball.Ingame
{
    public class RiftBody : EffectBodyBase
    {
        public const float RIFT_WIDTH = 0.48f;
        public const float HIDE_DURATION = 0.15f;

        const float SHOW_DURATION = 0.12f;
        const float SHOW_OVERSHOOT = 2.6f;
        const int LINE_POINT_COUNT = 2;
        const float MIN_RIFT_LENGTH = 0.01f;
        const float HALF_RATIO = 0.5f;

        [SerializeField] LineRenderer _line = null;

        Tween _scaleTween = null;
        float _length = 0f;
        float _angleDegree = 0f;

        public float Length => _length;
        public float AngleDegree => _angleDegree;

        public bool TryOpen(Vector2 startPosition, Vector2 endPosition)
        {
            Vector2 offset = endPosition - startPosition;
            _length = offset.magnitude;
            if (_length <= MIN_RIFT_LENGTH)
                return false;

            _angleDegree = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            MoveTo(startPosition + offset * HALF_RATIO);
            SetRotation(_angleDegree);
            BuildLine(_length);
            PlayShow();
            return true;
        }

        public void PlayHide()
        {
            KillScaleTween();
            _scaleTween = transform.DOScaleY(0f, HIDE_DURATION).SetEase(Ease.InBack);
        }

        public override void OnReturn()
        {
            base.OnReturn();
            KillScaleTween();
            transform.localScale = Vector3.one;
        }

        void PlayShow()
        {
            KillScaleTween();
            transform.localScale = new Vector3(1f, 0f, 1f);
            _scaleTween = transform.DOScaleY(1f, SHOW_DURATION).SetEase(Ease.OutBack, SHOW_OVERSHOOT);
        }

        void KillScaleTween()
        {
            if (null == _scaleTween)
                return;

            _scaleTween.Kill(false);
            _scaleTween = null;
        }

        void BuildLine(float length)
        {
            if (null == _line)
                return;

            float halfLength = length * HALF_RATIO;
            _line.useWorldSpace = false;
            _line.positionCount = LINE_POINT_COUNT;
            _line.SetPosition(0, new Vector3(-halfLength, 0f, 0f));
            _line.SetPosition(1, new Vector3(halfLength, 0f, 0f));
        }
    }
}
