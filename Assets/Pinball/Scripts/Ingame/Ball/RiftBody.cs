using UnityEngine;

namespace Pinball.Ingame
{
    public class RiftBody : EffectBodyBase
    {
        public const float RIFT_WIDTH = 0.48f;

        const int LINE_POINT_COUNT = 2;
        const float MIN_RIFT_LENGTH = 0.01f;
        const float HALF_RATIO = 0.5f;

        [SerializeField] LineRenderer _line = null;

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
            return true;
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
