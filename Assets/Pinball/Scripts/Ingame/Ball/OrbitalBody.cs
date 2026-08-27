using System;
using UnityEngine;

namespace Pinball.Ingame
{
    public class OrbitalBody : EffectBodyBase
    {
        const float ORBIT_DEGREES_PER_SECOND = 180f;
        const float ORBIT_RADIUS = 0.75f;
        const float FULL_CIRCLE_DEGREE = 360f;

        [SerializeField] Rigidbody2D _rigidbody = null;

        float _angleDegree = 0f;

        public event Action<Collider2D> OnContact;

        public void Setup(float phaseDegree, Vector2 centerPosition)
        {
            _angleDegree = phaseDegree;

            Vector2 position = centerPosition + GetOrbitOffset();
            _rigidbody.position = position;
            MoveTo(position);
        }

        public void Tick(Vector2 centerPosition, float deltaTime)
        {
            _angleDegree += ORBIT_DEGREES_PER_SECOND * deltaTime;
            if (_angleDegree >= FULL_CIRCLE_DEGREE)
                _angleDegree -= FULL_CIRCLE_DEGREE;

            _rigidbody.MovePosition(centerPosition + GetOrbitOffset());
        }

        public override void OnReturn()
        {
            base.OnReturn();
            OnContact = null;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            OnContact?.Invoke(other);
        }

        Vector2 GetOrbitOffset()
        {
            float angleRadian = _angleDegree * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angleRadian), Mathf.Sin(angleRadian)) * ORBIT_RADIUS;
        }
    }
}
