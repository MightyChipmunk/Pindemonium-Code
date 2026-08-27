using System;
using Pinball.Audio;
using Pinball.FX;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame
{
    public class Ball : MonoBehaviour, IPoolObject
    {
        const float OPAQUE_ALPHA = 1f;
        const float AIM_ARROW_UP_ANGLE_OFFSET = 90f;
        const float MIN_HIT_SPEED = 2.5f;
        const float MAX_HIT_SPEED = 12f;
        const float MIN_HIT_VOLUME_RATIO = 0.4f;
        const float MIN_HIT_FX_IMPULSE = 3f;
        const PoolableFX.EFXType WALL_HIT_FX = PoolableFX.EFXType.BallCollide;
        const float MAX_HIT_VOLUME_RATIO = 1f;
        public const float DEFAULT_DAMAGE_STACK = 1f;

        const float MAX_DAMAGE_STACK = 1000000000000f;

        static readonly Color STONE_TINT = new Color(0.49f, 0.59f, 1f, 1f);

        [SerializeField] Rigidbody2D _rigidbody = null;
        [SerializeField] Collider2D _collider = null;
        [SerializeField] SpriteRenderer _renderer = null;
        [SerializeField] BallTrail _trail = null;
        [SerializeField] BallAuraController _auraController = null;
        [SerializeField] SpriteRenderer _spikeRenderer = null;
        [SerializeField] SpriteRenderer _scopeRenderer = null;
        [SerializeField] Transform _nudgeAimPivot = null;
        [SerializeField] LayerMask _hitSoundLayerMask = 0;

        Action<PoolableFX.EFXType, Vector2> _hitFXPlayer = null;
        Action<Vector2> _surfaceHitReporter = null;
        float _damageStackMultiplier = DEFAULT_DAMAGE_STACK;

        public IPoolBase OwnPool { get; set; }
        public int ColliderInstanceID => _collider.GetInstanceID();
        public float DamageStackMultiplier => _damageStackMultiplier;

        public void MultiplyDamageStack(float scale)
        {
            _damageStackMultiplier = Mathf.Min(_damageStackMultiplier * scale, MAX_DAMAGE_STACK);
        }

        public void ResetDamageStack()
        {
            _damageStackMultiplier = DEFAULT_DAMAGE_STACK;
        }

        public void SetMass(float mass)
        {
            _rigidbody.mass = mass;
        }

        public void SetAlpha(float alpha)
        {
            Color color = _renderer.color;
            color.a = alpha;
            _renderer.color = color;
        }

        public Vector2 GetVelocity()
        {
            return _rigidbody.linearVelocity;
        }

        public bool IsSimulated()
        {
            return _rigidbody.bodyType == RigidbodyType2D.Dynamic;
        }

        public void SetHitFXPlayer(Action<PoolableFX.EFXType, Vector2> hitFXPlayer)
        {
            _hitFXPlayer = hitFXPlayer;
        }

        public void SetSurfaceHitReporter(Action<Vector2> surfaceHitReporter)
        {
            _surfaceHitReporter = surfaceHitReporter;
        }

        public void SetSpikesActive(bool isActive)
        {
            _spikeRenderer.enabled = isActive;
        }

        public void SetScopeActive(bool isActive)
        {
            _scopeRenderer.enabled = isActive;
        }

        public void SetNudgeAimActive(bool isActive)
        {
            _nudgeAimPivot.gameObject.SetActive(isActive);
        }

        public void SetNudgeAimDirection(Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - AIM_ARROW_UP_ANGLE_OFFSET;
            _nudgeAimPivot.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void SetStoneActive(bool isActive)
        {
            Color tint = isActive ? STONE_TINT : Color.white;
            tint.a = _renderer.color.a;
            _renderer.color = tint;
        }

        public void SetAura(BallAura.EAuraType auraType, bool isActive)
        {
            _auraController.SetAuraActive(auraType, isActive);
            RefreshTrailColor();
        }

        public void SetKinematic(bool isKinematic)
        {
            if (isKinematic)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;
                _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            }
            else
            {
                _rigidbody.bodyType = RigidbodyType2D.Dynamic;
            }
        }

        public void MoveTo(Vector3 position)
        {
            _rigidbody.position = position;
            transform.position = position;
            _trail.Clear();
            _auraController.ClearParticles();
        }

        public void ScaleVelocity(float scale)
        {
            _rigidbody.linearVelocity *= scale;
        }

        public void AddImpulse(Vector2 impulse)
        {
            _rigidbody.AddForce(impulse, ForceMode2D.Impulse);
        }

        public void OnGenerate() { }

        public void OnGet()
        {
            gameObject.SetActive(true);
            ResetDamageStack();
            SetAlpha(OPAQUE_ALPHA);
            SetSpikesActive(false);
            SetScopeActive(false);
            SetNudgeAimActive(false);
            SetStoneActive(false);
            _trail.Init();
            _auraController.Init();
        }

        public void OnReturn()
        {
            SetKinematic(true);
            _auraController.Release();
            _hitFXPlayer = null;
            _surfaceHitReporter = null;
            _trail.Release();
        }

        public void OnTerminate() { }

        void OnCollisionEnter2D(Collision2D collision)
        {
            _surfaceHitReporter?.Invoke(collision.GetContact(0).point);

            if (!_hitSoundLayerMask.HasLayer(collision.gameObject.layer))
                return;

            float hitSpeedSqr = collision.relativeVelocity.sqrMagnitude;
            if (hitSpeedSqr < MIN_HIT_SPEED * MIN_HIT_SPEED)
                return;

            ContactPoint2D contact = collision.GetContact(0);
            if (contact.normalImpulse >= MIN_HIT_FX_IMPULSE)
                _hitFXPlayer?.Invoke(WALL_HIT_FX, contact.point);

            float speedRatio = Mathf.InverseLerp(MIN_HIT_SPEED, MAX_HIT_SPEED, Mathf.Sqrt(hitSpeedSqr));
            AudioManager.PlayScaled(AudioStorage.EAudioClipType.BallHitNormal,
                Mathf.Lerp(MIN_HIT_VOLUME_RATIO, MAX_HIT_VOLUME_RATIO, speedRatio));
        }

        void RefreshTrailColor()
        {
            if (_auraController.TryGetBlendedColor(out Color blendedColor))
            {
                _trail.SetColor(blendedColor);
                return;
            }

            _trail.ResetColor();
        }
    }
}
