using DG.Tweening;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class Lamp : BehaviourBase
    {
        const float LIT_ALPHA = 1f;
        const float LIGHT_UP_DURATION = 0.3f;
        const float SHOW_DURATION = 0.24f;
        const float HIDE_DURATION = 0.16f;
        const float SHOW_OVERSHOOT = 2.4f;

        [SerializeField] SpriteRenderer _renderer = null;

        Tween _fadeTween = null;
        Tween _scaleTween = null;
        Vector3 _baseScale = Vector3.one;
        Color _baseColor = Color.white;
        float _dimAlpha = 1f;

        public void Init()
        {
            _baseScale = transform.localScale;

            if (null == _renderer)
                return;

            _baseColor = _renderer.color;
            _dimAlpha = _renderer.color.a;
            SetLit(false);
        }

        public void Release()
        {
            KillFadeTween();
            KillScaleTween();
            transform.localScale = _baseScale;
            ClearTint();
            SetLit(false);
        }

        public void SetTint(Color tint)
        {
            if (null == _renderer)
                return;

            Color color = _renderer.color;
            color.r = tint.r;
            color.g = tint.g;
            color.b = tint.b;
            _renderer.color = color;
        }

        public void ClearTint()
        {
            SetTint(_baseColor);
        }

        public void SetLit(bool isLit)
        {
            KillFadeTween();
            ApplyAlpha(isLit ? LIT_ALPHA : _dimAlpha);
        }

        public void PlayLightUp()
        {
            KillFadeTween();
            ApplyAlpha(_dimAlpha);

            if (null != _renderer)
                _fadeTween = _renderer.DOFade(LIT_ALPHA, LIGHT_UP_DURATION);
        }

        public void PlayShow(float delay)
        {
            KillScaleTween();
            transform.localScale = Vector3.zero;
            _scaleTween = transform.DOScale(_baseScale, SHOW_DURATION).SetEase(Ease.OutBack, SHOW_OVERSHOOT).SetDelay(delay);
        }

        public void PlayHide(float delay)
        {
            KillScaleTween();
            _scaleTween = transform.DOScale(Vector3.zero, HIDE_DURATION).SetEase(Ease.InBack).SetDelay(delay);
        }

        void ApplyAlpha(float alpha)
        {
            if (null == _renderer)
                return;

            Color color = _renderer.color;
            color.a = alpha;
            _renderer.color = color;
        }

        void KillFadeTween()
        {
            if (null == _fadeTween)
                return;

            _fadeTween.Kill(false);
            _fadeTween = null;
        }

        void KillScaleTween()
        {
            if (null == _scaleTween)
                return;

            _scaleTween.Kill(false);
            _scaleTween = null;
        }

#if UNITY_EDITOR
        protected override void OnBindSerializedField()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }
#endif // UNITY_EDITOR
    }
}
