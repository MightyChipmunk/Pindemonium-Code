using System;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class BallHoldZone : MonoBehaviour
    {
        const float HELD_GRAVITY_SCALE = 0f;

        [SerializeField] Collider2D[] _colliders = null;
        [SerializeField] LayerMask _ballLayerMask = default;

        Func<Vector2, bool> _holdPredicate = null;
        Rigidbody2D _heldBody = null;
        float _baseGravityScale = 0f;

        public void SetHoldPredicate(Func<Vector2, bool> holdPredicate)
        {
            _holdPredicate = holdPredicate;
        }

        public void Arm()
        {
            ReleaseHold();
            SetCollidersEnabled(true);
        }

        public void Disarm()
        {
            ReleaseHold();
            SetCollidersEnabled(false);
        }

        public bool IsHolding()
        {
            return null != _heldBody;
        }

        public void ReleaseHold()
        {
            if (null == _heldBody)
                return;

            _heldBody.gravityScale = _baseGravityScale;
            _heldBody = null;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            TryHold(other);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            TryHold(other);
        }

        void TryHold(Collider2D other)
        {
            if (null != _heldBody || null == other)
                return;

            if (!_ballLayerMask.HasLayer(other.gameObject.layer))
                return;

            Rigidbody2D body = other.attachedRigidbody;
            if (null == body || body.bodyType != RigidbodyType2D.Dynamic)
                return;

            if (null != _holdPredicate && !_holdPredicate(body.position))
                return;

            _heldBody = body;
            _baseGravityScale = body.gravityScale;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.gravityScale = HELD_GRAVITY_SCALE;
            SetCollidersEnabled(false);
        }

        void SetCollidersEnabled(bool isEnabled)
        {
            if (null == _colliders)
                return;

            for (int i = 0; i < _colliders.Length; ++i)
                if (null != _colliders[i])
                    _colliders[i].enabled = isEnabled;
        }
    }
}
