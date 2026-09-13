using System;
using UnityEngine;
using GameCore.Util;

namespace Pinball.Ingame.Map
{
    public class BallHoldZone : MonoBehaviour
    {
        const float HELD_GRAVITY_SCALE = 0f;
        // A held ball sits at zero velocity with gravity off, so any speed it regains
        // came from outside the zone — a flipper strike or a nudge. Without releasing
        // it there, the ball keeps drifting weightlessly and the zone goes on
        // reporting a ball it no longer holds.
        const float RELEASE_SPEED_SQR = 0.04f;

        [SerializeField] Collider2D[] _colliders = null;
        [SerializeField] LayerMask _ballLayerMask = default;

        Func<Vector2, bool> _holdPredicate = null;
        Rigidbody2D _heldBody = null;
        Rigidbody2D _knockedOutBody = null;
        float _baseGravityScale = 0f;

        public void SetHoldPredicate(Func<Vector2, bool> holdPredicate)
        {
            _holdPredicate = holdPredicate;
        }

        public void Arm()
        {
            ReleaseHold();
            _knockedOutBody = null;
            SetCollidersEnabled(true);
        }

        public void Disarm()
        {
            ReleaseHold();
            _knockedOutBody = null;
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

        void FixedUpdate()
        {
            if (null == _heldBody)
                return;

            if (_heldBody.linearVelocity.sqrMagnitude < RELEASE_SPEED_SQR)
                return;

            _knockedOutBody = _heldBody;
            ReleaseHold();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            TryHold(other);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            TryHold(other);
        }

        // Re-holding the ball the player just knocked away would freeze it again on
        // its way out, so it stays ignored until it has actually left the zone.
        void OnTriggerExit2D(Collider2D other)
        {
            if (null == other)
                return;

            Rigidbody2D body = other.attachedRigidbody;
            if (null == body)
                return;

            if (body == _knockedOutBody)
                _knockedOutBody = null;

            if (body != _heldBody)
                return;

            ReleaseHold();
        }

        void TryHold(Collider2D other)
        {
            if (null != _heldBody || null == other)
                return;

            if (!_ballLayerMask.HasLayer(other.gameObject.layer))
                return;

            Rigidbody2D body = other.attachedRigidbody;
            if (null == body || body == _knockedOutBody || body.bodyType != RigidbodyType2D.Dynamic)
                return;

            if (null != _holdPredicate && !_holdPredicate(body.position))
                return;

            _heldBody = body;
            _baseGravityScale = body.gravityScale;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.gravityScale = HELD_GRAVITY_SCALE;
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
