using UnityEngine;

namespace Pinball.Ingame
{
    public class OrbitalBody : EffectBodyBase
    {
        public const float HIT_RADIUS = 0.16f;

        const float ORBIT_DEGREES_PER_SECOND = 180f;
        const float ORBIT_RADIUS = 0.75f;
        const float FULL_CIRCLE_DEGREE = 360f;
        const float HIT_INTERVAL_SECONDS = 0.45f;

        float _angleDegree = 0f;
        float _hitElapsed = 0f;

        public void Setup(float phaseDegree, Vector2 centerPosition)
        {
            _angleDegree = phaseDegree;
            _hitElapsed = 0f;
            MoveTo(centerPosition + GetOrbitOffset());
        }

        public void Tick(Vector2 centerPosition, float deltaTime)
        {
            _angleDegree += ORBIT_DEGREES_PER_SECOND * deltaTime;
            if (_angleDegree >= FULL_CIRCLE_DEGREE)
                _angleDegree -= FULL_CIRCLE_DEGREE;

            MoveTo(centerPosition + GetOrbitOffset());
            _hitElapsed += deltaTime;
        }

        public bool TryConsumeHitInterval()
        {
            if (_hitElapsed < HIT_INTERVAL_SECONDS)
                return false;

            _hitElapsed = 0f;
            return true;
        }

        Vector2 GetOrbitOffset()
        {
            float angleRadian = _angleDegree * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angleRadian), Mathf.Sin(angleRadian)) * ORBIT_RADIUS;
        }
    }
}
