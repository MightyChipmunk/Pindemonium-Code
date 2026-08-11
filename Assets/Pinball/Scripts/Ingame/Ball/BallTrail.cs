using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame
{
    public class BallTrail : BehaviourBase
    {
        [SerializeField] TrailRenderer _trail = null;
        [SerializeField] Rigidbody2D _rigidbody = null;
        [SerializeField] float _minSpeed = 1f;
        [SerializeField] float _maxSpeed = 14f;
        [SerializeField] float _minTime = 0.06f;
        [SerializeField] float _maxTime = 0.3f;
        [SerializeField] float _minWidth = 0.04f;
        [SerializeField] float _maxWidth = 0.2f;

        Color _baseStartColor = Color.white;
        Color _baseEndColor = Color.white;
        bool _isBaseColorCached = false;

        public void Init()
        {
            CacheBaseColor();
            ApplySpeedRatio(0f);
            _trail.Clear();
            _trail.emitting = true;
        }

        public void Release()
        {
            RestoreBaseColor();
            _trail.emitting = false;
            _trail.Clear();
        }

        public void SetColor(Color color)
        {
            CacheBaseColor();
            _trail.startColor = new Color(color.r, color.g, color.b, _baseStartColor.a);
            _trail.endColor = new Color(color.r, color.g, color.b, _baseEndColor.a);
        }

        void CacheBaseColor()
        {
            if (_isBaseColorCached)
                return;

            _baseStartColor = _trail.startColor;
            _baseEndColor = _trail.endColor;
            _isBaseColorCached = true;
        }

        void RestoreBaseColor()
        {
            if (!_isBaseColorCached)
                return;

            _trail.startColor = _baseStartColor;
            _trail.endColor = _baseEndColor;
        }

        public void Clear()
        {
            _trail.Clear();
        }

        void LateUpdate()
        {
            ApplySpeedRatio(GetSpeedRatio());
        }

        float GetSpeedRatio()
        {
            float sqrSpeed = _rigidbody.linearVelocity.sqrMagnitude;
            return Mathf.Clamp01(Mathf.InverseLerp(_minSpeed * _minSpeed, _maxSpeed * _maxSpeed, sqrSpeed));
        }

        void ApplySpeedRatio(float speedRatio)
        {
            _trail.time = Mathf.Lerp(_minTime, _maxTime, speedRatio);
            _trail.widthMultiplier = Mathf.Lerp(_minWidth, _maxWidth, speedRatio);
        }

#if UNITY_EDITOR
        protected override void OnBindSerializedField()
        {
            _trail = GetComponentInChildren<TrailRenderer>(true);
            _rigidbody = GetComponent<Rigidbody2D>();
        }
#endif // UNITY_EDITOR
    }
}
