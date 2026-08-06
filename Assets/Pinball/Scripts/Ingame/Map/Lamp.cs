using DG.Tweening;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class Lamp : BehaviourBase
    {
        const float LIT_ALPHA = 1f;
        const float LIGHT_UP_DURATION = 0.3f;

        [SerializeField] SpriteRenderer _renderer = null;

        Tween _fadeTween = null;
        float _dimAlpha = 1f;

        public void Init()
        {
            if (null == _renderer)
                return;

            _dimAlpha = _renderer.color.a;
            SetLit(false);
        }

        public void Release()
        {
            KillFadeTween();
            SetLit(false);
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

#if UNITY_EDITOR
        protected override void OnBindSerializedField()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }
#endif // UNITY_EDITOR
    }
}
