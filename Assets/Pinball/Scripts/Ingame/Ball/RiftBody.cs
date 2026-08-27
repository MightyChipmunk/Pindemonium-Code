using UnityEngine;

namespace Pinball.Ingame
{
    public class RiftBody : EffectBodyBase
    {
        public const float RIFT_WIDTH = 0.42f;

        const float CRACK_JITTER = 0.16f;
        const int CRACK_POINT_COUNT = 9;
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
            BuildCrackLine(_length);
            return true;
        }

        void BuildCrackLine(float length)
        {
            if (null == _line)
                return;

            _line.useWorldSpace = false;
            _line.positionCount = CRACK_POINT_COUNT;

            float halfLength = length * HALF_RATIO;
            int lastIndex = CRACK_POINT_COUNT - 1;
            for (int i = 0; i <= lastIndex; ++i)
            {
                float ratio = (float)i / lastIndex;
                float x = Mathf.Lerp(-halfLength, halfLength, ratio);
                float y = i == 0 || i == lastIndex ? 0f : UnityEngine.Random.Range(-CRACK_JITTER, CRACK_JITTER);
                _line.SetPosition(i, new Vector3(x, y, 0f));
            }
        }
    }
}
