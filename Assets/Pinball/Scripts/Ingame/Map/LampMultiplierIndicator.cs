using DG.Tweening;
using Pinball.Util;
using TMPro;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class LampMultiplierIndicator : BehaviourBase
    {
        const string MULTIPLIER_FORMAT = "x{0:0.##}";
        const float NO_MULTIPLIER = 1f;
        const float MULTIPLIER_EPSILON = 0.001f;
        const float SHOW_DURATION = 0.22f;
        const float SHOW_OVERSHOOT = 2.6f;
        const float POP_SCALE_DELTA = 0.35f;
        const float POP_DURATION = 0.24f;
        const int POP_VIBRATO = 6;
        const float POP_ELASTICITY = 0.6f;

        [SerializeField] TMP_Text _label = null;

        Tween _scaleTween = null;
        Vector3 _baseScale = Vector3.one;
        float _multiplier = NO_MULTIPLIER;

        public void Init()
        {
            _baseScale = transform.localScale;
            _multiplier = NO_MULTIPLIER;
            KillScaleTween();
            transform.localScale = _baseScale;
            gameObject.SetActive(false);
        }

        public void Release()
        {
            _multiplier = NO_MULTIPLIER;
            KillScaleTween();
            transform.localScale = _baseScale;
            gameObject.SetActive(false);
        }

        public void SetMultiplier(float multiplier)
        {
            if (Mathf.Abs(multiplier - _multiplier) < MULTIPLIER_EPSILON)
                return;

            bool wasHidden = !gameObject.activeSelf;
            _multiplier = multiplier;

            if (multiplier <= NO_MULTIPLIER + MULTIPLIER_EPSILON)
            {
                Release();
                return;
            }

            if (null != _label)
                _label.text = string.Format(MULTIPLIER_FORMAT, multiplier);

            gameObject.SetActive(true);
            PlayChangeMotion(wasHidden);
        }

        void PlayChangeMotion(bool isAppearing)
        {
            KillScaleTween();

            if (isAppearing)
            {
                transform.localScale = Vector3.zero;
                _scaleTween = transform.DOScale(_baseScale, SHOW_DURATION).SetEase(Ease.OutBack, SHOW_OVERSHOOT);
                return;
            }

            transform.localScale = _baseScale;
            _scaleTween = transform.DOPunchScale(_baseScale * POP_SCALE_DELTA, POP_DURATION, POP_VIBRATO, POP_ELASTICITY);
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
            _label = GetComponent<TMP_Text>();
        }
#endif // UNITY_EDITOR
    }
}
